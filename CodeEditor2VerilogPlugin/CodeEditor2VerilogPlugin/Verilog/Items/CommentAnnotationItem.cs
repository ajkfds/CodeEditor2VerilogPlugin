using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public static class CommentAnnotationItem
    {
        public static void Parse(WordScanner word,NameSpace nameSpace)
        {
            if (!word.GetPreviousComment().Contains("@")) return;

            CommentScanner comment = word.GetPreviousCommentScanner();
            while (!comment.EOC)
            {
                if (comment.Text.StartsWith("@"))
                {
                    if (comment.Text == word.ProjectProperty.AnnotationCommands.RefInstance)
                    {
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        if (comment.Text != ":") continue;
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        if (comment.EOC) continue;
                        var buildingBlock = word.ProjectProperty.GetBuildingBlock(comment.Text);
                        if (buildingBlock == null) continue;
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        if (comment.EOC) continue;
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                    }
                    else if(comment.Text == word.ProjectProperty.AnnotationCommands.Discard)
                    {
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                        if (comment.Text != ":") continue;
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                        while (!comment.EOC)
                        {
                            if (!word.Prototype)
                            {
                                string name = comment.Text;
                                DataObjects.DataObject? dataObject = nameSpace.NamedElements.GetDataObject(name);
                                if(dataObject == null)
                                {
                                    break;
                                }
                                else
                                {
                                    dataObject.CommentAnnotation_Discarded = true;
                                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                                    comment.MoveNext();
                                }
                            }
                            if (comment.Text != ",") break;
                            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                            comment.MoveNext();
                        }
                    }
                    else if(comment.Text == word.ProjectProperty.AnnotationCommands.Markdown)
                    {
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNextUntilEol();

                        while (!comment.EOC)
                        {
                            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                            comment.MoveNextUntilEol();
                        }
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
}
