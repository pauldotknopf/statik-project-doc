using System.Collections.Generic;

namespace StatikProject.Models
{
    public class PageModel
    {
        public PageModel()
        {
            SideMenu = new List<MenuItemModel>();
        }
        
        public string Markup { get; set; }
        
        public List<MenuItemModel> SideMenu { get; set; }
    }
}