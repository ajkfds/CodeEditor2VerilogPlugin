using System;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;

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


                ///////////////////

                // YAMLを抽象構文木（AST）として読み込む
                var yaml = new YamlStream();
                using (var reader = new StringReader(text))
                {
                    yaml.Load(reader);
                }

                // ルートノードを取得（直キャストせず、安全に受け取る）
                YamlNode rootNode = yaml.Documents[0].RootNode;
                PrintNodePosition(yamlParser, rootNode);


                ////////////////

                ExternalLibrariesSetup externalLibrariesSetup = new ExternalLibrariesSetup() { ExternalLibraries = libs };
                CodeEditor2.Parser.YamlParsedDocument? yamlParsedDocument = yamlParser.ParsedDocument as CodeEditor2.Parser.YamlParsedDocument;
                if (yamlParsedDocument != null) yamlParsedDocument.ParsedObject = externalLibrariesSetup;

                ProjectProperty? projectProperty = yamlParser.TextFile.Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) return;

                projectProperty.ExtenralModuleLibraryPath.Clear();
                projectProperty.ExtenralPrimitiveLibraryPath.Clear();

                foreach (ExternalLibarary externalLibarary in externalLibrariesSetup.ExternalLibraries)
                {
                    if (externalLibarary == null) continue;

                    if(externalLibarary.Modules != null)
                    {
                        foreach (string module in externalLibarary.Modules)
                        {
                            if (module == null) continue;
                            if (projectProperty.ExtenralModuleLibraryPath.ContainsKey(module)) continue;
                            projectProperty.ExtenralModuleLibraryPath.Add(module, externalLibarary.Path);
                        }
                    }
                    if (externalLibarary.Primitives != null)
                    {
                        foreach (string primitive in externalLibarary.Primitives)
                        {
                            if (primitive == null) continue;
                            if (projectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(primitive)) continue;
                            projectProperty.ExtenralPrimitiveLibraryPath.Add(primitive, externalLibarary.Path);
                        }
                    }
                }

            }
            catch (YamlException ex)
            {
                yamlParser.ColorLine(CodeEditor2.Parser.YamlParser.Style.Color.Keyword, 1);
                yamlParser.Document.Marks.SetMarkAt((int)ex.Start.Index, (int)ex.End.Index - (int)ex.Start.Index+1, 1);
            }
            catch (Exception)
            {
                return;
            }
        }
        private static void PrintNodePosition(CodeEditor2.Parser.YamlParser yamlParser, YamlNode node, int indent = 0)
        {
            string indentStr = new string(' ', indent * 2);

            // 1. ノードが「連想配列（マッピング）」の場合
            if (node is YamlMappingNode mapping)
            {
                foreach (var entry in mapping.Children)
                {
                    var keyScalar = entry.Key as YamlScalarNode;
                    string keyName = keyScalar?.Value ?? entry.Key.ToString();
                    if (keyScalar != null) yamlParser.Color(CodeEditor2.Parser.YamlParser.Style.Color.Header, (int)(keyScalar.Start.Index), (int)(keyScalar.End.Index - keyScalar.Start.Index));
                    PrintNodePosition(yamlParser,entry.Value, indent + 1);
                }
            }
            // 2. ノードが「配列（シーケンス）」の場合
            else if (node is YamlSequenceNode sequence)
            {
                if (sequence != null) yamlParser.Color(CodeEditor2.Parser.YamlParser.Style.Color.Paramater, (int)(sequence.Start.Index), (int)(sequence.End.Index - sequence.Start.Index));
                foreach (var item in sequence.Children)
                {
                    // 配列の各要素を再帰呼び出し
                    PrintNodePosition(yamlParser,item, indent + 1);
                }
            }
            // 3. ノードが「単一の値（スカラ）」の場合
            else if (node is YamlScalarNode scalar)
            {
                if (scalar != null) yamlParser.Color(CodeEditor2.Parser.YamlParser.Style.Color.Identifier , (int)(scalar.Start.Index), (int)(scalar.End.Index - scalar.Start.Index));
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
            public List<string> Primitives { get; set; }
        }
    }
}
