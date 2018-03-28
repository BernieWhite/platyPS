using Markdown.MAML.Configuration;

namespace Markdown.MAML.Pipeline
{
    public delegate void MamlCommandPipelineConfiguration(MamlCommandBuilder builder);

    public delegate void MamlXmlPipelineConfiguration(MamlXmlBuilder builder);

    public delegate void MarkdownPipelineConfiguration(MarkdownBuilder builder);

    public static class PipelineBuilder
    {
        public static MamlCommandBuilder ToMamlCommand(MarkdownHelpOption option = null)
        {
            var builder = new MamlCommandBuilder();

            return builder.Configure(option);
        }

        public static MamlXmlBuilder ToMamlXml(MarkdownHelpOption option = null)
        {
            var builder = new MamlXmlBuilder();

            return builder.Configure(option);
        }

        public static MarkdownBuilder ToMarkdown(MarkdownHelpOption option = null)
        {
            var builder = new MarkdownBuilder();

            return builder.Configure(option);
        }

        public static IAboutTopicPipeline ToAboutTopic()
        {
            var builder = new AboutTopicBuilder();

            return builder.Build();
        }

        public static IAboutTextPipeline ToAboutText()
        {
            var builder = new AboutTextBuilder();

            return builder.Build();
        }

        public static IMetadataPipline ToMetadata()
        {
            return new MetadataBuilder().Build();
        }
    }
}
