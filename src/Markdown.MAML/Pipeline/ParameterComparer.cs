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
        private const string PARAMETERNAME_INCLUDETOTALCOUNT = "includetotalcount";
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
}
