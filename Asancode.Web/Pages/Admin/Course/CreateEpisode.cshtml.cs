using Asancode.Web.Media;
using Asancode.Web.Options;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class CreateEpisodeModel : PageModel
    {
        private readonly ICourseGroupService _courseGroupService;
        private readonly MediaUploadOptions _mediaUploadOptions;

        public CreateEpisodeModel(
            ICourseGroupService courseGroupService,
            IOptions<MediaUploadOptions> mediaUploadOptions)
        {
            _courseGroupService = courseGroupService;
            _mediaUploadOptions = mediaUploadOptions.Value;
        }

        [BindProperty]
        public CourseEpisode CourseEpisode { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (_courseGroupService.GetCourseByCourseId(id) is null)
            {
                return NotFound();
            }

            CourseEpisode = new CourseEpisode { CourseId = id };
            return Page();
        }

        public IActionResult OnPost(int id, IFormFile? fileEpisode)
        {
            // These fields are assigned by the server, not supplied by the create form.
            ModelState.Remove("CourseEpisode.CourseId");
            ModelState.Remove("CourseEpisode.EpisodeId");
            ModelState.Remove("CourseEpisode.EpisodeFileName");
            ModelState.Remove("CourseEpisode.Courses");

            if (!MediaArchiveUploadValidator.TryValidate(
                    fileEpisode,
                    _mediaUploadOptions.MaxArchiveBytes,
                    out var failureStatusCode))
            {
                return StatusCode(failureStatusCode);
            }

            if (!ModelState.IsValid)
            {
                CourseEpisode.CourseId = id;
                return Page();
            }

            if (_courseGroupService.GetCourseByCourseId(id) is null)
            {
                return NotFound();
            }

            CourseEpisode.CourseId = id;
            _courseGroupService.AddCourseEpisode(CourseEpisode, fileEpisode!);

            return Redirect("/Admin/Course/IndexEpisode/" + id);
        }
    }
}
