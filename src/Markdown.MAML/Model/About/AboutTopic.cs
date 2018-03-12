using Markdown.MAML.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Markdown.MAML.Model.About
{
    public sealed class AboutTopic
    {
        public AboutTopic()
        {
            Children = new List<MarkdownToken>();
        }

        public string Name { get; set; }

        public List<MarkdownToken> Children { get; set; }
    }
}
