using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class VariableReference : Primary
    {
        protected VariableReference() { }
        public string VariableName { get; init; }
        public RangeExpression? RangeExpression { get; protected set; }
        public List<Expression> Dimensions = new List<Expression>();
        public DataObjects.DataObject? Variable = null;
        public string NameSpaceText = "";

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(NameSpaceText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));

            if(Variable is Reg || Variable is Bit || Variable is Logic)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));
            }
            else if (Variable is Net)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Net));
            }
            else if(Variable is DataObjects.Constants.Constants)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Parameter));
            }
            else
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));
            }

            if (RangeExpression != null)
            {
                label.AppendText(" ");
                label.AppendLabel(RangeExpression.GetLabel());
            }
            foreach (Expression expression in Dimensions)
            {
                label.AppendText(" [");
                label.AppendLabel(expression.GetLabel());
                label.AppendText("]");
            }
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(CreateString());
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(NameSpaceText);
            sb.Append(VariableName);
            if (RangeExpression != null)
            {
                sb.Append(RangeExpression.CreateString());
            }
            foreach (Expression expression in Dimensions)
            {
                sb.Append(" [");
                sb.Append(expression.CreateString());
                sb.Append("]");
            }
            return sb.ToString();
        }


        public new static VariableReference ParseCreate(WordScanner word, INamedElement nameSpace)
        {
            throw new Exception("illegal access");
        }

        public static VariableReference Create(DataObjects.Variables.Variable variable,NameSpace nameSpace)
        {
            VariableReference val = new VariableReference()
            {
                VariableName = variable.Name
            };
            val.Variable = variable;
            if(variable.DefinedReference != null) val.Reference = variable.DefinedReference;

            return val;
        }

        // nameSpace is reqired for range expression parse
        public static VariableReference? ParseCreate(WordScanner word,NameSpace nameSpace, INamedElement owner, bool assigned)
        {
            if (!owner.NamedElements.ContainsDataObject(word.Text)) return null;
            DataObjects.DataObject dataObject = (DataObjects.DataObject)owner.NamedElements[word.Text];

            VariableReference val = new VariableReference()
            {
                VariableName = dataObject.Name
            };
            val.Variable = dataObject;
            val.Reference = word.GetReference();

            word.Color(dataObject.ColorType);

            if (dataObject is DataObjects.Constants.Constants)
            {
                val.Constant = true;
            }

            if (assigned)
            {
                val.Variable.AssignedReferences.Add(word.GetReference());
            }
            else
            {
                val.Variable.UsedReferences.Add(word.GetReference());
            }
            word.MoveNext();

            // parse dimensions
            while (!word.Eof && val.Dimensions.Count < dataObject.Dimensions.Count)
            {
                if (word.GetCharAt(0) != '[')
                {
                    word.AddError("lacked dimension");
                    break;
                }
                word.MoveNext();
                Expression? exp = Expression.ParseCreate(word, nameSpace);
                if (exp != null) val.Dimensions.Add(exp);
                if (word.GetCharAt(0) != ']')
                {
                    word.AddError("illegal dimension");
                    break;
                }
                word.MoveNext();
            }

            // parse ranges
            if (word.GetCharAt(0) == '[')
            {
                if (!parseRange(word, nameSpace, val)) return null;
            }
            else
            {   // w/o range
                if (dataObject is DataObjects.Variables.IntegerVectorValueVariable)
                {
                    var original = dataObject as DataObjects.Variables.IntegerVectorValueVariable;
                    if (original == null) throw new Exception();
                    if (original.Range != null) val.BitWidth = original.Range.Size;
                    else val.BitWidth = 1;
                }
                else if (dataObject is DataObjects.Constants.Parameter)
                {
                    var constants = (DataObjects.Constants.Parameter)dataObject;
                    if (constants.Expression != null)
                    {
                        val.Value = constants.Expression.Value;
                        val.BitWidth = constants.Expression.BitWidth;
                    }
                }
                else if (dataObject is Net)
                {
                    if (((Net)dataObject).Range != null) val.BitWidth = ((Net)dataObject).Range.Size;
                    else val.BitWidth = 1;
                }
                else if (dataObject is DataObjects.Variables.Genvar)
                {
                    val.Constant = true;
                }
            }


            return val;
        }


        //public static VariableReference? ParseCreate(WordScanner word, NameSpace nameSpace,INamedElement owner, bool assigned)
        //{
        //    if (nameSpace == null) System.Diagnostics.Debugger.Break();
        //    DataObjects.DataObject? dataObject = getDataObject(word, word.Text, nameSpace);
        //    if (dataObject == null) return null;

        //    VariableReference val = new VariableReference() {
        //        VariableName = dataObject.Name
        //    };
        //    val.Variable = dataObject;
        //    val.Reference = word.GetReference();


        //    word.Color(dataObject.ColorType);
        //    if(dataObject is DataObjects.Constants.Constants)
        //    {
        //        val.Constant = true;
        //    }

        //    if (assigned)
        //    {
        //        val.Variable.AssignedReferences.Add(word.GetReference());
        //    }
        //    else
        //    {
        //        val.Variable.UsedReferences.Add(word.GetReference());
        //    }
        //    word.MoveNext();

        //    // parse dimensions
        //    while (!word.Eof && val.Dimensions.Count < dataObject.Dimensions.Count)
        //    {
        //        if (word.GetCharAt(0) != '[')
        //        {
        //            word.AddError("lacked dimension");
        //            break;
        //        }
        //        word.MoveNext();
        //        Expression? exp = Expression.ParseCreate(word, nameSpace);
        //        if(exp != null) val.Dimensions.Add(exp);
        //        if (word.GetCharAt(0) != ']')
        //        {
        //            word.AddError("illegal dimension");
        //            break;
        //        }
        //        word.MoveNext();
        //    }

        //    // parse ranges
        //    if (word.GetCharAt(0) == '[')
        //    {
        //        if (!parseRange(word, nameSpace, val)) return null;
        //    }
        //    else
        //    {   // w/o range
        //        if (dataObject is DataObjects.Variables.IntegerVectorValueVariable)
        //        {
        //            var original = dataObject as DataObjects.Variables.IntegerVectorValueVariable;
        //            if (original == null) throw new Exception();
        //            if (original.Range != null) val.BitWidth = original.Range.Size;
        //            else val.BitWidth = 1;
        //        }
        //        else if(dataObject is DataObjects.Constants.Parameter)
        //        {
        //            var constants = (DataObjects.Constants.Parameter)dataObject;
        //            if(constants.Expression != null)
        //            {
        //                val.Value = constants.Expression.Value;
        //                val.BitWidth = constants.Expression.BitWidth;
        //            }
        //        }
        //        else if (dataObject is Net)
        //        {
        //            if (((Net)dataObject).Range != null) val.BitWidth = ((Net)dataObject).Range.Size;
        //            else val.BitWidth = 1;
        //        }
        //        else if (dataObject is DataObjects.Variables.Genvar)
        //        {
        //            val.Constant = true;
        //        }
        //    }


        //    return val;
        //}

        //private static DataObjects.DataObject? getDataObject(WordScanner word, string identifier, INamedElement owner)
        //{
        //    if (owner.NamedElements.ContainsKey(identifier))
        //    {
        //        return owner.NamedElements.GetDataObject(identifier);
        //    }

        //    if (owner is Function)
        //    {
        //        Function function = (Function)owner;
        //        if (function.Parent != null)
        //        {
        //            DataObjects.DataObject? val = function.Parent.NamedElements.GetDataObject(identifier);
        //            if(function.BuildingBlock is BuildingBlocks.Interface || function.BuildingBlock is BuildingBlocks.Program)
        //            {

        //            }
        //            else
        //            {
        //                if (val != null && !(val is DataObjects.Constants.Constants)) word.AddWarning("external function reference");
        //            }
        //            return val;
        //        }
        //    }else if(owner is IDataObject)
        //    {
        //        //IDataObject dataObject = (IDataObject)owner;


        //    }else if(owner is NameSpace)
        //    {
        //        NameSpace nameSpace = (NameSpace)owner;
        //        if (nameSpace.Parent != null)
        //        {
        //            return nameSpace.Parent.NamedElements.GetDataObject(identifier);
        //        }
        //        else
        //        {
        //            return nameSpace.NamedElements.GetDataObject(identifier);
        //        }
        //    }
        //    return null;
        //}
        private static bool parseRange(WordScanner word, NameSpace nameSpace, VariableReference val)
        {
            if (word.Text != "[") throw new Exception();
            word.MoveNext();

            Expression? exp1 = Expression.ParseCreate(word, nameSpace);
            Expression? exp2;
            switch (word.Text)
            {
                case ":":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new AbsoluteRangeExpression(exp1, exp2);
                    break;
                case "+:":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new RelativePlusRangeExpression(exp1, exp2);
                    break;
                case "-:":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new RelativeMinusRangeExpression(exp1, exp2);
                    break;
                case "]":
                    word.MoveNext();
                    val.RangeExpression = new SingleBitRangeExpression(exp1);
                    break;
                default:
                    word.AddError("illegal range/dimension");
                    return false;
            }
            val.BitWidth = val.RangeExpression.BitWidth;
            return true;
        }

    }
}
