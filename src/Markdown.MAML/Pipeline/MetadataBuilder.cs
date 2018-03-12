namespace Markdown.MAML.Pipeline
{
    public sealed class MetadataBuilder
    {
        public IMetadataPipline Build()
        {
            return new MetadataPipline();
        }
    }
}