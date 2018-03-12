using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    public sealed class AboutTopicBuilder
    {
        private List<string> _Tags;

        internal AboutTopicBuilder()
        {
            _Tags = new List<string>();
        }

        public IAboutTopicPipeline Build()
        {
            return new AboutTopicPipeline(null, _Tags.ToArray());
        }

        public AboutTopicBuilder UseApplicableTag(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return this;
            }

            _Tags.AddRange(tags);

            return this;
        }
    }
}