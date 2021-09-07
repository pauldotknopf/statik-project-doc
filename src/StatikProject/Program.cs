using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json.Linq;
using PowerArgs;
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
        // ReSharper disable InconsistentNaming
        private static readonly Services.IMarkdownParser _parser = new Services.Impl.MarkdownParser();
        private static readonly string _rootDirectory = Path.Combine(Environment.CurrentDirectory);
        private static readonly string _contentDirectory = Path.Combine(_rootDirectory, "content");
        private static readonly string _staticDirectory = Path.Combine(_rootDirectory, "static");
        private static IWebBuilder _webBuilder;
        // ReSharper restore InconsistentNaming

        private static async Task<int> Main(string[] args)
        {
            try
            {
                _webBuilder = Statik.Statik.GetWebBuilder();
                _webBuilder.RegisterMvcServices();
                _webBuilder.RegisterServices(services =>
                {
                    services.AddSingleton(_parser);
                    services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
                    services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
                    {
                        options.FileProviders.Add(new PhysicalFileProvider(
                            "/home/pknopf/git/statik-project-doc/src/StatikProject/Resources"));
                        //options.FileProviders.Add(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "StatikProject.Resources"));
                    });
                });
                
                _webBuilder.RegisterDirectory(
                    "/home/pknopf/git/statik-project-doc/src/StatikProject/Resources/wwwroot");
                //_webBuilder.RegisterFileProvider(new EmbeddedFileProvider(typeof(Program).Assembly, "StatikProject.Resources.wwwroot"));
                
                if (Directory.Exists(_staticDirectory))
                {
                    _webBuilder.RegisterFileProvider(new PhysicalFileProvider(_staticDirectory, ExclusionFilters.None));
                }

                await RegisterConfig();
                await RegisterPages();

                try
                {
                    Args.InvokeAction<Program>(args);
                }
                catch (ArgException ex)
                {
                    Console.WriteLine(ex.Message);
                    ArgUsage.GenerateUsageFromTemplate<Program>().WriteLine();
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

            return 0;
        }

        [ArgActionMethod, ArgIgnoreCase]
        public void Serve()
        {
            Console.WriteLine("serve");
            using (var host = _webBuilder.BuildWebHost(port: 8000))
            {
                host.Listen();
                Console.WriteLine("Listening on port 8000...");
                Console.ReadLine();
            }
        }

        public class BuildArgs
        {
            [ArgDefaultValue("output"), ArgShortcut("o")]
            public string Output { get; set; }
        }
        
        [ArgActionMethod, ArgIgnoreCase]
        public async Task Build(BuildArgs args)
        {
            using (var host = _webBuilder.BuildVirtualHost())
            {
                await Statik.Statik.ExportHost(host, args.Output);
            }
        }
        
        private static async Task RegisterConfig()
        {
            var configFile = Path.Combine(_rootDirectory, "config.yml");
            
            string configContents;
            try
            {
                configContents = await File.ReadAllTextAsync(configFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"Problem loading file {configFile}", ex);
            }

            var parsed = _parser.Parse($"---\n{configContents}\n---");

            var config = new ProjectConfig
            {
                Name = parsed.Yaml.name,
                Logo = parsed.Yaml.logo,
                EditUrl = parsed.Yaml.edit_url,
                Footer = parsed.Yaml.footer,
                GoogleTrackingId = parsed.Yaml.google_tracking_id
            };

            var moreLinks = new List<MoreLink>();
            JArray links = parsed.Yaml.more_links;
            if (links != null)
            {
                foreach (dynamic link in links)
                {
                    moreLinks.Add(new MoreLink
                    {
                        Text = link.text,
                        Url = link.url,
                        Icon = link.icon
                    });
                }
            }
            
            _webBuilder.RegisterServices(services =>
            {
                services.AddSingleton(moreLinks);
                services.AddSingleton(config);
            });
        }

        private static async Task RegisterPages()
        {
            var pages = await LoadPages();
            
            var root = new MenuItem { Path = "/", Title = "Home" };
            // Register the root node so we can get it later.
            _webBuilder.RegisterServices(services => services.AddSingleton(root));
            
            void Walk(PageTreeItem<PageNode> pageItem, MenuItem menuItem)
            {
                if (pageItem.Data == null)
                {
                    // There isn't a markdown file associated with this level.
                    // Maybe missing index.md?
                    _webBuilder.RegisterMvc(pageItem.Path, new
                        {
                            controller = "Page",
                            action = "EmptyParent",
                            menu = menuItem
                        },
                        new PageOptions(null, menuItem.Title));
                }
                else
                {
                    _webBuilder.RegisterMvc(pageItem.Path, new
                        {
                            controller = "Page",
                            action = "Page",
                            page = pageItem.Data,
                            menu = menuItem
                        },
                        new PageOptions(pageItem.FilePath, menuItem.Title));
                }
                
                foreach (var child in pageItem.Children)
                {
                    var childMenuItem = new MenuItem();
                    childMenuItem.Title = child.Data.Title;
                    childMenuItem.Path = child.Path;
                    childMenuItem.Parent = menuItem;
                    childMenuItem.Order = child.Data.Order;
                    childMenuItem.Level = menuItem.Level + 1;
                    menuItem.Children.Add(childMenuItem);
                    
                    Walk(child, childMenuItem);
                }
            }

            Walk(pages, root);
        }

        private static async Task<PageTreeItem<PageNode>> LoadPages()
        {
            return await Statik.Statik.GetPageDirectoryLoader()
                .LoadFiles(
                    new PhysicalFileProvider(_contentDirectory),
                    new PageDirectoryLoaderOptions
                    {
                        IndexPageMatcher = new Matcher(StringComparison.Ordinal).AddInclude("index.md"),
                        NormalPageMatcher = new Matcher(StringComparison.Ordinal).AddInclude("*.md")
                    })
                .Convert(async x =>
                {
                    if (x.Data == null || x.Data.IsDirectory) return null;
                    
                    Services.MarkdownParseResult markdown;
                    using (var stream = x.Data.CreateReadStream())
                    using(var reader = new StreamReader(stream))
                    {
                        markdown = _parser.Parse(await reader.ReadToEndAsync());
                    }

                    if (markdown.Yaml == null)
                    {
                        throw new Exception($"Front matter is required for {x.Data.PhysicalPath}");
                    }
                    
                    var pageNode = new PageNode();
                    pageNode.Title = markdown.Yaml.title;
                    if (string.IsNullOrEmpty(pageNode.Title))
                    {
                        throw new Exception($"\"Title\" required for {x.Data.PhysicalPath}");
                    }
                    var order = (int?)markdown.Yaml.order;
                    if (order.HasValue)
                    {
                        pageNode.Order = order.Value;
                    }
                    pageNode.Markdown = markdown.Markdown;
                    
                    return pageNode;
                });
        }
    }
}
