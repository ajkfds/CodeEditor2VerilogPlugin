using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class TestResultFile : TextFile
    {
        public static async Task<TestResultFile> CreateAsync(string relativePath, Project project)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }
            TestResultFile fileItem = new TestResultFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };
            await fileItem.FileCheck();
            if (fileItem.document == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }

        public override async Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            await base.AcceptParsedDocumentAsync(newParsedDocument);

            //            Project.FileClassify = new TestR(Project);
        }



        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.TestResultParser(this, parseMode, token);
        }


    }
}
