using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Markdown.MAML.Pipeline;
using System;
using System.IO;

namespace Markdown.Benchmark
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            // Do profiling
            RunProfile();
#endif

#if !DEBUG
            // Do benchmarks
            var summary = BenchmarkRunner.Run<PlatyPS>();   
#endif
        }

        public static void RunProfile()
        {
            var commandMarkdown = File.ReadAllText("..\\..\\..\\Invoke-Command.md");
            var aboutMarkdown = File.ReadAllText("..\\..\\..\\about_Preference_Variables.md");
            var command = PipelineBuilder.ToMamlCommand().Process(commandMarkdown, path: null);
            var topic = PipelineBuilder.ToAboutTopic().Process(aboutMarkdown, path: null);

            for (var i = 0; i < 1000; i++)
            {
                //PipelineBuilder.ToMamlCommand().Process(markdown);
                //PipelineBuilder.ToMamlXml().Process(new[] { command });
                //PipelineBuilder.ToMarkdown().Process(command);
                PipelineBuilder.ToAboutText().Process(topic);
            }
        }
    }
}
