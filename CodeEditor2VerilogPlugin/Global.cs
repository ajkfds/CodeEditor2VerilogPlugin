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
