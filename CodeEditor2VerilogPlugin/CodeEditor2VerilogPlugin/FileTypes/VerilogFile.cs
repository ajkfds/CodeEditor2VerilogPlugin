using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.FileTypes
{
    public class VerilogFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get { return "VerilogFile"; } }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath.ToLower().EndsWith(".v")
            )
            {
                return true;
            }
            return false;
        }

        public override CodeEditor2.Data.File CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return Data.VerilogFile.Create(relativeFilePath, project);
        }

        public override void CreateNewFile(string relativeFilePath, Project project)
        {
            string body = relativeFilePath;
            if (relativeFilePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                body = body.Substring(body.IndexOf(System.IO.Path.DirectorySeparatorChar));
            }

            if(body.EndsWith(".v") || body.EndsWith(".V"))
            {
                body = body.Substring(0, body.Length - 2);
            }

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(project.GetAbsolutePath(relativeFilePath)))
            {
                sw.Write("module "+body+";\n");
                sw.Write("\n");
                sw.Write("endmodule\n");
            }

        }
    }
}
