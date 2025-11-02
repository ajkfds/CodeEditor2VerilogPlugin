using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.CommentAnnotation
{
    public static class PortAnnotation
    {
        /// <summary>
        /// Check for comment annotations
        /// </summary>
        public static void ParseComment(WordScanner word, NameSpace nameSpace, Verilog.DataObjects.Port? port, ref string? portGroup)
        {
            string commentText = word.GetNextComment();
            if (!commentText.Contains("@")) return;

            var comment = word.GetNextCommentScanner();
            while (!comment.EOC)
            {
                if (!comment.Text.StartsWith("@"))
                {
                    comment.MoveNext();
                    continue;
                }

                if (commentText.Contains(word.ProjectProperty.AnnotationCommands.PortGroup))
                {
                    pasePortGroup(comment, nameSpace, ref portGroup);
                }
                else
                {
                    if (port != null)
                    {
                        if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Synchronized))
                        {
                            parseSyncAnnotation(comment, nameSpace, port);
                        }
                        else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Clock))
                        {
                            parseClockAnnotation(comment, nameSpace, port);
                        }
                        else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Reset))
                        {
                            parseResetAnnotation(comment, nameSpace, port);
                        }
                        else
                        {
                            comment.MoveNext();
                        }
                    }
                    else
                    {
                        comment.MoveNext();
                    }
                }
            }

        }
        private static void pasePortGroup(CommentScanner comment, NameSpace nameSpace, ref string? portGroup)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNextUntilEol(); // :

                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                portGroup = comment.Text;
            }
        }

        private static void parseSyncAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext(); // :

                while (!comment.EOC)
                {
                    if (!nameSpace.BuildingBlock.NamedElements.ContainsDataObject(comment.Text))
                    {
                        break;
                    }
                    port.AppendAnnotation("sync", comment.Text);
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();

                    if (comment.Text != ",") break;
                    comment.MoveNext(); // ,

                    if (comment.Text.StartsWith("@")) break;
                }
            }
        }

        private static void parseClockAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext(); // :

                while (!comment.EOC)
                {
                    if (comment.Text == "posedge" || comment.Text == "negedge")
                    {
                        port.AppendAnnotation("clock", comment.Text);
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                    }
                    else
                    {
                        break;
                    }

                    if (comment.Text != ",") break;
                    comment.MoveNext(); // ,

                    if (comment.Text.StartsWith("@")) break;
                }
            }
        }

        private static void parseResetAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            port.AppendAnnotation("reset", "");
        }

    }
}
