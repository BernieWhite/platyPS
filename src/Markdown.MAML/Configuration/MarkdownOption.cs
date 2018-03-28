using Markdown.MAML.Model.Markdown;
using System.ComponentModel;

namespace Markdown.MAML.Configuration
{
    /// <summary>
    /// Options that affect markdown formatting.
    /// </summary>
    public sealed class MarkdownOption
    {
        /// <summary>
        /// 110 is a good width value, because it doesn't cause github to add horizontal scroll bar
        /// </summary>
        internal const int DEFAULT_SYNTAX_WIDTH = 110;

        private const ParameterSort DEFAULT_PARAMETER_SORT = ParameterSort.None;

        private const SectionFormatOption DEFAULT_SECTION_FORMAT = SectionFormatOption.LineBreakAfterHeader;

        public MarkdownOption()
        {
            InfoString = null;
            Width = DEFAULT_SYNTAX_WIDTH;
            ParameterSort = DEFAULT_PARAMETER_SORT;
            SectionFormat = DEFAULT_SECTION_FORMAT;
        }

        /// <summary>
        /// The default infoString to use when code fenced section do not specify a langage.
        /// </summary>
        [DefaultValue(null)]
        public string InfoString { get; set; }

        [DefaultValue(DEFAULT_SYNTAX_WIDTH)]
        public int Width { get; set; }

        /// <summary>
        /// Determines if parameters should be sorted.
        /// </summary>
        [DefaultValue(DEFAULT_PARAMETER_SORT)]
        public ParameterSort ParameterSort { get; set; }

        [DefaultValue(DEFAULT_SECTION_FORMAT)]
        public SectionFormatOption SectionFormat { get; set; }
    }
}
