using Markdown.MAML.Model.MAML;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IMarkdownPipeline
    {
        void Process(MamlCommand command, string path, Encoding encoding);

        string Process(MamlCommand command);
    }
}