using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.CourseGroup
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]

    public class EditCourseModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public EditCourseModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        [BindProperty]
        public DataLayer.Course.CourseGroup CourseGroup { get; set; }

        public void OnGet(int id)
        {
            CourseGroup = _courseGroupService.GetCourseGroupById(id);
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            _courseGroupService.UpdateCourseGroup(CourseGroup);
            return RedirectToPage("index");
        }
    }
}
