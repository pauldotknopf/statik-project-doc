namespace StatikProject.Misc
{
    public class PageOptions : IFilePathHint, ITitleHint
    {
        public PageOptions(string filePath, string title)
        {
            FilePath = filePath;
            Title = title;
        }
        
        public string FilePath { get; }
        
        public string Title { get; }
    }

    public interface IFilePathHint
    {
        string FilePath { get; }
    }

    public interface ITitleHint
    {
        string Title { get; }
    }
}