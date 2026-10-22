using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.Blog
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class IndexModel : PageModel
    {
        private IBlogService _blogService;

        public IndexModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public ShowBlogViewModel BlogViewModel  { get; set; }
        public void OnGet(int pageId = 1)
        {
           BlogViewModel = _blogService.GetListBySHowBlog(pageId);
        }

    }
}
