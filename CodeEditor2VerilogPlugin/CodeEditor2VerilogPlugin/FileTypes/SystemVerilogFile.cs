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

        public override CodeEditor2.Data.File CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return Data.VerilogFile.CreateSystemVerilog(relativeFilePath, project);
        }
    }
}
