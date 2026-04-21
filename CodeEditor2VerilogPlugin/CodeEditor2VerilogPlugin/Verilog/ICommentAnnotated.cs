using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    public interface ICommentAnnotated
    {
        Dictionary<string, string> CommentAnnotations { get; }
        void AppendAnnotation(string key, string value);

    }
}
