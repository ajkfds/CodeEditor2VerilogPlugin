using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Assertion;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ConcurrentAssertionStatementItem : INamedElement
    {
        /// <summary>
        /// Parse assert property statement
        /// </summary>
        public static bool ParseAssertProperty(WordScanner word, NameSpace nameSpace, string? blockIdentifier)
        {
            if (word.Text != "assert" || word.NextText != "property")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            if (blockIdentifier == null)
            {
                AssertPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    AssertPropertyStatement = AssertPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier),
                    Name = blockIdentifier
                };
                nameSpace.NamedElements.Add(item.Name, item);
            }

            return true;
        }

        /// <summary>
        /// Parse assume property statement
        /// </summary>
        public static bool ParseAssumeProperty(WordScanner word, NameSpace nameSpace, string? blockIdentifier)
        {
            if (blockIdentifier == null)
            {
                AssumePropertyStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    AssumePropertyStatement = AssumePropertyStatement.ParseCreate(word, nameSpace, blockIdentifier),
                    Name = blockIdentifier
                };
                nameSpace.NamedElements.Add(item.Name, item);
            }

            return true;
        }

        /// <summary>
        /// Parse cover property statement
        /// </summary>
        public static bool ParseCoverProperty(WordScanner word, NameSpace nameSpace, string? blockIdentifier)
        {
            if (blockIdentifier == null)
            {
                CoverPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    CoverPropertyStatement = CoverPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier),
                    Name = blockIdentifier
                };
                nameSpace.NamedElements.Add(item.Name, item);
            }

            return true;
        }

        /// <summary>
        /// Parse restrict property statement
        /// </summary>
        public static bool ParseRestrictProperty(WordScanner word, NameSpace nameSpace, string? blockIdentifier)
        {
            if (blockIdentifier == null)
            {
                RestrictPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    RestrictPropertyStatement = RestrictPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier),
                    Name = blockIdentifier
                };
                nameSpace.NamedElements.Add(item.Name, item);
            }

            return true;
        }

        /// <summary>
        /// Parse cover sequence statement
        /// </summary>
        public static bool ParseCoverSequence(WordScanner word, NameSpace nameSpace, string? blockIdentifier)
        {
            if (blockIdentifier == null)
            {
                CoverSequenceStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    CoverSequenceStatement = CoverSequenceStatement.ParseCreate(word, nameSpace, blockIdentifier),
                    Name = blockIdentifier
                };
                nameSpace.NamedElements.Add(item.Name, item);
            }

            return true;
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reference to the AssertPropertyStatement (if this item represents an assert property)
        /// </summary>
        public AssertPropertyStatement? AssertPropertyStatement { get; set; }

        /// <summary>
        /// Reference to the AssumePropertyStatement (if this item represents an assume property)
        /// </summary>
        public AssumePropertyStatement? AssumePropertyStatement { get; set; }

        /// <summary>
        /// Reference to the CoverPropertyStatement (if this item represents a cover property)
        /// </summary>
        public CoverPropertyStatement? CoverPropertyStatement { get; set; }

        /// <summary>
        /// Reference to the RestrictPropertyStatement (if this item represents a restrict property)
        /// </summary>
        public RestrictPropertyStatement? RestrictPropertyStatement { get; set; }

        /// <summary>
        /// Reference to the CoverSequenceStatement (if this item represents a cover sequence)
        /// </summary>
        public CoverSequenceStatement? CoverSequenceStatement { get; set; }

        public required string Name { get; set; }

        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Keyword;

        public NamedElements NamedElements => throw new NotImplementedException();
    }
}
