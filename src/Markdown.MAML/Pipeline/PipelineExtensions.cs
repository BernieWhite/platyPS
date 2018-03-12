using Markdown.MAML.Model.MAML;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    public static class PipelineExtensions
    {
        public static MamlCommand Process(this IMamlCommandPipeline pipeline, string markdown)
        {
            return pipeline.Process(markdown, path: null);
        }

        public static IDictionary<string, string> Process(this IMetadataPipline pipeline, string markdown)
        {
            return pipeline.Process(markdown, path: null);
        }
    }
}
