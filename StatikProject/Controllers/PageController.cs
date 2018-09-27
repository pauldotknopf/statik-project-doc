using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StatikProject.Misc;
using StatikProject.Models;
using StatikProject.Services;

namespace StatikProject.Controllers
{
    public class PageController : Controller
    {
        private readonly IMarkdownRenderer _markdownRenderer;

        public PageController(IMarkdownRenderer markdownRenderer)
        {
            _markdownRenderer = markdownRenderer;
        }
        
        public async Task<ActionResult> Page(
            [FromRouteData]PageNode page,
            [FromRouteData]MenuItem menu)
        {
            var model = new PageModel();
            
            if (menu != null)
            {
                model.SideMenu.AddRange(GetSideMenu(menu));
            }

            model.Markup = _markdownRenderer.Render(page.Markdown);
            
            return View(model);
        }
        
        private List<MenuItemModel> GetSideMenu(MenuItem current)
        {
            var activePath = new List<MenuItem>();
            
            var root = current;
            while (root.Parent != null)
            {
                activePath.Add(root.Parent);
                root = root.Parent;
            }

            // "root" is the very top level menu item.
            // First, we want to find the active second level
            // item, since the top nav is our first level.
            var activeSecondLevel = root.Children
                .FirstOrDefault(x => x == current || activePath.Contains(x));
            
            // If no second level is selected, nothing to show.
            // The user must select a menu from the top navigation.
            if (activeSecondLevel == null) return new List<MenuItemModel>();

            root = activeSecondLevel;

            MenuItemModel Convert(MenuItem menuItem)
            {
                return new MenuItemModel
                {
                    Title = menuItem.Title,
                    Selected = menuItem == current,
                    Active = activePath.Contains(menuItem) || menuItem == current,
                    Path = menuItem.Path
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

            var model = Walk(root);
            
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
            
            CollapseChildren(model);

            return model.Children;
        }
    }
}