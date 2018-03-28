using Markdown.MAML.Configuration;
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
        private readonly VisitMamlCommand _ReadMamlCommand;
        private readonly VisitMarkdown _ReadMarkdown;
        private readonly string[] _Tags;
        private readonly MamlRenderer _Renderer;
        private readonly MamlCommandLexer _Lexer;

        internal MamlXmlPipeline(VisitMamlCommand readMamlCommand, VisitMarkdown readMarkdown, string[] tags)
        {
            _ReadMamlCommand = readMamlCommand;
            _ReadMarkdown = readMarkdown;
            _Tags = tags;
            _Renderer = new MamlRenderer();
            _Lexer = new MamlCommandLexer(preserveFomatting: false);
        }

        public string Process(IEnumerable<MamlCommand> command)
        {
            var commands = new List<MamlCommand>();

            foreach (var c in command)
            {
                if (_ReadMamlCommand(c))
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

                commands.Add(ProcessCore(_ReadMarkdown(markdown, p), p));
            }

            return _Renderer.MamlModelToString(commands);
        }

        private MamlCommand ProcessCore(string markdown, string path)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: false);

            var command = _Lexer.Process(reader.Read(markdown, path), _Tags);

            if (command == null || !_ReadMamlCommand(command))
            {
                return null;
            }

            return command;
        }
    }
}