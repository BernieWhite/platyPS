using Markdown.MAML.Configuration;
using Markdown.MAML.Model.MAML;
using System;
using System.Linq;

namespace Markdown.MAML.Pipeline
{
    internal static class MamlCommandActions
    {
        public static bool DetectLanguage(MamlCommand node, VisitMamlCommand next, string defaultInfoString)
        {
            var shouldDefault = !string.IsNullOrEmpty(defaultInfoString);

            // Process example code blocks
            if (node.Examples != null)
            {
                foreach (var example in node.Examples)
                {
                    foreach (var code in example.Code)
                    {
                        // Only process code blocks that do not already have a language set
                        if (string.IsNullOrEmpty(code.LanguageMoniker))
                        {
                            if (IsPowerShellExample(code.Text))
                            {
                                code.LanguageMoniker = "powershell";
                            }
                            else if (shouldDefault)
                            {
                                code.LanguageMoniker = defaultInfoString;
                            }
                        }
                    }
                }
            }

            // If default is set process other code blocks
            if (shouldDefault)
            {
                // TODO: Revisit syntax blocks
                //if (node.Syntax != null)
                //{
                //    foreach (var syntax in node.Syntax)
                //    {
                        
                //    }
                //}
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

        private const string ONLINE_VERSION_YAML_HEADER = "online version";

        public static bool UpdateOnlineVersionLink(MamlCommand node, VisitMamlCommand next)
        {
            if (!string.IsNullOrEmpty(node.OnlineVersionUrl))
            {
                var first = node.Links.FirstOrDefault();

                if (first == null || first.LinkUri != node.OnlineVersionUrl)
                {
                    var link = new MamlLink
                    {
                        LinkName = MAML_ONLINE_LINK_DEFAULT_MONIKER,
                        LinkUri = node.OnlineVersionUrl
                    };

                    node.Links.Insert(0, link);
                }

            }

            return next(node);
        }

        public static bool CheckSchema(MamlCommand node, VisitMamlCommand next)
        {
            if (!node.Metadata.ContainsKey("schema") || node.Metadata["schema"] != "2.0.0")
            {
                throw new Exception("PlatyPS schema version 1.0.0 is deprecated and not supported anymore. Please install platyPS 0.7.6 and migrate to the supported version.");
            }

            return next(node);
        }
    }
}
