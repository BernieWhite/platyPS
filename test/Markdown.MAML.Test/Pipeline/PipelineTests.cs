using Markdown.MAML.Configuration;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Pipeline;
using Xunit;

namespace Markdown.MAML.Test.Pipeline
{
    public sealed class PipelineTests
    {
        [Fact]
        public void ConfirmPipelineExecutes()
        {
            var actionCalled = false;
            var expected = "This is a description";

            var pipeline = PipelineBuilder.ToMamlCommand().Configure(config =>
            {
                config.AddMamlAction((node, next) => {
                    actionCalled = true;
                    node.Description = new SectionBody(expected);
                    return next(node);
                });
            }).Build();

            var actual = pipeline.Process(@"
# Invoke-Action

", path: null);

            Assert.True(actionCalled);
            Assert.NotNull(actual);
            Assert.Equal(expected, actual.Description.Text);
        }

        [Fact]
        public void ParametersAreSorted()
        {
            var command = new MamlCommand();

            command.Parameters.Add(new MamlParameter
            {
                Name = "xyz"
            });

            command.Parameters.Add(new MamlParameter
            {
                Name = "Confirm"
            });

            command.Parameters.Add(new MamlParameter
            {
                Name = "abc"
            });

            command.Parameters.Add(new MamlParameter
            {
                Name = "whatif"
            });

            command.Parameters.Add(new MamlParameter
            {
                Name = "Def"
            });

            command.Parameters.Sort(ParameterComparer.Ordered);

            var parameters = command.Parameters.ToArray();

            Assert.Equal("abc", parameters[0].Name);
            Assert.Equal("Def", parameters[1].Name);
            Assert.Equal("xyz", parameters[2].Name);
            Assert.Equal("Confirm", parameters[3].Name);
            Assert.Equal("whatif", parameters[4].Name);
        }

        [Fact]
        public void InfoStringIsDetected()
        {
            var option = new MarkdownHelpOption();
            option.Markdown.InfoString = "output";

            var command = new MamlCommand
            {
                Name = "Test-PowerShell"
            };

            command.Examples.Add(new MamlExample
            {
                Title = "Example 1",
                Code = new[] {
                    new MamlCodeBlock("PS> Test-PowerShell -Name 'Test';"),
                    new MamlCodeBlock("PS C:\\> Test-PowerShell -Name 'Test;"),
                    new MamlCodeBlock("This is the output.")
                }
            });

            var markdown = PipelineBuilder.ToMarkdown(option).Build().Process(command);

            Assert.Contains("```powershell\r\nPS> Test-PowerShell -Name 'Test';\r\n```", markdown);
            Assert.Contains("```powershell\r\nPS C:\\> Test-PowerShell -Name 'Test;\r\n```", markdown);
            Assert.Contains("```output\r\nThis is the output.\r\n```", markdown);
        }

        [Fact]
        public void ToMarkdownHooksCalled()
        {
            var option = new MarkdownHelpOption();

            option.Pipeline.WriteCommand.Add(mamlCommand =>
            {
                mamlCommand.RemoveParameter("AsJob");
            });

            option.Pipeline.WriteMarkdown.Add((markdown, path) =>
            {
                return markdown.Replace("Test-HookTwo", "Test-HookOne");
            });

            option.Pipeline.WriteMarkdown.Add((markdown, path) =>
            {
                return markdown.Replace("Test-PowerShell", "Test-HookTwo");
            });

            var command = new MamlCommand
            {
                Name = "Test-PowerShell"
            };

            var parameter = new MamlParameter
            {
                Name = "AsJob",
                Description = "This is a parameter that should be deleted."
            };

            var syntax = new MamlSyntax();
            syntax.Parameters.Add(parameter);
            command.Parameters.Add(parameter);
            command.Syntax.Add(syntax);

            var actual = PipelineBuilder.ToMarkdown(option).Build().Process(command);

            // Check that hook changes have been applied
            Assert.Contains("# Test-HookOne", actual);
            Assert.DoesNotContain("-AsJob", actual);
        }

        [Fact]
        public void ToMamlCommandHooksCalled()
        {
            var markdown = @"
# Update-MarkdownHelp

## SYNOPSIS

Example markdown to test that markdown is preserved.

## SYNTAX

```
Update-MarkdownHelp [-Name] <String> [-Path <String>]
```

## DESCRIPTION
When calling Update - MarkdownHelp line breaks should be preserved.

";

            var option = new MarkdownHelpOption();

            // Handle markdown hooks
            option.Pipeline.ReadMarkdown.Add((m, path) =>
            {
                return m.Replace("Test-MarkdownTwo", "Test-MarkdownOne");
            });

            option.Pipeline.ReadMarkdown.Add((m, path) =>
            {
                return m.Replace("Update-MarkdownHelp", "Test-MarkdownTwo");
            });

            // Handle MamlCommand hooks
            option.Pipeline.ReadCommand.Add((c) =>
            {
                c.Synopsis.Text = c.Synopsis.Text.Replace("two", "one");
            });

            option.Pipeline.ReadCommand.Add((c) =>
            {
                c.Synopsis.Text = c.Synopsis.Text.Replace("markdown", "two");
            });

            var actual = PipelineBuilder.ToMamlCommand(option).Build().Process(markdown, path: null);

            Assert.NotNull(actual);
            Assert.Equal("Test-MarkdownOne", actual.Name);
            Assert.Equal("Example one to test that one is preserved.", actual.Synopsis.Text);
        }
    }
}
