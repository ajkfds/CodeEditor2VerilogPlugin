using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects
{
    public class SyncContext
    {
        public List<string> Data = new List<string>();
        private bool isClock = false;
        private bool isReset = false;

        // Names of other SyncContexts that have been merged with this one via
        // `@samesync A = B`. Stored as net/variable (or port) names so that
        // ModuleInstantiation.SyncCheck can resolve the merged SyncContext
        // when the local SyncContext.Data does not contain a matching port
        // connection.
        //
        // When MergeFrom is invoked, the partner's Data is unioned into this
        // Data and vice-versa, and the partner's Name (if known) is also
        // added to SameSyncTargets so that subsequent SyncCheck passes can
        // find the partner's SyncContext even after a re-parse that may have
        // rebuilt only one side of the pair.
        public List<string> SameSyncTargets = new List<string>();

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

            if (SameSyncTargets.Count != 0)
            {
                label.AppendText("@samesync ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
                bool firstSame = true;
                foreach (var same in SameSyncTargets)
                {
                    if (!firstSame) label.AppendText(",");
                    if (same != null) label.AppendText(same, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
                    firstSame = false;
                }
                label.AppendText("\r\n");
            }
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
        public void AddClockDomain(string domainName, WordReference? alartWordRef)
        {
            if (Data.Count == 0)
            { // assign new context
                Data.Add(domainName);
            }
            else
            {
                bool matched = true;
                if (!Data.Contains(domainName)) matched = false;
                if (!matched && alartWordRef != null) alartWordRef.AddWarning("sync mismatch " + domainName + " assigned");
            }
        }
        public void PropageteClockDomainFrom(SyncContext syncContext, WordReference? alartWordRef)
        {
            if (Data.Count == 0)
            { // assign new context
                foreach (var sync in syncContext.EffectiveSyncTargets())
                {
                    Data.Add(sync);
                }
            }
            else
            {
                bool matched = true;
                foreach (var sync in syncContext.EffectiveSyncTargets())
                {
                    if (!Data.Contains(sync)) matched = false;
                }
                if (!matched && alartWordRef != null) alartWordRef.AddWarning("sync mismatch");
            }
        }

        /// <summary>
        /// Merge another SyncContext into this one (bidirectional when called
        /// on both sides). Both Data lists are unioned and the partner's name
        /// is added to SameSyncTargets so that downstream sync checks can
        /// resolve the merged partner even when the original Data does not
        /// contain a direct match.
        /// </summary>
        public void MergeFrom(SyncContext other, string? otherName = null)
        {
            if (other == null) return;
            foreach (var sync in other.Data)
            {
                if (!Data.Contains(sync)) Data.Add(sync);
            }
            if (!string.IsNullOrEmpty(otherName) && !SameSyncTargets.Contains(otherName))
            {
                SameSyncTargets.Add(otherName);
            }
        }

        /// <summary>
        /// Returns the effective list of sync target names visible to this
        /// SyncContext, including any partners merged via `@samesync` so that
        /// callers (e.g. ModuleInstantiation.SyncCheck) can iterate them as if
        /// they had been declared directly in @sync.
        /// </summary>
        public IEnumerable<string> EffectiveSyncTargets()
        {
            foreach (var s in Data) yield return s;
            foreach (var s in SameSyncTargets) yield return s;
        }

        /// <summary>
        /// Resolves any deferred @samesync partners (registered only as
        /// names in <see cref="SameSyncTargets"/>) against the supplied
        /// NameSpace. When a partner is found, both SyncContexts are merged
        /// in both directions and the resolved name is removed from
        /// <see cref="SameSyncTargets"/>. This is invoked from
        /// <c>NameSpace.ApplyPendingSameSyncPairs</c> after every parse pass
        /// so that forward references can be linked once their symbols become
        /// available.
        /// </summary>
        /// <param name="nameSpace">Namespace used to look up partner DataObjects.</param>
        /// <param name="selfName">Name of the DataObject owning this SyncContext,
        /// recorded on the partner's SameSyncTargets so that the link is
        /// visible from the partner side as well.</param>
        public void ResolveSameSyncTargets(NameSpace? nameSpace, string? selfName)
        {
            if (nameSpace == null) return;
            if (SameSyncTargets.Count == 0) return;

            var snapshot = new List<string>(SameSyncTargets);
            foreach (var partnerName in snapshot)
            {
                DataObjects.DataObject? partner = nameSpace.NamedElements.GetDataObject(partnerName);
                if (partner == null) continue;

                // Merge in both directions. The partner's own SameSyncTargets
                // is updated so that even if the partner was registered via a
                // half-link, the partner's resolved set also reflects the
                // relationship.
                MergeFrom(partner.SyncContext, partnerName);
                if (!string.IsNullOrEmpty(selfName))
                {
                    partner.SyncContext.AddSameSyncTarget(selfName);
                }
                SameSyncTargets.Remove(partnerName);
            }
        }

        /// <summary>
        /// Adds a partner name to <see cref="SameSyncTargets"/> without
        /// resolving it, used by the @samesync parser when only one of the
        /// pair's operands has been registered so far.
        /// </summary>
        public void AddSameSyncTarget(string partnerName)
        {
            if (string.IsNullOrEmpty(partnerName)) return;
            if (!SameSyncTargets.Contains(partnerName)) SameSyncTargets.Add(partnerName);
        }

    }
}
