using Microsoft.Extensions.FileProviders;

namespace StatikProject
{
    public class PageNode
    {
        public string Title { get; set; }
        
        public string Markdown { get; set; }
        
        public int Order { get; set; }
    }
}