using Markdown.MAML.Model.MAML;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IMamlCommandPipeline
    {
        MamlCommand Process(string markdown, string path);

        MamlCommand Process(string path, Encoding encoding);
    }
}