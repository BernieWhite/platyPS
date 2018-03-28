using Markdown.MAML.Model.Markdown;
using System;
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

        /// <summary>
        /// The positional order of the parameter. When this value is null the parameter is Named.
        /// </summary>
        public byte? Position { get; set; }

        public string[] Aliases { get; set; }

        public string[] Applicable { get; set; }

        public bool ValueRequired
        {
            get
            {
                return !(StringComparer.OrdinalIgnoreCase.Equals(Type, "SwitchParameter") || StringComparer.OrdinalIgnoreCase.Equals(Type, "switch"));
            }
        }

        public bool ValueVariableLength { get; set; }

        /// <summary>
        /// Corresponds to "Accepted values"
        /// </summary>
        public string[] ParameterValueGroup { get; set; }

        public string[] ParameterSet { get; set; }

        public MamlParameter()
        {
            VariableLength = true;
            ValueVariableLength = false;
            Globbing = false;
            PipelineInput = "false";
            Position = null;
            Aliases = new string[0];
        }

        public MamlParameter Clone()
        {
            return (MamlParameter)MemberwiseClone();
        }
    }
}