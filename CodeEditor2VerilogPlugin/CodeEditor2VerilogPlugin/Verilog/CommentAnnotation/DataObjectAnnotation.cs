using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.CommentAnnotation
{
    public static class DataObjectAnnotaion
    {

        /// <summary>
        /// Check for comment annotations
        /// </summary>
        public static void ParsePostComment(WordScanner word, NameSpace nameSpace, Verilog.DataObjects.DataObject? dataObject)
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

                if (dataObject != null)
                {
                    if (comment.Text.Contains(word.ProjectProperty.AnnotationCommands.Synchronized))
                    {
                        parseSyncAnnotation(comment, nameSpace, dataObject, word.ProjectProperty);
                    }
                    else if (comment.Text.Contains(word.ProjectProperty.AnnotationCommands.Clock))
                    {
                        parseClockAnnotation(comment, nameSpace, dataObject, word.ProjectProperty);
                    }
                    else if (comment.Text.Contains(word.ProjectProperty.AnnotationCommands.Reset))
                    {
                        parseResetAnnotation(comment, nameSpace, dataObject, word.ProjectProperty);
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
        private static void parseSyncAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, ProjectProperty projectProperty)
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
                if (syncTarget == "clock")
                {
                    dataObject.SyncContext.AssignToClock();
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }
                if (syncTarget == "reset")
                {
                    dataObject.SyncContext.AssignToReset();
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }

                if (!nameSpace.BuildingBlock.NamedElements.ContainsDataObject(syncTarget))
                {
                    break;
                }
                dataObject.SyncContext.AddClockDomain(syncTarget);
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext();

                if (comment.Text != ",") break;
                comment.MoveNext(); // ,

                if (comment.Text.StartsWith("@")) break;
            }
        }

        private static void parseClockAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, ProjectProperty projectProperty)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            dataObject.SyncContext.AssignToClock();

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
                if (comment.Text == "posedge" || comment.Text == "negedge")
                {
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
        private static void parseResetAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, ProjectProperty projectProperty)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            dataObject.SyncContext.AssignToReset();
            comment.MoveNext();
      }

    }
}
