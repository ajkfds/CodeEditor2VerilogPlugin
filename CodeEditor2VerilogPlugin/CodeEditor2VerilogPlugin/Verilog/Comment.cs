using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    public class Comment
    {

        public void ParseComment(string fullComment, out string followedComment, out List<string> tags)
        {
            tags = null;
            followedComment = fullComment;
        }
    }
}
