using Core.DTO;
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
    public class DeleteEpisodeModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public DeleteEpisodeModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        public CourseEpisode CourseEpisode { get; set; }
        public InfoShowEpisodeDeleteViewModel InfoShowEpisodeDeleteViewModel { get; set; }
        public void OnGet(int id)
        {
            ViewData["EpisodeId"] = id;
            InfoShowEpisodeDeleteViewModel = _courseGroupService.GetEpisodeByDelete(id);
        }
        public IActionResult OnPost(int episodeId, IFormFile fileEpisode)
        {
            _courseGroupService.DeleteEpisode(episodeId, fileEpisode);
            return RedirectToPage("Index");
        }
    }
}
