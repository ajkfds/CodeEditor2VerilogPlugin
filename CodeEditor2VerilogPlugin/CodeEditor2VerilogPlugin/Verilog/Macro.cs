﻿using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class Macro
    {
        //        text_macro_definition::= ‘define text_macro_name macro_text 
        // text_macro_name ::= text_macro_identifier[(list_of_formal_arguments)] 
        // list_of_formal_arguments ::= formal_argument_identifier { ,  formal_argument_identifier }
        //        text_macro_identifier::= (From Annex A - A.9.3) simple_identifier
        protected Macro() { }

        public static Macro Create(string name,string macroText)
        {
            Macro macro = new Macro();

            // trim start/end blank of macro text
            string text = macroText;
            text = text.TrimStart(new char[] { ' ', '\t' });
            text = text.TrimEnd(new char[] { ' ', '\t' });

            // seprarate identifier & argument
            if (text.StartsWith("(") && text.Contains(")"))
            {
                string argumentsText = text.Substring(1, text.IndexOf(")")-1);
                text = text.Substring(argumentsText.Length + 2);

                string[] arguments = argumentsText.Split(',');
                macro.Aurguments = new List<string>();
                foreach (string argument in arguments)
                {
                    macro.Aurguments.Add(argument.Trim());
                }
            }

            text = text.TrimStart(new char[] { ' ', '\t' });

            macro.Name = name;
            macro.MacroText = text;
            return macro;
        }

        public string Name;
        public List<string> Aurguments = null;
        public string MacroText;

        public void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label,Dictionary<string,Macro> macros)
        {
            if (Name == null) return;
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            label.AppendText(" : ");
            label.AppendText(MacroText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            label.AppendText("\r\n");

            string fixedText = MacroText;
            while (fixedText.Contains("`")){
                foreach(Macro macro in macros.Values)
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
