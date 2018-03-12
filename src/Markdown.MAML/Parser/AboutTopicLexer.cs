using Markdown.MAML.Model.About;
using Markdown.MAML.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Markdown.MAML.Parser
{
    /// <summary>
    /// A lexer that inteprets markdown as an about topic.
    /// </summary>
    internal sealed class AboutTopicLexer : MarkdownLexer
    {
        private const int ABOUT_TOPIC_HEADING_LEVEL = 1;
        private const int ABOUT_TOPIC_NAME_HEADING_LEVEL = 2;

        public AboutTopic Process(TokenStream stream, string[] tags)
        {
            stream.MoveTo(0);

            AboutTopic topic = null;

            while (!stream.EOF)
            {
                if (IsHeading(stream.Current, ABOUT_TOPIC_NAME_HEADING_LEVEL) && topic == null)
                {
                    topic = new AboutTopic
                    {
                        Name = stream.Current.Text
                    };
                }
                else if (topic != null)
                {
                    topic.Children.Add(stream.Current);
                }

                stream.Next();
            }

            return topic;
        }
    }
}
