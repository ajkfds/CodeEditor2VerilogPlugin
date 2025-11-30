using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ProceduralTimingControlStatement : IStatement
    {

        protected ProceduralTimingControlStatement() { }
        public void DisposeSubReference()
        {
            if(Statement != null) Statement.DisposeSubReference();
        }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public DelayControl? DelayControl { get; protected set; }
        public EventControl? EventControl { get; protected set; }
        public IStatement? Statement { get; protected set; }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public static async Task<ProceduralTimingControlStatement?> ParseCreate(WordScanner word, NameSpace nameSpace,string? statement_label)
        {
            switch (word.Text)
            {
                case "#":
                    {
                        ProceduralTimingControlStatement statement = new ProceduralTimingControlStatement() { Name = "" };
                        if (statement_label != null) { statement.Name = statement_label; }

                        statement.DelayControl = DelayControl.ParseCreate(word, nameSpace);
                        statement.Statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
                        return statement;
                    }
                case "@":
                    {
                        ProceduralTimingControlStatement statement = new ProceduralTimingControlStatement(){ Name = "" };
                        if (statement_label != null) { statement.Name = statement_label; }

                        statement.EventControl = EventControl.ParseCreate(word, nameSpace);
                        statement.Statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
                        return statement;
                    }
                default:
                    return null;
            }
        }
    }

}
