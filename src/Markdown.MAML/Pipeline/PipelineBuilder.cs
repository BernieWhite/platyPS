namespace Markdown.MAML.Pipeline
{
    public delegate void MamlCommandPipelineConfiguration(MamlCommandBuilder builder);

    public delegate void MamlXmlPipelineConfiguration(MamlXmlBuilder builder);

    public delegate void MarkdownPipelineConfiguration(MarkdownBuilder builder);

    public static class PipelineBuilder
    {
        public static IMamlCommandPipeline ToMamlCommand(MamlCommandPipelineConfiguration config = null)
        {
            var builder = new MamlCommandBuilder();

            config?.Invoke(builder);

            return builder.Build();
        }

        public static IMamlXmlPipeline ToMamlXml(MamlXmlPipelineConfiguration config = null)
        {
            var builder = new MamlXmlBuilder();

            config?.Invoke(builder);

            return builder.Build();
        }

        public static IMarkdownPipeline ToMarkdown(MarkdownPipelineConfiguration config = null)
        {
            var builder = new MarkdownBuilder();

            config?.Invoke(builder);

            return builder.Build();
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
