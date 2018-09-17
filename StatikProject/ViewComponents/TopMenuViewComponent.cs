using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Statik.Web;
using StatikProject.Misc;
using StatikProject.Models;

namespace StatikProject.ViewComponents
{
    public class TopMenuViewComponent : ViewComponent
    {
        private readonly IPageRegistry _pageRegistry;
        private readonly ProjectConfig _projectConfig;

        public TopMenuViewComponent(IPageRegistry pageRegistry,
            ProjectConfig projectConfig)
        {
            _pageRegistry = pageRegistry;
            _projectConfig = projectConfig;
        }
        
        public Task<IViewComponentResult> InvokeAsync()
        {
            var model = new TopMenuModel();
            
            var curent = (MenuItem) ViewContext.RouteData.Values["menu"];
            if (curent != null)
            {
                var item = curent;
                while (item != null)
                {
                    model.Crumbs.Add(new MenuItemModel
                    {
                        Title = item.Title,
                        Path = item.Path,
                        Active = true,
                        Selected = curent == item
                    });
                    item = item.Parent;
                }

                model.Crumbs.Reverse();
            }

            if (!string.IsNullOrEmpty(_projectConfig.EditUrl))
            {
                var page = _pageRegistry.GetPage(HttpContext.Request.Path);
                if (page != null)
                {
                    var filePathHint = page.State as IFilePathHint;
                    if (filePathHint != null)
                    {
                        model.EditUrl = _projectConfig.EditUrl.Replace("{path}", filePathHint.FilePath);
                    }
                }
            }
            
            return Task.FromResult<IViewComponentResult>(View(model));
        }
    }
}