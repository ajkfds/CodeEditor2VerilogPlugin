using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static pluginVerilog.Verilog.ModPort;

namespace pluginVerilog.Verilog.DataObjects
{
    public class SyncContext
    {
        public List<string> Data = new List<string>();

        public void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (Data.Count == 0) return;

            label.AppendText("@sync ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            bool first = true;
            foreach (var sync in Data)
            {
                if (!first) label.AppendText(",");
                if (sync != null) label.AppendText(sync, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
                first = false;
            }
            label.AppendText("\r\n");
        }

        public void AssignToClock()
        {
            if(!Data.Contains("clock")) Data.Add("clock");
        }
        public void AssignToReset()
        {
            if(!Data.Contains("reset")) Data.Add("reset");
        }
        public void AddClockDomain(string domainName)
        {

        }

        public void PropageteClockDomainFrom(SyncContext syncContext,WordReference alartWordRef)
        {
            if (Data.Count == 0)
            { // assign new context
                foreach(var sync in syncContext.Data)
                {
                    Data.Add(sync);
                }
            }
            else
            {
                bool matched = true;
                if (Data.Count == syncContext.Data.Count) matched = false;
                foreach(var sync in syncContext.Data)
                {
                    if(!Data.Contains(sync)) matched = false;
                }
                if(!matched) alartWordRef.AddWarning("sync mismatch");
            }
        }

    }
}
