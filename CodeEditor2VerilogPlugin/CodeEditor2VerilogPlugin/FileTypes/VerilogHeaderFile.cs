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

        public override CodeEditor2.Data.File CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return Data.VerilogHeaderFile.Create(relativeFilePath, project);
        }

    }
}
