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
    public class EditEpisodeModel : PageModel
    {
        private readonly ICourseGroupService _courseGroup;
        private readonly MediaUploadOptions _mediaUploadOptions;

        public EditEpisodeModel(
            ICourseGroupService courseGroup,
            IOptions<MediaUploadOptions> mediaUploadOptions)
        {
            _courseGroup = courseGroup;
            _mediaUploadOptions = mediaUploadOptions.Value;
        }

        [BindProperty]
        public CourseEpisode CourseEpisode { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var episode = _courseGroup.GetEpisodeById(id);
            if (episode is null)
            {
                return NotFound();
            }

            CourseEpisode = episode;
            return Page();
        }

        public IActionResult OnPost(int id, IFormFile? fileEpisode)
        {
            var existingEpisode = _courseGroup.GetEpisodeById(id);
            if (existingEpisode is null)
            {
                return NotFound();
            }

            // Episode identity, course and stored media key come from the database.
            ModelState.Remove("CourseEpisode.CourseId");
            ModelState.Remove("CourseEpisode.EpisodeId");
            ModelState.Remove("CourseEpisode.EpisodeFileName");
            ModelState.Remove("CourseEpisode.Courses");

            if (!ModelState.IsValid)
            {
                CourseEpisode.CourseId = existingEpisode.CourseId;
                CourseEpisode.EpisodeId = existingEpisode.EpisodeId;
                CourseEpisode.EpisodeFileName = existingEpisode.EpisodeFileName;
                return Page();
            }

            if (fileEpisode is not null &&
                !MediaArchiveUploadValidator.TryValidate(
                    fileEpisode,
                    _mediaUploadOptions.MaxArchiveBytes,
                    out var failureStatusCode))
            {
                return StatusCode(failureStatusCode);
            }

            existingEpisode.EpisodeTitle = CourseEpisode.EpisodeTitle;
            existingEpisode.EpisodeTime = CourseEpisode.EpisodeTime;
            existingEpisode.IsFree = CourseEpisode.IsFree;

            _courseGroup.EditEpisode(existingEpisode, fileEpisode);
            return Redirect("/Admin/Course/IndexEpisode/" + existingEpisode.CourseId);
        }
    }
}
