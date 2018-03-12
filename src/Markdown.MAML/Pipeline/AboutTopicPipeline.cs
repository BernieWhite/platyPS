using Markdown.MAML.Model.About;
using Markdown.MAML.Parser;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    /// <summary>
    /// A pipeline to generate an about topic.
    /// </summary>
    internal sealed class AboutTopicPipeline : IAboutTopicPipeline
    {
        private VisitMamlCommand _Action;
        private string[] _Tags;

        internal AboutTopicPipeline(VisitMamlCommand action, string[] tags)
        {
            _Action = action;
            _Tags = tags;
        }

        public AboutTopic Process(string markdown, string path)
        {
            return ProcessCore(markdown, path);
        }

        public AboutTopic Process(string path, Encoding encoding)
        {
            var markdown = File.ReadAllText(path, encoding);

            return ProcessCore(markdown, path);
        }

        private AboutTopic ProcessCore(string markdown, string path)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: false);
            var stream = reader.Read(markdown, path);

            var lexer = new AboutTopicLexer();

            var topic = lexer.Process(stream, _Tags);

            //if (topic == null || !_Action(topic))
            //{
            //    return null;
            //}

            return topic;
        }
    }
}