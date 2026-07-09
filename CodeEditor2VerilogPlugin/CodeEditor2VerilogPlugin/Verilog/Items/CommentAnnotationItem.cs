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

            // Skip if an equivalent @scope reference has already been registered
            // for this nameSpace in this parse pass. This makes the annotation
            // idempotent so that re-invocation (which can happen when the same
            // @scope comment precedes multiple module items) does not produce
            // duplicate VirtualScopeNameSpace entries.
            string newEntryName = !string.IsNullOrEmpty(scopeRef.InstanceName)
                ? scopeRef.InstanceName
                : scopeRef.BuildingBlockName;
            bool alreadyRegistered = false;
            foreach (var existingRef in nameSpace.CommentScopeReferences)
            {
                if (existingRef.BuildingBlockName == scopeRef.BuildingBlockName
                    && existingRef.InstanceName == scopeRef.InstanceName
                    && string.Equals(existingRef.VirtualScopeEntryName, newEntryName, System.StringComparison.Ordinal))
                {
                    alreadyRegistered = true;
                    break;
                }
            }
            if (!alreadyRegistered)
            {
                scopeRef.VirtualScopeEntryName = newEntryName;
                nameSpace.CommentScopeReferences.Add(scopeRef);
            }

            // Immediately apply the scope reference so that subsequent expressions
            // in the same parse pass can resolve identifiers that go through the
            // virtual scope (e.g. `wire aa = TEST_RTL_MODULE_0.DATA_I;` after
            // `// @scope TEST_RTL_MODULE TEST_RTL_MODULE_0`). Without this,
            // the VirtualScopeNameSpace would only be registered later in
            // VerilogFile.AcceptParsedDocumentAsync via ApplyCommentScopeReferences,
            // and the in-flight parse would emit "unbound object" errors.
            if (!word.Prototype && !string.IsNullOrEmpty(newEntryName))
            {
                // Always re-resolve the target. On the first parse this sets
                // scopeRef.ResolvedBuildingBlock; on subsequent reparses (the
                // annotation may be re-encountered) it refreshes the binding
                // in case the referenced building block has since been
                // registered (its file finally parsed, or it was re-parsed and
                // a new BuildingBlock instance is now in the registry).
                scopeRef.ResolvedBuildingBlock = word.ProjectProperty.GetBuildingBlock(scopeRef.BuildingBlockName);

                // Even when the target BuildingBlock is not yet registered (it
                // may live in a different file whose parse hasn't completed),
                // we still register a VirtualScopeNameSpace with a null target.
                // The subsequent sub-identifier lookup (e.g. DATA_I in
                // TEST_RTL_MODULE_0.DATA_I) will fail to resolve, but the
                // first identifier TEST_RTL_MODULE_0 itself will be found,
                // avoiding the spurious "unbound object" error on the
                // virtual-scope identifier. The missing target will cause a
                // different, more accurate error (and triggers a reparse).
                BuildingBlocks.BuildingBlock? effectiveTarget = scopeRef.ResolvedBuildingBlock;
                bool needReparse = false;
                if (effectiveTarget == null)
                {
                    needReparse = true;
                }

                if (nameSpace.NamedElements.ContainsKey(newEntryName))
                {
                    INamedElement? existing = nameSpace.NamedElements[newEntryName];
                    if (existing is VirtualScopeNameSpace v && v.SourceCommentScopeReference == scopeRef)
                    {
                        // Already registered by an earlier @scope in this parse
                        // pass. Update its target so that the now-resolved
                        // building block becomes visible to subsequent
                        // expression parses via the NamedElements override
                        // even if no re-registration happens.
                        v.UpdateTarget(effectiveTarget);
                    }
                    else
                    {
                        // Don't overwrite an existing real instance/identifier
                        // (e.g. a real module instance) with a virtual scope;
                        // the real binding should take priority.
                    }
                }
                else
                {
                    var virtualNs = VirtualScopeNameSpace.Create(
                        sourceScopeRef: scopeRef,
                        target: effectiveTarget, // may be null; resolved later
                        entryName: newEntryName,
                        parent: nameSpace);

                    nameSpace.NamedElements.Add(newEntryName, virtualNs);
                }

                if (needReparse)
                {
                    word.RootParsedDocument.ReparseRequested = true;
                }
            }
        }
    }
}
