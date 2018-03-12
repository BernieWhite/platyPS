namespace Markdown.MAML.Parser
{
    public enum ParserMode
    {
        Full,
        /// <summary>
        /// It's aimed to be used in a merge scenario. 
        /// It will allow us preserve formatting existin Markdown as is.
        /// It doesn't try to do the following:
        /// 
        /// - escaping characters
        /// - parse hyper-links
        /// - handle soft-breaks and hard-breaks
        /// </summary>
        FormattingPreserve
    }
}
