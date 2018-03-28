using Markdown.MAML.Model.Markdown;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Markdown.MAML.Configuration
{
    /// <summary>
    /// A delgate to allow callback to PowerShell to get current working path.
    /// </summary>
    public delegate string GetWorkingPathDelegate();

    public sealed class MarkdownHelpOption
    {
        public MarkdownHelpOption()
        {
            // Set defaults
            Markdown = new MarkdownOption();

            Pipeline = new PipelineHook();
        }

        public MarkdownHelpOption(MarkdownHelpOption option)
        {
            // Set from existing option instance
            Markdown = new MarkdownOption
            {
                InfoString = option.Markdown.InfoString,
                ParameterSort = option.Markdown.ParameterSort,
                SectionFormat = option.Markdown.SectionFormat,
                Width = option.Markdown.Width
            };

            Pipeline = new PipelineHook
            {
                ReadMarkdown = option.Pipeline.ReadMarkdown,
                WriteMarkdown = option.Pipeline.WriteMarkdown,
                ReadCommand = option.Pipeline.ReadCommand,
                WriteCommand = option.Pipeline.WriteCommand
            };
        }

        /// <summary>
        /// A callback that is overridden by PowerShell so that the current working path can be retrieved.
        /// </summary>
        public static GetWorkingPathDelegate GetWorkingPath = () => Directory.GetCurrentDirectory();

        /// <summary>
        /// Options that affect markdown formatting.
        /// </summary>
        public MarkdownOption Markdown { get; set; }

        [YamlIgnore()]
        public PipelineHook Pipeline { get; set; }

        public string ToYaml()
        {
            var s = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return s.Serialize(this);
        }

        public MarkdownHelpOption Clone()
        {
            return new MarkdownHelpOption(this);
        }

        public static MarkdownHelpOption FromFile(string path, bool silentlyContinue = false)
        {
            // Ensure that a full path instead of a path relative to PowerShell is used for .NET methods
            var rootedPath = GetRootedPath(path);

            // Fallback to defaults even if file does not exist when silentlyContinue is true
            if (!File.Exists(rootedPath))
            {
                if (!silentlyContinue)
                {
                    throw new FileNotFoundException("", rootedPath);
                }
                else
                {
                    // Use the default options
                    return new MarkdownHelpOption();
                }
            }

            return FromYaml(File.ReadAllText(rootedPath));
        }

        public static MarkdownHelpOption FromYaml(string yaml)
        {
            var d = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return d.Deserialize<MarkdownHelpOption>(yaml) ?? new MarkdownHelpOption();
        }

        /// <summary>
        /// Convert from hashtable to options by processing key values. This enables -Option @{ } from PowerShell.
        /// </summary>
        /// <param name="hashtable"></param>
        public static implicit operator MarkdownHelpOption(Hashtable hashtable)
        {
            var option = new MarkdownHelpOption();

            // Build index to allow mapping
            var index = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (DictionaryEntry entry in hashtable)
            {
                index.Add(entry.Key.ToString(), entry.Value);
            }

            // Start loading matching values

            object value;

            if (index.TryGetValue("markdown.width", out value))
            {
                option.Markdown.Width = (int)value;
            }

            if (index.TryGetValue("markdown.infostring", out value))
            {
                option.Markdown.InfoString = (string)value;
            }

            if (index.TryGetValue("markdown.parametersort", out value))
            {
                option.Markdown.ParameterSort = (ParameterSort)Enum.Parse(typeof(ParameterSort), (string)value, ignoreCase: true);
            }

            if (index.TryGetValue("markdown.sectionformat", out value))
            {
                option.Markdown.SectionFormat = (SectionFormatOption)Enum.Parse(typeof(SectionFormatOption), (string)value, ignoreCase: true);
            }

            return option;
        }

        /// <summary>
        /// Convert from string to options by loading the yaml file from disk. This enables -Option '.\.platyps.yml' from PowerShell.
        /// </summary>
        /// <param name="path"></param>
        public static implicit operator MarkdownHelpOption(string path)
        {
            var option = FromFile(path);

            return option;
        }

        /// <summary>
        /// Get a full path instead of a relative path that may be passed from PowerShell.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetRootedPath(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(GetWorkingPath(), path);
        }
    }
}
