using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Data.VerilogCommon;

namespace pluginVerilog.Data.VerilogCommon
{
    public class AutoCompleteKeyword
    {
        public static void AppendKeywordAutoCompleteItems(
            List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items,
            string candidate, 
            int candidateStartIndex,
            int lineStartIndex,
            bool systemVerilog
            )
        {


            appendItems(items, candidate, new Verilog.AutoComplete.BeginAutoCompleteItem());
            appendItems(items, candidate, new Verilog.AutoComplete.CaseAutocompleteItem());
            appendItems(items, candidate, new Verilog.AutoComplete.FunctionAutocompleteItem());
            appendItems(items, candidate, new Verilog.AutoComplete.GenerateAutoCompleteItem());
            appendItems(items, candidate, new Verilog.AutoComplete.ModuleAutocompleteItem());
            appendItems(items, candidate, new Verilog.AutoComplete.TaskAutocompleteItem());
            if (candidate == "<=") items.Add(new Verilog.AutoComplete.NonBlockingAssignmentAutoCompleteItem());

            appendKeywordItems(items, candidate, "sync"); // annotation
            appendKeywordItems(items, candidate, "async"); // annotation
            appendKeywordItems(items, candidate, "clock"); // annotation
            appendKeywordItems(items, candidate, "reset"); // annotation

            appendKeywordItems(items, candidate, "always"); // verilog
            appendKeywordItems(items, candidate, "integer"); // verilog


            List<(string, int)> keywords = new List<(string, int)>
                {
                // Verilog
                ("and",1),
                ("assign",1),
                ("automatic",2),
                /*"begin",*/    
                ("case",1),
                ("casex",1),
                ("casez",1),

                ("deassign",1),
                ("default",1),
                ("defparam",1),
                ("design",1),
                ("disable",1),
                ("edge",1),
                ("else",1),
                ("end",1),
                ("endcase",1),
                ("endfunction",1),
                ("endgenerate",1),
                ("endmodule",1),
                ("endprimitive",1),
                ("endspecify",1),
                ("endtask",1),
                ("for",1),
                ("force",1),
                ("forever",1),
                ("fork",1),
                /*"function",1),*/
                /*"generate",1),*/
                ("genvar",1),
                ("if",1),
                ("incdir",2),
                ("include",1),
                ("initial",1),
                ("inout",1),
                ("input",1),
                /*"interface",1),*/
                ("join",1),
                ("localparam",1),
                /*"module",1),*/  
                ("nand",1),
                ("negedge",1),
                ("nor",1),
                ("not",1),
                ("or",1),
                ("output",1),
                ("parameter",1),
                ("posedge",1),
                ("pulldown",1),    ("pullup",1),       ("real",1),
               ("realtime",1),    ("reg",1),         ("release",1),      ("repeat",1),
               ("signed",1),       /*"task",1),*/    ("time",1),         ("tri0",1),
               ("tri1",1),        ("trireg",1),      ("unsigned",1),    ("vectored",1),
               ("wait",1),        ("wand",1),        ("weak0",1),       ("weak1",1),
               ("while",1),       ("wire",1),        ("wor",1)};
            List<(string, int)> systemVerilogKeywords = new List<(string, int)> {
               // SystemVerilog
               ("accept_on",1),
                ("alias",1),
                ("always_comb",1),
                ("always_ff",1),
               ("always_latch",1),
                ("assert",1),
                ("assume",1),
                ("before",1),
               ("bind",1),
                ("bins",1),        ("binsof",1),      ("bit",1),
               ("break",1),       ("byte",1),        ("chandle",1),     ("checker",1),
               ("class",1),       ("clocking",1),    ("const",1),       ("constraint",1),
               ("context",1),     ("continue",1),    ("cover",1),       ("covergroup",1),
               ("coverpoint",1),  ("cross",1),       ("dist",1),        ("do",1),
               ("endchecker",1),  ("endclass",1),    ("endclocking",1), ("endgroup",1),
               ("endinterface",1),("endpackage",1),  ("endprogram",1),  ("endproperty",1),
               ("endsequence",1), ("enum",1),        ("eventually",1),  ("expect",1),
               ("export",1),      ("extends",1),     ("extern",1),      ("final",1),
               ("first_match",1), ("foreach",1),     ("forkjoin",1),    ("global",1),
               ("iff",1),         ("ignore_bins",1), ("illegal_bins",1),("implements",1),
               ("implies",1),     ("import",1),      ("inside",1),      ("int",1),
               ("interconnect",1),("interface",1),   ("intersect",1),   ("join_any",1),
               ("join_none",1),   ("let",1),         ("local",1),       ("logic",1),
               ("longint",1),     ("matches",1),     ("modport",1),     ("nettype",1),
               ("new",1),         ("nexttime",1),    ("null",1),        ("package",1),
               ("packed",1),      ("priority",1),    ("program",1),     ("property",1),
               ("protected",1),   ("pure",1),        ("rand",1),        ("randc",1),
               ("randcase",1),    ("randsequence",1),("ref",1),         ("reject_on",1),
               ("restrict",1),    ("return",1),      ("s_always",1),    ("s_eventually",1),
               ("s_nexttime",1),  ("s_until",1),     ("s_until_with",1),("sequence",1),
               ("shortint",1),    ("shortreal",1),   ("soft",1),        ("solve",1),
               ("static",1),      ("string",1),      ("strong",1),      ("struct",1),
               ("super",1),       ("sync_accept_on",1),  ("sync_reject_on",1),  ("tagged",1),
               ("this",1),        ("throughout",1),  ("timeprecision",1),   ("timeunit",1),
               ("type",1),        ("typedef",1),     ("union",1),       ("unique",1),
               ("unique0",1),     ("until",1),       ("until_with",1),  ("untyped",1),
               ("uwire",1),       ("var",1),         ("virtual",1),     ("void",1),
               ("wait_order",1),  ("weak",1),        ("wildcard",1),    ("with",1),
               ("within",1)
            };



            foreach ((string, int) keyword in keywords)
            {
                if (!keyword.Item1.StartsWith(candidate)) continue;
                if (candidate.Length < keyword.Item2) continue;
                Data.VerilogCommon.AutoCompleteItem item = new pluginVerilog.Data.VerilogCommon.AutoCompleteItem(
                    keyword.Item1,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                    "CodeEditor2/Assets/Icons/bookmark.svg"
                    );
                items.Add(item);
            }

            appendItems(items, candidate, new Verilog.AutoComplete.InterfaceAutocompleteItem());

            if (systemVerilog)
            {
                foreach ((string, int) keyword in systemVerilogKeywords)
                {
                    if (!keyword.Item1.StartsWith(candidate)) continue;
                    if (candidate.Length < keyword.Item2) continue;
                    pluginVerilog.Data.VerilogCommon.AutoCompleteItem item = new pluginVerilog.Data.VerilogCommon.AutoCompleteItem(
                        keyword.Item1,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                        "CodeEditor2/Assets/Icons/bookmark.svg"
                        );
                    items.Add(item);
                }

            }

        }

        private static void appendItems(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items, string cantidate, pluginVerilog.Data.VerilogCommon.AutoCompleteItem item)
        {
            if (!item.Text.StartsWith(cantidate)) return;
            items.Add(item);
        }
        private static void appendKeywordItems(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items, string cantidate, string keyword)
        {
            if (!keyword.StartsWith(cantidate)) return;


            pluginVerilog.Data.VerilogCommon.AutoCompleteItem item = new pluginVerilog.Data.VerilogCommon.AutoCompleteItem(
                keyword,
                CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                "CodeEditor2/Assets/Icons/bookmark.svg"
                );
            items.Add(item);
        }

    }
}
