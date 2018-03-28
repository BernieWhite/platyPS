namespace Markdown.MAML.Model.Markdown
{
    /// <summary>
    /// A section of text with formatting options.
    /// </summary>
    public sealed class SectionBody
    {
        public SectionBody(string text, SectionFormatOption formatOption = SectionFormatOption.None)
        {
            Text = text;
            FormatOption = formatOption;
        }

        /// <summary>
        /// The text of the section body.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Additional options that determine how the section will be formated when rendering markdown.
        /// </summary>
        public SectionFormatOption FormatOption { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
