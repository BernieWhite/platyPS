using Markdown.MAML.Model.Markdown;
using System;

namespace Markdown.MAML.Model.MAML
{
    public sealed class MamlCommandBuilder
    {
        private MamlCommand _Command;

        private MamlCommandBuilder(string name, string module)
        {
            _Command = MamlCommand.Create();
            _Command.Name = name;
            _Command.ModuleName = module;
        }

        public static MamlCommandBuilder Create(string name, string module)
        {
            return new MamlCommandBuilder(name, module);
        }

        public void Synopsis(string text)
        {
            if (text == null)
            {
                return;
            }

            _Command.Synopsis = new SectionBody(text.Trim());
        }

        public void Description(string[] text)
        {
            if (text == null)
            {
                return;
            }

            _Command.Description = new SectionBody(string.Join("\r\n\r\n", text));
        }

        public void Notes(string[] text)
        {
            if (text == null)
            {
                return;
            }

            _Command.Notes = new SectionBody(string.Join("\r\n\r\n", text));
        }

        public void Link(string text, string uri)
        {
            _Command.Links.Add(new MamlLink { LinkName = text, LinkUri = uri });
        }

        public void Example(string title, string introduction, string code, string[] remarks)
        {
            _Command.Examples.Add(new MamlExample
            {
                Title = title,
                Introduction = introduction,
                Code = new [] { new MamlCodeBlock(code) },
                Remarks = string.Join("\r\n\r\n", remarks)
            });
        }

        public void Input(string typeName, string[] description)
        {
            _Command.Inputs.Add(new MamlInputOutput
            {
                TypeName = typeName,
                Description = description != null ? string.Join("\r\n\r\n", description) : string.Empty
            });
        }

        public void Output(string typeName, string[] description)
        {
            _Command.Outputs.Add(new MamlInputOutput
            {
                TypeName = typeName,
                Description = description != null ? string.Join("\r\n\r\n", description) : string.Empty
            });
        }

        public MamlParameter Parameter(string parameterSetName, string name, string description, bool required, string type, string[] aliases, string pipelineInput, string fullType)
        {
            var parameter = new MamlParameter
            {
                Name = name,
                Description = description,
                Required = required,
                Type = type ?? "SwitchParameter",
                Aliases = aliases,
                PipelineInput = pipelineInput,
                FullType = fullType
            };

            // we have well-known parameters and can generate a reasonable description for them
            // https://github.com/PowerShell/platyPS/issues/211
            if (string.IsNullOrEmpty(description))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(name, "Confirm"))
                {
                    parameter.Description = "Prompts you for confirmation before running the cmdlet.";
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(name, "WhatIf"))
                {
                    parameter.Description = "Shows what would happen if the cmdlet runs. The cmdlet is not run.";
                }
                else
                {
                    parameter.Description = string.Concat("{{Fill ", name, " Description}}");
                }
            }
            

            if (!_Command.Syntax.ContainsKey(parameterSetName))
            {
                var syntax = new MamlSyntax { ParameterSetName = parameterSetName };

                syntax.Parameters.Add(parameter);

                _Command.Syntax.Add(syntax);
            }
            else
            {
                _Command.Syntax[parameterSetName].Parameters.Add(parameter);
            }

            return parameter;
        }

        public void Syntax(string parameterSetName, bool isDefault)
        {
            _Command.Syntax.Add(new MamlSyntax
            {
                ParameterSetName = parameterSetName,
                IsDefault = isDefault
            });
        }

        public MamlCommand Get()
        {
            return _Command;
        }
    }
}
