using Markdown.MAML.Model.MAML;
using Markdown.MAML.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    /// <summary>
    /// A pipeline to generate MamlCommand objects from markdown.
    /// </summary>
    internal sealed class MamlCommandPipeline : IMamlCommandPipeline
    {
        private readonly VisitMamlCommand _Action;
        private readonly bool _PreserveFormatting;
        private readonly string[] _Tags;

        internal MamlCommandPipeline(VisitMamlCommand action, string[] tags, bool preserveFormatting)
        {
            _Action = action;
            _PreserveFormatting = preserveFormatting;
            _Tags = tags;
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
            var stream = reader.Read(markdown, path);

            var lexer = new MamlCommandLexer(preserveFomatting: _PreserveFormatting);

            var command = lexer.Process(stream, _Tags);

            if (command == null || !_Action(command))
            {
                return null;
            }

            return command;
        }
    }
}