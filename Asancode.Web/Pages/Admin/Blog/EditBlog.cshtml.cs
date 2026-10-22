using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Globalization;

namespace Asancode.Web.Pages.Admin.Blog
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]

    public class EditBlogModel : PageModel
    {
        private IBlogService _blogService;

        public EditBlogModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [BindProperty]
        public DataLayer.Entities.Bloger.Blog Blog { get; set; }
        public void OnGet(int id)
        {

            Blog = _blogService.GetBlogById(id);

        }

        public IActionResult OnPost(IFormFile uplodImg, string startDt = "")
        {
            if (startDt != "")
            {
                string[] sdt = startDt.Split('/');
                Blog.Date = new DateTime(int.Parse(sdt[0]),

                    int.Parse(sdt[1]),
                    int.Parse(sdt[2]),
                    new PersianCalendar()
                );
            }

            if (!ModelState.IsValid)
                return Page();
            //update Course
            _blogService.UpdateBlog(Blog, uplodImg);

            return RedirectToPage("Index");
        }
    }
}
