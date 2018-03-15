using System.Collections;
using System.IO;

namespace Markdown.MAML.Configuration
{
    public sealed class MarkdownHelpOptions
    {
        public MarkdownHelpOptions()
        {
            // Set defaults
            Markdown = new MarkdownOptions
            {
                InfoString = string.Empty,
                Width = 110
            };


            // Attempt to load .platyps.yml
            //LoadCore(".platyps.yml");
        }

        public MarkdownOptions Markdown { get; set; }

        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("", path);
            }


        }

        /// <summary>
        /// Convert from hashtable to options by processing key values.
        /// </summary>
        /// <param name="hashtable"></param>
        public static implicit operator MarkdownHelpOptions(Hashtable hashtable)
        {
            var options = new MarkdownHelpOptions();

            return options;
        }

        /// <summary>
        /// Convert from string to options by loading the yaml file from disk.
        /// </summary>
        /// <param name="path"></param>
        public static implicit operator MarkdownHelpOptions(string path)
        {
            var options = new MarkdownHelpOptions();
            options.Load(path);

            return options;
        }
    }
}
