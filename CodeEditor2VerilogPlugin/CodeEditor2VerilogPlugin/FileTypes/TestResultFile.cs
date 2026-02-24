using Avalonia.Media;
using CodeEditor2.FileTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.FileTypes
{
    public class TestResultFile : FileType
    {
        public override string ID { get { return "TestResultFile"; } }

        public override bool IsThisFileType(string relativeFilePath,CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath.ToLower().EndsWith(".verilog.result")
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<CodeEditor2.Data.File?> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.TestResultFile.CreateAsync(relativeFilePath, project);
        }

        public override IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2/Assets/Icons/text.svg",
                Avalonia.Media.Color.FromArgb(100, 200, 200, 200)
                );
        }

    }
}
