using AjkAvaloniaLibs.Controls;
using DynamicData;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ClassType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Class;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public int? BitWidth
        {
            get
            {
                return null;
            }
        }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();



        //public Class? Class
        //{
        //    get
        //    {

        //    }
        //}

        /*
        class_type ::=  ps_class_identifier [ parameter_value_assignment ] 
                        { "::" class_identifier [ parameter_value_assignment ] }

        parameter_value_assignment ::= # ( [ list_of_parameter_assignments ] ) 
        */
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("class", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return ClassType.Create(array);
        }
        public static ClassType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            ClassType classType = new ClassType() { };
            if (packedDimensions == null)
            {
                classType.PackedDimensions.Clear();
            }
            else
            {
                classType.PackedDimensions = packedDimensions;
            }
            return classType;
        }
        public static ClassType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.NextText != ":" && word.NextText != "#") throw new Exception();
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal class name");
                return null;
            }

            ClassType dType = new ClassType();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            return dType;
        }
        public bool IsVector { get { return false; } }


    }
}
