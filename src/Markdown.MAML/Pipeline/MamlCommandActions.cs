using Markdown.MAML.Model.MAML;
using System;

namespace Markdown.MAML.Pipeline
{
    public delegate bool VisitMamlCommand(MamlCommand node);

    public delegate bool VisitMamlCommandAction(MamlCommand node, VisitMamlCommand next);

    internal static class MamlCommandActions
    {
        public static VisitMamlCommand EmptyMamlCommandDelegate = next => { return true; };

        public static bool DetectLanguage(MamlCommand node, VisitMamlCommand next, string infoString)
        {
            // Process example code blocks
            if (node.Examples != null)
            {
                foreach (var example in node.Examples)
                {

                }
            }

            // Continue to next action
            return next(node);
        }

        /// <summary>
        /// Detect PowerShell based on first line of code block
        /// - Look for PS C:\> prefix
        /// - Look for PS> prefix - https://github.com/PowerShell/PowerShell-Docs/blob/staging/contributing/FORMATTING-CODE.md
        /// </summary>
        private static bool IsPowerShellExample(string text)
        {
            return text.StartsWith("PS C:\\>") || text.StartsWith("PS>", StringComparison.OrdinalIgnoreCase);
        }

        public static bool AddFirstExample(MamlCommand node, VisitMamlCommand next)
        {
            if (node.Examples.Count == 0)
            {
                var example = new MamlExample
                {
                    Title = "Example 1",
                    Code = new[] { new MamlCodeBlock(@"PS C:\> {{ Add example code here }}", "powershell") },
                    Remarks = "{{ Add example description here }}"
                };

                node.Examples.Add(example);
            }

            return next(node);
        }

        public static bool SortParamsAlphabetic(MamlCommand node, VisitMamlCommand next)
        {
            if (node.Parameters.Count > 0)
            {
                node.Parameters.Sort(ParameterComparer.Ordered);

                // Sort by name

                // But always add confirm and whatif last
            }

            return next(node);
        }

        /// <summary>
        /// Sets the link moniker to detect as the online version.
        /// </summary>
        private const string MAML_ONLINE_LINK_DEFAULT_MONIKER = "Online Version:";

        public static bool DetectOnlineVersionMetadata(MamlCommand node, VisitMamlCommand next)
        {
            if (node.OnlineVersionUrl == null)
            {
                if (node.Links?.Count > 0)
                {
                    node.OnlineVersionUrl = node.Links[0].LinkUri;

                    if (StringComparer.OrdinalIgnoreCase.Compare(node.Links[0].LinkName, MAML_ONLINE_LINK_DEFAULT_MONIKER) == 0)
                    {
                        node.Links.RemoveAt(0);
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Compare(node.Links[0].LinkName, node.Links[0].LinkUri) == 0)
                    {
                        node.Links.RemoveAt(0);
                    }
                }
            }

            return next(node);
        }
    }
}
