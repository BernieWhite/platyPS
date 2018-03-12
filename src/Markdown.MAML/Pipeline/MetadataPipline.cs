using Markdown.MAML.Parser;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    internal sealed class MetadataPipline : IMetadataPipline
    {
        public Dictionary<string, string> Process(string path, Encoding encoding)
        {
            var markdown = File.ReadAllText(path, encoding);

            return ProcessCore(markdown, path);
        }

        public Dictionary<string, string> Process(string markdown, string path)
        {
            return ProcessCore(markdown, path);
        }

        private Dictionary<string, string> ProcessCore(string markdown, string path)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: true);
            var stream = reader.Read(markdown, path);

            var lexer = new MetadataLexer();
            return lexer.Process(stream);
        }
    }
}