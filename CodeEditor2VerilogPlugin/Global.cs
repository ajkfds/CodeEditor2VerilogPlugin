using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace pluginVerilog
{
    public static class Global
    {
        //        public static SetupForm SetupForm = new SetupForm();
        public static CodeDrawStyle CodeDrawStyle = new CodeDrawStyle();

        public static class Icons
        {
            public static IImage Exclamation = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/questionPaper.svg");

            /// <summary>
            /// <see cref="AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(string, Color)"/>
            /// that returns null instead of throwing when no Avalonia
            /// application is running.
            ///
            /// <para>
            /// SVG decoding goes through <c>Avalonia.Platform.AssetLoader</c>,
            /// which is only registered once Avalonia is initialised. The same
            /// parser code also runs in the headless language server (and in
            /// unit tests), where that loader does not exist and the call
            /// throws <c>Unable to locate 'Avalonia.Platform.IAssetLoader'</c>.
            /// Any object built during parsing that loads an icon therefore has
            /// to tolerate a missing icon: without this, a single icon lookup
            /// aborted the whole completion request (the caret at the start of
            /// a module-body line goes through
            /// <c>CompletionContext.AppendModuleInstanceSnippets</c>, which
            /// constructs a <c>ModuleInstanceSnippet</c> per module name).
            /// </para>
            /// </summary>
            public static IImage? TryGetSvgBitmap(string svgPath, Color color)
            {
                try
                {
                    return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(svgPath, color);
                }
                catch (Exception)
                {
                    // headless host (LSP server / tests): no icon, but the
                    // caller still works
                    return null;
                }
            }
        }

        public static Avalonia.Media.Color Color_Verilog = Avalonia.Media.Color.FromRgb(124, 140, 140);
        public static Avalonia.Media.Color Color_VerilogExternalProject = Avalonia.Media.Color.FromRgb(146, 85, 85);
        public static void CreateSnapShot()
        {
            using (StreamWriter sw = new StreamWriter("snapshot.log"))
            {
                List<WeakReference<CodeEditor2.Data.File>> disposeRefs = new List<WeakReference<CodeEditor2.Data.File>>();
                foreach (WeakReference<CodeEditor2.Data.File> wRef in CodeEditor2.Data.File.FileWeakReferences)
                {
                    CodeEditor2.Data.File? file;
                    if (!wRef.TryGetTarget(out file))
                    {
                        disposeRefs.Add(wRef);
                    }
                    else
                    {
                        Data.VerilogFile? vFile = file as Data.VerilogFile;
                        if (vFile == null) continue;
                    }
                }

                foreach (var disposeRef in disposeRefs)
                {
                    CodeEditor2.Data.File.FileWeakReferences.Remove(disposeRef);
                }
            }
        }


    }
}
