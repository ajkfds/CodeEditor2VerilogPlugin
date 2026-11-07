using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SystemVerilogCore.AutoConnect;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free auto-connect form builder.
    ///
    /// <para>
    /// The desktop editor's <c>Views.AutoConnectWindow</c> shows one row per
    /// port of the instantiated module, with the signal the editor would
    /// connect. That window mixes two things: deciding <b>which signal matches
    /// a port</b> (pure logic) and <b>rendering the list</b> (Avalonia). This
    /// adapter keeps only the first part, so the LSP host can serve the same
    /// form to VS Code (rendered as a webview) without an Avalonia dispatcher.
    /// </para>
    ///
    /// <para>
    /// The matching rule is the editor's: a signal whose name equals the port
    /// name, or the port name with an <c>_I</c> / <c>_O</c> / <c>_IO</c> suffix
    /// removed (case-insensitive). Candidates are ordered best match first.
    /// </para>
    /// </summary>
    public static class AutoConnectAdapter
    {
        /// <summary>Port name suffixes the editor strips when matching signals.</summary>
        private static readonly string[] RemovePortSuffixes = { "_I", "_O", "_IO" };

        /// <summary>
        /// Builds the auto-connect form for the module instantiation that
        /// contains <paramref name="index"/>. Returns a model whose
        /// <see cref="AutoConnectModel.Message"/> explains why the form is
        /// empty when the caret is not on a resolvable instantiation.
        /// </summary>
        public static AutoConnectModel Build(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index)
        {
            if (parsedDocument?.Root == null)
            {
                return new AutoConnectModel { Message = "the file has not been parsed yet" };
            }

            Verilog.BuildingBlocks.BuildingBlock? enclosing = parsedDocument.GetBuildingBlockAt(index);
            if (enclosing == null)
            {
                return new AutoConnectModel { Message = "the caret is not inside a module" };
            }

            Verilog.Items.ModuleInstantiation? instantiation =
                FindInstantiationAt(enclosing, parsedDocument, index);
            if (instantiation == null)
            {
                return new AutoConnectModel { Message = "the caret is not on a module instantiation" };
            }

            Verilog.BuildingBlocks.BuildingBlock? instanced = instantiation.GetInstancedBuildingBlock();
            if (instanced is not Verilog.IPortNameSpace portNameSpace)
            {
                return new AutoConnectModel
                {
                    Message = "the instantiated module could not be resolved",
                    ModuleName = instantiation.SourceName ?? "",
                    InstanceName = instantiation.Name ?? "",
                };
            }

            List<AutoConnectPort> ports = new();
            foreach (Verilog.DataObjects.Port port in portNameSpace.PortsList)
            {
                string? current = null;
                if (instantiation.PortConnection.TryGetValue(port.Name, out Verilog.Expressions.Expression? expression)
                    && expression != null)
                {
                    current = expression.CreateString();
                }

                List<string> candidates = GetCandidates(port, enclosing);
                ports.Add(new AutoConnectPort
                {
                    Name = port.Name,
                    Direction = DirectionName(port.Direction),
                    PortGroup = port.PortGroupName,
                    CurrentConnection = current,
                    Candidates = candidates,
                    // A row needs no decision when it is already connected and
                    // no better candidate exists (the editor's Accept() would
                    // pick candidates[0], which is the same signal).
                    IsSettled = current != null
                        && (candidates.Count == 0 || candidates[0] == current),
                });
            }

            return new AutoConnectModel
            {
                ModuleName = instantiation.SourceName ?? "",
                InstanceName = instantiation.Name ?? "",
                Ports = ports,
            };
        }

        /// <summary>
        /// The module instantiation whose source range contains
        /// <paramref name="index"/>, or null. Mirrors the editor's
        /// <c>AutoConnectSnippet</c> lookup (BeginIndexReference ..
        /// LastIndexReference).
        /// </summary>
        private static Verilog.Items.ModuleInstantiation? FindInstantiationAt(
            Verilog.BuildingBlocks.BuildingBlock enclosing,
            Verilog.ParsedDocument parsedDocument,
            int index)
        {
            Verilog.IndexReference iref = Verilog.IndexReference.Create(parsedDocument, null!, index);

            Verilog.Items.ModuleInstantiation? found = null;
            foreach (Verilog.Items.IBuildingBlockInstantiation instance
                     in enclosing.GetBuildingBlockInstantiations())
            {
                if (instance is not Verilog.Items.ModuleInstantiation module) continue;
                if (iref.IsSmallerThan(instance.BeginIndexReference)) continue;
                if (instance.LastIndexReference == null) continue;
                if (iref.IsGreaterThan(instance.LastIndexReference)) continue;
                found = module;
            }
            return found;
        }

        /// <summary>
        /// Signals of the enclosing module that match <paramref name="port"/>,
        /// best match first. Same rule as the editor's
        /// <c>AutoConnectWindow.ConnectionItem.GetCantidates</c>.
        /// </summary>
        private static List<string> GetCandidates(
            Verilog.DataObjects.Port port,
            Verilog.BuildingBlocks.BuildingBlock enclosing)
        {
            List<string> candidates = new();
            string portName = port.Name;

            foreach (Verilog.INamedElement element in enclosing.NamedElements)
            {
                if (string.Equals(element.Name, portName, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(element.Name);
                    continue;
                }

                foreach (string suffix in RemovePortSuffixes)
                {
                    // The editor compares the suffix case-sensitively, but port
                    // names are commonly written lower-case (data_i) while the
                    // suffix list is upper-case, so match case-insensitively to
                    // keep the same intent.
                    if (!port.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                    string stripped = port.Name.Substring(0, port.Name.Length - suffix.Length);
                    if (string.Equals(element.Name, stripped, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(element.Name);
                        break;
                    }
                }
            }

            // de-duplicate while keeping the best-match order
            return candidates.Distinct(StringComparer.Ordinal).ToList();
        }

        private static string DirectionName(Verilog.DataObjects.Port.DirectionEnum direction) => direction switch
        {
            Verilog.DataObjects.Port.DirectionEnum.Input => "input",
            Verilog.DataObjects.Port.DirectionEnum.Output => "output",
            Verilog.DataObjects.Port.DirectionEnum.Inout => "inout",
            Verilog.DataObjects.Port.DirectionEnum.Ref => "ref",
            _ => "",
        };

        /// <summary>
        /// Applies the user's choices and returns the replacement text for the
        /// instantiation, or null when the instantiation cannot be rebuilt.
        ///
        /// <para>
        /// <paramref name="selections"/> maps a port name to the signal to
        /// connect; a port missing from the map (or mapped to null / "") is
        /// left unconnected. The instantiation's <c>PortConnection</c> is
        /// updated in place and <c>CreateString</c> regenerates the text, the
        /// same way the editor's <c>AutoConnectWindow.Complete()</c> does.
        /// </para>
        /// </summary>
        public static string? Apply(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            IReadOnlyDictionary<string, string?> selections,
            string indent)
        {
            if (parsedDocument?.Root == null) return null;
            Verilog.BuildingBlocks.BuildingBlock? enclosing = parsedDocument.GetBuildingBlockAt(index);
            if (enclosing == null) return null;

            Verilog.Items.ModuleInstantiation? instantiation =
                FindInstantiationAt(enclosing, parsedDocument, index);
            if (instantiation == null) return null;

            foreach (KeyValuePair<string, string?> selection in selections)
            {
                if (string.IsNullOrEmpty(selection.Value))
                {
                    instantiation.PortConnection.Remove(selection.Key);
                    continue;
                }
                instantiation.PortConnection[selection.Key] =
                    Verilog.Expressions.Expression.CreateTempExpression(selection.Value!);
            }

            return instantiation.CreateString(indent);
        }
    }
}
