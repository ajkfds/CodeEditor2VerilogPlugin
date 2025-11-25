using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.CommentAnnotation
{
    public static class PortAnnotation
    {
        public static void ParsePreComment(WordScanner word, NameSpace nameSpace, Verilog.DataObjects.Port? port, ref string? portGroup)
        {
            if (word.Prototype) return;
            string commentText = word.GetPreviousComment();
            if (!commentText.Contains("@")) return;
            var comment = word.GetPreviousCommentScanner();
            while (!comment.EOC)
            {
                if (commentText.Contains(word.ProjectProperty.AnnotationCommands.PortGroup))
                {
                    pasePortGroup(comment, nameSpace, ref portGroup, word.ProjectProperty);
                }
                else
                {
                    comment.MoveNext();
                }

            }
        }


        /// <summary>
        /// Check for comment annotations
        /// </summary>
        public static void ParsePostComment(WordScanner word, NameSpace nameSpace, Verilog.DataObjects.Port? port)
        {
            if (word.Prototype) return;

            string commentText = word.GetPreviousComment();
            if (!commentText.Contains("@")) return;

            var comment = word.GetPreviousCommentScanner();
            while (!comment.EOC)
            {
                if (!comment.Text.StartsWith("@"))
                {
                    comment.MoveNext();
                    continue;
                }

                if (port != null)
                {
                    if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Synchronized))
                    {
                        parseSyncAnnotation(comment, nameSpace, port,word.ProjectProperty);
                    }
                    else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Clock))
                    {
                        parseClockAnnotation(comment, nameSpace, port, word.ProjectProperty);
                    }
                    else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Reset))
                    {
                        parseResetAnnotation(comment, nameSpace, port, word.ProjectProperty);
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
        private static void pasePortGroup(CommentScanner comment, NameSpace nameSpace, ref string? portGroup, ProjectProperty projectProperty)
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

        private static void parseSyncAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port,ProjectProperty projectProperty)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();

            if (projectProperty.AnnotationKeyValueDelimiter != "")
            {
                if (comment.Text == ":")
                {
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext(); // :
                }
                else
                {
                    return;
                }
            }


            while (!comment.EOC)
            {
                string syncTarget = comment.Text;
                if(syncTarget == "clock")
                {
                    port.AppendAnnotation("clock", "");
                    if (port.DataObject != null) port.DataObject.SyncContext.AssignToClock();
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }
                if (syncTarget == "reset")
                {
                    port.AppendAnnotation("reset", "");
                    if (port.DataObject != null) port.DataObject.SyncContext.AssignToReset();
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }

                if (!nameSpace.BuildingBlock.NamedElements.ContainsDataObject(syncTarget))
                {
                    break;
                }
                port.AppendAnnotation("sync", syncTarget);
                if (port.DataObject != null) port.DataObject.SyncContext.AddClockDomain(syncTarget);
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext();

                if (comment.Text != ",") break;
                comment.MoveNext(); // ,

                if (comment.Text.StartsWith("@")) break;
            }
        }

        private static void parseClockAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port, ProjectProperty projectProperty)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            
            if (projectProperty.AnnotationKeyValueDelimiter != "")
            {
                if (comment.Text == ":")
                {
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    if (port.DataObject != null) port.DataObject.SyncContext.AssignToClock();
                    comment.MoveNext(); // :
                }
                else
                {
                    port.AppendAnnotation("clock", "");
                    return;
                }
            }

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

        private static void parseResetAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.Port port, ProjectProperty projectProperty)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            if (port.DataObject != null) port.DataObject.SyncContext.AssignToReset();
            comment.MoveNext();
            port.AppendAnnotation("reset", "");
        }

    }
}
