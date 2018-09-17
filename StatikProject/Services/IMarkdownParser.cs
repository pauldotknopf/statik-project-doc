namespace StatikProject.Services
{
    public interface IMarkdownParser
    {
        MarkdownParseResult Parse(string markdown);
    }
}