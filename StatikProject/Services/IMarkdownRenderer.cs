using System;

namespace StatikProject.Services
{
    public interface IMarkdownRenderer
    {
        string Render(string markdown, Func<string, string> linkRewriter = null);
    }
}