using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pluginVerilog.Data
{
    public class VerilogRelatedFileReference
    {
        public VerilogRelatedFileReference(IVerilogRelatedFile file)
        {
            ID = file.ID;
            ProjectName = file.Project.Name;
            RelativeFilePath = file.RelativePath;
        }
        public required string ID { set; get; }
        public required string ProjectName { get; set; }
        public required string RelativeFilePath { get; set; }

        public IVerilogRelatedFile? GetTargetFile()
        {
            CodeEditor2.Data.Project? project;
            if (!CodeEditor2.Global.Projects.TryGetValue(ProjectName, out project)) return null;

            Data.IVerilogRelatedFile? file = project.GetItemHierarchy(RelativeFilePath) as IVerilogRelatedFile;
            if (file == null) return null;

            return null;
        }

        public IVerilogRelatedFile? GetVerilogRelatedFile()
        {
            CodeEditor2.Data.Project? project;
            if (!CodeEditor2.Global.Projects.TryGetValue(ProjectName, out project)) throw new Exception("Project not found: " + ProjectName);
            Data.IVerilogRelatedFile? file = project.GetItemHierarchy(RelativeFilePath) as IVerilogRelatedFile;
            if (file == null) return null;
            VerilogFile? verilogFile = file as VerilogFile;
            if(verilogFile != null && verilogFile.ID != ID)
            {
                if(verilogFile.TryGetInstanceTextFile(ID,out InstanceTextFile? instanceTextFile))
                {
                    return instanceTextFile;
                }
                else
                {
                    return null;
                }
            }
            return verilogFile;
        }
    }
}
