using System.Collections.Generic;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IMetadataPipline
    {
        Dictionary<string, string> Process(string path, Encoding encoding);

        Dictionary<string, string> Process(string markdown, string path);
    }
}