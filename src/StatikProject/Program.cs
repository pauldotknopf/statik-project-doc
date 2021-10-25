using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json.Linq;
using PowerArgs;
using Serilog;
using Statik.Files;
using Statik.Mvc;
using Statik.Pages;
using Statik.Web;
using StatikProject.Misc;
using StatikProject.Services;
using StatikProject.Services.Impl;

namespace StatikProject
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            
            var rootCommand = new RootCommand
            {
                Commands.Generate.Create(),
                Commands.Serve.Create()
            };
            rootCommand.Name = "statik-project-doc";
            rootCommand.Description = "A tool for generating documentation for projects.";
            
            var builder = new CommandLineBuilder(rootCommand).UseDefaults();
            builder.UseExceptionHandler((exception, context) =>
            {
                void ProcessException(Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        Log.Logger.Error("The process was cancelled.");
                    }
                    else if (ex is TargetInvocationException targetInvocationException)
                    {
                        ProcessException(targetInvocationException.InnerException);
                    }
                    else
                    {
                        Log.Logger.Error(ex, "An unhandled exception occured.");
                    }
                }
            
                ProcessException(exception);

                context.ExitCode = 1;
            });
            
            return await builder.Build().Parse(args).InvokeAsync();
        }
    }
}
