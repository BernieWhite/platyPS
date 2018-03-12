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
        private readonly VisitMamlCommand _Action;
        private readonly bool _NoMetadata;
        private readonly bool _PreserveFormatting;

        internal MarkdownPipeline(VisitMamlCommand action, bool noMetadata, bool preserveFormatting)
        {
            _Action = action;
            _PreserveFormatting = preserveFormatting;
            _NoMetadata = noMetadata;
        }

        public void Process(MamlCommand command, string path, Encoding encoding)
        {
            File.WriteAllText(path, ProcessCore(command), encoding);
        }

        public string Process(MamlCommand command)
        {
            return ProcessCore(command);
        }

        private string ProcessCore(MamlCommand command)
        {
            if (_Action(command))
            {
                var renderer = new MarkdownV2Renderer(_PreserveFormatting ? ParserMode.FormattingPreserve : ParserMode.Full);

                return renderer.MamlModelToString(command, _NoMetadata);
            }

            return string.Empty;
        }
    }
}