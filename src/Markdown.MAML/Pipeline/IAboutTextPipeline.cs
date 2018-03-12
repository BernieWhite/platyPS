using Markdown.MAML.Model.About;
using System.Text;

namespace Markdown.MAML.Pipeline
{
    public interface IAboutTextPipeline
    {
        string Process(AboutTopic topic);

        string Process(string markdown, string path);

        string Process(string path, Encoding encoding);
    }
}