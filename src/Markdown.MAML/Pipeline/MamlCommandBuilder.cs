using Markdown.MAML.Configuration;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    public sealed class MamlCommandBuilder
    {
        private VisitMamlCommand _ReadMamlCommandHook;
        private VisitMarkdown _ReadMarkdownHook;
        private bool _PreserveFormatting;
        private List<string> _Tags;

        internal MamlCommandBuilder()
        {
            _ReadMamlCommandHook = PipelineHook.EmptyMamlCommandDelegate;
            _ReadMarkdownHook = PipelineHook.EmptyMarkdownDelegate;
            _PreserveFormatting = false;
            _Tags = new List<string>();
        }

        public IMamlCommandPipeline Build()
        {
            return new MamlCommandPipeline(_ReadMamlCommandHook, _ReadMarkdownHook, _Tags.ToArray(), _PreserveFormatting);
        }

        public void UseApplicableTag(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return;
            }

            _Tags.AddRange(tags);
        }

        public void UsePreserveFormatting()
        {
            _PreserveFormatting = true;
        }

        public void UseSchema()
        {
            AddMamlAction(MamlCommandActions.CheckSchema);
        }

        public void SetOnlineVersionUrlLink()
        {
            AddMamlAction(MamlCommandActions.UpdateOnlineVersionLink);
        }

        public void AddMamlAction(VisitMamlCommandAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _ReadMamlCommandHook;
            _ReadMamlCommandHook = node => action(node, previous);
        }

        public void AddMarkdownAction(VisitMarkdownAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _ReadMarkdownHook;
            _ReadMarkdownHook = (markdown, path) => action(markdown, path, previous);
        }

        public MamlCommandBuilder Configure(MamlCommandPipelineConfiguration config)
        {
            config?.Invoke(this);

            return this;
        }

        public MamlCommandBuilder Configure(MarkdownHelpOption option)
        {
            if (option == null)
            {
                return this;
            }

            if (option.Pipeline.ReadCommand.Count > 0)
            {
                foreach (var action in option.Pipeline.ReadCommand)
                {
                    AddMamlAction((command, next) =>
                    {
                        action(command);

                        return next(command);
                    });
                }
            }

            if (option.Pipeline.ReadMarkdown.Count > 0)
            {
                foreach (var action in option.Pipeline.ReadMarkdown)
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
