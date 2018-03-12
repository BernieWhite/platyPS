using Markdown.MAML.Pipeline;

namespace Markdown.MAML
{
    public sealed class MarkdownOption
    {
        // Set default empty delegate
        //private static VisitMamlCommand EmptyMamlCommandDelegate = next => { };

        public VisitMamlCommand MarkdownWriteAction { get; private set; }

        public string DefaultInfoString { get; set; }

        public MarkdownOption()
        {
            //MarkdownWriteAction = EmptyMamlCommandDelegate;
            DefaultInfoString = string.Empty;
        }

        ///// <summary>
        ///// Add a markdown write action.
        ///// </summary>
        ///// <param name="action">The action to add</param>
        //public void AddMarkdownWriteAction(VisitMamlCommandAction action)
        //{
        //    // Nest the previous write action in the new supplied action
        //    // Execution chain will be: action -> previous -> previous..n
        //    var previous = MarkdownWriteAction;
        //    MarkdownWriteAction = node => action(node, previous);
        //}
    }
}