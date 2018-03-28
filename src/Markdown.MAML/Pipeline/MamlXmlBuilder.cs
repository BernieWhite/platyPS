using Markdown.MAML.Configuration;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    public sealed class MamlXmlBuilder
    {
        private VisitMamlCommand _ReadMamlCommandHook;
        private VisitMarkdown _ReadMarkdownHook;
        private List<string> _Tags;

        internal MamlXmlBuilder()
        {
            _ReadMamlCommandHook = PipelineHook.EmptyMamlCommandDelegate;
            _ReadMarkdownHook = PipelineHook.EmptyMarkdownDelegate;
            _Tags = new List<string>();

            SetOnlineVersionUrlLink();
        }

        public IMamlXmlPipeline Build()
        {
            return new MamlXmlPipeline(_ReadMamlCommandHook, _ReadMarkdownHook, _Tags.ToArray());
        }

        public void UseApplicableTag(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return;
            }

            _Tags.AddRange(tags);
        }

        public void UseSchema()
        {
            AddMamlAction(MamlCommandActions.CheckSchema);
        }

        private void SetOnlineVersionUrlLink()
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

        public MamlXmlBuilder Configure(MamlXmlPipelineConfiguration config)
        {
            config?.Invoke(this);

            return this;
        }

        public MamlXmlBuilder Configure(MarkdownHelpOption option)
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

                        return true;
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