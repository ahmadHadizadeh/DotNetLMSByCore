using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
  

    public class IndexModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public IndexModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public ShowListForCourse ShowListForCourse { get; set; }
        public void OnGet(int pageId = 1, string filterTitle = "")
        {
            ShowListForCourse = _courseGroupService.GetListCourse(pageId, filterTitle);
        }
    }
}
