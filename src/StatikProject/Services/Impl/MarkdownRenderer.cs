using System;
using System.IO;
using Markdig;
using Markdig.Renderers;
using Pek.Markdig.HighlightJs;

namespace StatikProject.Services.Impl
{
    public class MarkdownRenderer : IMarkdownRenderer
    {
        public string Render(string markdown, Func<string, string> linkRewriter = null)
        {
            var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.LinkRewriter = linkRewriter;

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseHighlightJs()
                .Build();
            pipeline.Setup(renderer);
            
            var document = Markdown.Parse(markdown, pipeline);
            renderer.Render(document);
            writer.Flush();

            return writer.ToString();
        }
    }
}