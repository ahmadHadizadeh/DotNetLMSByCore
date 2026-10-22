using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.CourseGrooup
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class indexModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public indexModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public List<DataLayer.Course.CourseGroup> CourseGroup { get; set; }
        public void OnGet()
        {
            CourseGroup = _courseGroupService.GetAllCourseGroups();
        }



    }
}
