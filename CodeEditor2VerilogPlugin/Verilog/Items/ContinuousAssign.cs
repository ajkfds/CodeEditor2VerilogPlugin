using CodeEditor2.CodeEditor.CodeComplete;
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
