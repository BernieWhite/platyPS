using Markdown.MAML.Configuration;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Parser;
using Markdown.MAML.Renderer;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    /// <summary>
    /// A pipeline to generate markdown from MamlCommand objects.
    /// </summary>
    internal sealed class MarkdownPipeline : IMarkdownPipeline
    {
        private readonly VisitMamlCommand _WriteMamlCommand;
        private readonly VisitMarkdown _WriteMarkdown;
        private readonly bool _NoMetadata;
        private readonly int _SyntaxWidth;
        private readonly bool _PreserveFormatting;

        internal MarkdownPipeline(VisitMamlCommand writeMamlCommand, VisitMarkdown writeMarkdown, bool noMetadata, bool preserveFormatting, int syntaxWidth)
        {
            _WriteMamlCommand = writeMamlCommand;
            _WriteMarkdown = writeMarkdown;
            _PreserveFormatting = preserveFormatting;
            _NoMetadata = noMetadata;
            _SyntaxWidth = syntaxWidth;
        }

        public void Process(MamlCommand command, string path, Encoding encoding)
        {
            File.WriteAllText(path, _WriteMarkdown(ProcessCore(command), path), encoding);
        }

        public string Process(MamlCommand command)
        {
            return _WriteMarkdown(ProcessCore(command), path: null);
        }

        private string ProcessCore(MamlCommand command)
        {
            if (_WriteMamlCommand(command))
            {
                var renderer = new MarkdownV2Renderer(_PreserveFormatting ? ParserMode.FormattingPreserve : ParserMode.Full, _SyntaxWidth);

                return renderer.MamlModelToString(command, _NoMetadata);
            }

            return string.Empty;
        }
    }
}