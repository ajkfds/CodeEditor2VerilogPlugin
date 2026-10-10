using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    // Reference to the built-in "process" class static scope (IEEE 1800-2017 section 9.7)
    // Handles "process::self()", "process::FINISHED", "process::status" etc.
    // ParseCreate is called from Primary.parseCreate when the identifier "process"
    // is followed by "::" and the process class is not shadowed by a user definition.
    public class ProcessClassReference : Primary
    {
        protected ProcessClassReference() { }

        public required DataObjects.DataTypes.IDataType ProcessType { init; get; }

        public override string CreateString()
        {
            return "process";
        }

        public static ProcessClassReference? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "process") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            WordReference beginRef = word.GetReference();
            word.MoveNext();

            if (word.Text != "::")
            {
                // "process" as a data type (variable declaration) is handled by DataTypeFactory.
                // reaching here means a bare "process" reference; report an error
                word.AddError(":: required after process");
                return null;
            }
            word.MoveNext(); // ::

            if (word.Eof || !General.IsIdentifier(word.Text))
            {
                word.AddError("process member required");
                return null;
            }

            string memberName = word.Text;

            // resolve the member in the static process class scope
            NamedElements staticElements = new NamedElements();
            DataObjects.DataTypes.ProcessType.AppendBuiltinMethods(staticElements);

            if (!staticElements.ContainsKey(memberName))
            {
                word.AddError("unfound object");
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                return null;
            }

            Verilog.BuiltInMethod method = (Verilog.BuiltInMethod)staticElements[memberName];

            // member with parentheses : parse as a built-in method call on a dummy process object
            // (BuiltinMethodCall.ParseCreate expects word.Text == the method name, so it must be
            //  called BEFORE consuming the member identifier)
            if (word.NextText == "(")
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                DataObjects.Variables.Process dummyProcess = DataObjects.Variables.Process.Create("process", DataObjects.DataTypes.ProcessType.Create());
                BuiltinMethodCall? methodCall = BuiltinMethodCall.ParseCreate(word, nameSpace, dummyProcess);
                if (methodCall == null) return null;

                ProcessClassReference methodRef = new ProcessClassReference()
                {
                    ProcessType = DataObjects.DataTypes.ProcessType.Create(),
                    Reference = WordReference.CreateReferenceRange(beginRef, methodCall.Reference)
                };
                methodRef.BitWidth = methodCall.BitWidth;
                return methodRef;
            }

            // member without parentheses (e.g. process::FINISHED, process::status)
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (method.Ports.Count != 0)
            {
                word.AddError("illegal function call");
                return null;
            }
            ProcessClassReference reference = new ProcessClassReference()
            {
                ProcessType = DataObjects.DataTypes.ProcessType.Create(),
                Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference())
            };
            reference.Constant = true;
            reference.BitWidth = method.ReturnVariable?.BitWidth;
            return reference;
        }
    }
}
