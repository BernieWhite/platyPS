using Markdown.MAML.Model.MAML;
using System.Collections.Generic;

namespace Markdown.MAML.Configuration
{
    public delegate bool VisitMamlCommand(MamlCommand command);

    public delegate bool VisitMamlCommandAction(MamlCommand command, VisitMamlCommand next);

    public delegate void MamlCommandScriptHook(MamlCommand command);

    public delegate string VisitMarkdown(string markdown, string path);

    public delegate string VisitMarkdownAction(string markdown, string path, VisitMarkdown next);

    public sealed class PipelineHook
    {
        public static VisitMarkdown EmptyMarkdownDelegate = (markdown, path) => { return markdown; };

        public static VisitMamlCommand EmptyMamlCommandDelegate = next => { return true; };

        public PipelineHook()
        {
            ReadMarkdown = new List<VisitMarkdown>();
            WriteMarkdown = new List<VisitMarkdown>();
            ReadCommand = new List<MamlCommandScriptHook>();
            WriteCommand = new List<MamlCommandScriptHook>();
        }

        public List<VisitMarkdown> ReadMarkdown { get; set; }

        public List<VisitMarkdown> WriteMarkdown { get; set; }

        public List<MamlCommandScriptHook> ReadCommand { get; set; }

        public List<MamlCommandScriptHook> WriteCommand { get; set; }
    }
}