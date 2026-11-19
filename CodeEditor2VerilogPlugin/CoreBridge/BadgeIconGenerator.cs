using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Composites the NavigatePanel-style node badges into the VS Code tree
    /// icons, from C# so it can run in the same step that writes
    /// <c>package.json</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The editor draws two overlays on top of the file icon
    /// (<c>VerilogFileNode.GetIcon</c>): an exclamation triangle at the
    /// bottom-left when the parsed document has errors (red) or warnings
    /// (yellow), and a shine at the top-right when the document has unsaved
    /// edits. VS Code's <c>TreeItem.iconPath</c> carries ONE icon and one theme
    /// colour and cannot draw an overlay, so the overlay is baked into the SVG:
    /// every base icon gets one file per badge
    /// (<c>{icon}-{badge}.svg</c> / <c>{icon}-{badge}-dark.svg</c>).
    /// </para>
    /// <para>
    /// This mirrors <c>RtlEditorVscodeExtension/scripts/generate-badge-icons.js</c>
    /// exactly (same paths, geometry and colours). It exists in C# because the
    /// manifest writer is the natural place to refresh everything the extension
    /// consumes from C# - and it must not require node.
    /// </para>
    /// </remarks>
    public static class BadgeIconGenerator
    {
        /// <summary>
        /// Badge names the server can put on a node
        /// (<c>ResolveItemBadge</c> / <c>BuildFileNodes</c>).
        /// </summary>
        public static readonly string[] BadgeNames = { "error", "warning", "dirty" };

        /// <summary>
        /// Base icon names that get badge variants. Folder / project are
        /// deliberately absent: the editor's <c>FolderNode</c> /
        /// <c>ProjectNode</c> never draw the error / warning / dirty overlay, so
        /// those icons have no badge variants.
        /// </summary>
        public static readonly string[] BadgeableIcons =
        {
            "module",
            "interface",
            "package",
            "header",
            "systemverilog-document",
            "verilog-document",
            "verilog-header-document",
        };

        /// <summary>Badge colours, matching the Messages pane / Problems panel.</summary>
        private static readonly Dictionary<string, (string Fill, bool TopRight)> badges =
            new(StringComparer.Ordinal)
            {
                ["error"] = ("#f14c4c", false),
                ["warning"] = ("#cca700", false),
                ["dirty"] = ("#ffd24a", true),
            };

        /// <summary>
        /// Badge geometry inside the 512x512 viewBox: the badge fills the
        /// bottom-left / top-right quarter, overlapping the base icon's corner so
        /// it reads as an overlay (the editor uses
        /// <c>OverridePosition.DownLeft</c> / <c>UpRight</c>).
        /// </summary>
        private const int BadgeStart = 288; // 512 - 224

        private const double BadgeScale = 224.0 / 512.0;

        private const string ExclamationPath =
            "M505.095,407.125L300.77,53.208c-9.206-15.944-26.361-25.849-44.774-25.849" +
            "c-18.412,0-35.552,9.905-44.751,25.849L6.905,407.109c-9.206,15.944-9.206,35.746,0,51.69" +
            "c9.206,15.944,26.354,25.842,44.758,25.842h408.674c18.405,0,35.568-9.897,44.759-25.842" +
            "C514.302,442.855,514.302,423.053,505.095,407.125z M256.004,426.437" +
            "c-17.668,0-32.013-14.33-32.013-32.004c0-17.668,14.345-31.997,32.013-31.997" +
            "c17.667,0,31.997,14.329,31.997,31.997C288.001,412.108,273.671,426.437,256.004,426.437z" +
            "M275.72,324.011c0,10.89-8.834,19.709-19.716,19.709c-10.898,0-19.717-8.818-19.717-19.709" +
            "l-12.296-144.724c0-17.676,14.345-32.005,32.013-32.005c17.667,0,31.997,14.33,31.997,32.005" +
            "L275.72,324.011z";

        private const string ShinePath =
            "M495.469,241.969c-113.594,0-152.875-28.5-174.906-50.531" +
            "c-22.031-22.125-50.578-61.344-50.578-174.922c0-4.328-0.453-16.516-14.016-16.516" +
            "C242.531,0,242,12.188,242,16.516c0,113.578-28.563,152.797-50.594,174.922" +
            "c-22.094,22.031-61.375,50.531-174.906,50.531c-4.344,0-16.5,0.5-16.5,14.047" +
            "c0,13.453,12.156,13.938,16.5,13.938c113.531,0,152.813,28.578,174.906,50.625" +
            "C213.438,342.625,242,381.922,242,495.5c0,4.344,0.531,16.5,13.969,16.5" +
            "c13.563,0,14.016-12.156,14.016-16.5c0-113.578,28.547-152.875,50.578-174.922" +
            "c22.031-22.078,61.313-50.625,174.906-50.625c4.328,0,16.531-0.422,16.531-13.984" +
            "C512,242.516,499.797,241.969,495.469,241.969z";

        /// <summary>
        /// Regenerates every badge variant under <paramref name="iconsDirectory"/>.
        /// </summary>
        /// <param name="iconsDirectory">The extension's <c>icons/</c> folder.</param>
        /// <param name="write">
        /// <c>true</c> to write the files, <c>false</c> to only report what is
        /// out of date.
        /// </param>
        /// <returns>How many files were written (or would be).</returns>
        /// <remarks>
        /// A missing base icon is skipped with a warning rather than failing:
        /// the caller may be running on a checkout that has not added a new icon
        /// yet, and a missing badge variant only degrades that one icon.
        /// </remarks>
        public static BadgeIconResult Generate(string iconsDirectory, bool write)
        {
            BadgeIconResult result = new BadgeIconResult();
            if (string.IsNullOrEmpty(iconsDirectory) || !Directory.Exists(iconsDirectory))
            {
                result.Message = "icons directory not found: " + iconsDirectory;
                result.Failed = true;
                return result;
            }

            foreach (string baseName in BadgeableIcons)
            {
                foreach (string variant in new[] { string.Empty, "-dark" })
                {
                    string baseFile = Path.Combine(iconsDirectory, baseName + variant + ".svg");
                    if (!File.Exists(baseFile))
                    {
                        result.Skipped.Add(baseName + variant + ".svg");
                        continue;
                    }

                    string paths;
                    try
                    {
                        paths = ExtractPaths(File.ReadAllText(baseFile));
                    }
                    catch (Exception ex)
                    {
                        result.Message = "could not read " + baseFile + ": " + ex.Message;
                        result.Failed = true;
                        return result;
                    }

                    foreach (string badgeName in BadgeNames)
                    {
                        (string fill, bool topRight) = badges[badgeName];
                        string target = Path.Combine(
                            iconsDirectory, baseName + "-" + badgeName + variant + ".svg");
                        string content = Compose(paths, fill, topRight);

                        if (File.Exists(target))
                        {
                            string current = File.ReadAllText(target);
                            // Compare on CONTENT: the committed assets are an
                            // inconsistent mix of LF / CRLF / mixed (they were
                            // written by several tools over time), so a byte
                            // comparison would report all of them as stale
                            // forever. The existing file's own endings are
                            // re-applied only when actually writing.
                            if (string.Equals(NormalizeEol(current), NormalizeEol(content),
                                    StringComparison.Ordinal))
                            {
                                continue;
                            }
                            content = MatchLineEndings(content, current);
                        }

                        result.Changed.Add(Path.GetFileName(target));
                        if (write)
                        {
                            File.WriteAllText(target, content);
                            result.Written++;
                        }
                    }
                }
            }

            result.Message = write
                ? "wrote " + result.Written + " badge icon(s)"
                : (result.Changed.Count == 0
                    ? "badge icons are up to date"
                    : result.Changed.Count + " badge icon(s) out of date");
            return result;
        }

        /// <summary>
        /// Extracts the <c>&lt;path&gt;</c> elements of a base icon, dropping the
        /// XML prolog and the <c>&lt;svg&gt;</c> wrapper.
        /// </summary>
        private static string ExtractPaths(string svg)
        {
            MatchCollection matches = Regex.Matches(svg, @"<path\b[^>]*/>");
            if (matches.Count == 0) throw new InvalidOperationException("no <path> element found");

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < matches.Count; i++)
            {
                if (i > 0) sb.Append('\n').Append("    ");
                sb.Append(matches[i].Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Wraps the base paths and the badge path into one 16x16 SVG, in the
        /// same shape the JavaScript generator produces so either can be run
        /// without producing a diff.
        /// </summary>
        private static string Compose(string basePathMarkup, string fill, bool topRight)
        {
            string badgePath = string.Equals(fill, badges["dirty"].Fill, StringComparison.Ordinal)
                ? ShinePath
                : ExclamationPath;
            int x = topRight ? BadgeStart : 0;
            int y = topRight ? 0 : BadgeStart;

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>\n");
            sb.Append("<svg width=\"16\" height=\"16\" viewBox=\"0 0 512 512\" version=\"1.1\" ");
            sb.Append("xmlns=\"http://www.w3.org/2000/svg\">\n");
            sb.Append("    ").Append(basePathMarkup).Append('\n');
            sb.Append("    <g transform=\"translate(").Append(x).Append(',')
              .Append(y).Append(") scale(").Append(FormatScale(BadgeScale)).Append(")\">\n");
            sb.Append("        <path d=\"").Append(badgePath)
              .Append("\" style=\"fill:").Append(fill).Append(";\"/>\n");
            sb.Append("    </g>\n");
            sb.Append("</svg>\n");
            return sb.ToString();
        }

        /// <summary>
        /// Formats the scale the way JavaScript's number-to-string does
        /// (<c>0.4375</c>), so the two generators produce identical bytes.
        /// </summary>
        private static string FormatScale(double value)
        {
            return value.ToString("0.0###########", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Re-applies the line endings of <paramref name="existing"/> to
        /// <paramref name="generated"/>.
        /// </summary>
        /// <remarks>
        /// The committed SVG assets are CRLF. Writing LF would rewrite all 42 of
        /// them on every run on Windows and make <c>--check</c> fail forever,
        /// which is exactly the "always out of date" trap the manifest writer
        /// had to avoid too.
        /// </remarks>
        private static string MatchLineEndings(string generated, string existing)
        {
            bool crlf = existing.Contains("\r\n", StringComparison.Ordinal);
            string normalized = NormalizeEol(generated);
            return crlf ? normalized.Replace("\n", "\r\n") : normalized;
        }

        /// <summary>Collapses CRLF / CR / LF to LF so contents can be compared.</summary>
        private static string NormalizeEol(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }

    /// <summary>Outcome of <see cref="BadgeIconGenerator.Generate"/>.</summary>
    public sealed class BadgeIconResult
    {
        /// <summary>Files written (0 when only checking).</summary>
        public int Written { get; set; }

        /// <summary>Files that differ from what the generator produces.</summary>
        public List<string> Changed { get; } = new List<string>();

        /// <summary>Base icons that were not present, so no variants were made.</summary>
        public List<string> Skipped { get; } = new List<string>();

        /// <summary>Human readable outcome.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>True when the generator could not run.</summary>
        public bool Failed { get; set; }
    }
}
