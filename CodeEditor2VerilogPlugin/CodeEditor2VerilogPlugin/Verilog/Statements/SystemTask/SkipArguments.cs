namespace pluginVerilog.Verilog.Statements.SystemTask
{
    public class SkipArguments : SystemTask
    {
        public static new SkipArguments ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SkipArguments taskEnable = new SkipArguments();

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                if (word.Text == ")")
                {
                    word.MoveNext();
                    word.AddWarning("remove ()");
                }
                else
                {
                    while (!word.Eof)
                    {
                        if (word.Text == ")")
                        {
                            break;
                        }
                        word.MoveNext();
                    }
                    if (word.Text == ")") word.MoveNext();
                    else word.AddError(") required");
                }
            }

            if (word.Text == ";") word.MoveNext();
            else word.AddError("; required");

            return taskEnable;
        }

    }
}
