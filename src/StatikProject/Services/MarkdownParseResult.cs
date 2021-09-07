namespace StatikProject.Services
{
    public class MarkdownParseResult
    {
        public MarkdownParseResult(dynamic yaml, string markdown)
        {
            Yaml = yaml;
            Markdown = markdown;
        }
        
        public dynamic Yaml { get; }
        
        public string Markdown { get; }
    }
}