using Markdown.MAML.Model.MAML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Markdown.MAML.Renderer
{
    /// <summary>
    /// This class contain logic to render maml from the Model.
    /// </summary>
    internal sealed class MamlRenderer
    {
        private static XNamespace mshNS = XNamespace.Get("http://msh");
        private static XNamespace mamlNS = XNamespace.Get("http://schemas.microsoft.com/maml/2004/10");
        private static XNamespace commandNS = XNamespace.Get("http://schemas.microsoft.com/maml/dev/command/2004/10");
        private static XNamespace devNS = XNamespace.Get("http://schemas.microsoft.com/maml/dev/2004/10");
        private static XNamespace msHelpNS = XNamespace.Get("http://msdn.microsoft.com/mshelp");

        private static XName mamlPara = XName.Get("para", mamlNS.NamespaceName);
        private static XName mamlName = XName.Get("name", mamlNS.NamespaceName);
        private static XName mamlDescription = XName.Get("description", mamlNS.NamespaceName);

        private const char Dash = '-';
        private const char Space = ' ';
        private const string NewLine = "\r\n";
        private const string LineBreak = "\r\n\r\n";

        private const string SwitchParameter = "SwitchParameter";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mamlCommands"></param>
        /// <returns></returns>
        public string MamlModelToString(IEnumerable<MamlCommand> mamlCommands)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(mshNS + "helpItems", 
                    new XAttribute("schema","maml"),
                    mamlCommands.Select(CreateCommandElement)
                )
            );

            //var memoryStream = new MemoryStream();
            //using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            //{

            //    //document.Save(writer, SaveOptions.OmitDuplicateNamespaces);
            //    //writer.Flush();

            //    //memoryStream.Seek(0, SeekOrigin.Begin);
            //    //using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
            //    //{
            //    //    return RenderCleaner.NormalizeWhitespaces(reader.ReadToEnd());
            //    //}
            //}

            return RenderCleaner.NormalizeWhitespaces(document.ToString(options: SaveOptions.OmitDuplicateNamespaces));
        }

        private static XElement CreateCommandElement(MamlCommand command)
        {
            var commandParts = command.Name.Split(Dash);
            var verb = commandParts[0];
            var noun = command.Name.Substring(Math.Min(verb.Length + 1, command.Name.Length));

            return new XElement(commandNS + "command",
                    new XAttribute(XNamespace.Xmlns + "maml", mamlNS),
                    new XAttribute(XNamespace.Xmlns + "command", commandNS),
                    new XAttribute(XNamespace.Xmlns + "dev", devNS),
                    new XAttribute(XNamespace.Xmlns + "MSHelp", msHelpNS),

                    new XElement(commandNS + "details",
                        new XElement(commandNS + "name", command.Name),
                        new XElement(commandNS + "verb", verb),
                        new XElement(commandNS + "noun", noun),
                        new XElement(mamlNS + "description", GenerateParagraphs(command.Synopsis?.Text))),
                    new XElement(mamlNS + "description", GenerateParagraphs(command.Description?.Text)),
                    new XElement(commandNS + "syntax", command.Syntax.Select(syn => CreateSyntaxItem(syn, command))),
                    new XElement(commandNS + "parameters", command.Parameters.Select(param => CreateParameter(param))),
                    new XElement(commandNS + "inputTypes", command.Inputs.Select(input => CreateInput(input))),
                    new XElement(commandNS + "returnValues", command.Outputs.Select(output => CreateOutput(output))),

                    Notes(command),
                    
                    new XElement(commandNS + "examples", command.Examples.Select(example => CreateExample(example))),
                    new XElement(commandNS + "relatedLinks", command.Links.Select(link => CreateLink(link))));
        }

        private static XElement Notes(MamlCommand command)
        {
            return new XElement(mamlNS + "alertSet", new XElement(mamlNS + "alert", GenerateParagraphs(command.Notes?.Text)));
        }

        private static IEnumerable<XElement> GenerateParagraphs(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            var lines = text.Split(new string[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0 && HasListPrefix(lines[i - 1]) && !HasListPrefix(lines[i]))
                {
                    yield return new XElement(mamlPara, string.Empty);
                }

                yield return new XElement(mamlPara, lines[i]);
            }
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

        private static XElement CreateSyntaxItem(MamlSyntax syntaxItem, MamlCommand command)
        {
            return new XElement(commandNS + "syntaxItem", 
                    new XElement(mamlName, command.Name),
                    syntaxItem.Parameters.Select(param => CreateParameter(param, isSyntax: true)));
        }

        
        /// <summary>
        /// Creates a command:parameter element 
        /// </summary>
        /// <param name="param">The parsed parameter object</param>
        /// <param name="isSyntax">Render using the format for syntax blocks where we do not render [switch]</param>
        /// <returns></returns>
        private static XElement CreateParameter(MamlParameter param, bool isSyntax = false) 
        {
            string mamlType = ConvertPSTypeToMamlType(param);
            bool isSwitchParameter = mamlType == SwitchParameter || mamlType == "System.Management.Automation.SwitchParameter";

            var parameterElement = new XElement(commandNS + "parameter",
                    new XAttribute("required", param.Required),
                    new XAttribute("variableLength", param.VariableLength),
                    new XAttribute("globbing", param.Globbing),
                    new XAttribute("pipelineInput", param.PipelineInput),
                    new XAttribute("position", param.IsNamed() ? "named" : param.Position.Value.ToString()),
                    new XAttribute("aliases", param.Aliases.Any() ? string.Join(", ", param.Aliases) : "none"),

                    new XElement(mamlName, param.Name),
                    new XElement(mamlNS + "Description", GenerateParagraphs(param.Description)),
                    isSyntax && param.ParameterValueGroup?.Length > 0 ? new XElement(commandNS + "parameterValueGroup", param.ParameterValueGroup.Select(pvg => CreateParameterValueGroup(pvg))) 
                        : null,
                    // we don't add [switch] info to make it appear good
                    !isSyntax || !isSwitchParameter 
                        ? new XElement(commandNS + "parameterValue",
                            new XAttribute("required", param.ValueRequired),
                            new XAttribute("variableLength", param.ValueVariableLength),
                            mamlType)
                        : null,
                    new XElement(devNS + "type", 
                        new XElement(mamlName, mamlType),
                        new XElement(mamlNS + "uri")),
                    new XElement(devNS + "defaultValue",
                        isSwitchParameter && (string.IsNullOrEmpty(param.DefaultValue) || param.DefaultValue == "None")
                            ? "False"
                            : param.DefaultValue ?? "None"));

            return parameterElement;
        }

        private static XElement CreateParameterValueGroup(string pvg)
        {
            return new XElement(commandNS + "parameterValue",
                    new XAttribute("required", "false"), new XAttribute(commandNS + "variableLength", "false"),
                    pvg);
        }

        private static XElement CreateInput(MamlInputOutput input)
        {
            return new XElement(commandNS + "inputType",
                    new XElement(devNS + "type",
                        new XElement(mamlName, input.TypeName)),
                    new XElement(mamlDescription, GenerateParagraphs(input.Description)));
        }

        private static XElement CreateOutput(MamlInputOutput output)
        {
            return new XElement(commandNS + "returnValue",
                    new XElement(devNS + "type",
                        new XElement(mamlName, output.TypeName)),
                    new XElement(mamlDescription, GenerateParagraphs(output.Description)));
        }

        private static XElement CreateExample(MamlExample example)
        {
            return new XElement(commandNS + "example",
                    new XElement(mamlNS + "title", PadExampleTitle(example.Title)),
                    new XElement(devNS + "code", string.Join(LineBreak, example.Code.Select(block => block.Text))),
                    new XElement(devNS + "remarks", GenerateParagraphs(example.Remarks)));
        }

        private static XElement CreateLink(MamlLink link)
        {
            // PowerShell help engine is not happy, when LinkUri doesn't represent a valid URI
            // but it's often convinient to have it (i.e. relative links between markdown files on github).
            // so, we are ignoring non-uri links when rendering the final maml.
            // https://github.com/PowerShell/platyPS/issues/164
            if (link.IsSimplifiedTextLink)
            {
                return null;
            }

            string uriValue = string.Empty;
            if (Uri.TryCreate(link.LinkUri, UriKind.Absolute, out Uri uri))
            {
                uriValue = link.LinkUri;
            }

            return new XElement(mamlNS + "navigationLink",
                    new XElement(mamlNS + "linkText", link.LinkName),
                    new XElement(mamlNS + "uri", uriValue));
        }

        /// <summary>
        /// Generate (-) padding for example title
        /// </summary>
        /// <param name="title">The title to pa.</param>
        /// <returns>The title padded by dashes</returns>
        private static string PadExampleTitle(string title)
        {
            // Filter out edge cases where title is too long or empty
            if (string.IsNullOrWhiteSpace(title) || title.Length >= 62)
            {
                return title;
            }

            // Pad example title with dash (-) to increase readability up to 64 characters

            int padLength = (64 - title.Length - 2) / 2;

            return title
                .PadLeft(title.Length + 1, Space)
                .PadRight(title.Length + 2, Space)
                .PadLeft(title.Length + 2 + padLength, Dash)
                .PadRight(title.Length + 2 + 2 * padLength, Dash);
        }

        private static string ConvertPSTypeToMamlType(MamlParameter parameter)
        {
            if (parameter.Type == null)
            {
                return string.Empty;
            }

            if (parameter.IsSwitchParameter())
            {
                return SwitchParameter;
            }

            return parameter.Type;
        }
    }
}
