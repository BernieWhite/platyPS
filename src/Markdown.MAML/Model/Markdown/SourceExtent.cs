using System.Diagnostics;

namespace Markdown.MAML.Model.Markdown
{
    [DebuggerDisplay("StartPos = (L: {Start}, C: {Column}), EndPos = (L: {End}, C: {Column.End}), Text = {Text}")]
    public sealed class SourceExtent
    {
        private string _Text;

        internal SourceExtent(string markdown, string path, int start, int end, int line, int column)
        {
            _Text = null;

            Markdown = markdown;
            Path = path;
            Start = start;
            End = end;
            Line = line;
            Column = column;
        }

        public string Markdown { get; private set; }

        public string Path { get; private set; }

        public int Start { get; private set; }

        public int End { get; private set; }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public string Text
        {
            get
            {
                if (_Text == null)
                {
                    _Text = Markdown.Substring(Start, (End - Start));
                }

                return _Text;
            }
        }
    }
}
