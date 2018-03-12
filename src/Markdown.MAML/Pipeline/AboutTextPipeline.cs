using Markdown.MAML.Model.About;
using Markdown.MAML.Parser;
using Markdown.MAML.Renderer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    /// <summary>
    /// A pipeline to generate an about topic.
    /// </summary>
    internal sealed class AboutTextPipeline : IAboutTextPipeline
    {
        private VisitMamlCommand _Action;
        private string[] _Tags;

        internal AboutTextPipeline(VisitMamlCommand action, string[] tags)
        {
            _Action = action;
            _Tags = tags;
        }

        public string Process(AboutTopic topic)
        {
            return Write(topic);
        }

        public string Process(string markdown, string path)
        {
            return Write(Read(markdown, path));
        }

        public string Process(string path, Encoding encoding)
        {
            var markdown = File.ReadAllText(path, encoding);

            return Write(Read(markdown, path));
        }

        private AboutTopic Read(string markdown, string path)
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

        private string Write(AboutTopic topic)
        {
            var renderer = new TextRenderer();

            return renderer.AboutMarkdownToString(topic);
        }
    }
}
