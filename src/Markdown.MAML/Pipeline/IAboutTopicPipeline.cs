using Markdown.MAML.Model.About;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IAboutTopicPipeline
    {
        AboutTopic Process(string markdown, string path);

        AboutTopic Process(string path, Encoding encoding);
    }
}