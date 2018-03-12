using Markdown.MAML.Model.MAML;
using System.Collections.Generic;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IMamlXmlPipeline
    {
        string Process(IEnumerable<MamlCommand> command);

        string Process(string[] path, Encoding encoding);
    }
}