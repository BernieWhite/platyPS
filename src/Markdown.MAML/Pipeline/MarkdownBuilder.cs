using Markdown.MAML.Configuration;
using Markdown.MAML.Model.MAML;
using System;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    internal sealed class ParameterComparer : IComparer<MamlParameter>
    {
        public static ParameterComparer Ordered = new ParameterComparer();

        private const string PARAMETERNAME_CONFIRM = "confirm";
        private const string PARAMETERNAME_WHATIF = "whatif";
        private const string PARAMETERNAME_INCLUDETOTALCOUNT = "IncludeTotalCount";
        private const string PARAMETERNAME_SKIP = "skip";
        private const string PARAMETERNAME_FIRST = "first";

        public int Compare(MamlParameter x, MamlParameter y)
        {
            var result = StringCompare(x.Name, y.Name);

            if (result == 0)
            {
                return 0;
            }

            if (IsCommon(x.Name) && IsCommon(y.Name))
            {
                // Order common
                return GetCommonOrder(x.Name) - GetCommonOrder(y.Name);
            }

            if (IsCommon(x.Name))
            {
                return 1;
            }

            if (IsCommon(y.Name))
            {
                return -1;
            }

            return result;
        }

        /// <summary>
        /// Check for common parameters. i.e. -Confirm, -WhatIf, -IncludeTotalCount, -Skip or -First
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>True when the parameter name matches a common parameter. Otherwise false.</returns>
        private bool IsCommon(string parameterName)
        {
            return StringCompare(parameterName, PARAMETERNAME_CONFIRM) == 0 ||
                StringCompare(parameterName, PARAMETERNAME_WHATIF) == 0 ||
                StringCompare(parameterName, PARAMETERNAME_INCLUDETOTALCOUNT) == 0 ||
                StringCompare(parameterName, PARAMETERNAME_SKIP) == 0 ||
                StringCompare(parameterName, PARAMETERNAME_FIRST) == 0;
        }

        /// <summary>
        /// Get the order of common parameters.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>Returns the order of the parameter.</returns>
        private int GetCommonOrder(string parameterName)
        {
            if (StringCompare(parameterName, PARAMETERNAME_CONFIRM) == 0)
            {
                return 1;
            }

            if (StringCompare(parameterName, PARAMETERNAME_WHATIF) == 0)
            {
                return 2;
            }

            if (StringCompare(parameterName, PARAMETERNAME_INCLUDETOTALCOUNT) == 0)
            {
                return 3;
            }

            if (StringCompare(parameterName, PARAMETERNAME_SKIP) == 0)
            {
                return 4;
            }

            if (StringCompare(parameterName, PARAMETERNAME_FIRST) == 0)
            {
                return 5;
            }

            return 0;
        }

        private int StringCompare(string x, string y)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(x, y);
        }
    }

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