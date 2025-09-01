using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Package : BuildingBlock
    {
        protected Package() : base(null, null)
        {

        }

        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public required override Data.IVerilogRelatedFile? File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            init
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



        public static Package Create(WordScanner word, Attribute attribute, BuildingBlock parent, Data.IVerilogRelatedFile file, bool protoType)
        {
            return Create(word, null, attribute, parent, file, protoType);
        }
        public static Package Create(
            WordScanner word,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute attribute,
            BuildingBlock parent,
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
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();


            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvPair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvPair.Key, kvPair.Value);
            }

            // module_identifier
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal package name");
                word.SkipToKeyword(";");
                return null;
            }

            Package package = new Package()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = file,
                Name = word.Text,
                Parent = word.RootParsedDocument.Root,
                Project = word.Project
            };
            package.BuildingBlock = package;
            if (word.CellDefine) package.cellDefine = true;

            word.MoveNext();

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
            }
            else
            {
                word.AddError("endpackage expected");
            }

            // register with the parent module
            if (!parent.BuildingBlocks.ContainsKey(package.Name))
            {
                parent.BuildingBlocks.Add(package.Name, package);
            }
            else
            {
                package.NameReference.AddError("duplicated package name");
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
                CheckVariablesUseAndDriven(word, module);
            }

            return;
        }

        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (var instantiation in NamedElements.Values.OfType<IBuildingBlockInstantiation>())
            {
                items.Add(newItem(instantiation.Name, CodeDrawStyle.ColorType.Identifier));
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
