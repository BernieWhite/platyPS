using System.Collections.Generic;

namespace Markdown.MAML.Parser
{
    internal sealed class MetadataLexer : MarkdownLexer
    {
        public Dictionary<string, string> Process(TokenStream stream)
        {
            stream.MoveTo(0);

            // Look for yaml header

            return YamlHeader(stream);
        }
    }
}
