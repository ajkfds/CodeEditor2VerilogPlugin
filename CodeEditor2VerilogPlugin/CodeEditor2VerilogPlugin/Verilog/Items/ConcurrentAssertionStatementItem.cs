using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Assertion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ConcurrentAssertionStatementItem : INamedElement
    {
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace,string? blockIdentifier)
        {
            if(word.NextText !="property" || word.Text != "assert")
            {
                if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            if (blockIdentifier == null)
            {
                await AssertPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier);
            }
            else
            {
                ConcurrentAssertionStatementItem item = new ConcurrentAssertionStatementItem()
                {
                    AssertPropertyStatement = await AssertPropertyStatement.ParseCreate(word, nameSpace, blockIdentifier),
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

        public required AssertPropertyStatement AssertPropertyStatement { get; set; }

        public required string Name {  get; set; }

        public CodeDrawStyle.ColorType ColorType
        {
            get
            {
                return CodeDrawStyle.ColorType.Keyword;
            }
        }

        public NamedElements NamedElements => throw new NotImplementedException();
    }
}
