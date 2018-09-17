using System.Collections.Generic;

namespace StatikProject.Misc
{
    public class MenuItem
    {
        public MenuItem()
        {
            Children = new List<MenuItem>();
        }
        
        public string Path { get; set; }
        
        public string Title { get; set; }
        
        public int Order { get; set; }
        
        public int Level { get; set; }
        
        public MenuItem Parent { get; set; }
        
        public List<MenuItem> Children { get; set; }
    }
}