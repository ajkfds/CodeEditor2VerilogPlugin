using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.FileTypes
{
    public class SystemVerilogFile : CodeEditor2.FileTypes.FileType
    {
        public static string TypeID = "SystemVerilogFile";
        public override string ID { get { return TypeID; } }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath.ToLower().EndsWith(".sv")
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<CodeEditor2.Data.File> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.VerilogFile.CreateSystemVerilog(relativeFilePath, project);
        }

        public override IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                );
        }

    }
}
