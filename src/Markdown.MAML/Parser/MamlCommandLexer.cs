using Markdown.MAML.Model.MAML;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Markdown.MAML.Parser
{
    /// <summary>
    /// A lexer that inteprets markdown as a maml command.
    /// </summary>
    internal sealed class MamlCommandLexer : MarkdownLexer
    {
        private const int COMMAND_NAME_HEADING_LEVEL = 1;
        private const int COMMAND_ENTRIES_HEADING_LEVEL = 2;
        private const int PARAMETER_NAME_HEADING_LEVEL = 3;
        private const int INPUT_OUTPUT_TYPENAME_HEADING_LEVEL = 3;
        private const int EXAMPLE_HEADING_LEVEL = 3;
        private const int PARAMETERSET_NAME_HEADING_LEVEL = 3;

        private const string SYNOPSIS = "SYNOPSIS";
        private const string SYNTAX = "SYNTAX";
        private const string DESCRIPTION = "DESCRIPTION";
        private const string EXAMPLES = "EXAMPLES";
        private const string PARAMETERS = "PARAMETERS";
        private const string INPUTS = "INPUTS";
        private const string OUTPUTS = "OUTPUTS";
        private const string NOTES = "NOTES";
        private const string RELATEDLINKS = "RELATED LINKS";

        public static readonly string ALL_PARAM_SETS_MONIKER = "(All)";

        private static readonly string[] LINE_BREAKS = new[] { "\r\n", "\n" };
        private static readonly char[] YAML_SEPARATORS = new[] { ':' };

        private readonly bool _PreserveFormatting;

        public MamlCommandLexer(bool preserveFomatting)
        {
            _PreserveFormatting = preserveFomatting;
        }

        public MamlCommand Process(TokenStream stream, string[] tags)
        {
            stream.MoveTo(0);

            // Look for yaml header

            var metadata = YamlHeader(stream);

            MamlCommand command = null;

            // Process sections

            while (!stream.EOF)
            {
                if (IsHeading(stream.Current, COMMAND_NAME_HEADING_LEVEL))
                {
                    command = new MamlCommand
                    {
                        Name = stream.Current.Text,
                        SupportCommonParameters = false,
                        Metadata = metadata
                    };
                }
                else if (command != null)
                {
                    if (IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL))
                    {
                        var matching = Synopsis(stream, command) ||
                            //Syntax(stream, command) ||
                            Description(stream, command) ||
                            Examples(stream, command) ||
                            Parameters(stream, command, tags) ||
                            Inputs(stream, command) ||
                            Outputs(stream, command) ||
                            Notes(stream, command) ||
                            RelatedLinks(stream, command);

                        if (matching)
                        {
                            continue;
                        }
                    }
                }

                // Skip the current token
                stream.Next();
            }

            return command;
        }

        private bool Synopsis(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, SYNOPSIS))
            {
                return false;
            }

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);

            stream.Next();

            command.Synopsis = SectionBody(stream, hasLineBreak);

            return true;
        }

        private bool Syntax(TokenStream stream, MamlCommand command)
        {
            if (!string.Equals(SYNTAX, stream.Current.Text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return false;
        }

        // Really just getting parameter set names for the purpose of determining the default
        private bool SyntaxEntry(TokenStream stream, MamlCommand command)
        {
            // grammar:
            // ### ParameterSetName 
            // ```
            // code
            // ```

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);
            var title = stream.Current.Text;

            stream.Next();

            // if header is omitted
            var syntax = new MamlSyntax()
            {
                ParameterSetName = ALL_PARAM_SETS_MONIKER,
                IsDefault = true
            };

            if (stream.IsTokenType(MarkdownTokenType.FencedBlock))
            {
                syntax.ParameterSetName = ALL_PARAM_SETS_MONIKER;
                syntax.IsDefault = true;
            }
            else if (!IsHeading(stream.Current, PARAMETERSET_NAME_HEADING_LEVEL))
            {
                return false;
            }

            bool isDefault = stream.Current.Text.EndsWith(MarkdownStrings.DefaultParameterSetModifier);

            syntax = new MamlSyntax()
            {
                ParameterSetName = isDefault ? stream.Current.Text.Substring(0, stream.Current.Text.Length - MarkdownStrings.DefaultParameterSetModifier.Length) : stream.Current.Text,
                IsDefault = isDefault
            };

            stream.Next();

            command.Syntax.Add(syntax);

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        private bool Description(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, DESCRIPTION))
            {
                return false;
            }

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);

            stream.Next();

            command.Description = SectionBody(stream, hasLineBreak);

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        /// <summary>
        /// Process examples.
        /// </summary>
        private bool Examples(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, EXAMPLES))
            {
                return false;
            }

            stream.Next();

            while (!stream.EOF && IsHeading(stream.Current, EXAMPLE_HEADING_LEVEL))
            {
                if (!Example(stream, command))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Example(TokenStream stream, MamlCommand command)
        {
            // grammar:
            // #### ExampleTitle
            // Introduction
            // ```
            // code
            // ```
            // Remarks

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);
            var title = stream.Current.Text;

            stream.Next();

            var example = new MamlExample
            {
                Title = title,
                FormatOption = hasLineBreak ? SectionFormatOption.LineBreakAfterHeader : SectionFormatOption.None,

                // TODO: Might contain fenced sections
                Introduction = SimpleTextSection(stream),

                Code = ExampleCodeBlock(stream),

                Remarks = SimpleTextSection(stream)
            };

            command.Examples.Add(example);

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        /// <summary>
        /// Process parameters.
        /// </summary>
        private bool Parameters(TokenStream stream, MamlCommand command, string[] tags)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, PARAMETERS))
            {
                return false;
            }

            stream.Next();

            while (!stream.EOF && IsHeading(stream.Current, PARAMETER_NAME_HEADING_LEVEL))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(stream.Current.Text, MarkdownStrings.CommonParametersToken))
                {
                    command.SupportCommonParameters = true;

                    stream.Next();
                    stream.SkipUntil(MarkdownTokenType.Header);
                    
                    continue;
                }

                if (!Parameter(stream, command, tags))
                {
                    return false;
                }
            }

            // Update syntaxes

            ParameterSets(command);

            return true;
        }

        private void ParameterSets(MamlCommand command)
        {
            MamlSyntax allSyntax = null;

            if (command.Syntax.Count > 1)
            {
                if (command.Syntax.ContainsKey("(All)"))
                {
                    allSyntax = command.Syntax["(All)"];

                    command.Syntax.Remove("(All)");
                }
            }
            //else
            //{
            //    if (command.Syntax.ContainsKey("(All)"))
            //    {
            //        allSyntax = command.Syntax["(All)"];

            //        allSyntax.ParameterSetName = null;
            //    }
            //}

            foreach (var syntax in command.Syntax)
            {
                if (allSyntax != null)
                {
                    syntax.Parameters.AddRange(allSyntax.Parameters);
                }

                // Sort parameters
                syntax.Parameters.Sort(SortParameter);
            }
        }

        private static int SortParameter(MamlParameter left, MamlParameter right)
        {
            if (string.IsNullOrEmpty(left.Position) && string.IsNullOrEmpty(right.Position))
            {
                return 0;
            }
            else if (string.IsNullOrEmpty(left.Position))
            {
                return -1;
            }
            else if (string.IsNullOrEmpty(right.Position))
            {
                return 1;
            }

            var compare = left.Position.CompareTo(right.Position);

            if (compare != 0)
            {
                return compare;
            }

            if (left.ParameterSet != right.ParameterSet)
            {
                if (left.ParameterSet.Contains("(All)"))
                {
                    return -1;
                }
                else if (right.ParameterSet.Contains("(All)"))
                {
                    return 1;
                }
            }

            return compare;
        }

        private bool Parameter(TokenStream stream, MamlCommand command, string[] tags)
        {
            // grammar:
            // #### -Name
            // Description              -  optional, there also could be codesnippets in the description
            //                             but no yaml codesnippets
            //
            // ```yaml                  -  one entry for every unique parameter metadata set
            // ...
            // ```

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);
            var name = stream.Current.Text;

            if (name.Length > 0 && name[0] == '-')
            {
                name = name.Substring(1);
            }

            stream.Next();

            var parameter = new MamlParameter
            {
                Name = name,
                FormatOption = hasLineBreak ? SectionFormatOption.LineBreakAfterHeader : SectionFormatOption.None,

                // TODO: Might contain fenced sections
                Description = SimpleTextSection(stream, includeNonYamlFencedBlocks: true),

                ParameterSet = new string[] { ALL_PARAM_SETS_MONIKER }
            };

            // we are filling up two pieces here: Syntax and Parameters
            // we are adding this parameter object to the parameters and later modifying it
            // in the rare case, when there are multiply yaml snippets,
            // the first one should be present in the resulted maml in the Parameters section
            // (all of them would be present in Syntax entry)
            //var parameterSetMap = new Dictionary<string, MamlParameter>(StringComparer.OrdinalIgnoreCase);

            //if (StringComparer.OrdinalIgnoreCase.Equals(parameter.Name, MarkdownStrings.CommonParametersToken))
            //{
            //    // ignore text body
            //    commmand.SupportCommonParameters = true;
            //    return true;
            //}

            //if (StringComparer.OrdinalIgnoreCase.Equals(parameter.Name, MarkdownStrings.WorkflowParametersToken))
            //{
            //    // ignore text body
            //    commmand.IsWorkflow = true;
            //    return true;
            //}

            parameter.ValueRequired = parameter.IsSwitchParameter() ? false : true;

            var addedParameter = false;

            while (stream.IsTokenType(MarkdownTokenType.FencedBlock))
            {
                if (stream.Current.Meta == "yaml")
                {
                    var yaml = ParseYamlKeyValuePairs(stream.Current.Text);

                    // TODO: Validate keys
                    //if (!IsKnownKey(pair.Key))
                    //{
                    //    throw new HelpSchemaException(yamlSnippet.SourceExtent, "Invalid yaml: unknown key " + pair.Key);
                    //}

                    ParameterYaml(yaml, parameter);
                }

                parameter.ValueRequired = parameter.IsSwitchParameter() ? false : true;

                stream.Next();

                if (stream.IsTokenType(MarkdownTokenType.FencedBlock))
                {
                    if (parameter.IsApplicable(tags))
                    {
                        AddSyntax(command, parameter);

                        if (!addedParameter)
                        {
                            command.Parameters.Add(parameter);
                            addedParameter = true;
                        }
                    }
                    
                    parameter = parameter.Clone();
                }
            }

            if (parameter.IsApplicable(tags))
            {
                AddSyntax(command, parameter);

                if (!addedParameter)
                {
                    command.Parameters.Add(parameter);
                    addedParameter = true;
                }
            }

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        private static void AddSyntax(MamlCommand command, MamlParameter parameter)
        {
            foreach (var parameterSet in parameter.ParameterSet)
            {
                MamlSyntax syntax = null;

                if (!command.Syntax.ContainsKey(parameterSet))
                {
                    syntax = new MamlSyntax
                    {
                        ParameterSetName = parameterSet
                    };

                    command.Syntax.Add(syntax);
                }
                else
                {
                    syntax = command.Syntax[parameterSet];
                }

                syntax.Parameters.Add(parameter);
            }
        }

        private bool Inputs(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, INPUTS))
            {
                return false;
            }

            stream.Next();

            while (!stream.EOF && IsHeading(stream.Current, INPUT_OUTPUT_TYPENAME_HEADING_LEVEL))
            {
                var typeEntity = InputOutput(stream);

                if (typeEntity == null)
                {
                    return false;
                }

                command.Inputs.Add(typeEntity);
            }

            return true;
        }

        private bool Outputs(TokenStream stream, MamlCommand command)
        {
            if (!string.Equals(OUTPUTS, stream.Current.Text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            stream.Next();

            while (!stream.EOF && IsHeading(stream.Current, INPUT_OUTPUT_TYPENAME_HEADING_LEVEL))
            {
                var typeEntity = InputOutput(stream);

                if (typeEntity == null)
                {
                    return false;
                }

                command.Outputs.Add(typeEntity);
            }

            return true;
        }

        private bool Notes(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, NOTES))
            {
                return false;
            }

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);

            stream.Next();

            command.Notes = SectionBody(stream, hasLineBreak);

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        private bool RelatedLinks(TokenStream stream, MamlCommand command)
        {
            if (!IsHeading(stream.Current, COMMAND_ENTRIES_HEADING_LEVEL, RELATEDLINKS))
            {
                return false;
            }

            stream.Next();

            while (stream.IsTokenType(MarkdownTokenType.Link, MarkdownTokenType.LinkReference, MarkdownTokenType.LineBreak))
            {
                if (stream.IsTokenType(MarkdownTokenType.LineBreak))
                {
                    stream.Next();

                    continue;
                }

                var link = new MamlLink
                {
                    LinkName = stream.Current.Meta,
                    LinkUri = stream.Current.Text
                };

                // Update link to point to resolved target
                if (stream.IsTokenType(MarkdownTokenType.LinkReference))
                {
                    var target = stream.ResolveLinkTarget(link.LinkUri);
                    link.LinkUri = target.Text;
                }

                command.Links.Add(link);

                stream.Next();
            }

            stream.SkipUntil(MarkdownTokenType.Header);

            return true;
        }

        private SectionBody SectionBody(TokenStream stream, bool hasLineBreak)
        {
            var text = SimpleTextSection(stream);

            return new SectionBody(text, hasLineBreak ? SectionFormatOption.LineBreakAfterHeader : SectionFormatOption.None);
        }

        private string SimpleTextSection(TokenStream stream, bool includeNonYamlFencedBlocks = false)
        {
            var sb = new StringBuilder();

            while (stream.IsTokenType(MarkdownTokenType.Text, MarkdownTokenType.Link, MarkdownTokenType.FencedBlock, MarkdownTokenType.LineBreak))
            {
                if (stream.IsTokenType(MarkdownTokenType.Text))
                {
                    AppendEnding(sb, stream.Peak(-1), _PreserveFormatting);
                    sb.Append(stream.Current.Text);
                }
                else if (stream.IsTokenType(MarkdownTokenType.Link))
                {
                    AppendEnding(sb, stream.Peak(-1), _PreserveFormatting);
                    sb.Append(stream.Current.Meta);

                    if (!string.IsNullOrEmpty(stream.Current.Text))
                    {
                        sb.AppendFormat(" ({0})", stream.Current.Text);
                    }
                }
                else if (stream.IsTokenType(MarkdownTokenType.LinkReference))
                {
                    AppendEnding(sb, stream.Peak(-1), _PreserveFormatting);

                    sb.Append(stream.Current.Meta);
                }
                else if (stream.IsTokenType(MarkdownTokenType.FencedBlock))
                {
                    // Only process fenced blocks if specified, and never process yaml blocks
                    if (!includeNonYamlFencedBlocks || string.Equals(stream.Current.Meta, "yaml", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    AppendEnding(sb, stream.Peak(-1), preserveEnding: true);
                    sb.Append(stream.Current.Text);
                }
                else if (stream.IsTokenType(MarkdownTokenType.LineBreak))
                {
                    AppendEnding(sb, stream.Peak(-1), _PreserveFormatting);
                }

                stream.Next();
            }

            if (stream.EOF && stream.Peak(-1).Flag.HasFlag(MarkdownTokenFlag.Preserve) && stream.Peak(-1).Flag.HasFlag(MarkdownTokenFlag.LineEnding))
            {
                AppendEnding(sb, stream.Peak(-1));
            }

            return sb.ToString();
        }

        private void AppendEnding(StringBuilder stringBuilder, MarkdownToken token, bool preserveEnding = false)
        {
            if (token == null || stringBuilder.Length == 0 || !token.Flag.IsEnding())
            {
                return;
            }

            if (!preserveEnding && token.Flag.ShouldPreserve())
            {
                preserveEnding = true;
            }

            if (token.IsDoubleLineEnding())
            {
                stringBuilder.Append(preserveEnding ? "\r\n\r\n" : "\r\n");
            }
            else if (token.IsSingleLineEnding())
            {
                stringBuilder.Append(preserveEnding ? "\r\n" : " ");
            }
        }

        private static MamlCodeBlock[] ExampleCodeBlock(TokenStream stream)
        {
            List<MamlCodeBlock> blocks = new List<MamlCodeBlock>();

            foreach (var token in stream.CaptureWhile(MarkdownTokenType.FencedBlock))
            {
                var block = new MamlCodeBlock(token.Text, token.Meta);

                blocks.Add(block);
            }

            return blocks.ToArray();
        }

        private MamlInputOutput InputOutput(TokenStream stream)
        {
            // grammar:
            // #### TypeName
            // Description

            var hasLineBreak = stream.Current.Flag.HasFlag(MarkdownTokenFlag.LineBreak);
            var typeName = stream.Current.Text;

            stream.Next();

            var typeEntity = new MamlInputOutput()
            {
                TypeName = typeName,
                Description = SimpleTextSection(stream),
                FormatOption = hasLineBreak ? SectionFormatOption.LineBreakAfterHeader : SectionFormatOption.None
            };

            return typeEntity;
        }

        private static void ParameterYaml(Dictionary<string, string> pairs, MamlParameter parameter)
        {
            // for all null keys, we should ignore the value in this context
            var newPairs = new Dictionary<string, string>(pairs.Comparer);

            foreach (var pair in pairs)
            {
                if (pair.Value != null)
                {
                    newPairs[pair.Key] = pair.Value;
                }
            }

            pairs = newPairs;

            string value;
            parameter.Type = pairs.TryGetValue(MarkdownStrings.Type, out value) ? value : null;
            parameter.Aliases = pairs.TryGetValue(MarkdownStrings.Aliases, out value) ? SplitByCommaAndTrim(value) : new string[0];
            parameter.ParameterValueGroup.AddRange(pairs.TryGetValue(MarkdownStrings.Accepted_values, out value) ? SplitByCommaAndTrim(value) : new string[0]);
            parameter.Required = pairs.TryGetValue(MarkdownStrings.Required, out value) ? StringComparer.OrdinalIgnoreCase.Equals("true", value) : false;
            parameter.Position = pairs.TryGetValue(MarkdownStrings.Position, out value) ? value : "named";
            parameter.DefaultValue = pairs.TryGetValue(MarkdownStrings.Default_value, out value) ? value : null;
            parameter.PipelineInput = pairs.TryGetValue(MarkdownStrings.Accept_pipeline_input, out value) ? value : "false";
            parameter.Globbing = pairs.TryGetValue(MarkdownStrings.Accept_wildcard_characters, out value) ? StringComparer.OrdinalIgnoreCase.Equals("true", value) : false;
            // having Applicable for the whole parameter is a little bit sloppy: ideally it should be per yaml entry.
            // but that will make the code super ugly and it's unlikely that these two features would need to be used together.
            parameter.Applicable = pairs.TryGetValue(MarkdownStrings.Applicable, out value) ? SplitByCommaAndTrim(value) : null;
            parameter.ParameterSet = pairs.TryGetValue(MarkdownStrings.Parameter_Sets, out value) ? SplitByCommaAndTrim(value) : new string[] { ALL_PARAM_SETS_MONIKER };
        }

        private static string[] SplitByCommaAndTrim(string input)
        {
            if (input == null)
            {
                return new string[0];
            }

            return input.Split(',').Select(x => x.Trim()).ToArray();
        }

        /// <summary>
        /// we only parse simple key-value pairs here
        /// </summary>
        /// <param name="yamlSnippet"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ParseYamlKeyValuePairs(string yamlSnippet)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string lineIterator in yamlSnippet.Split(LINE_BREAKS, StringSplitOptions.None))
            {
                var line = lineIterator.Trim();
                if (string.IsNullOrEmpty(line.Trim()))
                {
                    continue;
                }

                string[] parts = line.Split(YAML_SEPARATORS, 2);
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid yaml: expected simple key-value pairs");
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();
                // we treat empty value as null
                result[key] = string.IsNullOrEmpty(value) ? null : value;
            }

            return result;
        }
    }
}
