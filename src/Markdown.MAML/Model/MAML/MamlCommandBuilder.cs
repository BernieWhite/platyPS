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

        public MamlParameter Parameter(string parameterSetName, string name, string description, bool required, string type, int? position, string[] aliases, string pipelineInput, string fullType)
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

            // TODO: Default parameter descriptions should be localized.

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
                else if (StringComparer.OrdinalIgnoreCase.Equals(name, "IncludeTotalCount"))
                {
                    parameter.Description = "Reports the number of objects in the data set (an integer) followed by the objects. If the cmdlet cannot determine the total count, it returns 'Unknown total count'.";
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(name, "Skip"))
                {
                    parameter.Description = "Ignores the first 'n' objects and then gets the remaining objects.";
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(name, "First"))
                {
                    parameter.Description = "Gets only the first 'n' objects.";
                }
                else
                {
                    parameter.Description = string.Concat("{{Fill ", name, " Description}}");
                }
            }
            
            if (position >= 0 && position <= byte.MaxValue)
            {
                parameter.Position = (byte)position;
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
