
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ContinuousAssign : IDocumentRegeion
    {
        protected ContinuousAssign() { }
        public DriveStrength? DriveStrength;
        public Delay3? Delay3;

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

        public DataObjects.VariableAssignment? VariableAssignment { get; protected set; }

        public static bool Parse(WordScanner word, NameSpace nameSpace, CompletionContext? completionContext = null)
        {
            List<Items.ContinuousAssign> continuousAssigns = Items.ContinuousAssign.ParseCreate(word, nameSpace, completionContext);


            return true;
        }

        /// <summary>
        /// Reports an error when a data object is the target of more than one
        /// continuous assignment, or of both a continuous and a procedural
        /// assignment (IEEE 1800-2017 6.5). A variable may be driven
        /// continuously by at most one assignment, and continuous assignments
        /// shall not be mixed with procedural assignments to the same variable.
        /// </summary>
        internal static void checkContinuousAssignmentConflict(WordScanner word, Expressions.Expression netLValue)
        {
            checkContinuousAssignmentConflict(word, netLValue, null);
        }

        /// <summary>
        /// Same check as <see cref="checkContinuousAssignmentConflict(WordScanner, Expressions.Expression)"/>
        /// but with a caller-supplied message for the "already driven" case.
        ///
        /// <para>
        /// A module instance's OUTPUT port drives the connected signal exactly
        /// like a continuous assignment does, so two instances connecting their
        /// output ports to the same net (or an output port plus an
        /// <c>assign</c> / procedural assignment on the same net) is a multiple
        /// drive and shall be reported (IEEE 1800-2017 6.5 / 23.3.3). The
        /// caller passes a message that names the actual source ("driven by
        /// multiple module instance output ports") instead of the generic
        /// continuous-assignment wording.
        /// </para>
        /// </summary>
        internal static void checkContinuousAssignmentConflict(
            WordScanner word, Expressions.Expression netLValue, string? multipleDriveMessage)
        {
            DataObjects.DataObject? dataObject = getAssignedDataObject(netLValue);
            if (dataObject == null) return;

            if (dataObject.ProceduralAssigned)
            {
                netLValue.Reference.AddError(
                    "'" + dataObject.Name + "' is assigned by both a continuous and a procedural assignment");
                dataObject.ContinuousAssigned = true;
                markContinuousAssignedRange(netLValue, dataObject);
                return;
            }

            if (isContinuousAssignmentOverlapped(netLValue, dataObject))
            {
                if (!IsWiredLogicNet(dataObject))
                {
                    netLValue.Reference.AddError(
                        multipleDriveMessage ?? ("'" + dataObject.Name + "' has multiple continuous assignments"));
                }
            }
            dataObject.ContinuousAssigned = true;
        }

        /// <summary>
        /// True for the wired-logic net types: <c>wor</c> / <c>wand</c> /
        /// <c>trior</c> / <c>triand</c>.
        ///
        /// <para>
        /// These nets resolve multiple drivers by their wired function (OR /
        /// AND), so driving one from several continuous assignments is exactly
        /// what the net type is for (IEEE 1364-2005 4.4.2, IEEE 1800-2017
        /// 6.6.7). Reporting a multiple drive there is wrong; only the
        /// single-driver net types (<c>wire</c>, <c>tri</c>, ...) are restricted
        /// to one continuous assignment (6.5).
        /// </para>
        ///
        /// <para>
        /// <c>supply0</c> / <c>supply1</c> are excluded: they are constant
        /// drivers, not wired logic.
        /// </para>
        /// </summary>
        private static bool IsWiredLogicNet(DataObjects.DataObject dataObject)
        {
            if (dataObject is not DataObjects.Nets.Net net) return false;
            switch (net.NetType)
            {
                case DataObjects.Nets.Net.NetTypeEnum.Wor:
                case DataObjects.Nets.Net.NetTypeEnum.Wand:
                case DataObjects.Nets.Net.NetTypeEnum.Trior:
                case DataObjects.Nets.Net.NetTypeEnum.Triand:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 連続代入の lvalue がドライブするビット範囲を「連続代入済み範囲」に
        /// 登録し、既にドライブ済みのビットと重なっていた場合に true を返す。
        /// 同じビットを複数の連続代入でドライブするのは違反だが、bit-select /
        /// part-select が互いに素な複数の連続代入
        /// ("assign a[0] = ...; assign a[1] = ...;") は合法であるため、
        /// DataObject 単位ではなくビット範囲単位で重複を判定する
        /// (IEEE 1800-2017 6.5)。
        /// 範囲が定数に解決できない場合や AssignedMap を持たない DataObject では
        /// 従来どおり「2 回目以降の連続代入は違反」として扱う。
        /// </summary>
        private static bool isContinuousAssignmentOverlapped(
            Expressions.Expression netLValue, DataObjects.DataObject dataObject)
        {
            if (dataObject.AssignedMap == null) return dataObject.ContinuousAssigned;

            if (netLValue is not Expressions.DataObjectReference dataObjectReference) return dataObject.ContinuousAssigned;

            // 連続代入済み範囲マップは、対象 DataObject の AssignedMap と同じ
            // 次元構成で遅延生成する (ArraysBoolMap.Clone は次元 / サイズのみを
            // 引き継いだ空のマップを作る)。
            dataObject.ContinuousAssignedMap ??= dataObject.AssignedMap.Clone();

            if (!dataObject.ContinuousAssignedMap.TryResolveRange(
                    dataObjectReference.RangesFromOriginal, out long start, out long last))
            {
                return dataObject.ContinuousAssigned;
            }
            return dataObject.ContinuousAssignedMap.AssertWithOverlapCheck(start, last);
        }

        /// <summary>
        /// 連続代入されたビット範囲を連続代入済み範囲マップに登録する
        /// (既に違反として報告済みの経路で、後続の判定を重複報告にしないための
        /// 記録用 helper)。
        /// </summary>
        private static void markContinuousAssignedRange(
            Expressions.Expression netLValue, DataObjects.DataObject dataObject)
        {
            if (dataObject.AssignedMap == null) return;
            if (netLValue is not Expressions.DataObjectReference dataObjectReference) return;
            dataObject.ContinuousAssignedMap ??= dataObject.AssignedMap.Clone();
            if (dataObject.ContinuousAssignedMap.TryResolveRange(
                    dataObjectReference.RangesFromOriginal, out long start, out long last))
            {
                dataObject.ContinuousAssignedMap.AssertWithOverlapCheck(start, last);
            }
        }

        internal static DataObjects.DataObject? getAssignedDataObject(Expressions.Expression expression)
        {
            if (expression is Expressions.DataObjectReference dataObjectReference)
            {
                return dataObjectReference.OrigainalDataObject;
            }
            return null;
        }

        /// <summary>
        /// Reports assigning a plain (non assignment-pattern) value to a whole
        /// unpacked array, which is a type mismatch (IEEE 1800-2017 7.4 / 10.9).
        /// </summary>
        /// <remarks>
        /// <para>
        /// An unpacked array may only be assigned as a whole from another array
        /// of the same type, from an assignment pattern (<c>arr = '{a, b};</c>)
        /// or element by element (<c>arr[i] = a;</c>). <c>arr = 8'hFF;</c> is a
        /// type mismatch that previously passed silently, because only the bit
        /// width was compared (the flattened width of the whole array against
        /// the value).
        /// </para>
        /// <para>
        /// The check lives here (not in <c>DataObjectReference</c>) because the
        /// right-hand side must be known: an assignment pattern is the legal way
        /// to assign a whole array and must not be reported. A bit / part select
        /// on the left-hand side is also legal, which is why an lvalue carrying
        /// <c>RangesFromOriginal</c> is skipped.
        /// </para>
        /// </remarks>
        internal static void checkWholeUnpackedArrayAssignment(
            Expressions.Expression lExpression, Expressions.Expression? rExpression)
        {
            if (rExpression == null) return;
            if (rExpression is DataObjects.AssignmentPattern) return;

            if (lExpression is not Expressions.DataObjectReference dataObjectReference) return;

            // an element / slice select ("arr[0] = ...", "arr[1:0] = ...") is legal
            if (dataObjectReference.RangesFromOriginal.Count != 0) return;

            DataObjects.DataObject? dataObject = dataObjectReference.OrigainalDataObject;
            if (dataObject == null) return;
            if (dataObject.UnpackedArrays.Count == 0) return;
            if (dataObject.UnpackedArrays[0].Size == null) return;

            // a whole-array copy from another array of the same shape is legal
            DataObjects.DataObject? sourceObject = getAssignedDataObject(rExpression);
            if (sourceObject != null &&
                sourceObject.UnpackedArrays.Count == dataObject.UnpackedArrays.Count)
            {
                return;
            }

            lExpression.Reference.AddError(
                "'" + dataObject.Name + "' is an unpacked array and cannot be assigned a scalar value; "
                + "assign its elements, use an assignment pattern ('{...}) or assign from an array of the same type");
        }

        /// <summary>
        /// Marks the data object as procedurally assigned and reports it when the
        /// variable is also the target of a continuous assignment, or when a
        /// DIFFERENT procedural block already assigned it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two assignments to the same variable from two different procedural
        /// blocks (e.g. two <c>always</c> blocks) are legal Verilog but almost
        /// always a design error: the two processes drive the same signal
        /// independently, so the value that wins depends on the execution order.
        /// This is reported as a Warning rather than an Error because the
        /// language permits it.
        /// </para>
        /// <para>
        /// Assignments from the SAME block (including several branches of one
        /// if / case inside the block) are not reported - that is the normal way
        /// to describe a register. A task / function body does not create a new
        /// procedural block, so a helper task assigning a variable is also not
        /// reported; only genuinely separate always / initial / final blocks are.
        /// </para>
        /// </remarks>
        internal static void checkProceduralAssignmentConflict(
            WordScanner word, Expressions.Expression lExpression, string? proceduralBlockMessage = null)
        {
            DataObjects.DataObject? dataObject = getAssignedDataObject(lExpression);
            if (dataObject == null) return;

            // a continuous assignment to the same variable shall not be mixed
            // with a procedural assignment (IEEE 1800-2017 6.5)
            if (dataObject.ContinuousAssigned)
            {
                lExpression.Reference.AddError(
                    "'" + dataObject.Name + "' is assigned by both a continuous and a procedural assignment");
            }

            int blockId = word.CurrentProceduralBlockId;
            if (blockId != 0)
            {
                if (dataObject.ProceduralAssignBlockId == 0)
                {
                    dataObject.ProceduralAssignBlockId = blockId;
                    dataObject.ProceduralAssignReference = lExpression.Reference;
                }
                else if (dataObject.ProceduralAssignBlockId != blockId)
                {
                    lExpression.Reference.AddWarning(
                        proceduralBlockMessage ??
                        ("'" + dataObject.Name + "' is assigned from multiple procedural blocks"));
                }
            }

            dataObject.ProceduralAssigned = true;
        }

        public static List<ContinuousAssign> ParseCreate(WordScanner word, NameSpace nameSpace, CompletionContext? completionContext = null)
        {
            // continuous_assign::= assign[drive_strength][delay3] list_of_net_assignments;
            // list_of_net_assignments::= net_assignment { , net_assignment }
            // net_assignment::= net_lvalue = expression
            if (word.Text != "assign")
            {
                System.Diagnostics.Debugger.Break();
            }
            IndexReference beginIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // A9: "assign |" EOF -> LHS DataObject candidates
            if (word.Eof && completionContext != null)
            {
                completionContext.AppendDataObjects();
                return new List<ContinuousAssign>();
            }

            List<ContinuousAssign> continuousAssigns = new List<ContinuousAssign>();

            DriveStrength? driveStrength = DriveStrength.ParseCreate(word, nameSpace);
            Delay3 delay3 = Delay3.ParseCreate(word, nameSpace);


            while (!word.Eof)
            {
                ContinuousAssign continuousAssign = new ContinuousAssign() { BeginIndexReference = beginIndexReference };
                continuousAssign.DriveStrength = driveStrength;
                continuousAssign.Delay3 = delay3;

                DataObjects.VariableAssignment? assignment = DataObjects.VariableAssignment.ParseCreate(
                    word,
                    nameSpace,
                    true,   // should accept implicit net declaration
                    completionContext
                    );
                if (assignment != null)
                {
                    continuousAssign.VariableAssignment = assignment;
                    assignment.NetLValue.AssertAssigned();
                    // It shall be an error to have multiple continuous
                    // assignments, or a mixture of continuous and procedural
                    // assignments, to the same variable (IEEE 1800-2017 6.5).
                    if (!word.Prototype && word.CompletionContext == null)
                    {
                        checkContinuousAssignmentConflict(word, assignment.NetLValue);
                        checkWholeUnpackedArrayAssignment(assignment.NetLValue, assignment.Expression);
                    }
                }
                else
                {
                    word.AddError("illegal assignment");
                }
                continuousAssigns.Add(continuousAssign);

                if (word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }
                else if (word.Text == ",")
                {
                    word.MoveNext();
                    // A9: "assign a = b, |" EOF -> next LHS DataObject candidates
                    if (word.Eof && completionContext != null)
                    {
                        completionContext.AppendDataObjects();
                        return continuousAssigns;
                    }
                    continue;
                }
                word.AddError("; expected");
                word.SkipToKeyword(";");
                word.MoveNext();
                break;
            }


            if (word.GetCharAt(0) == ';')
            {
                word.MoveNext();
            }
            else
            {
            }

            // register the region of the whole "assign ... ;" statement
            IndexReference lastIndexReference = word.CreateIndexReferenceBefore();
            foreach (ContinuousAssign continuousAssign in continuousAssigns)
            {
                continuousAssign.LastIndexReference = lastIndexReference;
            }
            if (!word.Prototype && word.CompletionContext == null && continuousAssigns.Count != 0)
            {
                nameSpace.DocumentRegions.Add(continuousAssigns[0]);
            }

            return continuousAssigns;
        }
    }
}
