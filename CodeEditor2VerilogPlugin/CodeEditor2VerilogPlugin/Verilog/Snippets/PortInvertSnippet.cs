using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.PopupMenu;
using pluginVerilog.Verilog.DataObjects;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace pluginVerilog.Verilog.Snippets
{
    public class PortInvertSnippet : CodeEditor2.CodeEditor.PopupMenu.ToolItem
    {
        public PortInvertSnippet() : base("PortInvert")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        private static readonly Regex PortPattern = new Regex(
            @"^(?<direction>input|output)\s+" +
            @"(?:(?<qualifiers>signed|wire|reg|automatic)\s+)*" +
            @"(?:(?<type>\w+)\s+)?" +
            @"(?<bitwidth>\[[^\[\]]*\])?\s*" +
            @"(?<name>\w+)" +
            @"\s*[;,]?\s*" +
            @"(?://[^\r\n]*)?$",
            RegexOptions.Compiled
        );

        public override async System.Threading.Tasks.Task ApplyAsync()
        {
            CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (file == null) return;

            CodeEditor.CodeDocument? codeDocument = file.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return;

            // Get selected text range
            int selectionStart = codeDocument.SelectionStart;
            int selectionLast = codeDocument.SelectionLast;

            // Check if text is selected
            if (selectionStart == selectionLast)
            {
                CodeEditor2.Controller.AppendLog("PortInvert: Please select port definitions first", Avalonia.Media.Colors.Yellow);
                return;
            }

            // Get selected text
            string selectedText = codeDocument.CreateString(selectionStart, selectionLast - selectionStart);
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return;
            }

            // Process each line
            string[] lines = selectedText.Split('\n');
            var processedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmedLine = line;

                // Try to preserve indentation
                int indentLength = 0;
                foreach (char c in line)
                {
                    if (c == ' ' || c == '\t') indentLength++;
                    else break;
                }

                string trimmedContent = line.TrimStart(' ', '\t');
                Match match = PortPattern.Match(trimmedContent);

                if (match.Success)
                {
                    string direction = match.Groups["direction"].Value;
                    string qualifiers = match.Groups["qualifiers"].Value.Trim();
                    string bitwidth = match.Groups["bitwidth"].Value;
                    string name = match.Groups["name"].Value;

                    // Invert direction
                    string newDirection = (direction == "input") ? "output" : "input";

                    // Handle port name suffix
                    string newName = name;
                    if (name.EndsWith("_I"))
                    {
                        newName = name.Substring(0, name.Length - 2) + "_O";
                    }
                    else if (name.EndsWith("_O"))
                    {
                        newName = name.Substring(0, name.Length - 2) + "_I";
                    }

                    // Build the new line
                    string indent = line.Substring(0, indentLength);
                    string newLine = indent + newDirection + " ";

                    if (!string.IsNullOrEmpty(qualifiers))
                    {
                        newLine += qualifiers + " ";
                    }

                    newLine += bitwidth.Trim() + " " + newName;

                    // Preserve trailing separator (semicolon or comma)
                    string trimmedEnd = trimmedContent.TrimEnd(' ', '\t');
                    if (trimmedEnd.EndsWith(";"))
                    {
                        newLine += ";";
                    }
                    else if (trimmedEnd.EndsWith(","))
                    {
                        newLine += ",";
                    }

                    processedLines.Add(newLine);
                }
                else
                {
                    // Keep line as-is if it doesn't match the pattern
                    processedLines.Add(line);
                }
            }

            // Combine processed lines
            string resultText = string.Join("\n", processedLines);

            // Replace the selected text
            codeDocument.Replace(selectionStart, selectionLast - selectionStart, 0, resultText);

            CodeEditor2.Controller.CodeEditor.SetSelection(selectionStart, selectionStart + resultText.Length);
            CodeEditor2.Controller.CodeEditor.RequestReparsePost();
        }
    }
}
