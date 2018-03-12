using System;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    public sealed class AboutTextBuilder
    {
        private List<string> _Tags;

        internal AboutTextBuilder()
        {
            _Tags = new List<string>();
        }

        public IAboutTextPipeline Build()
        {
            return new AboutTextPipeline(null, _Tags.ToArray());
        }
    }
}