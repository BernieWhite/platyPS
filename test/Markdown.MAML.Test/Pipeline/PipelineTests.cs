using Markdown.MAML.Model.MAML;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Pipeline;
using System.Linq;
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

            var pipeline = PipelineBuilder.ToMamlCommand(config =>
            {
                config.AddMamlAction((node, next) => {
                    actionCalled = true;
                    node.Description = new SectionBody(expected);
                    return next(node);
                });
            });

            var actual = pipeline.Process(@"
# Invoke-Action

");

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
    }
}
