using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Markdown.MAML.Pipeline;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace Markdown.Benchmark
{
    public sealed class Program
    {
        private class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig(string artifactsPath)
            {
                ArtifactsPath = artifactsPath;
            }
        }

        public static void Main(string[] args)
        {
            


#if DEBUG
            // Do profiling
            RunProfile();
#endif

#if !DEBUG
            var config = DefaultConfig.Instance;

            if (args != null && args.Length == 1)
            {
                config = new BenchmarkConfig(args[0]);
            }

            var app = new CommandLineApplication();

            app.Command("benchmark", cmd =>
            {
                var output = cmd.Option("-o | --output", "The path to store report output.", CommandOptionType.SingleValue);

                cmd.OnExecute(() =>
                {
                    // Do benchmarks
                    var summary = BenchmarkRunner.Run<PlatyPS>(config);
                    
                    return 0;
                });

                cmd.HelpOption("-? | -h | --help");
            });

            app.HelpOption("-? | -h | --help");
            app.Execute(args);
#endif
        }

        public static void RunProfile()
        {
            var commandMarkdown = File.ReadAllText("Invoke-Command.md");
            var aboutMarkdown = File.ReadAllText("about_Preference_Variables.md");
            var command = PipelineBuilder.ToMamlCommand().Build().Process(commandMarkdown, path: null);
            var topic = PipelineBuilder.ToAboutTopic().Process(aboutMarkdown, path: null);

            for (var i = 0; i < 1000; i++)
            {
                PipelineBuilder.ToMamlCommand().Build().Process(commandMarkdown, path: null);
                PipelineBuilder.ToMamlXml().Build().Process(new[] { command });
                PipelineBuilder.ToMarkdown().Build().Process(command);
                PipelineBuilder.ToAboutText().Process(topic);
            }
        }
    }
}
