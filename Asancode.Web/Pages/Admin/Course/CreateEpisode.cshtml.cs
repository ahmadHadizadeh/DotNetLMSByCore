using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class CreateEpisodeModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public CreateEpisodeModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        [BindProperty]
        public CourseEpisode CourseEpisode { get; set; }
        public void OnGet(int id)
        {
            CourseEpisode = new CourseEpisode();
            CourseEpisode.CourseId = id;
        }

        public IActionResult OnPost(IFormFile fileEpisode)
        {
            if (!ModelState.IsValid || fileEpisode == null)
            {
                return Page();
            }
            //File Name Episode
            if (_courseGroupService.IsExistFile(fileEpisode.FileName))
            {
                ViewData["IsExistFil"] = true;
                return Page();
            }
            // addEpisode
            _courseGroupService.AddCourseEpisode(CourseEpisode, fileEpisode);
            return Redirect("/Admin/Course/IndexEpisode/" + CourseEpisode.CourseId);
        }
    }
}
