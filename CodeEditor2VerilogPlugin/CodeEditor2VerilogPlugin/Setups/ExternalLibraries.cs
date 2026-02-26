using CodeEditor2.Parser;
using ExCSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Net.Mime.MediaTypeNames;

namespace pluginVerilog.Setups
{
    public class ExternalLibrariesSetup
    {

        public static string YamlPath = "ExternalLibraries.yaml";

        public static void ParseYaml(CodeEditor2.Parser.YamlParser yamlParser)
        {
            if (yamlParser.TextFile.RelativePath != YamlPath) return;
            string text = yamlParser.Document.CreateString();
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    //.WithNamingConvention(CamelCaseNamingConvention.Instance) // YAMLが小文字(server)ならCamelCaseを指定
                    .Build();
                var libs = deserializer.Deserialize<List<ExternalLibarary>>(text);

                ExternalLibrariesSetup externalLibrariesSetup = new ExternalLibrariesSetup() { ExternalLibraries = libs };
                CodeEditor2.Parser.YamlParsedDocument? yamlParsedDocument = yamlParser.ParsedDocument as CodeEditor2.Parser.YamlParsedDocument;
                if(yamlParsedDocument != null) yamlParsedDocument.ParsedObject = externalLibrariesSetup;
            }
            catch(YamlException ex)
            {
                yamlParser.Document.Marks.SetMarkAt((int)ex.Start.Index, (int)ex.End.Index - (int)ex.Start.Index, 0);
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public static void AcceptYamlParsedDocument(CodeEditor2.Data.YamlFile yamlFile)
        {
            if (yamlFile.RelativePath != YamlPath) return;
            CodeEditor2.Parser.YamlParsedDocument? yamlParsedDocument = yamlFile.ParsedDocument as CodeEditor2.Parser.YamlParsedDocument;
            if (yamlParsedDocument == null) return;
            ExternalLibrariesSetup? externalLibrariesSetup = yamlParsedDocument.ParsedObject as ExternalLibrariesSetup;
//            yamlFile.Project.
        }
        public List<ExternalLibarary> ExternalLibraries { get; set; }

        public class ExternalLibarary
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public List<string> Modules { get; set; }
        }
    }
}
