using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using StatikProject.Extensions;

namespace StatikProject.Commands
{
    public class Generate
    {
        public static Command Create()
        {
            var command = new Command("gen")
            {
                new Option<string>(new []{"-r", "--root-directory"}, Directory.GetCurrentDirectory)
                {
                },
                new Argument<string>("output-directory")
                {
                }
            };

            command.Handler = CommandHandler.Create<string, string, CancellationToken>((rootDirectory, outputDirectory, token) => Run(rootDirectory, outputDirectory, token));

            return command;
        }
        
        static async Task Run(string rootDirectory, string outputDirectory, CancellationToken token)
        {
            var webBuilder = await CommandHelpers.GetWebBuilder(rootDirectory);
            using (var host = webBuilder.BuildVirtualHost())
            {
                Log.Information("Exporting {root} to {output} ...", rootDirectory, outputDirectory);
                await Statik.Statik.ExportHost(host, outputDirectory);
            }
        }
    }
}