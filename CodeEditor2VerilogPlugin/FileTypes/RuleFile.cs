using System.Threading.Tasks;

namespace pluginVerilog.FileTypes
{
    /// <summary>
    /// <c>.rule.yaml</c> file type: YAML rule set (name / severity) that applies
    /// to every source file below the directory containing the file.
    /// </summary>
    /// <remarks>
    /// The older <c>.rule</c> spelling is accepted too, so an existing workspace
    /// keeps its severities; new files are created as <c>.rule.yaml</c> (see
    /// <see cref="ProjectProperty.RuleFileExtension"/>).
    /// </remarks>
    public class RuleFile : CodeEditor2.FileTypes.FileType
    {
        public static string TypeID = "RuleFile";
        public override string ID { get { return TypeID; } }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return ProjectProperty.IsRuleFilePath(relativeFilePath);
        }

        public override async Task<CodeEditor2.Data.File> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.RuleFile.CreateAsync(relativeFilePath, project);
        }

        public override void CreateNewFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            // a new .rule file starts from the built-in defaults so the user
            // only has to change the severities to override
            SystemVerilogCore.Verilog.RuleSet ruleSet = new SystemVerilogCore.Verilog.RuleSet();
            ruleSet.SaveFile(project.GetAbsolutePath(relativeFilePath));
        }

        public override Avalonia.Media.IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2VerilogPlugin/Assets/Icons/document.svg",
                Avalonia.Media.Color.FromArgb(100, 240, 200, 140)
                );
        }
    }
}
