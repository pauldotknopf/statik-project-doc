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
    public class Serve
    {
        public static Command Create()
        {
            var command = new Command("serve")
            {
                new Option<string>(new []{"-r", "--root-directory"}, Directory.GetCurrentDirectory)
                {
                }
            };

            command.Handler =
                CommandHandler.Create<string, CancellationToken>((rootDirectory, token) => Run(rootDirectory, token));

            return command;
        }
        
        static async Task Run(string rootDirectory, CancellationToken token)
        {
            var webBuilder = await CommandHelpers.GetWebBuilder(rootDirectory);
            using (var host = webBuilder.BuildWebHost(null, 8080))
            {
                Log.Information("Listening on http://localhost:8080/ ...");
                host.Listen();
                await token;
                Log.Information("Stopped the server...");
            }
        }
    }
}