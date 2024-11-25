﻿using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            label.AppendText(NameSpaceText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));

            if (Variable is DataObjects.Variables.Reg)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));
            }
            else if (Variable is Net)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Net));
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

        private static DataObjects.DataObject? getDataObject(WordScanner word, string identifier, NameSpace nameSpace)
        {
            if (nameSpace.NamedElements.ContainsKey(identifier))
            {
                return nameSpace.NamedElements.GetDataObject(identifier);
            }

            if (nameSpace is Function)
            {
                if (nameSpace.Parent != null)
                {
                    DataObjects.DataObject? val = nameSpace.Parent.NamedElements.GetDataObject(identifier);
                    if(nameSpace.BuildingBlock is BuildingBlocks.Interface || nameSpace.BuildingBlock is BuildingBlocks.Program)
                    {

                    }
                    else
                    {
                        if (val != null && !(val is DataObjects.Constants.Constants)) word.AddWarning("external function reference");
                    }
                    return val;
                }
            }
            else
            {
                if (nameSpace.Parent != null)
                {
                    return nameSpace.Parent.NamedElements.GetDataObject(identifier);
                }
                else
                {
                    return nameSpace.NamedElements.GetDataObject(identifier);
                }
            }
            return null;
        }

        public new static VariableReference ParseCreate(WordScanner word, NameSpace nameSpace)
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
            val.Reference = variable.DefinedReference;

            return val;
        }

        /// <summary>
        /// parse local variable references.
        /// hierarchy reference will parsed on upper module
        /// </summary>
        /// <param name="word"></param>
        /// <param name="nameSpace"></param>
        /// <param name="assigned"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static VariableReference? ParseCreate(WordScanner word, NameSpace nameSpace, bool assigned)
        {
            if (nameSpace == null) System.Diagnostics.Debugger.Break();
            DataObjects.DataObject? dataObject = getDataObject(word, word.Text, nameSpace);
            if (dataObject == null) return null;

            VariableReference val = new VariableReference() {
                VariableName = dataObject.Name
            };
            val.Variable = dataObject;
            val.Reference = word.GetReference();

            // color
            if (dataObject is DataObjects.Variables.Reg)
            {
                word.Color(CodeDrawStyle.ColorType.Register);
            }
            else if (dataObject is Net)
            {
                word.Color(CodeDrawStyle.ColorType.Net);
            }
            else if(dataObject is DataObjects.Constants.Constants)
            {
                word.Color(CodeDrawStyle.ColorType.Parameter);
                DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)dataObject;
                val.Constant = true;
            }
            else
            {
                word.Color(CodeDrawStyle.ColorType.Variable);
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
                if(exp != null) val.Dimensions.Add(exp);
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
                            return null;
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
                            return null;
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
                            return null;
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
                        return null;
                }
                val.BitWidth = val.RangeExpression.BitWidth;
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
                else if(dataObject is DataObjects.Constants.Parameter)
                {
                    var constants = (DataObjects.Constants.Parameter)dataObject;
                    if(constants.Expression != null)
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

    }
}
