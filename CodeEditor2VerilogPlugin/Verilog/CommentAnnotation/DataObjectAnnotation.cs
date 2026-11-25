using SystemVerilogCore.Documents;
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
                        break;
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
        private static void parseSyncAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, IVerilogParserSettings projectProperty)
        {
            comment.Color(VerilogColorType.CommentAnnotation);
            comment.MoveNext();

            if (projectProperty.AnnotationKeyValueDelimiter != "")
            {
                if (comment.Text == projectProperty.AnnotationKeyValueDelimiter)
                {
                    comment.Color(VerilogColorType.CommentAnnotation);
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
                    comment.Color(VerilogColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }
                if (syncTarget == "reset")
                {
                    dataObject.SyncContext.AssignToReset();
                    comment.Color(VerilogColorType.CommentAnnotation);
                    comment.MoveNext();
                    return;
                }

                if (!nameSpace.BuildingBlock.NamedElements.ContainsDataObject(syncTarget))
                {
                    break;
                }
                dataObject.SyncContext.AddClockDomain(syncTarget, dataObject.DefinedReference,nameSpace.BuildingBlock.SameSync);
                comment.Color(VerilogColorType.CommentAnnotation);
                comment.MoveNext();

                if (comment.Text != ",") break;
                comment.MoveNext(); // ,

                if (comment.Text.StartsWith("@")) break;
            }
        }

        private static void parseClockAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, IVerilogParserSettings projectProperty)
        {
            comment.Color(VerilogColorType.CommentAnnotation);
            comment.MoveNext();
            dataObject.SyncContext.AssignToClock();

            if (projectProperty.AnnotationKeyValueDelimiter != "")
            {
                if (comment.Text == projectProperty.AnnotationKeyValueDelimiter)
                {
                    comment.Color(VerilogColorType.CommentAnnotation);
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
                    comment.Color(VerilogColorType.CommentAnnotation);
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
        private static void parseResetAnnotation(CommentScanner comment, NameSpace nameSpace, Verilog.DataObjects.DataObject dataObject, IVerilogParserSettings projectProperty)
        {
            comment.Color(VerilogColorType.CommentAnnotation);
            dataObject.SyncContext.AssignToReset();
            comment.MoveNext();
        }

    }
}
