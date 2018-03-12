using System.Collections.Generic;
using Markdown.MAML.Model.Markdown;
using System;
using System.Linq;
using System.Diagnostics;

namespace Markdown.MAML.Model.MAML
{
    [DebuggerDisplay("Name = {Name}, Required = {Required}")]
    public sealed class MamlParameter : INamed
    {
        public SourceExtent Extent { get; set; }

        /// <summary>
        /// Additional options that determine how the section will be formated when rendering markdown.
        /// </summary>
        public SectionFormatOption FormatOption { get; set; }

        public string Type { get; set; }

        public string FullType { get; set; }

        public string Name { get; set; }

        public bool Required { get; set; }

        public string Description { get; set; }

        public string DefaultValue { get; set; }

        public bool VariableLength { get; set; }

        /// <summary>
        /// Corresponds to "Accept wildcard characters"
        /// </summary>
        public bool Globbing { get; set; }

        public string PipelineInput { get; set; }

        public string Position { get; set; }

        public string[] Aliases { get; set; }

        public string[] Applicable { get; set; }

        public bool ValueRequired { get; set; }

        public bool ValueVariableLength { get; set; }

        /// <summary>
        /// This string is used only in schema version 1.0.0 internal processing
        /// </summary>
        internal string AttributesMetadata { get; set; }

        public List<string> ParameterValueGroup
        {
            get { return _parameterValueGroup; }
        }

        public string[] ParameterSet { get; set; }

        private readonly List<string> _parameterValueGroup = new List<string>();

        public MamlParameter()
        {
            VariableLength = true;
            ValueVariableLength = false;
            Globbing = false;
            PipelineInput = "false";
            Position = "Named";
            Aliases = new string[] {};
        }

        public MamlParameter Clone()
        {
            return (MamlParameter)this.MemberwiseClone();
        }
    }
}