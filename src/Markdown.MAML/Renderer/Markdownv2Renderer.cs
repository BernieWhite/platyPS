using Markdown.MAML.Model.MAML;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Parser;
using Markdown.MAML.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Markdown.MAML.Renderer
{
    /// <summary>
    /// Renders MamlModel as markdown with schema v 2.0.0
    /// </summary>
    internal sealed class MarkdownV2Renderer
    {
        private const int COMMAND_NAME_HEADING_LEVEL = 1;
        private const int COMMAND_ENTRIES_HEADING_LEVEL = 2;
        private const int PARAMETER_NAME_HEADING_LEVEL = 3;
        private const int INPUT_OUTPUT_TYPENAME_HEADING_LEVEL = 3;
        private const int EXAMPLE_HEADING_LEVEL = 3;
        private const int PARAMETERSET_NAME_HEADING_LEVEL = 3;
        public static readonly string ALL_PARAM_SETS_MONIKER = "(All)";

        public static Lazy<Regex> ExampleTitle = new Lazy<Regex>(() => new Regex(@"^(-| ){0,}(?<title>([^\f\n\r\t\v\x85\p{Z}-][^\f\n\r\t\v\x85]+[^\f\n\r\t\v\x85\p{Z}-]))(-| ){0,}$", RegexOptions.Compiled));

        private ParserMode _Mode;
        private StreamWriter _Writer;
        private char[] _WriteBuffer = new char[104];

        public int MaxSyntaxWidth { get; private set; }

        private const string NewLine = "\r\n";
        private const string LineBreak = "\r\n\r\n";
        private const string TripleBacktick = "```";
        private const char Space = ' ';
        private const char Colon = ':';
        private const char Blackslash = '\\';
        private const string ArraySeperator = ", ";
        private const string TripleDash = "---";

        /// <summary>
        /// 110 is a good width value, because it doesn't cause github to add horizontal scroll bar
        /// </summary>
        public const int DEFAULT_SYNTAX_WIDTH = 110;

        public MarkdownV2Renderer(ParserMode mode) : this(mode, DEFAULT_SYNTAX_WIDTH) { }

        public MarkdownV2Renderer(ParserMode mode, int maxSyntaxWidth)
        {
            MaxSyntaxWidth = maxSyntaxWidth;
            _Mode = mode;
        }

        public string MamlModelToString(MamlCommand mamlCommand, bool skipYamlHeader)
        {
            return MamlModelToString(mamlCommand, null, skipYamlHeader);
        }

        public string MamlModelToString(MamlCommand mamlCommand, Hashtable yamlHeader)
        {
            return MamlModelToString(mamlCommand, yamlHeader, false);
        }

        private string MamlModelToString(MamlCommand mamlCommand, Hashtable yamlHeader, bool skipYamlHeader)
        {
            using (var stream = new MemoryStream())
            {
                using (_Writer = new StreamWriter(stream, Encoding.UTF8, 100, true))
                {
                    // Add front matter metadata
                    if (!skipYamlHeader)
                    {
                        mamlCommand.SetMetadata("schema", "2.0.0");

                        AddYamlHeader(mamlCommand);
                    }

                    // Process the command
                    AddCommand(mamlCommand);
                }

                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Process metadata.
        /// </summary>
        private void AddYamlHeader(MamlCommand command)
        {
            WriteRaw(TripleDash);
            WriteRaw(NewLine);

            // Use a sorted dictionary to force the metadata into alphabetical order by key for consistency.
            var sortedHeader = new SortedDictionary<string, string>(command.Metadata, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in sortedHeader)
            {
                AppendYamlKeyValue(pair.Key, pair.Value);
            }

            WriteRaw(TripleDash);
            WriteRaw(LineBreak);
        }

        /// <summary>
        /// Process a MAML command.
        /// </summary>
        private void AddCommand(MamlCommand command)
        {
            AddHeader(COMMAND_NAME_HEADING_LEVEL, command.Name);
            AddEntryHeaderWithText(MarkdownStrings.SYNOPSIS, command.Synopsis);
            AddSyntax(command);
            AddEntryHeaderWithText(MarkdownStrings.DESCRIPTION, command.Description);
            AddExamples(command);
            AddParameters(command);
            AddInputs(command);
            AddOutputs(command);
            AddEntryHeaderWithText(MarkdownStrings.NOTES, command.Notes);
            AddLinks(command);
        }

        /// <summary>
        /// Process links.
        /// </summary>
        private void AddLinks(MamlCommand command)
        {
            var extraNewLine = command.Links != null && command.Links.Count > 0;

            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.RELATED_LINKS, extraNewLine);
            foreach (var link in command.Links)
            {
                Link(link);
            }
        }

        private void Link(MamlLink link)
        {
            if (link.IsSimplifiedTextLink)
            {
                WriteRaw(link.LinkName);
            }
            else
            {
                var name = link.LinkName;
                if (string.IsNullOrEmpty(name))
                {
                    // we need a valid name to produce a valid markdown
                    name = link.LinkUri;
                }

                WriteRaw('[');
                WriteRaw(name);
                WriteRaw("](");
                WriteRaw(link.LinkUri);
                WriteRaw(')');
                WriteRaw(LineBreak);
            }
        }

        private void AddInputOutput(MamlInputOutput io)
        {
            if (string.IsNullOrEmpty(io.TypeName) && string.IsNullOrEmpty(io.Description))
            {
                // in this case ignore
                return;
            }

            var extraNewLine = string.IsNullOrEmpty(io.Description) || ShouldBreak(io.FormatOption);
            AddHeader(INPUT_OUTPUT_TYPENAME_HEADING_LEVEL, io.TypeName, extraNewLine);
            AddParagraphs(io.Description);
        }

        private void AddOutputs(MamlCommand command)
        {
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.OUTPUTS);
            foreach (var io in command.Outputs)
            {
                AddInputOutput(io);
            }
        }

        private void AddInputs(MamlCommand command)
        {
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.INPUTS);
            foreach (var io in command.Inputs)
            {
                AddInputOutput(io);
            }
        }

        private void AddParameters(MamlCommand command)
        {
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.PARAMETERS);
            foreach (var param in command.Parameters)
            {
                AddParameter(param, command);
            }

            if (command.IsWorkflow)
            {
                AddWorkflowParameters();
            }

            // Workflows always support CommonParameters
            if (command.SupportCommonParameters || command.IsWorkflow)
            {
                AddCommonParameters();
            }
        }

        private void AddCommonParameters()
        {
            AddHeader(PARAMETERSET_NAME_HEADING_LEVEL, MarkdownStrings.CommonParametersToken, extraNewLine: false);
            AddParagraphs(MarkdownStrings.CommonParametersText);
        }

        private void AddWorkflowParameters()
        {
            AddHeader(PARAMETERSET_NAME_HEADING_LEVEL, MarkdownStrings.WorkflowParametersToken, extraNewLine: false);
            AddParagraphs(MarkdownStrings.WorkflowParametersText);
        }

        private Dictionary<string, MamlParameter> GetParamSetDictionary(string parameterName, IEnumerable<MamlSyntax> syntaxes)
        {
            var result = new Dictionary<string, MamlParameter>();

            foreach (var syntax in syntaxes)
            {
                foreach (var param in syntax.Parameters)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(parameterName, param.Name))
                    {
                        if (string.IsNullOrEmpty(syntax.ParameterSetName))
                        {
                            // Note (vors) : I guess that means it's applicable to all parameter sets,
                            // but it's hard to tell anymore...
                            result[ALL_PARAM_SETS_MONIKER] = param;
                        }
                        else
                        {
                            result[syntax.ParameterSetName] = param;
                        }
                        // there could be only one parameter in the param set with the same name
                        break;
                    }
                }
            }
            return result;
        }

        private List<Tuple<List<string>, MamlParameter>> SimplifyParamSets(Dictionary<string, MamlParameter> parameterMap)
        {
            var res = new List<Tuple<List<string>, MamlParameter>>();
            // using a O(n^2) algorithm, because it's simpler and n is very small.
            foreach (var pair in parameterMap)
            {
                var seekValue = pair.Value;
                var paramSetName = pair.Key;
                bool found = false;
                foreach (var tuple in res)
                {
                    if (tuple.Item2.IsMetadataEqual(seekValue))
                    {
                        tuple.Item1.Add(paramSetName);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // create a new entry
                    var paramSets = new List<string>();
                    paramSets.Add(paramSetName);
                    res.Add(new Tuple<List<string>, MamlParameter>(paramSets, seekValue));
                }
            }

            return res;
        }

        private bool ShouldBreak(SectionFormatOption formatOption)
        {
            return formatOption.HasFlag(SectionFormatOption.LineBreakAfterHeader);
        }

        private void AddParameter(MamlParameter parameter, MamlCommand command)
        {
            var extraNewLine = ShouldBreak(parameter.FormatOption);

            AddHeader(PARAMETERSET_NAME_HEADING_LEVEL, '-' + parameter.Name, extraNewLine: extraNewLine);

            // for some reason, in the update mode parameters produces extra newline.
            AddParagraphs(parameter.Description);

            var sets = SimplifyParamSets(GetParamSetDictionary(parameter.Name, command.Syntax));

            foreach (var set in sets)
            {
                WriteRaw("```yaml");
                WriteRaw(NewLine);

                AppendYamlKeyValue(MarkdownStrings.Type, parameter.Type);

                if (command.Syntax.Count == 1 || set.Item1.Count == command.Syntax.Count)
                {
                    // ignore, if there is just one parameter set
                    // or this parameter belongs to All parameter sets, use (All)
                    AppendYamlKeyValue(MarkdownStrings.Parameter_Sets, ALL_PARAM_SETS_MONIKER);
                }
                else
                {
                    AppendYamlKeyValue(MarkdownStrings.Parameter_Sets, set.Item1.ToArray());
                }

                AppendYamlKeyValue(MarkdownStrings.Aliases, parameter.Aliases);

                if (parameter.ParameterValueGroup.Count > 0)
                {
                    AppendYamlKeyValue(MarkdownStrings.Accepted_values, parameter.ParameterValueGroup.ToArray());
                }

                if (parameter.Applicable != null)
                {
                    AppendYamlKeyValue(MarkdownStrings.Applicable, parameter.Applicable);
                }

                WriteRaw(NewLine);

                AppendYamlKeyValue(MarkdownStrings.Required, set.Item2.Required.ToString());
                AppendYamlKeyValue(MarkdownStrings.Position, set.Item2.IsNamed() ? "Named" : set.Item2.Position);
                AppendYamlKeyValue(MarkdownStrings.Default_value, string.IsNullOrWhiteSpace(parameter.DefaultValue) ? "None" : parameter.DefaultValue);
                AppendYamlKeyValue(MarkdownStrings.Accept_pipeline_input, parameter.PipelineInput);
                AppendYamlKeyValue(MarkdownStrings.Accept_wildcard_characters, parameter.Globbing.ToString());

                WriteRaw(TripleBacktick);
                WriteRaw(LineBreak);
            }
        }

        /// <summary>
        /// Append a YAML entry. i.e. "key: value" or "key: value, value"
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">One or more values.</param>
        private void AppendYamlKeyValue(string key, params string[] value)
        {
            WriteRaw(key);
            WriteRaw(Colon);

            if (value != null)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    // Seperate entries by ", "
                    if (i > 0)
                    {
                        WriteRaw(ArraySeperator);
                    }
                    // Don't add a seperating space if the value is null
                    else if (!string.IsNullOrEmpty(value[i]))
                    {
                        WriteRaw(Space);
                    }

                    WriteRaw(value[i]);
                }
            }

            WriteRaw(NewLine);
        }

        private void AddExamples(MamlCommand command)
        {
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.EXAMPLES);

            foreach (var example in command.Examples)
            {
                var extraNewLine = ShouldBreak(example.FormatOption);

                AddHeader(EXAMPLE_HEADING_LEVEL, GetExampleTitle(example.Title), extraNewLine: extraNewLine);

                if (!string.IsNullOrEmpty(example.Introduction))
                {
                    AddParagraphs(example.Introduction);
                }

                if (example.Code != null)
                {
                    for (var i = 0; i < example.Code.Length; i++)
                    {
                        AddCodeSnippet(example.Code[i].Text, example.Code[i].LanguageMoniker);
                    }
                }

                if (!string.IsNullOrEmpty(example.Remarks))
                {
                    AddParagraphs(example.Remarks);
                }
            }
        }

        private static string GetExampleTitle(string title)
        {
            var match = ExampleTitle.Value.Match(title);

            if (match.Success)
            {
                return match.Groups["title"].Value;
            }

            return title;
        }

        public static string GetSyntaxString(MamlCommand command, MamlSyntax syntax)
        {
            return GetSyntaxString(command, syntax, DEFAULT_SYNTAX_WIDTH);
        }

        public static string GetSyntaxString(MamlCommand command, MamlSyntax syntax, int maxSyntaxWidth)
        {
            // TODO: we may want to add ParameterValueGroup info here,
            // but it's fine for now

            var sb = new StringBuilder();
            sb.Append(command.Name);

            var paramStrings = new List<string>();

            // first we create list of param string we want to add
            foreach (var param in syntax.Parameters)
            {
                string paramStr;
                if (param.IsSwitchParameter())
                {
                    paramStr = string.Format("[-{0}]", param.Name);
                }
                else
                {
                    paramStr = string.Format("-{0}", param.Name);
                    if (!param.IsNamed())
                    {
                        // for positional parameters, we can avoid specifying the name
                        paramStr = string.Format("[{0}]", paramStr);
                    }

                    paramStr = string.Format("{0} <{1}>", paramStr, param.Type);
                    if (!param.Required)
                    {
                        paramStr = string.Format("[{0}]", paramStr);
                    }
                }
                paramStrings.Add(paramStr);
            }

            if (command.IsWorkflow)
            {
                paramStrings.Add("[<" + MarkdownStrings.WorkflowParametersToken + ">]");
            }

            if (command.SupportCommonParameters)
            {
                paramStrings.Add("[<" + MarkdownStrings.CommonParametersToken + ">]");
            }

            // then we format them properly with repsect to max width for window.
            int widthBeforeLastBreak = 0;
            foreach (string paramStr in paramStrings) {

                if (sb.Length - widthBeforeLastBreak + paramStr.Length > maxSyntaxWidth)
                {
                    sb.AppendLine();
                    widthBeforeLastBreak = sb.Length;
                }

                sb.AppendFormat(" {0}", paramStr);
            }

            return sb.ToString();
        }

        private void AddSyntax(MamlCommand command)
        {
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, MarkdownStrings.SYNTAX);
            foreach (var syntax in command.Syntax)
            {
                if (command.Syntax.Count > 1)
                {
                    AddHeader(PARAMETERSET_NAME_HEADING_LEVEL, string.Format("{0}{1}", syntax.ParameterSetName, syntax.IsDefault ? MarkdownStrings.DefaultParameterSetModifier : null), extraNewLine: false);
                }

                AddCodeSnippet(GetSyntaxString(command, syntax));
            }
        }

        private void AddEntryHeaderWithText(string header, SectionBody body)
        {
            var extraNewLine = body == null || string.IsNullOrEmpty(body.Text) || ShouldBreak(body.FormatOption);

            // Add header
            AddHeader(COMMAND_ENTRIES_HEADING_LEVEL, header, extraNewLine: extraNewLine);

            // to correctly handle empty text case, we are adding new-line here
            if (body != null && !string.IsNullOrEmpty(body.Text))
            {
                AddParagraphs(body.Text);
            }
        }

        private void AddCodeSnippet(string code, string lang = "")
        {
            WriteRaw(TripleBacktick);
            WriteRaw(lang);
            WriteRaw(NewLine);
            WriteRaw(code);
            WriteRaw(NewLine);
            WriteRaw(TripleBacktick);
            WriteRaw(LineBreak);
        }

        private void AddHeader(int level, string header, bool extraNewLine = true)
        {
            WriteRaw("".PadLeft(level, '#'));
            WriteRaw(Space);
            WriteRaw(header);

            WriteRaw(NewLine);

            if (extraNewLine)
            {
                WriteRaw(NewLine);
            }
        }

        private string GetAutoWrappingForNonListLine(string line)
        {
            return Regex.Replace(line, @"([\.\!\?]) ( )*([^\r\n])", "$1$2\r\n$3");
        }

        private string GetAutoWrappingForMarkdown(string[] lines)
        {
            // this is an implementation of https://github.com/PowerShell/platyPS/issues/93

            // algorithm: identify chunks that represent lists
            // Every entry in a list should be preserved as is and 1 EOL between them
            // Every entry not in a list should be split with GetAutoWrappingForNonListLine
            // delimiters between lists and non-lists are 2 EOLs.

            var sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(NewLine);
                }

                if (HasListPrefix(lines[i]))
                {
                    if (i > 0 && !HasListPrefix(lines[i - 1]))
                    {
                        // we are in a list and it just started
                        sb.Append(NewLine);
                        sb.Append(lines[i]);
                    }
                    else
                    {
                        sb.Append(lines[i]);
                    }
                }
                else
                {
                    if (i > 0)
                    {
                        // we are just finished a list
                        sb.Append(NewLine);
                        sb.Append(GetAutoWrappingForNonListLine(lines[i]));
                    }
                    else
                    {
                        sb.Append(GetAutoWrappingForNonListLine(lines[i]));
                    }
                }
            }

            return sb.ToString();
        }

        private void AddParagraphs(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            if (_Mode == ParserMode.FormattingPreserve)
            {
                WriteRaw(body);
            }
            else
            {
                string[] paragraphs = body.Split(new string[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);
                //body = GetAutoWrappingForMarkdown(paragraphs.Select(para => GetEscapedMarkdownText(para.Trim())).ToArray());
                body = GetAutoWrappingForMarkdown(paragraphs.Select(para => para.Trim()).ToArray());

                Write(body);
            }

            // The the body already ended in a line break don't add extra lines on to the end
            var noNewLines = body.EndsWith(NewLine);

            if (!noNewLines)
            {
                WriteRaw(LineBreak);
            }
        }

        private void WriteRaw(string text)
        {
            _Writer.Write(text);
        }

        private void WriteRaw(char c)
        {
            _Writer.Write(c);
        }

        private void Write(string text)
        {
            var bufferPos = 0;

            for (var inputPos = 0; inputPos < text.Length; inputPos++)
            {
                var c = text[inputPos];

                if (ShouldEscape(c))
                {
                    if (c == Blackslash)
                    {
                        if (inputPos + 1 < text.Length && ShouldEscapeSlash(text[inputPos + 1]))
                        {
                            _WriteBuffer[bufferPos++] = Blackslash;
                        }
                        // Check for \\
                        else if (text[inputPos + 1] == Blackslash)
                        {
                            _WriteBuffer[bufferPos++] = Blackslash;
                            _WriteBuffer[bufferPos++] = Blackslash;
                            _WriteBuffer[bufferPos++] = Blackslash;

                            inputPos++;
                        }
                    }
                    else
                    {
                        _WriteBuffer[bufferPos++] = Blackslash;
                    }
                }

                if (IsSpace(c))
                {
                    _WriteBuffer[bufferPos++] = Space;
                }
                else if (IsDash(c))
                {
                    _WriteBuffer[bufferPos++] = '-';
                }
                else if (IsSingleQuote(c))
                {
                    _WriteBuffer[bufferPos++] = '\'';
                }
                else if (IsDoubleQuote(c))
                {
                    _WriteBuffer[bufferPos++] = '\"';
                }
                else
                {
                    _WriteBuffer[bufferPos++] = c;
                }

                if (bufferPos >= 100)
                {
                    _Writer.Write(_WriteBuffer, 0, bufferPos);
                    bufferPos = 0;
                }
            }

            if (bufferPos > 0)
            {
                _Writer.Write(_WriteBuffer, 0, bufferPos);
            }
        }

        private static bool ShouldEscape(char c)
        {
            // per https://github.com/PowerShell/platyPS/issues/121 we don't perform escaping for () in markdown renderer, but we do in the parser
            return c == Blackslash || c == '<' || c == '>' || c == '[' || c == ']' || c == '`';
        }

        private static bool ShouldEscapeSlash(char c)
        {
            return c == '<' || c == '>' || c == '[' || c == ']' || c == '`' || c == '(' || c == ')';
        }

        private static bool IsSpace(char c)
        {
            return c == Space || c == '\u00a0' || c == '\uc2a0';
        }

        private static bool IsDash(char c)
        {
            return c == '-' || c == '\u05be' || c == '\u1806' || c == '\u2010' || c == '\u2011' || c == '\u00AD' || c == '\u2012' || c == '\u2013' || c == '\u2014' || c == '\u2015' || c == '\u2212';
        }

        private static bool IsSingleQuote(char c)
        {
            return c == '\'' || c == '\u2018' || c == '\u2019' || c == '\u201b';
        }

        private static bool IsDoubleQuote(char c)
        {
            return c == '\"' || c == '\u201c' || c == '\u201d' || c == '\u201e' || c == '\u201f';
        }

        private static bool HasListPrefix(string s)
        {
            if (s.Length >= 2)
            {
                if (s[0] == '-' && s[1] == '-' ||
                    s[0] == '-' && s[1] == ' ' ||
                    s[0] == '*' && s[1] == ' ')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
