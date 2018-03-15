namespace Markdown.MAML.Configuration
{
    public sealed class MarkdownOptions
    {
        /// <summary>
        /// The default infoString to use when code fenced section do not specify a langage.
        /// </summary>
        public string InfoString { get; set; }

        public int Width { get; set; }
    }
}
