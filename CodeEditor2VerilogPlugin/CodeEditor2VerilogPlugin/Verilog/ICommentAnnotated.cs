using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface ICommentAnnotated
    {
        Dictionary<string, string> CommentAnnotations { get; }
        void AppendAnnotation(string key, string value);

    }
}
