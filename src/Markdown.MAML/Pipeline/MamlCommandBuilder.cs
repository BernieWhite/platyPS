using Markdown.MAML.Model.MAML;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Markdown.MAML.Pipeline
{
    public sealed class MamlCommandBuilder
    {
        private VisitMamlCommand _MamlAction;
        private bool _PreserveFormatting;
        private List<string> _Tags;

        internal MamlCommandBuilder()
        {
            _MamlAction = MamlCommandActions.EmptyMamlCommandDelegate;
            _PreserveFormatting = false;
            _Tags = new List<string>();
        }

        public IMamlCommandPipeline Build()
        {
            return new MamlCommandPipeline(_MamlAction, _Tags.ToArray(), _PreserveFormatting);
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
            AddMamlAction(CheckSchema);
        }

        public void SetOnlineVersionUrlLink()
        {
            AddMamlAction(UpdateOnlineVersionLink);
        }

        public void AddMamlAction(VisitMamlCommandAction action)
        {
            // Nest the previous write action in the new supplied action
            // Execution chain will be: action -> previous -> previous..n
            var previous = _MamlAction;
            _MamlAction = node => action(node, previous);
        }

        private const string ONLINE_VERSION_YAML_HEADER = "online version";
        private const string MAML_ONLINE_LINK_DEFAULT_MONIKER = "Online Version:";

        private bool UpdateOnlineVersionLink(MamlCommand node, VisitMamlCommand next)
        {
            if (node.Metadata.ContainsKey(ONLINE_VERSION_YAML_HEADER))
            {
                var onlineUrl = node.Metadata[ONLINE_VERSION_YAML_HEADER];

                if (!string.IsNullOrEmpty(onlineUrl))
                {
                    var first = node.Links.FirstOrDefault();

                    if (first == null || first.LinkUri != onlineUrl)
                    {
                        var link = new MamlLink
                        {
                            LinkName = MAML_ONLINE_LINK_DEFAULT_MONIKER,
                            LinkUri = onlineUrl
                        };

                        node.Links.Insert(0, link);
                    }
                }
            }

            return next(node);
        }

        private bool CheckSchema(MamlCommand node, VisitMamlCommand next)
        {
            if (!node.Metadata.ContainsKey("schema") || node.Metadata["schema"] != "2.0.0")
            {
                throw new Exception("PlatyPS schema version 1.0.0 is deprecated and not supported anymore. Please install platyPS 0.7.6 and migrate to the supported version.");
            }

            return next(node);
        }
    }
}
