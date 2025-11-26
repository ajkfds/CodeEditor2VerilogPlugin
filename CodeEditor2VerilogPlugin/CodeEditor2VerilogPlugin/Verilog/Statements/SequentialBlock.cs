using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog.Statements
{
    public class SequentialBlock : IStatement
    {
        protected SequentialBlock() { }

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            foreach (IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }

        public List<IStatement> Statements = new List<IStatement>();

        /*
        A.6.3 Parallel and sequential blocks
        function_seq_block      ::= begin[ : block_identifier { block_item_declaration } ] { function_statement }
        end variable_assignment ::= variable_lvalue = expression
        par_block               ::= fork [ : block_identifier { block_item_declaration } ] { statement }
        join seq_block          ::= begin[ : block_identifier { block_item_declaration } ] { statement } end  


        block_item_declaration ::=  { attribute_instance } block_reg_declaration          
                                    | { attribute_instance } event_declaration          
                                    | { attribute_instance } integer_declaration          
                                    | { attribute_instance } local_parameter_declaration          
                                    | { attribute_instance } parameter_declaration          
                                    | { attribute_instance } real_declaration          
                                    | { attribute_instance } realtime_declaration          
                                    | { attribute_instance } time_declaration 
        block_reg_declaration ::= reg [ signed ] [ range ] list_of_block_variable_identifiers ;
        list_of_block_variable_identifiers ::=  block_variable_type { , block_variable_type } 
        block_variable_type ::=  variable_identifier        | variable_identifier dimension { dimension }  
        */
        public static async Task<IStatement?> ParseCreate(WordScanner word,NameSpace nameSpace,string? statement_label)
        {
            if (word.Text != "begin") throw new Exception();

            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginIndex = word.CreateIndexReference();
            word.MoveNext(); // begin

            if (word.GetCharAt(0) == ':')
            {
                word.MoveNext(); // :
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal ifdentifier name");
                    return await parseCreateUnnamedSequentialBlock(word, nameSpace, beginIndex);
                }
                else
                {
                    return await parseCreateNamedSequentialBlock(word, nameSpace, beginIndex);
                }
            }
            else
            {
                return await parseCreateUnnamedSequentialBlock(word, nameSpace, beginIndex);
            }
        }

        private static List<string> endKeyword = new List<string> { "endmodule","endtask","endtask","endinterface","endfunction"};
        private static async Task<IStatement?> parseCreateUnnamedSequentialBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex)
        {
            SequentialBlock sequentialBlock = new SequentialBlock();

            // An unnamed block creates a new hierarchy scope only if it directly contains a block item declaration, 
            // such as a variable declaration or a type declaration. This hierarchy scope is unnamed and the items declared in it cannot be hierarchically referenced.

            // local item declaration

            // create temporary name
            {
                IndexReference startRef = word.CreateIndexReference();
                string name = "\0" + startRef.GetIndexID();
                NamedSequentialBlock namedBlock = createNamedSequentialBlock(word, nameSpace, beginIndex, name);
                while (word.SystemVerilog && !word.Eof && word.Text != "end")
                {
                    if (!await Items.BlockItemDeclaration.Parse(word, namedBlock)) break;
                }
                if (!startRef.IsSameAs(word.CreateIndexReference()))
                {
                    // parse statements
                    while (!word.Eof && word.Text != "end")
                    {
                        IStatement? statement = await Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
                        if (statement != null)
                        {
                            namedBlock.Statements.Add(statement);
                        }
                        else
                        {
                            if (endKeyword.Contains(word.Text)) break;
                            while (!word.Eof && word.Text != "end" && !endKeyword.Contains(word.Text))
                            {
                                if (word.Text == ";")
                                {
                                    word.MoveNext();
                                    break;
                                }
                                word.MoveNext();
                            }
                        }
                    }
                    if (word.Text != "end")
                    {
                        word.AddError("'end' required");
                        return null;
                    }
                    namedBlock.LastIndexReference = word.CreateIndexReference();
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext(); // end

                    if (!nameSpace.NamedElements.ContainsKey(namedBlock.Name))
                    {
                        nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                    }

                    return namedBlock;

                }
            }

            while (!word.Eof && word.Text != "end")
            {
                IStatement? statement = await Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
                if(statement != null)
                {
                    sequentialBlock.Statements.Add(statement);
                } else {
                    if (endKeyword.Contains(word.Text)) break;
                    while(!word.Eof && word.Text != "end" && !endKeyword.Contains(word.Text))
                    {
                        if (word.Text == ";")
                        {
                            word.MoveNext();
                            break;
                        }
                        word.MoveNext();
                    }
                }
            }
            if (word.Text != "end")
            {
                word.AddError("'end' required");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // end

            return sequentialBlock;
        }

        private static NamedSequentialBlock createNamedSequentialBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex, string name)
        {
            // create namedBlock
            NamedSequentialBlock namedBlock;
            if (word.Prototype)
            { // prototype
                if (nameSpace.NamedElements.ContainsKey(word.Text))
                {
                    word.AddError("duplicated name");
                    namedBlock = new NamedSequentialBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = name,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                }
                else
                {
                    namedBlock = new NamedSequentialBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = name,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            else
            { // implementation
                if (nameSpace.NamedElements.ContainsKey(name) && nameSpace.NamedElements[name] is NamedSequentialBlock)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    namedBlock = (NamedSequentialBlock)nameSpace.NamedElements[name];
                }
                else
                { // inside of task & function can skip parse. In this case namedBlock is not registered in namedElements
                    namedBlock = new NamedSequentialBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = name,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            return namedBlock;
        }
        private static async Task<IStatement?> parseCreateNamedSequentialBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex)
        {
            // start at identifier
            string name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // create namedBlock
            NamedSequentialBlock namedBlock = createNamedSequentialBlock(word, nameSpace, beginIndex, name);

            // local item declaration
            while (!word.Eof && word.Text != "end")
            {
                if (!await Items.BlockItemDeclaration.Parse(word, namedBlock)) break;
            }

            // parse statements
            while (!word.Eof && word.Text != "end")
            {
                IStatement? statement = await Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
                if (statement != null)
                {
                    namedBlock.Statements.Add(statement);
                }
                else
                {
                    if (endKeyword.Contains(word.Text)) break;
                    while (!word.Eof && word.Text != "end" && !endKeyword.Contains(word.Text))
                    {
                        if (word.Text == ";")
                        {
                            word.MoveNext();
                            break;
                        }
                        word.MoveNext();
                    }
                }
            }
            if (word.Text != "end")
            {
                word.AddError("'end' required");
                return null;
            }
            namedBlock.LastIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // end

            if (word.Text == ":")
            {
                word.MoveNext();
                if(endKeyword.Contains(word.Text) || word.Text == "end")
                {
                    word.AddError("block name required");
                }
                else if (namedBlock.Name != word.Text)
                {
                    word.AddError("illegal block name");
                }
                else
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
            }

            if (!nameSpace.NamedElements.ContainsKey(namedBlock.Name))
            {
                nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
            }

            return namedBlock;
        }

    }

    public class NamedSequentialBlock : Verilog.NameSpace,IStatement
    {
        public void DisposeSubReference()
        {
            foreach (IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }
        public NamedSequentialBlock(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }

        public List<IStatement> Statements = new List<IStatement>();
    }

}
