using Markdown.MAML.Model.MAML;

namespace Markdown.MAML.Parser
{
    public interface IMamlCommandVisitor
    {
        void Visit(MamlCommand mamlCommand);
    }
}
