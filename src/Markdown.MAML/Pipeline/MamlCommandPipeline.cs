using Markdown.MAML.Configuration;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Parser;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    /// <summary>
    /// A pipeline to generate MamlCommand objects from markdown.
    /// </summary>
    internal sealed class MamlCommandPipeline : IMamlCommandPipeline
    {
        private readonly VisitMamlCommand _ReadMamlCommand;
        private readonly VisitMarkdown _ReadMarkdown;
        private readonly bool _PreserveFormatting;
        private readonly string[] _Tags;
        private readonly MamlCommandLexer _Lexer;

        internal MamlCommandPipeline(VisitMamlCommand readMamlCommand, VisitMarkdown readMarkdown, string[] tags, bool preserveFormatting)
        {
            _ReadMamlCommand = readMamlCommand;
            _ReadMarkdown = readMarkdown;
            _PreserveFormatting = preserveFormatting;
            _Tags = tags;
            _Lexer = new MamlCommandLexer(preserveFomatting: _PreserveFormatting);
        }

        public MamlCommand Process(string markdown, string path)
        {
            return ProcessCore(markdown, path);
        }

        public MamlCommand Process(string path, Encoding encoding)
        {
            var markdown = File.ReadAllText(path, encoding);

            return ProcessCore(markdown, path);
        }

        private MamlCommand ProcessCore(string markdown, string path)
        {
            var reader = new MarkdownReader(preserveFormatting: _PreserveFormatting, yamlHeaderOnly: false);

            var command = _Lexer.Process(reader.Read(_ReadMarkdown(markdown, path), path), _Tags);

            if (command == null || !_ReadMamlCommand(command))
            {
                return null;
            }

            return command;
        }
    }
}