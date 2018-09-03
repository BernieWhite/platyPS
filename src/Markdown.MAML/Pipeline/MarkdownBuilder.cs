using Markdown.MAML.Configuration;

namespace Markdown.MAML.Pipeline
{
    public sealed class MarkdownBuilder
    {
        private VisitMamlCommand _MamlCommandAction;
        private VisitMarkdown _MarkdownAction;
        private bool _NoMetadata;
        private bool _PreserveFormatting;
        private int _SyntaxWidth;

        internal MarkdownBuilder()
        {
            _MamlCommandAction = PipelineHook.EmptyMamlCommandDelegate;
            _MarkdownAction = PipelineHook.EmptyMarkdownDelegate;
            _NoMetadata = false;
            _PreserveFormatting = false;
            _SyntaxWidth = MarkdownOption.DEFAULT_SYNTAX_WIDTH;
        }

        public void UseNoMetadata()
        {
            _NoMetadata = true;
        }

        public void UsePreserveFormatting()
        {
            _PreserveFormatting = true;
        }

        public void UseFirstExample()
        {
            AddMamlAction(MamlCommandActions.AddFirstExample);
        }

        public void UseSortParamsAlphabetic()
        {
            AddMamlAction(MamlCommandActions.SortParamsAlphabetic);
        }

        public void SetOnlineVersionUrl()
        {
            AddMamlAction(MamlCommandActions.DetectOnlineVersionMetadata);
        }

        private void DetectPowerShellLanguage(string infoString)
        {
            AddMamlAction((node, next) => MamlCommandActions.DetectLanguage(node, next, infoString));
        }

        public void AddMamlAction(VisitMamlCommandAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _MamlCommandAction;
            _MamlCommandAction = node => action(node, previous);
        }

        public void AddMarkdownAction(VisitMarkdownAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _MarkdownAction;
            _MarkdownAction = (markdown, path) => action(markdown, path, previous);
        }

        public IMarkdownPipeline Build()
        {
            return new MarkdownPipeline(_MamlCommandAction, _MarkdownAction, _NoMetadata, _PreserveFormatting, _SyntaxWidth);
        }

        public MarkdownBuilder Configure(MarkdownPipelineConfiguration config)
        {
            config?.Invoke(this);

            return this;
        }

        public MarkdownBuilder Configure(MarkdownHelpOption option)
        {
            if (option == null)
            {
                return this;
            }

            _SyntaxWidth = option.Markdown.Width;

            if (!string.IsNullOrEmpty(option.Markdown.InfoString))
            {
                DetectPowerShellLanguage(option.Markdown.InfoString);
            }

            if (option.Markdown.ParameterSort == ParameterSort.Name)
            {
                UseSortParamsAlphabetic();
            }

            if (option.Pipeline.WriteCommand.Count > 0)
            {
                foreach (var action in option.Pipeline.WriteCommand)
                {
                    AddMamlAction((command, next) =>
                    {
                        action(command);

                        return true;
                    });
                }
            }

            if (option.Pipeline.WriteMarkdown.Count > 0)
            {
                foreach (var action in option.Pipeline.WriteMarkdown)
                {
                    AddMarkdownAction((markdown, path, next) =>
                    {
                        return next(action(markdown, path), path);
                    });
                }
            }

            return this;
        }
    }
}