using System.Collections.Generic;

namespace StatikProject.Models
{
    public class MenuItemModel
    {
        public MenuItemModel()
        {
            Children = new List<MenuItemModel>();
        }
        
        public string Title { get; set; }
        
        public bool Active { get; set; }
        
        public bool Selected { get; set; }
        
        public string Path { get; set; }
        
        public int Order { get; set; }
        
        public int Level { get; set; }
        
        public List<MenuItemModel> Children { get; set; }
    }
}