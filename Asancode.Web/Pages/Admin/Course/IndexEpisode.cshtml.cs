using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Course;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
  

    public class IndexEpisodeModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public IndexEpisodeModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public List<CourseEpisode> CourseEpisodes { get; set; }
        public void OnGet(int id)
        {
            ViewData["CourseId"] = id;
            CourseEpisodes = _courseGroupService.GetListEpisode(id);

        }
    }
}
