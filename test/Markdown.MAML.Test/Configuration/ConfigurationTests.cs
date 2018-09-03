using Markdown.MAML.Configuration;
using Xunit;

namespace Markdown.MAML.Test.Configuration
{
    public sealed class ConfigurationTests
    {
        [Fact]
        public void UsesDefaultConfiguration()
        {
            var actual = MarkdownHelpOption.GetYamlPath(@"..\..\..\");
            Assert.Contains(@"\.platyps.yml", actual);
        }
    }
}
