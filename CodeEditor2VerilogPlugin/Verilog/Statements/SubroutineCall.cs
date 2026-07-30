using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class SubroutineCall
    {
        /* SystemVerilog IEEE1800-2017
        
            subroutine_call ::=               tf_call
                                            | system_tf_call
                                            | method_call
                                            | [ std :: ] randomize_call

            list_of_arguments ::=             [ expression ] { , [ expression ] } { , . identifier ( [ expression ] ) }
                                            | .identifier ( [ expression ] ) { , . identifier ( [ expression ] ) }
            ps_or_hierarchical_tf_identifier ::=      [ package_scope ] tf_identifier
                                                    | hierarchical_tf_identifier
            hierarchical_tf_identifier ::= hierarchical_identifier
            hierarchical_identifier ::= [ $root . ] { identifier constant_bit_select . } identifier
            tf_identifier ::= identifier


            tf_call ::=                   ps_or_hierarchical_tf_identifier { attribute_instance } [ ( list_of_arguments ) ]
            method_call_body ::=          method_identifier { attribute_instance } [ ( list_of_arguments ) ]


            method_call ::=               method_call_root . method_call_body
                                        | built_in_method_call
            built_in_method_call ::=      array_manipulation_call
                                        | randomize_call
            method_call_root ::=          primary
                                        | implicit_class_handle

            package_scope ::=     package_identifier "::"
                                | "$unit" "::"

            class_scope ::= class_type "::"
            class_type ::=  ps_class_identifier [ parameter_value_assignment ] { "::" class_identifier [ parameter_value_assignment ] }
            ps_class_identifier ::= [ package_scope ] class_identifier

            array_manipulation_call ::= array_method_name { attribute_instance }    [ "(" list_of_arguments ")" ]　[ "with" "(" expression ")" ]


            randomize_call ::=  "randomize" { attribute_instance }
                                [ "(" [ variable_identifier_list | "null" ] ")" ]
                                [ "with" [ "(" [ identifier_list ] ")" ] constraint_block ]

            constraint_block ::= constraint_set_item { constraint_set_item }

            variable_identifier_list ::= variable_identifier { , variable_identifier }
            identifier_list ::= identifier { , identifier }
         */

        /// <summary>
        /// Parse randomize() with constraint expression
        /// IEEE 1800-2017
        /// 
        /// randomize_call ::=  "randomize" { attribute_instance }
        ///                     [ "(" [ variable_identifier_list | "null" ] ")" ]
        ///                     [ "with" [ "(" [ identifier_list ] ")" ] constraint_block ]
        /// </summary>
        public static async Task<RandomizeCall?> ParseCreateRandomizeCall(WordScanner word, NameSpace nameSpace)
        {
            /*
            randomize_call ::=  "randomize" { attribute_instance }
                                [ "(" [ variable_identifier_list | "null" ] ")" ]
                                [ "with" [ "(" [ identifier_list ] ")" ] constraint_block ]
            */

            if (word.Text != "randomize") return null;

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            RandomizeCall call = new RandomizeCall
            {
                BeginIndexReference = beginReference,
                Project = word.Project
            };

            // Parse optional attribute_instance { attribute_instance }
            while (word.Text == "(*)")
            {
                Attribute.ParseCreate(word, nameSpace);
            }

            // Parse optional port list: ( [ variable_identifier_list | "null" ] )
            if (word.Text == "(")
            {
                word.MoveNext();

                if (word.Text != ")")
                {
                    // Check for "null"
                    if (word.Text == "null")
                    {
                        call.WithNull = true;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        // Parse variable_identifier_list
                        while (!word.Eof && word.Text != ")" && word.Text != ";")
                        {
                            if (General.IsIdentifier(word.Text))
                            {
                                call.VariableIdentifiers.Add(word.Text);
                                word.Color(CodeDrawStyle.ColorType.Variable);
                                word.MoveNext();
                            }
                            else
                            {
                                word.AddError("variable identifier expected");
                                break;
                            }

                            if (word.Text == ",")
                            {
                                word.MoveNext();
                                continue;
                            }
                            else if (word.Text == ")")
                            {
                                break;
                            }
                            else
                            {
                                word.AddError(", or ) expected");
                                break;
                            }
                        }
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError(") expected");
                }
            }

            // Parse optional "with constraint_block"
            if (word.Text == "with")
            {
                call.HasWithConstraint = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Optional ( identifier_list )
                if (word.Text == "(")
                {
                    word.MoveNext();

                    if (word.Text != ")")
                    {
                        // Parse identifier_list for inline constraint scope
                        while (!word.Eof && word.Text != ")" && word.Text != ";")
                        {
                            if (General.IsIdentifier(word.Text))
                            {
                                call.WithIdentifierList.Add(word.Text);
                                word.Color(CodeDrawStyle.ColorType.Variable);
                                word.MoveNext();
                            }
                            else
                            {
                                word.AddError("identifier expected");
                                break;
                            }

                            if (word.Text == ",")
                            {
                                word.MoveNext();
                                continue;
                            }
                            else if (word.Text == ")")
                            {
                                break;
                            }
                            else
                            {
                                word.AddError(", or ) expected");
                                break;
                            }
                        }
                    }

                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(") expected");
                    }
                }

                // Parse constraint_block: constraint_set_item { constraint_set_item }
                call.Constraints = await ParseConstraintBlock(word, nameSpace);
            }

            call.LastIndexReference = word.CreateIndexReference();
            return call;
        }

        /// <summary>
        /// Parse constraint_block
        /// constraint_block ::= constraint_set_item { constraint_set_item }
        /// constraint_set_item ::= [ "disable" "iff" "(" expression ")" ] constraint_expr
        /// </summary>
        private static async Task<List<ConstraintItem>> ParseConstraintBlock(WordScanner word, NameSpace nameSpace)
        {
            List<ConstraintItem> constraints = new List<ConstraintItem>();

            while (!word.Eof && word.Text != ";" && word.Text != ")" && word.Text != "endclass")
            {
                ConstraintItem item = new ConstraintItem();

                // Check for disable iff
                if (word.Text == "disable")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    if (word.Text == "iff")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        if (word.Text == "(")
                        {
                            word.MoveNext();
                            item.DisableIffExpression = Expression.ParseCreate(word, nameSpace);

                            if (word.Text == ")")
                            {
                                word.MoveNext();
                            }
                            else
                            {
                                word.AddError(") expected");
                            }
                        }
                    }
                }

                // Parse constraint expression
                item.ConstraintExpression = Expression.ParseCreate(word, nameSpace);
                if (item.ConstraintExpression != null)
                {
                    constraints.Add(item);
                }

                // Check for semicolon (end of constraint)
                if (word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }
            }

            return constraints;
        }
    }

    /// <summary>
    /// Represents a randomize() call with optional constraint
    /// IEEE 1800-2017
    /// </summary>
    public class RandomizeCall
    {
        public IndexReference? BeginIndexReference { get; set; }
        public IndexReference? LastIndexReference { get; set; }
        public CodeEditor2.Data.Project Project { get; set; }

        /// <summary>
        /// Whether null was specified: randomize(null)
        /// </summary>
        public bool WithNull { get; set; } = false;

        /// <summary>
        /// Variable identifiers passed to randomize()
        /// </summary>
        public List<string> VariableIdentifiers { get; } = new List<string>();

        /// <summary>
        /// Whether with constraint is specified
        /// </summary>
        public bool HasWithConstraint { get; set; } = false;

        /// <summary>
        /// Identifier list for inline constraint scope: randomize() with (identifier_list)
        /// </summary>
        public List<string> WithIdentifierList { get; } = new List<string>();

        /// <summary>
        /// Constraint items from constraint_block
        /// </summary>
        public List<ConstraintItem> Constraints { get; set; } = new List<ConstraintItem>();
    }

    /// <summary>
    /// Represents a single constraint item in a constraint block
    /// </summary>
    public class ConstraintItem
    {
        /// <summary>
        /// Expression for disable iff condition (optional)
        /// </summary>
        public Expression? DisableIffExpression { get; set; }

        /// <summary>
        /// The constraint expression
        /// </summary>
        public Expression? ConstraintExpression { get; set; }
    }
}
