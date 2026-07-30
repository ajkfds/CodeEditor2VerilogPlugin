using System.Collections.Generic;
using System.Linq;

namespace pluginVerilog.Verilog.DataObjects
{
    public class SyncContext
    {
        public List<string> Data = new List<string>();
        private bool isClock = false;
        private bool isReset = false;


        public void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (isClock)
            {
                label.AppendText("@clock\n\n", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            }
            if (isReset)
            {
                label.AppendText("@reset\n\n", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            }
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

            //if (SameSyncTargets.Count != 0)
            //{
            //    label.AppendText("@samesync ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            //    bool firstSame = true;
            //    foreach (var same in SameSyncTargets)
            //    {
            //        if (!firstSame) label.AppendText(",");
            //        if (same != null) label.AppendText(same, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            //        firstSame = false;
            //    }
            //    label.AppendText("\r\n");
            //}
        }

        public void AssignToClock()
        {
            isClock = true;
        }
        public void AssignToReset()
        {
            isReset = true;
        }
        public bool IsClock
        {
            get
            {
                return isClock;
            }
        }

        public bool IsReset
        {
            get
            {
                return isReset;
            }
        }
        public void AddClockDomain(string domainName, WordReference? alartWordRef, Dictionary<string, List<string>> SameSync)
        {
            if (Data.Count == 0)
            { // assign new context
                Data.Add(domainName);
            }
            else
            {
                bool matched = true;
                if (SameSync.Count == 0)
                {

                    if (!Data.Contains(domainName))
                    {
                        matched = false;
                    }
                }
                else
                {
                    List<string> acceptableSync = Data.ToList();
                    foreach (var syncCopyFrom in SameSync)
                    {
                        if (!acceptableSync.Contains(syncCopyFrom.Key)) continue;

                        foreach (var syncCopyTo in syncCopyFrom.Value)
                        {
                            if (!acceptableSync.Contains(syncCopyTo))
                            {
                                acceptableSync.Add(syncCopyTo);
                            }
                        }
                    }

                    if (!acceptableSync.Contains(domainName))
                    {
                        matched = false;
                    }
                }
                if (!matched && alartWordRef != null) alartWordRef.AddWarning("sync mismatch " + domainName + " assigned");
            }
        }
        public void PropageteClockDomainFrom(SyncContext syncContext, WordReference? alartWordRef, Dictionary<string, List<string>> SameSync)
        {
            if (Data.Count == 0)
            { // assign new context
                foreach (var syncFrom in syncContext.Data)
                {
                    Data.Add(syncFrom);
                }
            }
            else
            {
                bool matched = true;

                if(SameSync.Count == 0)
                {
                    foreach (var syncFrom in syncContext.Data)
                    {
                        if (!Data.Contains(syncFrom))
                        {
                            matched = false;
                        }
                    }
                }
                else
                {
                    List<string> acceptableSync = Data.ToList();
                    foreach(var syncCopyFrom in SameSync)
                    {
                        if (!acceptableSync.Contains(syncCopyFrom.Key)) continue;

                        foreach(var syncCopyTo in syncCopyFrom.Value)
                        {
                            if (!acceptableSync.Contains(syncCopyTo))
                            {
                                acceptableSync.Add(syncCopyTo);
                            }
                        }
                    }

                    foreach (var syncFrom in syncContext.Data)
                    {
                        if (!acceptableSync.Contains(syncFrom))
                        {
                            matched = false;
                        }
                    }
                }
                if (!matched && alartWordRef != null) alartWordRef.AddWarning("sync mismatch");

            }
        }



    }
}
