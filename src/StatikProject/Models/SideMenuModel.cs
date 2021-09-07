using System.Collections.Generic;

namespace StatikProject.Models
{
    public class SideMenuModel
    {
        public SideMenuModel()
        {
            MenuItems = new List<MenuItemModel>();
        }
        
        public List<MenuItemModel> MenuItems { get; set; }
    }
}