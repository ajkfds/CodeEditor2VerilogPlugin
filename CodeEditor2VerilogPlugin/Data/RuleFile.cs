using CodeEditor2.CodeEditor.Parser;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    /// <summary>
    /// TextFile representation of a <c>.rule</c> file.
    ///
    /// <para>
    /// A <c>.rule</c> file holds the severity settings of the parser rules as
    /// YAML (<c>rules: - name: ... / severity: ...</c>). The file applies to
    /// every source file below the directory that contains it: on accept the
    /// parsed rule set is registered in
    /// <see cref="pluginVerilog.ProjectProperty.SetRuleSet(string, RuleSet?)"/>
    /// and looked up per file during parsing
    /// (<see cref="pluginVerilog.ProjectProperty.GetRuleSet(string)"/>).
    /// </para>
    /// </summary>
    public class RuleFile : CodeEditor2.Data.TextFile
    {
        public static new async Task<RuleFile> CreateAsync(string relativePath, CodeEditor2.Data.Project project)
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

            RuleFile fileItem = new RuleFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };
            await fileItem.FileCheckAsync();
            return fileItem;
        }

        public override async Task AcceptParsedDocumentAsync(DocumentParser parser)
        {
            await base.AcceptParsedDocumentAsync(parser);

            if (!Project.ProjectProperties.TryGetValue(Plugin.StaticID, out CodeEditor2.Data.ProjectProperty? property)) return;
            ProjectProperty? projectProperty = property as ProjectProperty;
            if (projectProperty == null) return;

            // The text is read from the parsed document (not from the file on
            // disk) so that editing a .rule file in the editor updates the rule
            // sets without saving first.
            RuleSet? ruleSet = RuleSet.ParseYaml(GetRuleText(), out _);
            projectProperty.SetRuleSet(RelativePath, ruleSet);
        }

        /// <summary>
        /// Text of this rule file (edited buffer when available).
        /// </summary>
        private string GetRuleText()
        {
            CodeEditor2.CodeEditor.CodeDocument? document = CodeDocument;
            if (document != null) return document.CreateString();

            try
            {
                string absolutePath = AbsolutePath;
                if (System.IO.File.Exists(absolutePath)) return System.IO.File.ReadAllText(absolutePath);
            }
            catch (System.Exception)
            {
                // unreadable file: treat as empty (built-in defaults)
            }
            return "";
        }

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.RuleFileParser(this, parseMode, token);
        }
    }
}
