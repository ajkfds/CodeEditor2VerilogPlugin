using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public class ContinuousAssign
    {
        protected ContinuousAssign() { }
        public DriveStrength? DriveStrength;
        public Delay3? Delay3;

        public DataObjects.VariableAssignment VariableAssignment { get; protected set; }

        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            List<ModuleItems.ContinuousAssign> continuousAssigns = ModuleItems.ContinuousAssign.ParseCreate(word, nameSpace);

            
            return await System.Threading.Tasks.Task.FromResult(true);
        }

        public static List<ContinuousAssign> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // continuous_assign::= assign[drive_strength][delay3] list_of_net_assignments;
            // list_of_net_assignments::= net_assignment { , net_assignment }
            // net_assignment::= net_lvalue = expression
            if(word.Text != "assign")
            {
                System.Diagnostics.Debugger.Break();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            List<ContinuousAssign> continuousAssigns = new List<ContinuousAssign>();

            DriveStrength? driveStrength = DriveStrength.ParseCreate(word, nameSpace);
            Delay3 delay3 = Delay3.ParseCreate(word, nameSpace);


            while (!word.Eof)
            {
                ContinuousAssign continuousAssign = new ContinuousAssign();
                continuousAssign.DriveStrength = driveStrength;
                continuousAssign.Delay3 = delay3;

                DataObjects.VariableAssignment? assignment = DataObjects.VariableAssignment.ParseCreate(
                    word,
                    nameSpace,
                    true    // should accept implicit net declaration
                    );
                if (assignment != null)
                {
                    continuousAssign.VariableAssignment = assignment;
                }
                else
                {
                    word.AddError("illegal assignment");
                }
                continuousAssigns.Add(continuousAssign);

                if(word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }else if(word.Text == ",")
                {
                    word.MoveNext();
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
            return continuousAssigns;
        }
    }
}
