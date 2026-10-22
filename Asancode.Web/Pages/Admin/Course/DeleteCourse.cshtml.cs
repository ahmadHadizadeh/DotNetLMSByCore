using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]

    public class DeleteCourseModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public DeleteCourseModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public InfoCourseViewModel InfoCourseViewModel { get; set; }

        public void OnGet(int id)
        {
            ViewData["CourseId"] = id;
            InfoCourseViewModel = _courseGroupService.GetCourseByListDelete(id);
        }
        public IActionResult OnPost(int courseId)
        {
            _courseGroupService.DeleteCourse(courseId);
            return RedirectToPage("Index");
        }
    }
}
