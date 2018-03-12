using System;
using System.Collections;
using System.Collections.Generic;
using Markdown.MAML.Model.Markdown;

namespace Markdown.MAML.Model.MAML
{
    public sealed class MamlCommand
    {
        private const string ONLINE_VERSION_YAML_HEADER = "online version";
        private const string MODULE_PAGE_MODULE_NAME = "Module Name";

        public string Name { get; set; }

        public SectionBody Synopsis { get; set; }

        public SectionBody Description { get; set; }

        public List<MamlInputOutput> Inputs 
        {
            get { return _inputs; }
        }

        public List<MamlInputOutput> Outputs
        {
            get { return _outputs; }
        }

        public List<MamlParameter> Parameters 
        {
            get { return _parameters; } 
        }

        public SectionBody Notes { get; set; }

        public bool IsWorkflow { get; set; }

        public bool SupportCommonParameters { get; set; }

        public string DefaultParameterSet { get; set; }

        public string ModuleName
        {
            get { return GetMetadata(MODULE_PAGE_MODULE_NAME); }
            set { SetMetadata(MODULE_PAGE_MODULE_NAME, value); }
        }

        public string OnlineVersionUrl
        {
            get { return GetMetadata(ONLINE_VERSION_YAML_HEADER); }
            set { SetMetadata(ONLINE_VERSION_YAML_HEADER, value); }
        }

        public List<MamlExample> Examples
        {
            get { return _examples; }
        } 

        public List<MamlLink> Links
        {
            get { return _links; }
        }

        public MamlPropertySet<MamlSyntax> Syntax
        {
            get { return _syntax; }
        }

        public Dictionary<string, string> Metadata { get; set; }

        private List<MamlParameter> _parameters = new List<MamlParameter>();

        private List<MamlInputOutput> _outputs = new List<MamlInputOutput>();

        private List<MamlInputOutput> _inputs = new List<MamlInputOutput>();

        private List<MamlExample> _examples = new List<MamlExample>();

        private List<MamlLink> _links = new List<MamlLink>();

        private MamlPropertySet<MamlSyntax> _syntax = new MamlPropertySet<MamlSyntax>();

        public MamlCommand()
        {
            // this is the default most often then not
            SupportCommonParameters = true;

            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetMetadata(Hashtable hashtable)
        {
            if (hashtable == null || hashtable.Count == 0)
            {
                return;
            }

            if (Metadata == null)
            {
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (DictionaryEntry pair in hashtable)
            {
                Metadata[pair.Key.ToString()] = pair.Value == null ? string.Empty : pair.Value.ToString();
            }
        }

        public void SetMetadata(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (Metadata == null)
            {
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            Metadata[key] = value;
        }

        public string GetMetadata(string key)
        {
            if (Metadata == null || !Metadata.ContainsKey(key))
            {
                return null;
            }

            return Metadata[key];
        }

        public static MamlCommand Create()
        {
            return new MamlCommand
            {
                Synopsis = new SectionBody("{{Fill in the Synopsis}}"),
                Description = new SectionBody("{{Fill in the Description}}")
            };
        }
    }
}
