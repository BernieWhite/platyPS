using Markdown.MAML.Model.MAML;
using Markdown.MAML.Parser;
using Markdown.MAML.Renderer;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    internal sealed class MamlXmlPipeline : IMamlXmlPipeline
    {
        private readonly VisitMamlCommand _Action;
        private readonly string[] _Tags;
        private readonly MamlRenderer _Renderer;

        internal MamlXmlPipeline(VisitMamlCommand action, string[] tags)
        {
            _Action = action;
            _Tags = tags;
            _Renderer = new MamlRenderer();
        }

        public string Process(IEnumerable<MamlCommand> command)
        {
            var commands = new List<MamlCommand>();

            foreach (var c in command)
            {
                if (_Action(c))
                {
                    commands.Add(c);
                }
            }

            return _Renderer.MamlModelToString(commands);
        }

        public string Process(string[] path, Encoding encoding)
        {
            var commands = new List<MamlCommand>();

            foreach (var p in path)
            {
                var markdown = File.ReadAllText(p, encoding);

                commands.Add(ProcessCore(markdown, p));
            }

            return _Renderer.MamlModelToString(commands);
        }

        private MamlCommand ProcessCore(string markdown, string path)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: false);
            var stream = reader.Read(markdown, path);

            var lexer = new MamlCommandLexer(preserveFomatting: false);

            var command = lexer.Process(stream, _Tags);

            if (command == null || !_Action(command))
            {
                return null;
            }

            return command;
        }
    }
}