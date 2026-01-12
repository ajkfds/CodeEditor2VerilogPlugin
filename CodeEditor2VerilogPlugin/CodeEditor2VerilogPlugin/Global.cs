using Avalonia.Media;
using pluginAi;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public static class Global
    {
//        public static SetupForm SetupForm = new SetupForm();
        public static CodeDrawStyle CodeDrawStyle = new CodeDrawStyle();

        public static class Icons
        {
            public static IImage Exclamation = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/questionPaper.svg");
        //    public static IconImage Exclamation = new IconImage(Properties.Resources.exclamation);
        //    public static IconImage ExclamationBox = new IconImage(Properties.Resources.exclamationBox);
        //    public static IconImage Play = new IconImage(Properties.Resources.play);
        //    public static IconImage Pause = new IconImage(Properties.Resources.pause);
        //    public static IconImage Verilog = new IconImage(Properties.Resources.verilog);
        //    public static IconImage VerilogHeader = new IconImage(Properties.Resources.verilogHeader);
        //    public static IconImage SystemVerilog = new IconImage(Properties.Resources.systemVerilog);
        //    public static IconImage SystemVerilogHeader = new IconImage(Properties.Resources.systemVerilogHeader);
        //    public static IconImage IcarusVerilog = new IconImage(Properties.Resources.icarusVerilog);
        //    public static IconImage NewBadge = new IconImage(Properties.Resources.newBadge);
        //    public static IconImage MedalBadge = new IconImage(Properties.Resources.medalBadge);
        }

        public static Func<LLMChat>? GetLLM = null;
        public static void CreateSnapShot()
        {
            using (StreamWriter sw = new StreamWriter("snapshot.log"))
            {
                List<WeakReference<CodeEditor2.Data.File>> disposeRefs = new List<WeakReference<CodeEditor2.Data.File>>();
                foreach(WeakReference<CodeEditor2.Data.File> wRef in CodeEditor2.Data.File.FileWeakReferences)
                {
                    CodeEditor2.Data.File? file;
                    if(!wRef.TryGetTarget(out file))
                    {
                        disposeRefs.Add(wRef);
                    }
                    else
                    {
                        Data.VerilogFile? vFile = file as Data.VerilogFile;
                        if (vFile == null) continue;

                        sw.Write(vFile.DebugInfo());
                    }
                }

                foreach(var disposeRef in disposeRefs)
                {
                    CodeEditor2.Data.File.FileWeakReferences.Remove(disposeRef);
                }
            }
        }


    }
}
