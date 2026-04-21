using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    public class ConnectionCheckSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ConnectionCheckSnippet() : base("connectionCheck")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/search.svg",
                    Plugin.ThemeColor
                    );
        }

        private CodeDocument? document;

        public override async System.Threading.Tasks.Task ApplyAsync()
        {
            CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (file == null) return;
            document = file.CodeDocument;
            if (document == null) return;

            int caretIndex = document.CaretIndex;
            ParsedDocument? parsedDocument = file.ParsedDocument as ParsedDocument;
            if (parsedDocument == null) return;

            // Find the DataObject at cursor position
            BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(caretIndex);
            if (buildingBlock == null) return;

            // Try to find the specific DataObject at the position
            DataObject? targetDataObject = FindDataObjectAt(buildingBlock, caretIndex);
            if (targetDataObject == null)
            {
                // Try to find from NamedElements
                targetDataObject = FindNearestDataObject(buildingBlock, caretIndex);
            }

            if (targetDataObject == null)
            {
                CodeEditor2.Controller.AppendLog("No signal found at cursor position", Colors.Orange);
                return;
            }

            // Trace the connection
            var connectionInfo = await TraceConnectionAsync(targetDataObject, buildingBlock);

            // Open new window with connection information
            await ShowConnectionWindowAsync(connectionInfo, targetDataObject.Name);
        }

        private DataObject? FindDataObjectAt(BuildingBlock buildingBlock, int caretIndex)
        {
            // Check AssignedReferences
            foreach (var element in buildingBlock.NamedElements)
            {
                DataObject? dataObject = element as DataObject;
                if (dataObject == null) continue;

                foreach (var reference in dataObject.AssignedReferences)
                {
                    if (reference != null && caretIndex >= reference.Index && caretIndex < reference.Index + reference.Length)
                    {
                        return dataObject;
                    }
                }
                foreach (var reference in dataObject.UsedReferences)
                {
                    if (reference != null && caretIndex >= reference.Index && caretIndex < reference.Index + reference.Length)
                    {
                        return dataObject;
                    }
                }
            }
            return null;
        }

        private DataObject? FindNearestDataObject(BuildingBlock buildingBlock, int caretIndex)
        {
            DataObject? nearest = null;
            int nearestDistance = int.MaxValue;

            foreach (var element in buildingBlock.NamedElements)
            {
                DataObject? dataObject = element as DataObject;
                if (dataObject == null) continue;

                if (dataObject.DefinedReference != null && dataObject.DefinedReference != null &&
                    caretIndex >= dataObject.DefinedReference.Index && caretIndex < dataObject.DefinedReference.Index + dataObject.DefinedReference.Length)
                {
                    return dataObject;
                }

                // Check references
                foreach (var reference in dataObject.AssignedReferences)
                {
                    if (reference == null) continue;
                    int dist = Math.Abs(reference.Index - caretIndex);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearest = dataObject;
                    }
                }
                foreach (var reference in dataObject.UsedReferences)
                {
                    if (reference == null) continue;
                    int dist = Math.Abs(reference.Index - caretIndex);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearest = dataObject;
                    }
                }
            }

            return nearest;
        }

        public class ConnectionNode
        {
            public string InstanceName { get; set; } = "";
            public string ModuleName { get; set; } = "";
            public string PortName { get; set; } = "";
            public Port.DirectionEnum Direction { get; set; }
            public string SignalName { get; set; } = "";
            public List<ConnectionNode> Children { get; } = new List<ConnectionNode>();
            public List<ConnectionNode> Parents { get; } = new List<ConnectionNode>();
            public int Depth { get; set; }
        }

        private async Task<ConnectionNode> TraceConnectionAsync(DataObject targetDataObject, BuildingBlock rootBlock)
        {
            ConnectionNode rootNode = new ConnectionNode
            {
                SignalName = targetDataObject.Name,
                InstanceName = rootBlock.Name,
                ModuleName = rootBlock.Name,
                Depth = 0
            };

            await System.Threading.Tasks.Task.Run(() =>
            {
                TraceReceivers(rootNode, targetDataObject, rootBlock, 0);
                TraceDrivers(rootNode, targetDataObject, rootBlock, 0);
            });

            return rootNode;
        }

        private void TraceReceivers(ConnectionNode node, DataObject dataObject, BuildingBlock block, int depth)
        {
            if (depth > 10) return; // Prevent infinite recursion

            // Find module instantiations in this block
            foreach (var element in block.NamedElements)
            {
                if (element is not ModuleInstantiation instantiation) continue;

                var instancedModule = instantiation.GetInstancedBuildingBlock() as Module;
                if (instancedModule == null) continue;

                // Check if this instantiation uses the signal
                foreach (var portConnection in instantiation.PortConnection)
                {
                    if (portConnection.Value is not DataObjectReference portRef) continue;
                    if (portRef.TargetDataObject != dataObject) continue;

                    // Found a receiver - check the module's output ports
                    if (instancedModule.Ports.TryGetValue(portConnection.Key, out Port? port))
                    {
                        if (port.Direction == Port.DirectionEnum.Output || port.Direction == Port.DirectionEnum.Inout)
                        {
                            var childNode = new ConnectionNode
                            {
                                InstanceName = instantiation.Name,
                                ModuleName = instancedModule.Name,
                                PortName = portConnection.Key,
                                Direction = port.Direction,
                                SignalName = dataObject.Name,
                                Depth = depth + 1
                            };
                            node.Children.Add(childNode);

                            // Trace further receivers from this module's output
                            if (port.DataObject != null)
                            {
                                TraceReceivers(childNode, port.DataObject, instancedModule, depth + 1);
                            }
                        }
                    }
                }
            }
        }

        private void TraceDrivers(ConnectionNode node, DataObject dataObject, BuildingBlock block, int depth)
        {
            if (depth > 10) return;

            // Find module instantiations in this block
            foreach (var element in block.NamedElements)
            {
                if (element is not ModuleInstantiation instantiation) continue;

                var instancedModule = instantiation.GetInstancedBuildingBlock() as Module;
                if (instancedModule == null) continue;

                // Check if this instantiation drives the signal
                foreach (var portConnection in instantiation.PortConnection)
                {
                    if (portConnection.Value is not DataObjectReference portRef) continue;
                    if (portRef.TargetDataObject != dataObject) continue;

                    // Found a driver - check the module's input ports
                    if (instancedModule.Ports.TryGetValue(portConnection.Key, out Port? port))
                    {
                        if (port.Direction == Port.DirectionEnum.Input || port.Direction == Port.DirectionEnum.Inout)
                        {
                            var parentNode = new ConnectionNode
                            {
                                InstanceName = instantiation.Name,
                                ModuleName = instancedModule.Name,
                                PortName = portConnection.Key,
                                Direction = port.Direction,
                                SignalName = dataObject.Name,
                                Depth = depth + 1
                            };
                            node.Parents.Add(parentNode);

                            // Trace further drivers from this module's input
                            if (port.DataObject != null)
                            {
                                TraceDrivers(parentNode, port.DataObject, instancedModule, depth + 1);
                            }
                        }
                    }
                }
            }
        }

        private System.Threading.Tasks.Task ShowConnectionWindowAsync(ConnectionNode rootNode, string signalName)
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = new ConnectionTreeWindow(rootNode, signalName);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }
    }

    public class ConnectionTreeWindow : Window
    {
        private ConnectionCheckSnippet.ConnectionNode rootNode;
        private string signalName;

        public ConnectionTreeWindow(ConnectionCheckSnippet.ConnectionNode rootNode, string signalName)
        {
            this.rootNode = rootNode;
            this.signalName = signalName;
            Title = $"Connection Tree: {signalName}";
            Width = 600;
            Height = 500;

            var scrollViewer = new ScrollViewer();
            var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(10) };

            // Header
            var header = new TextBlock
            {
                Text = $"Signal: {signalName}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(header);

            // Drivers section
            if (rootNode.Parents.Count > 0)
            {
                var driversHeader = new TextBlock
                {
                    Text = "← Drivers",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Blue),
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                };
                stackPanel.Children.Add(driversHeader);

                foreach (var driver in rootNode.Parents)
                {
                    var driverItem = CreateNodeTextBlock(driver, true);
                    stackPanel.Children.Add(driverItem);
                }
            }
            else
            {
                var noDrivers = new TextBlock
                {
                    Text = "← No drivers found",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                };
                stackPanel.Children.Add(noDrivers);
            }

            // Current signal
            var currentSignal = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 255)),
                Padding = new Avalonia.Thickness(10),
                Margin = new Avalonia.Thickness(20, 10, 20, 10),
                Child = new TextBlock
                {
                    Text = $"● {signalName} (in {rootNode.ModuleName}.{rootNode.InstanceName})",
                    FontWeight = FontWeight.Bold
                }
            };
            stackPanel.Children.Add(currentSignal);

            // Receivers section
            if (rootNode.Children.Count > 0)
            {
                var receiversHeader = new TextBlock
                {
                    Text = "Receivers →",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Green),
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                };
                stackPanel.Children.Add(receiversHeader);

                foreach (var receiver in rootNode.Children)
                {
                    var receiverItem = CreateNodeTextBlock(receiver, false);
                    stackPanel.Children.Add(receiverItem);
                }
            }
            else
            {
                var noReceivers = new TextBlock
                {
                    Text = "No receivers found →",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                };
                stackPanel.Children.Add(noReceivers);
            }

            scrollViewer.Content = stackPanel;
            Content = scrollViewer;
        }

        private TextBlock CreateNodeTextBlock(ConnectionCheckSnippet.ConnectionNode node, bool isDriver)
        {
            string direction = node.Direction == Port.DirectionEnum.Input ? "←" :
                              node.Direction == Port.DirectionEnum.Output ? "→" : "↔";
            string indent = new string(' ', node.Depth * 4);

            var textBlock = new TextBlock
            {
                Text = $"{indent}{direction} {node.ModuleName}.{node.InstanceName}.{node.PortName}",
                FontSize = 12,
                Margin = new Avalonia.Thickness(0, 2, 0, 2)
            };

            if (isDriver)
            {
                textBlock.Foreground = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                textBlock.Foreground = new SolidColorBrush(Colors.Green);
            }

            return textBlock;
        }
    }
}
