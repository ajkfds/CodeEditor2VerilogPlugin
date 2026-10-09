
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    /// <summary>
    /// IEEE 1800-2017 section 7.12 array manipulation methods shared by
    /// fixed-size / dynamic / associative arrays and queues.
    /// Registered with null return values and permissive ports so that the
    /// parser accepts the call syntax (detailed type checking is out of scope).
    /// </summary>
    public static class ArrayMethods
    {
        private static void addMethod(NamedElements namedElements, string name, List<Port>? ports)
        {
            if (namedElements.ContainsKey(name)) return;
            BuiltInMethod builtInMethod = BuiltInMethod.Create(name, null, ports ?? new List<Port>());
            namedElements.Add(builtInMethod.Name, builtInMethod);
        }

        private static Port? makePort(string name, Port.DirectionEnum direction, DataObject dataObject)
        {
            return Port.Create(name, null, direction, dataObject);
        }

        private static DataObject makeIntegerPortObject(string name)
        {
            return DataObjects.Variables.Integer.Create(name, DataTypes.IntegerType.Create(false));
        }

        /// <summary>
        /// void delete() with no arguments (dynamic array).
        /// </summary>
        public static void AddDelete(NamedElements namedElements)
        {
            addMethod(namedElements, "delete", null);
        }

        /// <summary>
        /// void delete([input integer index]) with an optional index argument (queue / associative array).
        /// </summary>
        public static void AddDeleteWithOptionalIndex(NamedElements namedElements)
        {
            List<Port> ports = new List<Port>();
            Port? port = makePort("index", Port.DirectionEnum.Input, makeIntegerPortObject("index"));
            if (port != null) ports.Add(port);
            addMethod(namedElements, "delete", ports);
        }

        /// <summary>
        /// void insert(input integer index, input element_t item) (queue).
        /// </summary>
        public static void AddInsert(NamedElements namedElements, DataObject dataObject)
        {
            List<Port> ports = new List<Port>();
            Port? port = makePort("index", Port.DirectionEnum.Input, makeIntegerPortObject("index"));
            if (port != null) ports.Add(port);
            port = makePort("item", Port.DirectionEnum.Input, dataObject);
            if (port != null) ports.Add(port);
            addMethod(namedElements, "insert", ports);
        }

        /// <summary>
        /// element_t pop_front() / element_t pop_back() (queue).
        /// </summary>
        public static void AddPop(NamedElements namedElements, string name)
        {
            List<Port> ports = new List<Port>();
            Variable returnVal = DataObjects.Variables.Int.Create(name, DataTypes.IntType.Create(false));
            if (namedElements.ContainsKey(name)) return;
            BuiltInMethod builtInMethod = BuiltInMethod.Create(name, returnVal, ports);
            namedElements.Add(builtInMethod.Name, builtInMethod);
        }

        /// <summary>
        /// void push_front(input element_t item) / void push_back(input element_t item) (queue).
        /// </summary>
        public static void AddPush(NamedElements namedElements, string name, DataObject dataObject)
        {
            List<Port> ports = new List<Port>();
            Port? port = makePort("item", Port.DirectionEnum.Input, dataObject);
            if (port != null) ports.Add(port);
            addMethod(namedElements, name, ports);
        }

        /// <summary>
        /// int size() / int num() with an int return value.
        /// </summary>
        public static void AddSizeOrNum(NamedElements namedElements, string name)
        {
            if (namedElements.ContainsKey(name)) return;
            List<Port> ports = new List<Port>();
            Variable returnVal = DataObjects.Variables.Int.Create(name, DataTypes.IntType.Create(false));
            BuiltInMethod builtInMethod = BuiltInMethod.Create(name, returnVal, ports);
            namedElements.Add(builtInMethod.Name, builtInMethod);
        }

        /// <summary>
        /// Appends the array manipulation methods of section 7.12.
        /// The element-typed methods accept an optional "with" clause which is
        /// handled by BuiltinMethodCall.ParseCreate.
        /// </summary>
        public static void AppendArrayLocatorMethods(NamedElements namedElements, DataObject dataObject)
        {
            // element-returning locator / reduction methods: find(expr), min(), max(), sum(), ...
            addMethod(namedElements, "find", null);
            addMethod(namedElements, "find_first", null);
            addMethod(namedElements, "find_last", null);
            addMethod(namedElements, "find_index", null);
            addMethod(namedElements, "find_first_index", null);
            addMethod(namedElements, "find_last_index", null);
            addMethod(namedElements, "unique", null);
            addMethod(namedElements, "unique_index", null);
            addMethod(namedElements, "min", null);
            addMethod(namedElements, "max", null);
            addMethod(namedElements, "sum", null);
            addMethod(namedElements, "product", null);
            addMethod(namedElements, "or", null);
            addMethod(namedElements, "and", null);
            addMethod(namedElements, "xor", null);

            // reordering methods: void
            addMethod(namedElements, "reverse", null);
            addMethod(namedElements, "sort", null);
            addMethod(namedElements, "rsort", null);
            addMethod(namedElements, "shuffle", null);
        }
    }
}
