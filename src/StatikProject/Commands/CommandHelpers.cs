using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

namespace StatikProject.Commands
{
    public class CommandHelpers
    {
        private static readonly IMarkdownParser Parser = new MarkdownParser();
        
        public static async Task<IWebBuilder> GetWebBuilder(string rootDirectory)
        {
            var staticDirectory = Path.Combine(rootDirectory, "static");
            
            var webBuilder = Statik.Statik.GetWebBuilder();
            webBuilder.RegisterMvcServices();
            webBuilder.RegisterServices(services =>
            {
                services.AddSingleton(Parser);
                services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
                services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
                {
                    options.FileProviders.Add(new PhysicalFileProvider(
                        "/home/pknopf/git/statik-project-doc/src/StatikProject/Resources"));
                    //options.FileProviders.Add(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "StatikProject.Resources"));
                });
            });
            
            webBuilder.RegisterDirectory(
                "/home/pknopf/git/statik-project-doc/src/StatikProject/Resources/wwwroot");
            //_webBuilder.RegisterFileProvider(new EmbeddedFileProvider(typeof(Program).Assembly, "StatikProject.Resources.wwwroot"));
                
            if (Directory.Exists(staticDirectory))
            {
                webBuilder.RegisterFileProvider(new PhysicalFileProvider(staticDirectory, ExclusionFilters.None));
            }

            await RegisterConfig(webBuilder, rootDirectory);
            await RegisterPages(webBuilder, rootDirectory);

            return webBuilder;
        }
        
        private static async Task RegisterPages(IWebBuilder webBuilder, string rootDirectory)
        {
            var pages = await LoadPages(rootDirectory);
            
            var root = new MenuItem { Path = "/", Title = "Home" };
            // Register the root node so we can get it later.
            webBuilder.RegisterServices(services => services.AddSingleton(root));
            
            void Walk(PageTreeItem<PageNode> pageItem, MenuItem menuItem)
            {
                if (pageItem.Data == null)
                {
                    // There isn't a markdown file associated with this level.
                    // Maybe missing index.md?
                    webBuilder.RegisterMvc(pageItem.Path, new
                        {
                            controller = "Page",
                            action = "EmptyParent",
                            menu = menuItem
                        },
                        new PageOptions(null, menuItem.Title));
                }
                else
                {
                    webBuilder.RegisterMvc(pageItem.Path, new
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
        
        private static async Task<PageTreeItem<PageNode>> LoadPages(string rootDirectory)
        {
            return await Statik.Statik.GetPageDirectoryLoader()
                .LoadFiles(
                    new PhysicalFileProvider(rootDirectory),
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
                        markdown = Parser.Parse(await reader.ReadToEndAsync());
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
        
        private static async Task RegisterConfig(IWebBuilder webBuilder, string rootDirectory)
        {
            var configFile = Path.Combine(rootDirectory, "config.yml");
            
            string configContents;
            try
            {
                configContents = await File.ReadAllTextAsync(configFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"Problem loading file {configFile}", ex);
            }

            var parsed = Parser.Parse($"---\n{configContents}\n---");

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
            
            webBuilder.RegisterServices(services =>
            {
                services.AddSingleton(moreLinks);
                services.AddSingleton(config);
            });
        }
    }
}