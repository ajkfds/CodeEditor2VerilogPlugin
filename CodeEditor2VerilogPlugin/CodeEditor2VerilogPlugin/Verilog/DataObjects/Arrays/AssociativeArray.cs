using pluginVerilog.Verilog.DataObjects.Variables;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class AssociativeArray : DataObject,IArray
    {
        protected AssociativeArray() { }
        public int? Size { get; protected set; } = null;
        public bool Constant { get; protected set; } = false;
        public bool IsValidForNet { get { return false; } }

        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;

        public static AssociativeArray Create(DataObject dataObject, DataTypes.IDataType? indexDataType)
        {
            AssociativeArray associativeArray = new AssociativeArray() { Name = dataObject.Name };
            associativeArray.IndexDataType = indexDataType;

            DataTypes.IntegerVectorType? integerVectorType = indexDataType as DataTypes.IntegerVectorType;
            if (integerVectorType != null)
            {
                associativeArray.Size = integerVectorType.BitWidth;
                return associativeArray;
            }

            DataTypes.IntegerAtomType? integerAtomType = indexDataType as DataTypes.IntegerAtomType;
            if (integerAtomType != null)
            {
                associativeArray.Size = integerAtomType.BitWidth;
            }

            // function void delete( [input index] );
            // 
            // 

            { // function int size();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("size", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("size", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { // function int num();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("num", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("num", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int first( ref index );
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("first", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("first", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int next( ref index );
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("next", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("next", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int prev( ref index );
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("prev", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("prev", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int last( ref index );
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("last", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("last", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int exists( input index );
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("exists", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("exists", returnVal, ports);
                associativeArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            return associativeArray;
        }
        /*
         * num()/size()、delete()、exists()、first()、last()、next()、prev()
         */


        public DataTypes.IDataType? IndexDataType { get; set; } = null;

        public override DataObject Clone()
        {
            throw new NotImplementedException();
        }

        public override DataObject Clone(string name)
        {
            throw new NotImplementedException();
        }
    }
}
