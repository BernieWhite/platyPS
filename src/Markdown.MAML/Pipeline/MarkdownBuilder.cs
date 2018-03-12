using Markdown.MAML.Model.MAML;
using System;
using System.Collections.Generic;

namespace Markdown.MAML.Pipeline
{
    internal sealed class ParameterComparer : IComparer<MamlParameter>
    {
        public static ParameterComparer Ordered = new ParameterComparer();

        public int Compare(MamlParameter x, MamlParameter y)
        {
            var result = StringCompare(x.Name, y.Name);

            if (result == 0)
            {
                return 0;
            }

            if (StringCompare(x.Name, "Confirm") == 0 || StringCompare(x.Name, "WhatIf") == 0)
            {
                if (StringCompare(x.Name, "Confirm") == 0 && StringCompare(y.Name, "WhatIf") == 0)
                {
                    return -1;
                }

                return 1;
            }
            else if (StringCompare(y.Name, "Confirm") == 0 || StringCompare(y.Name, "WhatIf") == 0)
            {
                return -1;
            }

            return result;
        }

        private int StringCompare(string x, string y)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(x, y);
        }
    }

    public sealed class MarkdownBuilder
    {
        private VisitMamlCommand _MamlAction;
        private bool _NoMetadata;
        private bool _PreserveFormatting;

        internal MarkdownBuilder()
        {
            _MamlAction = MamlCommandActions.EmptyMamlCommandDelegate;
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

        public void AddMamlAction(VisitMamlCommandAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _MamlAction;
            _MamlAction = node => action(node, previous);
        }

        public IMarkdownPipeline Build()
        {
            return new MarkdownPipeline(_MamlAction, _NoMetadata, _PreserveFormatting);
        }
    }
}