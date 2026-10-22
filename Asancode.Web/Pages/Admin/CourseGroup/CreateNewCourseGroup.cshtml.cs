using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.CourseGrooup
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class CreateNewCourseGroupModel : PageModel
    {
        private ICourseGroupService _courseGroupService;
        public CreateNewCourseGroupModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        [BindProperty]
        public DataLayer.Course.CourseGroup CourseGroup { get; set; }

        public void OnGet(int? id)
        {
            CourseGroup = new DataLayer.Course.CourseGroup()
            {
                ParentId = id
            };
        }


        public IActionResult OnPost()
        {
            _courseGroupService.AddCourseGroup(CourseGroup);
            return RedirectToPage("index");
        }
    }
}
