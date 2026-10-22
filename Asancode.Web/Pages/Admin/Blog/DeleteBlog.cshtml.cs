using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Blog
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class DeleteBlogModel : PageModel
    {
        private IBlogService _blogService;

        public DeleteBlogModel(IBlogService blogService)
        {
            _blogService = blogService;
        }
        public DeleteBlogViewModel DeleteBlogViewModel { get; set; }
        public void OnGet(int id)
        {
            @ViewData["BlogId"] = id;
            DeleteBlogViewModel = _blogService.GetDeleteBlog(id);
        }
        public IActionResult OnPost(int blogId)
        {
            _blogService.DeleteBlog(blogId);
            return RedirectToPage("Index");
        }
    }
}
