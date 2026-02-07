using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.FileTypes
{
    public class VerilogHeaderFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get { return "VerilogHeaderFile"; } }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath.ToLower().EndsWith(".vh")
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<CodeEditor2.Data.File> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.VerilogHeaderFile.Create(relativeFilePath, project);
        }

        public override IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                );
        }
    }
}
