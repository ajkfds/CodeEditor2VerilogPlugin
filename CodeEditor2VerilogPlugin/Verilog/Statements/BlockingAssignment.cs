using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Statements
{
    public class BlockingAssignment : IStatement, Items.IDocumentRegeion
    {
        protected BlockingAssignment() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
            Expression.DisposeSubReference(true);
        }
        public Expressions.Expression? LValue { get; protected set; }
        public Expressions.Expression? Expression { get; protected set; }
        /* IEEE1800-2017
        blocking_assignment ::=   variable_lvalue = delay_or_event_control expression
                                | nonrange_variable_lvalue = dynamic_array_new
                                | [ implicit_class_handle . | class_scope | package_scope ] hierarchical_variable_identifier select = class_new
                                | operator_assignment         
         */
        /*
        A.6.2 Procedural blocks and assignments
        initial_construct   ::= initial statement
        always_construct    ::= always statement
        blocking_assignment ::= variable_lvalue = [ delay_or_event_control ] expression
        nonblocking_assignment ::= variable_lvalue <= [ delay_or_event_control ] expression
        procedural_continuous_assignments   ::= assign variable_assignment
                                                | deassign variable_lvalue
                                                | force variable_assignment
                                                | force net_assignment
                                                | release variable_lvalue
                                                | release net_lvalue
         */

        public delegate void BlockingAssignedAction(WordScanner word, NameSpace nameSpace, BlockingAssignment blockingAssignment);
        public static BlockingAssignedAction? Assigned;

        public static BlockingAssignment? ParseCreate(
        WordScanner word, NameSpace nameSpace
        )
        {
            // EOF at LHS position (e.g. "x|"): suggest DataObject / function candidates
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            IndexReference expressionIref = word.CreateIndexReference();
            Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);
            if (expression == null) return null;
            switch (word.Text)
            {
                case "=":
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "%=":
                case "&=":
                case "|=":
                case "^=":
                case "<<=":
                case ">>=":
                case "<<<=":
                case ">>>=":
                    break;
                default:
                    return null;
            }
            return ParseCreateAfterAssignmentOperator(word, nameSpace, expression, expressionIref);
        }
        public static BlockingAssignment? ParseCreateAfterAssignmentOperator(
            WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression,
            IndexReference expressionIref
            )
        {
            switch (word.Text)
            {
                case "=":
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "%=":
                case "&=":
                case "|=":
                case "^=":
                case "<<=":
                case ">>=":
                case "<<<=":
                case ">>>=":
                    break;
                default:
                    throw new Exception();
            }
            WordReference equalPointer = word.CrateWordReference();
            string assignmentOperator = word.Text;

            word.MoveNext();    // <=

            // delay_or_event_control ::= delay_control | event_control
            //                         | repeat ( expression ) event_control
            ParseDelayOrEventControl(word, nameSpace);

            if (word.Text == "new")
            {
                return parseCreateClassNewAssignment(word, nameSpace, lExpression, expressionIref);
            }

            // EOF just after "=" (e.g. "x = |"): suggest expression candidates for RHS
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            // delay or event control

            Expressions.Expression? expression;

            if (word.Text == "'" && word.NextText == "{")
            {
                // use the DataObjects.AssignmentPattern implementation (nested
                // repetition / keyed / comma list); Expressions.AssignmentPattern
                // is a legacy stub that only handled the first element and is the
                // cause of "'; expected'" on nested patterns (e.g. '{2{'{3{...}}}).
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference=expressionIref};
                assignment.LValue = lExpression;
                assignment.Expression = DataObjects.AssignmentPattern.ParseCreate(word, nameSpace, false, false);
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                assignment.LastIndexReference = word.CreateIndexReferenceBefore();
                if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(assignment);
                return assignment;
            }
            else
            {
                IndexReference expIref = word.CreateIndexReference();
                expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    // classname :: new ();
                    BlockingAssignment? assignment = parseCreateClassNewAssignment(word, nameSpace, lExpression, expIref);
                    if (assignment != null)
                    {
                        assignment.LastIndexReference = word.CreateIndexReferenceBefore();
                        if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(assignment);
                        return assignment;
                    }

                    word.AddError("illegal expression");
                    word.SkipToKeyword(";");
                    word.AddError("illegal non blocking assignment");
                    return null;
                }
            }


            if (!word.Prototype)
            {
                if (
                    lExpression != null &&
                    lExpression.BitWidth != null &&
                    expression.BitWidth != null &&
                    lExpression.BitWidth != expression.BitWidth &&
                    expression is not Expressions.ConstantString
                    )
                {
                    WordReference wRef = WordReference.CreateReferenceRange(
                        lExpression.Reference,
                        expression.Reference
                        );
                    wRef.AddWarning("bit width mismatch " + lExpression.BitWidth + " <- " + expression.BitWidth);
                }
            }

            if (!word.Prototype && lExpression != null && expression != null)
            {
                lExpression.SyncContext.PropageteClockDomainFrom(expression.SyncContext, equalPointer, nameSpace.BuildingBlock.SameSync);
            }

            if (!word.Prototype && word.CompletionContext == null && lExpression != null && expression != null)
            {
                checkEnumAssignment(equalPointer, assignmentOperator, lExpression, expression);
            }

            // a procedural assignment shall not be mixed with a continuous
            // assignment to the same variable (IEEE 1800-2017 6.5)
            if (!word.Prototype && word.CompletionContext == null && lExpression != null)
            {
                markProceduralAssignment(lExpression);
                // it shall be illegal to procedurally assign to a net
                // (IEEE 1800-2017 10.3 / Table 10-1)
                if (assignmentOperator == "=")
                {
                    checkProceduralNetAssignment(word, lExpression);
                }
            }

            if (lExpression != null)
            {
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference=expressionIref};
                assignment.LValue = lExpression;
                assignment.Expression = expression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);

                assignment.LastIndexReference = word.CreateIndexReferenceBefore();
                if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(assignment);
                return assignment;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Enum variables enforce strict type checking (IEEE 1800-2017 6.19.3):
        ///   - a plain integer literal cannot be assigned to an enum variable
        ///     (an explicit cast is required), and
        ///   - the assignment / arithmetic assignment operators (+=, -=, ...)
        ///     are not applicable to enum operands. The ++ / -- operators are
        ///     legal (they walk the enum items).
        /// </summary>
        private static void checkEnumAssignment(
            WordReference equalPointer, string assignmentOperator,
            Expressions.Expression lExpression, Expressions.Expression expression)
        {
            DataObject? lValueObject = getAssignedEnumVariable(lExpression);
            if (lValueObject == null) return;

            if (assignmentOperator != "=")
            {
                equalPointer.AddError("assignment operator is not applicable to an enum variable");
                return;
            }

            // "val = 1;" : an integer literal requires an explicit cast to the
            // enum type (val = e'(1);)
            if (expression is Expressions.Number number && number.Constant && !(expression is Expressions.Cast))
            {
                equalPointer.AddError("an integer literal cannot be assigned to an enum variable without a cast");
            }
        }

        /// <summary>
        /// A net shall not be procedurally assigned from a procedural context
        /// (always / initial / task / function); only a "net" variable declared
        /// with a variable type may be assigned procedurally (IEEE 1800-2017
        /// 10.3 / Table 10-1).
        /// </summary>
        private static void checkProceduralNetAssignment(WordScanner word, Expressions.Expression lExpression)
        {
            // The lvalue must resolve to a DataObject that is a net. Non-ANSI
            // ports declared as "output q;" plus a matching "logic q;" are
            // re-registered as a variable (the port's DataObject is replaced by
            // the variable declaration), so the resolved element here is the
            // current declaration; a plain "wire w;" stays a Net.
            DataObject? dataObject = Items.ContinuousAssign.getAssignedDataObject(lExpression);
            if (dataObject is not DataObjects.Nets.Net) return;
            lExpression.Reference.AddError("it shall be illegal to procedurally assign to a net");
        }

        /// <summary>
        /// Marks the assigned data object as procedurally assigned and reports
        /// the conflict when it is also the target of a continuous assignment
        /// (IEEE 1800-2017 6.5).
        /// </summary>
        private static void markProceduralAssignment(Expressions.Expression lExpression)
        {
            DataObject? dataObject = Items.ContinuousAssign.getAssignedDataObject(lExpression);
            if (dataObject == null) return;
            if (dataObject.ContinuousAssigned)
            {
                lExpression.Reference.AddError(
                    "'" + dataObject.Name + "' is assigned by both a continuous and a procedural assignment");
            }
            dataObject.ProceduralAssigned = true;
        }

        /// <summary>
        /// Returns the referenced DataObject when the lvalue is (a select of) an
        /// enum variable, otherwise null.
        /// </summary>
        private static DataObject? getAssignedEnumVariable(Expressions.Expression lExpression)
        {
            if (lExpression is not Expressions.DataObjectReference dataObjectReference) return null;
            DataObject? dataObject = dataObjectReference.OrigainalDataObject;
            if (dataObject == null) return null;

            DataObjects.DataTypes.IDataType? dataType = dataObject.DataType;
            if (dataType is DataObjects.DataTypes.UserDefinedType userDefinedType)
            {
                dataType = userDefinedType.OriginalDataType;
            }
            if (dataType is DataObjects.DataTypes.Enum) return dataObject;
            return null;
        }

        /// <summary>
        /// Parse the optional delay_or_event_control on the RHS of an assignment
        /// (IEEE 1800-2017 A.6.5):
        ///   delay_or_event_control ::= delay_control
        ///                            | event_control
        ///                            | repeat ( expression ) event_control
        /// </summary>
        internal static void ParseDelayOrEventControl(WordScanner word, NameSpace nameSpace)
        {
            if (word.GetCharAt(0) == '#')
            {
                DelayControl? delayControl = DelayControl.ParseCreate(word, nameSpace);
            }
            else if (word.GetCharAt(0) == '@')
            {
                EventControl? eventControl = EventControl.ParseCreate(word, nameSpace);
            }
            else if (word.Text == "repeat" && word.NextText == "(")
            {
                // repeat ( expression ) event_control
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // repeat
                word.MoveNext(); // (
                Expressions.Expression? repeatCount = Expressions.Expression.ParseCreate(word, nameSpace);
                if (repeatCount == null)
                {
                    word.AddError("illegal repeat control");
                    return;
                }
                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return;
                }
                word.MoveNext(); // )
                // event_control (@ ...) is required after repeat(expression)
                if (word.GetCharAt(0) == '@')
                {
                    EventControl? eventControl = EventControl.ParseCreate(word, nameSpace);
                }
            }
        }

        public static BlockingAssignment? parseCreateClassNewAssignment(WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression,IndexReference expIref)
        {
            // class_new ::= [ class_scope ] "new" [ ( list_of_arguments ) ] | "new" expression
            // dynamic_array_new ::= "new" [ expression ] [ ( expression ) ]

            // Check if this is a dynamic array allocation (new followed by [size])
            bool isDynamicArrayNew = word.Text == "new" && word.NextText == "[";

            // If not dynamic array new, try to resolve class reference
            BuildingBlocks.Class? class_ = null;
            if (!isDynamicArrayNew)
            {
                class_ = word.ProjectProperty.UnitNameSpace.Get(word.Text) as BuildingBlocks.Class;
                if (class_ != null)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text != "::")
                    {
                        word.AddError(":: required");
                        return null;
                    }
                    word.MoveNext();
                }
                else
                {
                    if (lExpression is Expressions.DataObjectReference)
                    {
                        Expressions.DataObjectReference dref = (Expressions.DataObjectReference)lExpression;
                        if (dref.TargetDataObject is DataObjects.Variables.Object)
                        {
                            DataObjects.Variables.Object? obj = (DataObjects.Variables.Object)dref.TargetDataObject;
                            class_ = obj.GetSourceClass();
                        }
                        else if (dref.TargetDataObject is DataObjects.Variables.InterfaceClassVariable)
                        {
                            // An interface class cannot be instantiated (IEEE
                            // 1800-2017 8.26.5); only a concrete class that
                            // implements it may be.
                            if (!word.Prototype && word.CompletionContext == null)
                            {
                                word.AddError("an interface class shall not be instantiated");
                            }
                        }
                    }
                }
            }

            if (word.Text != "new") return null;

            // It shall be illegal to instantiate an abstract class (IEEE
            // 1800-2017 8.21). A class declared "virtual" is abstract and cannot
            // be constructed ("base_cls obj = new;" is an error), while a
            // derived non-virtual class may be.
            if (class_ != null && class_.IsVirtual)
            {
                word.AddError("an abstract class shall not be instantiated");
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Handle dynamic array new: new [expression] [(expression)]
            if (word.Text == "[")
            {
                word.MoveNext(); // [
                Expressions.Expression? sizeExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (sizeExpression == null)
                {
                    word.AddError("size expression expected");
                    return null;
                }
                if (word.Text != "]")
                {
                    word.AddError("] expected");
                    return null;
                }
                word.MoveNext(); // ]

                // Optional constructor arguments
                if (word.Text == "(")
                {
                    word.MoveNext();
                    while (word.Text != ")" && !word.Eof)
                    {
                        Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expression == null) break;
                        if (word.Text != ",") break;
                        word.MoveNext();
                    }
                    if (word.Text != ")")
                    {
                        word.AddError(") expected");
                        return null;
                    }
                    word.MoveNext();
                }

                if (word.Text != ";")
                {
                    word.AddError("; expected");
                    return null;
                }
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference= expIref };
                assignment.LValue = lExpression;
                assignment.Expression = null; // new
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }

            // Handle class new: new [(arguments)] or new expression
            if (word.Text == "(")
            {
                word.MoveNext();
                while (word.Text != ")" && !word.Eof)
                {
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null) break;
                    if (word.Text != ",") break;
                    word.MoveNext();
                }
                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return null;
                }
                word.MoveNext();
            }
            else
            {
                Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);
                if (exp != null)
                {
                    // shallow clone
                }
            }

            if (word.Text != ";")
            {
                word.AddError("; expected");
                return null;
            }
            BlockingAssignment blockingAssignment = new BlockingAssignment() { BeginIndexReference=expIref};
            blockingAssignment.LValue = lExpression;
            blockingAssignment.Expression = null; // new
            if (Assigned != null) Assigned(word, nameSpace, blockingAssignment);
            return blockingAssignment;

        }
    }
}
