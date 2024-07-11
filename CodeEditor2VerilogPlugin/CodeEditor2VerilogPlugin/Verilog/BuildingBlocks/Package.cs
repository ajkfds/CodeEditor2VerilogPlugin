using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Package : BuildingBlock
    {
        protected Package() : base(null, null)
        {

        }

        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public override Data.IVerilogRelatedFile? File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                fileRef = new WeakReference<Data.IVerilogRelatedFile>(value);
            }
        }

        public override string FileId { get; protected set; }
        private bool cellDefine = false;
        public bool CellDefine
        {
            get { return cellDefine; }
        }



        public static Package Create(WordScanner word, Attribute attribute, Data.IVerilogRelatedFile file, bool protoType)
        {
            return Create(word, null, attribute, file, protoType);
        }
        public static Package Create(
            WordScanner word,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute attribute,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
             * 
             * 
            
            package_declaration ::= 
                { attribute_instance } "package" [ lifetime ] package_identifier ; 
                [ timeunits_declaration ] { { attribute_instance } package_item } 
                "endpackage" [ : package_identifier ] 

            package_item ::= 
                  package_or_generate_item_declaration 
                | anonymous_program 
                | package_export_declaration 
                | timeunits_declaration
            */

            if (word.Text != "package") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            Package package = new Package();
            package.Parent = word.RootParsedDocument.Root;
            package.Project = word.Project;
            package.BuildingBlock = package;
            package.File = file;
            package.BeginIndexReference = word.CreateIndexReference();
            if (word.CellDefine) package.cellDefine = true;
            word.MoveNext();


            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvPair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvPair.Key, kvPair.Value);
            }


            if (!word.CellDefine && !protoType)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parsePackageItems(prototypeWord, parameterOverrides, null, package);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parsePackageItems(word, parameterOverrides, null, package);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                parsePackageItems(word, parameterOverrides, null, package);
                word.Prototype = false;
            }

            // endmodule keyword
            if (word.Text == "endpackage")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                package.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(package.BeginIndexReference, package.LastIndexReference);
                word.MoveNext();
                return package;
            }

            {
                word.AddError("endpackage expected");
            }

            return package;
        }

        /*
            package_item ::= 
                  package_or_generate_item_declaration 
                | anonymous_program 
                | package_export_declaration 
                | timeunits_declaration
        */
        protected static void parsePackageItems(
            WordScanner word,
            //            string parameterOverrideModuleName,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute,
            Package module
            )
        {

            // module_identifier
            module.Name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal package name");
            }
            else
            {
                module.NameReference = word.GetReference();
            }
            word.MoveNext();

            while (true)
            {
                if (word.Eof || word.Text == "endpackage")
                {
                    break;
                }

                if (word.Eof || word.Text == "endpackage") break;

                if (word.GetCharAt(0) == ';')
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                }

                while (!word.Eof)
                {
                    if (!Items.PackageItem.Parse(word, module))
                    {
                        if (word.Text == "endpackage") break;
                        word.AddError("illegal package item");
                        word.MoveNext();
                    }
                }
                break;
            }

            if (!word.Prototype)
            {
                checkVariablesUseAndDriven(word, module);
            }

            return;
        }

        private CodeEditor2.CodeEditor.AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (ModuleItems.IInstantiation instantiation in Instantiations.Values)
            {
                if (instantiation.Name == null) throw new Exception();
                items.Add(newItem(instantiation.Name, CodeDrawStyle.ColorType.Identifier));
            }
        }

        protected static void checkVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        {
            foreach (var variable in nameSpace.DataObjects.Values)
            {
                if (variable.DefinedReference == null) continue;

                DataObjects.Variables.ValueVariable? valueVar = variable as DataObjects.Variables.ValueVariable;
                if (valueVar == null) continue;

                if (valueVar.AssignedReferences.Count == 0)
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        word.AddNotice(variable.DefinedReference, "undriven & unused");
                    }
                    else
                    {
                        word.AddNotice(variable.DefinedReference, "undriven");
                    }
                }
                else
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        word.AddNotice(variable.DefinedReference, "unused");
                    }
                }
            }
        }


        public override List<string> GetExitKeywords()
        {
            return new List<string>
            {
                //                "module","endmodule",
                //                "function","endfunction",
                //                "task","endtask",
                //                "always","initial",
                //                "assign","specify","endspecify",
                //                "generate","endgenerate"
            };
        }


    }
}
