using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatikProject.Misc;
using StatikProject.Models;

namespace StatikProject.ViewComponents
{
    public class SideMenuViewComponent : ViewComponent
    {
        private readonly MenuItem _root;

        public SideMenuViewComponent(MenuItem root)
        {
            _root = root;
        }
        
        public Task<IViewComponentResult> InvokeAsync()
        {
            var current = (MenuItem) ViewContext.RouteData.Values["menu"];
            
            var activePath = new List<MenuItem>();

            if (current != null)
            {
                var root = current;
                while (root.Parent != null)
                {
                    activePath.Add(root.Parent);
                    root = root.Parent;
                }
            }

            MenuItemModel Convert(MenuItem menuItem)
            {
                return new MenuItemModel
                {
                    Title = menuItem.Title,
                    Selected = menuItem == current,
                    Active = activePath.Contains(menuItem) || menuItem == current,
                    Path = menuItem.Path,
                    Level = menuItem.Level
                };
            }
            
            MenuItemModel Walk(MenuItem menuItem)
            {
                var menuItemModel = Convert(menuItem);

                foreach (var child in menuItem.Children.OrderBy(x => x.Order))
                {
                    var childModel = Walk(child);
                    menuItemModel.Children.Add(childModel);
                }

                return menuItemModel;
            }

            var menu = Walk(_root);
            
            // Collapse all children not in the active path
            void CollapseChildren(MenuItemModel menuItemModel)
            {
                if (!menuItemModel.Active)
                {
                    menuItemModel.Children.Clear();
                }
                else
                {
                    foreach (var child in menuItemModel.Children)
                    {
                        CollapseChildren(child);
                    }
                }
            }
            
            CollapseChildren(menu);
            
            var model = new SideMenuModel();
            model.MenuItems.AddRange(menu.Children);
            
            return Task.FromResult<IViewComponentResult>(View(model));
        }
    }
}