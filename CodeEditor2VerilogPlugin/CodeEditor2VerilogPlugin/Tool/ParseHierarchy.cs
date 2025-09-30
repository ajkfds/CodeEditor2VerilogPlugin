using Avalonia.Threading;
using CodeEditor2.Data;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace pluginVerilog.Tool
{
    public class ParseHierarchy
    {
        public static async Task Parse(CodeEditor2.Data.TextFile textFile)
        {
            if (textFile.ParseValid && !textFile.ReparseRequested) return;

            for(int i = 0; i < 2; i++)
            {
                if (textFile is Data.VerilogModuleInstance)
                {
                    await Task.Run(() => parseVerilogModule((Data.VerilogModuleInstance)textFile));
                }
                else if (textFile is Data.VerilogFile)
                {
                    await Task.Run(() => parseVerilogModule((Data.VerilogFile)textFile));
                }
                else if(textFile is Data.InterfaceInstance)
                {
                    await Task.Run(() => parseVerilogModule((Data.InterfaceInstance)textFile));
                }
            }

            return;
        }

        private static async Task parseVerilogModule(Data.IVerilogRelatedFile verilogFile)
        {
            CodeEditor2.Controller.AppendLog("parseHier : " + verilogFile.ID);

            var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse);
            if (parser == null) return;

            parser.Parse();
            List<Item> items = new List<Item>();
            if (parser.ParsedDocument != null)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                    verilogFile.Update();
                    lock (verilogFile.Items)
                    {
                        foreach (var item in verilogFile.Items.Values)
                        {
                            items.Add(item);
                        }
                    }
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(
                        async () =>
                        {
                            await Task.Run(
                                () =>
                                {
                                    verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                                    verilogFile.Update();
                                    lock (verilogFile.Items)
                                    {
                                        foreach (var item in verilogFile.Items.Values)
                                        {
                                            items.Add(item);
                                        }
                                    }
                                }
                            );
                        }
                    );
                }
            }
            foreach (var item in items)
            {
                if(item is CodeEditor2.Data.TextFile)
                {
                    await Parse((CodeEditor2.Data.TextFile)item);
                }
            }
        }


   }
}
