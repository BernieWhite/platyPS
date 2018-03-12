using Markdown.MAML.Model.About;
using Markdown.MAML.Parser;
using Markdown.MAML.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Markdown.MAML.Renderer
{
    public sealed class TextRenderer
    {
        private const int TOPIC_NAME_HEADING_LEVEL = 1;
        private const int level2Heading = 2;

        private StringBuilder _Output = new StringBuilder();
        private TokenStream _Stream;
        private const string AboutIndentation = "    ";
        
        private const string NewLine = "\r\n";
        private const char Space = ' ';

        private int _MaxLineWidth { get; set; }

        public TextRenderer() : this(80) { }

        public TextRenderer(int maxLineWidth)
        {
            _MaxLineWidth = maxLineWidth;
        }
        
        public string AboutMarkdownToString(AboutTopic topic)
        {
            //ensure that all node types in the about topic are handeled.
            //var acceptableNodeTypes = new List<MarkdownNodeType>
            //{
            //    MarkdownNodeType.Heading,
            //    MarkdownNodeType.Paragraph,
            //    MarkdownNodeType.CodeBlock
            //};
            //if (document.Children.Any(c => (!acceptableNodeTypes.Contains(c.NodeType))))
            //{
            //    throw new NotSupportedException("About Topics can only have heading, paragraph or code block nodes in their Markdown Model.");
            //}

            _Output.AppendLine(MarkdownStrings.AboutTopicFirstHeader.ToUpper());
            _Output.AppendFormat("{0}{1}{2}{2}", AboutIndentation, topic.Name.ToLower(), NewLine, NewLine);

            _Stream = new TokenStream(topic.Children);
            _Stream.MoveTo(0);

            while (!_Stream.EOF)
            {
                var matching = Heading() || FencedBlock() || Text();

                if (!matching)
                {
                    _Stream.Next();
                }
            }

            return _Output.ToString();
        }

        private bool Text()
        {
            if (!_Stream.IsTokenType(MarkdownTokenType.Text))
            {
                return false;
            }

            Wrap();

            return true;
        }

        private bool Heading()
        {
            if (!_Stream.IsTokenType(MarkdownTokenType.Header))
            {
                return false;
            }

            if (IsHeading(_Stream.Current, TOPIC_NAME_HEADING_LEVEL))
            {
                Append(_Stream.Current.Text.ToUpper());
            }
            else if (IsHeading(_Stream.Current, level2Heading))
            {
                Append(_Stream.Current.Text);
            }
            else
            {
                AppendIndent();
                Append(_Stream.Current.Text.ToLower());
            }

            AppendEnding(MarkdownTokenFlag.LineEnding);

            _Stream.Next();

            return true;
        }

        private bool FencedBlock()
        {
            if (!_Stream.IsTokenType(MarkdownTokenType.FencedBlock))
            {
                return false;
            }

            var lines = _Stream.Current.Text.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                AppendIndent();
                _Output.AppendLine(line);
            }

            _Output.AppendLine();

            _Stream.Next();

            return true;
        }

        private void AppendIndent()
        {
            _Output.Append(AboutIndentation);
        }

        private void Append(string text)
        {
            _Output.Append(text);
        }

        private void AppendEnding(MarkdownTokenFlag ending)
        {
            if (_Output.Length == 0 || !ending.IsEnding())
            {
                return;
            }

            if (ending.IsLineBreak())
            {
                _Output.Append("\r\n\r\n");
            }
            else
            {
                _Output.Append("\r\n");
            }
        }

        private bool IsHeading(MarkdownToken token, int level)
        {
            return token.Type == MarkdownTokenType.Header && token.Depth == level;
        }

        private void Wrap()
        {
            // Keep track of how many characters are remaining in the line
            var maxWidth = _MaxLineWidth - 4;
            var remaining = maxWidth;
            var innerPos = 0;
            var chunkSize = 0;
            var splitPos = 0;

            while (_Stream.IsTokenType(MarkdownTokenType.Text))
            {
                if (remaining == 0)
                {
                    AppendEnding(MarkdownTokenFlag.LineEnding);
                    remaining = maxWidth;
                }

                if (remaining == maxWidth)
                {
                    AppendIndent();
                }

                var text = _Stream.Current.Text;

                // The text will fit in the remaining character of the line
                if (_Stream.Current.Flag.ShouldPreserve())
                {
                    if (remaining < maxWidth)
                    {
                        AppendEnding(MarkdownTokenFlag.LineEnding);
                    }

                    Append(text);
                }
                else if (text.Length <= remaining)
                {
                    Append(text);

                    remaining -= text.Length;
                }
                else // Split the text along spaces
                {
                    var seperate = (remaining < maxWidth);

                    innerPos = 0;
                    chunkSize = Math.Min(remaining, text.Length);
                    splitPos = chunkSize - 1;

                    while (innerPos < text.Length)
                    {
                        if (text.Length - innerPos <= remaining)
                        {
                            if (remaining == maxWidth)
                            {
                                AppendIndent();
                            }
                            else if (seperate)
                            {
                                Append(" ");
                                seperate = false;
                            }

                            _Output.Append(text, innerPos, text.Length - innerPos);

                            remaining -= text.Length - innerPos;

                            break;
                        }

                        splitPos = text.LastIndexOf(Space, splitPos, chunkSize);

                        // Count the number of characters to excluding the space
                        var splitCount = splitPos - innerPos;

                        if (splitCount > 0)
                        {
                            if (remaining == maxWidth)
                            {
                                AppendIndent();
                            }
                            else if (seperate)
                            {
                                Append(" ");
                                seperate = false;
                            }

                            _Output.Append(text, innerPos, splitCount);

                            remaining -= splitCount;
                            chunkSize = Math.Min(remaining, text.Length - splitPos - 1);
                            innerPos = splitPos + 1;

                            splitPos = Math.Min(chunkSize + innerPos - 1, text.Length - 1);
                        }

                        if (remaining == 0 || splitCount <= 0)
                        {
                            AppendEnding(MarkdownTokenFlag.LineEnding);
                            remaining = maxWidth;
                            chunkSize = Math.Min(remaining, text.Length - innerPos);
                            splitPos = innerPos + chunkSize - 1;
                            seperate = false;
                        }
                    }
                }

                if (_Stream.PeakTokenType() != MarkdownTokenType.Text)
                {
                    AppendEnding(_Stream.Current.Flag);
                }
                else if (_Stream.Current.Flag.IsLineBreak() || _Stream.Current.Flag.ShouldPreserve())
                {
                    AppendEnding(MarkdownTokenFlag.LineEnding);

                    remaining = maxWidth;
                }

                _Stream.Next();
            }
        }

        private void WrapAndAppendLines(string text, StringBuilder sb)
        {
            const string singleSpace = " ";

            var words = text.Split(' ');
            text = "";

            foreach (var word in words)
            {
                if (word.Contains(NewLine))
                {
                    var breakLine = word.Split(new[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in breakLine)
                    {
                        if (part == breakLine.Last())
                        {
                            text += part + singleSpace;
                        }
                        else
                        {
                            text += part;
                            sb.AppendFormat("{0}{1}{2}", AboutIndentation, text, NewLine);
                            text = "";
                        }
                    }
                }
                else if (text.Length + word.Length > (_MaxLineWidth - 4))
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(text.Substring(text.Length - 1), singleSpace))
                    {
                        text = text.Substring(0, text.Length - 1);
                    }
                    sb.AppendFormat("{0}{1}{2}", AboutIndentation, text, NewLine);

                    text = word + singleSpace;
                }
                else
                {
                    text += word + singleSpace;
                }
            }

            if (text.Length <= 0 || StringComparer.OrdinalIgnoreCase.Equals(text, singleSpace))
            {
                return;
            }
            if (StringComparer.OrdinalIgnoreCase.Equals(text.Substring(text.Length - 1), singleSpace))
            {
                text = text.Substring(0, text.Length - 1);
            }

            sb.AppendFormat("{0}{1}", AboutIndentation, text);
        }
    }
}
