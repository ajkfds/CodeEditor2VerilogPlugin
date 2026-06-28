using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    public static class CommentAnnotationItem
    {
        public static void Parse(WordScanner word, NameSpace nameSpace)
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
                    else if (comment.Text == word.ProjectProperty.AnnotationCommands.Discard)
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
                                if (dataObject == null)
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
                    else if (comment.Text == word.ProjectProperty.AnnotationCommands.Markdown)
                    {
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNextUntilEol();

                        while (!comment.EOC)
                        {
                            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                            comment.MoveNextUntilEol();
                        }
                    }
                    else if (comment.Text == "@scope")
                    {
                        // Parse @scope annotation
                        // Format: @scope buildingBlockName [#(.paramName(paramValue),...)] [instanceName]
                        parseScopeAnnotation(comment, word, nameSpace);
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

        private static void parseScopeAnnotation(CommentScanner comment, WordScanner word, NameSpace nameSpace)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();

            if (comment.EOC) return;

            // Get building block name
            string buildingBlockName = comment.Text;
            if (!General.IsSimpleIdentifier(buildingBlockName))
            {
                // Not a valid identifier, skip
                while (!comment.EOC && comment.Text != "@") comment.MoveNext();
                return;
            }
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();

            // Create the scope reference
            CommentScopeReference scopeRef = new CommentScopeReference
            {
                BuildingBlockName = buildingBlockName
            };

            // Check for parameter overrides (#(...))
            if (!comment.EOC && comment.Text == "#")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext();

                if (!comment.EOC && comment.Text == "(")
                {
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();

                    // Parse parameter overrides
                    scopeRef.ParameterOverrides = new Dictionary<string, Expressions.Expression>();
                    while (!comment.EOC && comment.Text != ")")
                    {
                        // Expect named parameter: .paramName(value)
                        if (comment.Text != ".")
                        {
                            // Skip until we find next valid token
                            comment.MoveNext();
                            continue;
                        }
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        if (comment.EOC) break;
                        string paramName = comment.Text;
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        if (comment.EOC) break;
                        if (comment.Text != "(")
                        {
                            // Expected (, skip
                            comment.MoveNext();
                            continue;
                        }
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();

                        // Parse the parameter value expression
                        // For comments, we need to create a simple document to parse
                        string paramValueText = "";
                        int parenDepth = 1;
                        while (!comment.EOC && parenDepth > 0)
                        {
                            paramValueText += comment.Text;
                            if (comment.Text == "(") parenDepth++;
                            else if (comment.Text == ")") parenDepth--;
                            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                            comment.MoveNext();
                        }

                        // Try to parse the expression
                        if (!string.IsNullOrEmpty(paramValueText))
                        {
                            try
                            {
                                // Create a temporary document to parse the expression
                                var tempDoc = new pluginVerilog.CodeEditor.CodeDocument(paramValueText);
                                var tempScanner = new WordScanner(tempDoc, word.RootParsedDocument, word.SystemVerilog);
                                var paramExpr = Expressions.Expression.ParseCreate(tempScanner, nameSpace);
                                if (paramExpr != null && paramExpr.Constant)
                                {
                                    scopeRef.ParameterOverrides[paramName] = paramExpr;
                                }
                            }
                            catch
                            {
                                // Failed to parse, ignore this parameter
                            }
                        }

                        // Skip comma if present
                        if (!comment.EOC && comment.Text == ",")
                        {
                            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                            comment.MoveNext();
                        }
                    }

                    // Move past closing parenthesis
                    if (!comment.EOC && comment.Text == ")")
                    {
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                    }
                }
            }

            // Check for instance name (optional)
            if (!comment.EOC && General.IsSimpleIdentifier(comment.Text))
            {
                scopeRef.InstanceName = comment.Text;
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext();
            }

            // Add to nameSpace
            nameSpace.CommentScopeReferences.Add(scopeRef);
        }
    }
}
