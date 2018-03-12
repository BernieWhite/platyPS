using System.Text;

namespace Markdown.MAML.Parser
{
    internal sealed class MarkdownWriter
    {
        private TokenStream _Stream;
        private StringBuilder _Output;

        public MarkdownWriter(TokenStream stream)
        {
            _Stream = stream;
            _Output = new StringBuilder();
        }

        public string Write()
        {
            _Stream.MoveTo(0);

            while (!_Stream.EOF)
            {
                switch (_Stream.Current.Type)
                {
                    case MarkdownTokenType.Header:

                        Header();

                        break;

                    case MarkdownTokenType.Text:

                        Text();

                        break;

                    case MarkdownTokenType.FencedBlock:

                        FencedBlock();

                        break;

                    case MarkdownTokenType.Link:

                        Link();

                        break;

                    case MarkdownTokenType.LineBreak:

                        LineBreak();

                        break;
                }

                _Stream.Next();
            }

            return _Output.ToString();
        }

        private void LineBreak()
        {
            LineEnding();
        }

        private void Link()
        {
            _Output.AppendFormat("[{0}]({1})", _Stream.Current.Meta, _Stream.Current.Text);

            LineEnding();
        }

        private void FencedBlock()
        {
            _Output.AppendFormat(
                "```{0}\r\n{1}\r\n```\r\n",
                _Stream.Current.Meta,
                _Stream.Current.Text
                );

            LineEnding();
        }

        private void Text()
        {
            _Output.Append(_Stream.Current.Text);

            LineEnding();
        }

        private void Header()
        {
            _Output.Append(_Stream.Current.Text);

            LineEnding();
        }

        private void LineEnding()
        {
            if (_Stream.Current.IsSingleLineEnding())
            {
                _Output.Append("\r\n");
            }
            else if (_Stream.Current.IsDoubleLineEnding())
            {
                _Output.Append("\r\n\r\n");
            }
        }
    }
}
