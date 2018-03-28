using BenchmarkDotNet.Attributes;
using Markdown.MAML.Model.About;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Pipeline;
using System.IO;

namespace Markdown.Benchmark
{
    /// <summary>
    /// Define a set of benchmarks for performance testing platyPS internals.
    /// </summary>
    [MemoryDiagnoser]
    public class PlatyPS
    {
        private string _CommandMarkdown;
        private string _AboutMarkdown;
        private MamlCommand _Command;
        private AboutTopic _Topic;

        [GlobalSetup]
        public void LoadMarkdown()
        {
            _CommandMarkdown = File.ReadAllText("Invoke-Command.md");
            _AboutMarkdown = File.ReadAllText("about_Preference_Variables.md");
            _Command = PipelineBuilder.ToMamlCommand().Build().Process(_CommandMarkdown, path: null);
            _Topic = PipelineBuilder.ToAboutTopic().Process(_CommandMarkdown, path: null);
        }

        [Benchmark]
        public MamlCommand ToMamlCommand() => PipelineBuilder.ToMamlCommand().Build().Process(_CommandMarkdown, path: null);

        [Benchmark]
        public string ToMamlXml() => PipelineBuilder.ToMamlXml().Build().Process(new[] { _Command });

        [Benchmark]
        public string ToMarkdown() => PipelineBuilder.ToMarkdown().Build().Process(_Command);

        [Benchmark]
        public AboutTopic ToAboutTopic() => PipelineBuilder.ToAboutTopic().Process(_AboutMarkdown, path: null);

        [Benchmark]
        public string ToAboutText() => PipelineBuilder.ToAboutText().Process(_Topic);
    }
}
