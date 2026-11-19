using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace pluginVerilog
{
    /// <summary>
    /// Severity settings of the parser rules.
    ///
    /// <para>
    /// Every rule has a fixed <see cref="Rule.Name"/> / <see cref="Rule.Message"/> /
    /// <see cref="Rule.Description"/> and a configurable
    /// <see cref="Rule.Severity"/>. A rule set can be written to a
    /// <c>.rule.yaml</c> file as a YAML list of "name / severity" pairs and
    /// loaded back, so the severity of each diagnostic can be tuned per
    /// directory: a <c>.rule.yaml</c> file applies to every source file below
    /// the directory it is placed in (see
    /// <see cref="ProjectProperty.GetRuleSet(string)"/>).
    /// </para>
    /// </summary>
    public class RuleSet
    {
        public Rule ImplicitNetDeclaretion = new Rule
        {
            Name = "ImplicitNetDeclaration",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit net declaration",
            Description = "Implicit net declaration. It is recommended to declare the net type explicitly."
        };

        public Rule AssignmentBitwidthMismatch = new Rule
        {
            Name = "AssignmentBitwidthMismatch",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Assignment bitwidth mismatch",
            Description = "The bitwidth of the left-hand side and right-hand side of an assignment do not match."
        };

        public Rule ImplicitModportDirection = new Rule
        {
            Name = "ImplicitModportDirection",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit Modport direction",
            Description = "The direction of modport declaration is missing. inout direction is used for this port."
        };

        public Rule ImplicitModportInterfaceConnectionToInstance = new Rule
        {
            Name = "ImplicitModportInterfaceConnectionToInstance",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit Modport connection",
            Description = "The implicit modport is connected to modport.use modport to connect to instances."
        };

        public Rule NotAllPortConnectedWithWildcardNamedPortConnections = new Rule
        {
            Name = "NotAllPortConnectedWithWildcardNamedPortConnections",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Not all ports connected with wildcard named port connections",
            Description = "Not all ports are connected with wildcard named port connections. It is recommended to connect all ports."
        };

        // ------------------------------------------------------------------
        // rule enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// All rule fields of this class, in declaration order.
        /// </summary>
        /// <remarks>
        /// Reflection is used so that adding a rule only requires adding the
        /// field above: the YAML writer, the YAML reader and the
        /// <c>.rule.yaml</c> template all pick it up automatically.
        /// </remarks>
        private static readonly FieldInfo[] ruleFields = typeof(RuleSet)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(Rule))
            .ToArray();

        /// <summary>
        /// Enumerates every rule of this set in declaration order.
        /// </summary>
        public IReadOnlyList<Rule> GetRules()
        {
            List<Rule> rules = new List<Rule>();
            foreach (FieldInfo field in ruleFields)
            {
                if (field.GetValue(this) is Rule rule) rules.Add(rule);
            }
            return rules;
        }

        /// <summary>
        /// Finds a rule by its <see cref="Rule.Name"/> (case insensitive).
        /// </summary>
        public Rule? GetRule(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (Rule rule in GetRules())
            {
                if (string.Equals(rule.Name, name, StringComparison.OrdinalIgnoreCase)) return rule;
            }
            return null;
        }

        /// <summary>
        /// Creates an independent copy of this rule set (the rule instances
        /// themselves are immutable, so they are shared).
        /// </summary>
        public RuleSet Clone()
        {
            RuleSet clone = new RuleSet();
            foreach (FieldInfo field in ruleFields)
            {
                if (field.GetValue(this) is Rule rule) field.SetValue(clone, rule);
            }
            return clone;
        }

        // ------------------------------------------------------------------
        // yaml
        // ------------------------------------------------------------------

        /// <summary>
        /// One "name / severity" pair of a <c>.rule.yaml</c> file.
        /// </summary>
        public class RuleSetting
        {
            public string Name { get; set; } = "";
            public string Severity { get; set; } = "";
        }

        /// <summary>
        /// Root object of a <c>.rule.yaml</c> file:
        /// <code>
        /// rules:
        ///   - name: ImplicitNetDeclaration
        ///     severity: Error
        /// </code>
        /// </summary>
        public class RuleSetYaml
        {
            public List<RuleSetting> Rules { get; set; } = new List<RuleSetting>();
        }

        private static DeserializerBuilder CreateDeserializerBuilder()
        {
            return new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties();
        }

        private static SerializerBuilder CreateSerializerBuilder()
        {
            return new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);
        }

        /// <summary>
        /// Serializes this rule set as the YAML body of a <c>.rule.yaml</c> file.
        /// </summary>
        public string ToYaml()
        {
            RuleSetYaml yaml = new RuleSetYaml();
            foreach (Rule rule in GetRules())
            {
                yaml.Rules.Add(new RuleSetting { Name = rule.Name, Severity = rule.Severity.ToString() });
            }
            return CreateSerializerBuilder().Build().Serialize(yaml);
        }

        /// <summary>
        /// Builds a rule set from the YAML body of a <c>.rule.yaml</c> file.
        /// </summary>
        /// <remarks>
        /// The returned set starts from the built-in defaults and only the
        /// severities listed in the file are overridden, so a <c>.rule.yaml</c>
        /// file may name a subset of the rules. Unknown rule names and
        /// unparsable severities are reported through
        /// <paramref name="errorMessage"/> (the remaining entries are still
        /// applied).
        /// </remarks>
        /// <param name="yamlText">the file content</param>
        /// <param name="errorMessage">null when the file is fully valid</param>
        /// <returns>the rule set, or null when the YAML itself is malformed</returns>
        public static RuleSet? ParseYaml(string? yamlText, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(yamlText)) return new RuleSet();

            RuleSetYaml? yaml;
            try
            {
                yaml = CreateDeserializerBuilder().Build().Deserialize<RuleSetYaml>(yamlText);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }

            RuleSet ruleSet = new RuleSet();
            if (yaml == null || yaml.Rules == null) return ruleSet;

            List<string> problems = new List<string>();
            ruleSet.ApplySettings(yaml.Rules, problems);
            if (problems.Count != 0) errorMessage = string.Join(" / ", problems);
            return ruleSet;
        }

        /// <summary>
        /// Overrides the severity of the rules named in <paramref name="settings"/>.
        /// </summary>
        /// <param name="problems">collects unknown rule names / invalid severities</param>
        public void ApplySettings(IEnumerable<RuleSetting> settings, List<string> problems)
        {
            foreach (RuleSetting setting in settings)
            {
                if (setting == null) continue;

                string name = (setting.Name ?? "").Trim();
                if (name.Length == 0)
                {
                    problems.Add("rule entry without a name");
                    continue;
                }

                FieldInfo? target = ruleFields.FirstOrDefault(
                    field => string.Equals((field.GetValue(this) as Rule)?.Name, name, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    problems.Add("unknown rule name : " + name);
                    continue;
                }

                if (!TryParseSeverity(setting.Severity, out Rule.SeverityEnum severity))
                {
                    problems.Add("unknown severity : " + setting.Severity + " (rule " + name + ")");
                    continue;
                }

                Rule current = (Rule)target.GetValue(this)!;
                target.SetValue(this, new Rule
                {
                    Name = current.Name,
                    Message = current.Message,
                    Description = current.Description,
                    Severity = severity
                });
            }
        }

        /// <summary>
        /// Parses a <see cref="Rule.SeverityEnum"/> name (case insensitive).
        /// </summary>
        public static bool TryParseSeverity(string? text, out Rule.SeverityEnum severity)
        {
            severity = Rule.SeverityEnum.Warning;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Enum.TryParse(text.Trim(), true, out severity) && Enum.IsDefined(typeof(Rule.SeverityEnum), severity);
        }

        // ------------------------------------------------------------------
        // file access
        // ------------------------------------------------------------------

        /// <summary>
        /// Loads a rule set from a <c>.rule.yaml</c> file. Returns null when the
        /// file does not exist or its YAML is malformed.
        /// </summary>
        public static RuleSet? LoadFile(string absolutePath, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                if (!File.Exists(absolutePath))
                {
                    errorMessage = "file not found : " + absolutePath;
                    return null;
                }
                return ParseYaml(File.ReadAllText(absolutePath), out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Loads a rule set from a <c>.rule.yaml</c> file, ignoring any error.
        /// </summary>
        public static RuleSet? LoadFile(string absolutePath)
        {
            return LoadFile(absolutePath, out _);
        }

        /// <summary>
        /// Writes this rule set to a <c>.rule.yaml</c> file as YAML
        /// (UTF-8, LF line endings).
        /// </summary>
        public void SaveFile(string absolutePath)
        {
            string? directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string yaml = ToYaml().Replace("\r\n", "\n");
            File.WriteAllText(absolutePath, yaml, new System.Text.UTF8Encoding(false));
        }
    }
}
