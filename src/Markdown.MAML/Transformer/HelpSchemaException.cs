using System;
using Markdown.MAML.Model.Markdown;

namespace Markdown.MAML.Transformer
{
    public class HelpSchemaException : ArgumentException
    {
        private static string GetSnippet(string input)
        {
            if (input.Length < 50)
            {
                return input;
            }
            return input.Substring(0, 50) + "...";
        }

        public HelpSchemaException(SourceExtent extent, string message)
            : base(
            String.Format(
                "{0}:{1}:({2}) '{3}'\n {4}",
                extent.Path,
                extent.Start, 
                extent.Column, 
                GetSnippet(extent.Text), 
                message)
            )
        {
        }
    }
}
