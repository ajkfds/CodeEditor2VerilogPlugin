using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using DataObjects = pluginVerilog.Verilog.DataObjects;
using DataTypes = pluginVerilog.Verilog.DataObjects.DataTypes;
using Variables = pluginVerilog.Verilog.DataObjects.Variables;

namespace pluginVerilog.Verilog.DataObjects
{
    /// <summary>
    /// Let Declaration
    /// IEEE 1800-2017 SystemVerilog
    /// 
    /// let_declaration ::=
    ///     "let" let_identifier [ ( [ let_port_list ] ) ] = expression ;
    /// 
    /// let_identifier ::= identifier
    /// let_port_list ::= let_port_item { , let_port_item }
    /// let_port_item ::= { attribute_instance } [ const ] let_formal_type formal_port_identifier { variable_dimension } [ = let_actual_arg ]
    /// let_formal_type ::= data_type_or_void
    /// let_actual_arg ::= expression
    /// 
    /// Let declarations allow defining reusable expressions with formal arguments.
    /// </summary>
    public class LetDeclaration : NameSpace,INamedElement
    {
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;

        /// <summary>
        /// Port list for let declaration arguments
        /// </summary>
        public List<LetPortItem> PortList { get; set; } = new List<LetPortItem>();

        /// <summary>
        /// The expression this let declaration defines
        /// </summary>
        public Expression? Expression { get; set; }

        public class LetPortItem
        {
            /// <summary>
            /// Whether this port is const
            /// </summary>
            public bool IsConst { get; set; }

            /// <summary>
            /// Data type of the port
            /// </summary>
            public DataTypes.IDataType? DataType { get; set; }

            /// <summary>
            /// Port identifier
            /// </summary>
            public string Identifier { get; set; } = "";

            /// <summary>
            /// Unpacked dimensions
            /// </summary>
            public List<Expressions.Expression> Dimensions { get; set; } = new List<Expressions.Expression>();

            /// <summary>
            /// Optional default value
            /// </summary>
            public Expression? DefaultValue { get; set; }
        }

        public static LetDeclaration? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "let")
            {
                return null;
            }

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // let


            // let_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("let identifier expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }

            LetDeclaration letDecl = new LetDeclaration
            {
                DefinitionReference = word.GetReference(),
                BeginIndexReference = beginReference,
                Name = word.Text,
                BuildingBlock = nameSpace.BuildingBlock,
                Parent = nameSpace
            };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Optional port list: ( [ let_port_list ] )
            if (word.Text == "(")
            {
                word.MoveNext(); // (

                while (!word.Eof && word.Text != ")")
                {
                    if (word.Text == ")")
                    {
                        break;
                    }

                    LetPortItem portItem = new LetPortItem();

                    // Optional const
                    if (word.Text == "const")
                    {
                        portItem.IsConst = true;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }

                    // Parse data type or void
                    if (word.Text == "void")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        portItem.DataType = DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
                    }

                    // Port identifier
                    if (General.IsIdentifier(word.Text))
                    {
                        portItem.Identifier = word.Text;
                        word.Color(CodeDrawStyle.ColorType.Variable);

                        // Create a data object for this port and add to letDecl's namespace
                        // This allows the expression parser to recognize x, y as valid identifiers
                        // If no data type specified, use default 1-bit logic
                        DataTypes.IDataType dataType;
                        if (portItem.DataType != null)
                        {
                            dataType = portItem.DataType;
                        }
                        else
                        {
                            // Default to 1-bit logic (implicit)
                            dataType = DataTypes.LogicType.Create(false, null)!;
                        }
                        Variables.Variable? portVar = Variables.Variable.Create(
                            portItem.Identifier,
                            dataType
                        );
                        if (portVar != null)
                        {
                            letDecl.NamedElements.Add(portItem.Identifier, portVar);
                        }

                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("port identifier expected");
                        word.SkipToKeyword(";)");
                        if (word.Text == ";") break;
                        if (word.Text == ")") break;
                        word.MoveNext();
                        continue;
                    }

                    // Optional dimensions
                    while (word.Text == "[")
                    {
                        word.MoveNext();
                        Expressions.Expression? dim = Expressions.Expression.ParseCreate(word, letDecl);
                        if (dim != null)
                        {
                            portItem.Dimensions.Add(dim);
                        }
                        if (word.Text == "]")
                        {
                            word.MoveNext();
                        }
                        else
                        {
                            word.AddError("] expected");
                            break;
                        }
                    }

                    // Optional default value: = expression
                    if (word.Text == "=")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        portItem.DefaultValue = Expressions.Expression.ParseCreate(word, letDecl);
                    }

                    letDecl.PortList.Add(portItem);

                    // Comma separator
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                    }
                }

                // Closing paren
                if (word.Text == ")")
                {
                //    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    word.AddError(") expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return null;
                }
            }

            // Expect =
            if (word.Text != "=")
            {
                word.AddError("= expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }
            //word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse expression using letDecl's namespace (which now contains the port variables)
            letDecl.Expression = Expressions.Expression.ParseCreate(word, letDecl);
            if (letDecl.Expression == null)
            {
                word.AddError("expression expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }

            // Semicolon
            if (word.Text != ";")
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            letDecl.LastIndexReference = word.CreateIndexReference();

            return letDecl;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }

        public void DisposeSubReference()
        {
            Expression?.DisposeSubReference(true);
            foreach (var port in PortList)
            {
                port.DefaultValue?.DisposeSubReference(true);
                foreach (var dim in port.Dimensions)
                {
                    dim.DisposeSubReference(true);
                }
            }
        }
    }
}
