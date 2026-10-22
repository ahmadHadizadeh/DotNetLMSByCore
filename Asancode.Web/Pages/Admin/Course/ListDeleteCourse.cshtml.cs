using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class ListDeleteCourseModel : PageModel
    {


        private ICourseGroupService _courseService;

        public ListDeleteCourseModel(ICourseGroupService courseService)
        {
            _courseService = courseService;
        }
        public AdminCourseViewModel AdminCourseViewModel { get; set; }
        public void OnGet(int pageId = 1, string filterCourseName = "")
        {
            AdminCourseViewModel = _courseService.GetDeleteCourse(pageId, filterCourseName);
        }
    }
}
