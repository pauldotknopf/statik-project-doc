using System.Collections.Generic;

namespace StatikProject.Models
{
    public class TopMenuModel
    {
        public TopMenuModel()
        {
            Crumbs = new List<MenuItemModel>();
        }
        
        public List<MenuItemModel> Crumbs { get; set; }
        
        public string EditUrl { get; set; }
    }
}