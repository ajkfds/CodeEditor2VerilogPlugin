using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    public class Macro
    {
        //        text_macro_definition::= ‘define text_macro_name macro_text 
        // text_macro_name ::= text_macro_identifier[(list_of_formal_arguments)] 
        // list_of_formal_arguments ::= formal_argument_identifier { ,  formal_argument_identifier }
        //        text_macro_identifier::= (From Annex A - A.9.3) simple_identifier
        protected Macro() { }

        public static Macro Create(string name, string macroText)
        {
            Macro macro = new Macro();

            // trim start/end blank of macro text
            string text = macroText;
            text = text.TrimStart(new char[] { ' ', '\t' });
            text = text.TrimEnd(new char[] { ' ', '\t' });

            // seprarate identifier & argument
            if (text.StartsWith("(") && text.Contains(")"))
            {
                string argumentsText = text.Substring(1, text.IndexOf(")") - 1);
                text = text.Substring(argumentsText.Length + 2);

                string[] arguments = argumentsText.Split(',');
                macro.Aurguments = new List<string>();
                foreach (string argument in arguments)
                {
                    string trimmed = argument.Trim();
                    // formal argument with default value : formal_argument_identifier = default_text
                    int equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex >= 0)
                    {
                        string argName = trimmed.Substring(0, equalsIndex).Trim();
                        string defaultValue = trimmed.Substring(equalsIndex + 1).Trim();
                        macro.Aurguments.Add(argName);
                        if (macro.ArgumentDefaults == null) macro.ArgumentDefaults = new List<string>();
                        while (macro.ArgumentDefaults.Count < macro.Aurguments.Count - 1) macro.ArgumentDefaults.Add(null);
                        macro.ArgumentDefaults.Add(defaultValue);
                    }
                    else
                    {
                        macro.Aurguments.Add(trimmed);
                        if (macro.ArgumentDefaults != null) macro.ArgumentDefaults.Add(null);
                    }
                }
            }

            text = text.TrimStart(new char[] { ' ', '\t' });

            macro.Name = name;
            macro.MacroText = text;
            return macro;
        }

        public string Name;
        public List<string> Aurguments = null;
        public List<string> ArgumentDefaults = null;   // default value per argument (null = no default), same length as Aurguments when not null
        public string MacroText;

        // replace whole-word occurrences only (formal argument identifier must not match inside other identifiers, e.g. "a" in "$display")
        public static string ReplaceArgument(string text, string argumentName, string actualText)
        {
            if (string.IsNullOrEmpty(argumentName)) return text;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                if (System.Char.IsLetter(text[i]) || text[i] == '_' || text[i] == '$')
                {
                    int j = i;
                    while (j < text.Length && (System.Char.IsLetterOrDigit(text[j]) || text[j] == '_' || text[j] == '$')) j++;
                    string word = text.Substring(i, j - i);
                    if (word == argumentName)
                    {
                        sb.Append(actualText);
                    }
                    else
                    {
                        sb.Append(word);
                    }
                    i = j;
                }
                else
                {
                    sb.Append(text[i]);
                    i++;
                }
            }
            return sb.ToString();
        }

        public void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label, Dictionary<string, Macro> macros)
        {
            if (Name == null) return;
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            label.AppendText(" : ");
            label.AppendText(MacroText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            label.AppendText("\r\n");

            string fixedText = MacroText;
            while (fixedText.Contains("`"))
            {
                foreach (Macro macro in macros.Values)
                {
                    string searchString = "`" + macro.Name;
                    if (fixedText.Contains(searchString))
                    {
                        fixedText = fixedText.Replace(searchString, macro.MacroText);
                        continue;
                    }
                }
                break;
            }
            if (fixedText != MacroText)
            {
                label.AppendText("  = ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                label.AppendText(fixedText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                label.AppendText("\r\n");
            }
        }
    }
}
