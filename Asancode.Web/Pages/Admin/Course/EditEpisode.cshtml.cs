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

    public class EditEpisodeModel : PageModel
    {
        private ICourseGroupService _courseGroup;

        public EditEpisodeModel(ICourseGroupService courseGroup)
        {
            _courseGroup = courseGroup;
        }
        [BindProperty]
        public CourseEpisode CourseEpisode { get; set; }
        public void OnGet(int id)
        {
            CourseEpisode = _courseGroup.GetEpisodeById(id);
        }
        public IActionResult OnPost(IFormFile fileEpisode)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (fileEpisode != null)
            {
                if (_courseGroup.IsExistFile(fileEpisode.FileName))
                {

                    ViewData["IsExistFil"] = true;
                    return Page();
                }
            }
            _courseGroup.EditEpisode(CourseEpisode, fileEpisode);
            // edite episode

            return Redirect("/Admin/Course/IndexEpisode/" + CourseEpisode.CourseId);
        }
    }
}
