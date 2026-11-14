using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Entities.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Asancode.Web.Options;

namespace Asancode.Web.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    [PermissionChecker(2)]
    public class Teacher : Controller
    {
        private readonly ICourseGroupService _courseService;
        private readonly IUserService _userService;
        private readonly MediaUploadOptions _mediaUploadOptions;

        public Teacher(
            ICourseGroupService courseService,
            IUserService userService,
            IOptions<MediaUploadOptions> mediaUploadOptions)
        {
            _courseService = courseService;
            _userService = userService;
            _mediaUploadOptions = mediaUploadOptions.Value;
        }

        [HttpGet("teacher-course")]
        public IActionResult TeacherListCourse()
        {
            var course = _courseService.GetAllCourseTeacher(User.Identity.Name);
            return View(course);
        }

        [HttpGet("course-Episode/{courseId}")]
        public IActionResult EpisodeLIst(int courseId)
        {
            var course = _courseService.GetCourseByCourseId(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var userId = _userService.GetUserIdByUsername(User.Identity.Name);
            if (course.TeacherId != userId)
            {
                return RedirectToAction("TeacherListCourse", "Teacher");
            }

            ViewBag.courseid = courseId;
            var episode = _courseService.GetEpisodeByCourseId(courseId);
            return View(episode);
        }

        [HttpGet("add-eppisode/{courseId}")]
        public IActionResult AddEpisode(int courseId)
        {
            var course = _courseService.GetCourseByCourseId(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var userId = _userService.GetUserIdByUsername(User.Identity.Name);
            if (course.TeacherId != userId)
            {
                return RedirectToAction("TeacherListCourse", "Teacher");
            }

            var result = new AddEpisodeTeacherViewModel
            {
                CourseId = courseId,
                IsFree = true
            };

            return View(result);
        }

        [HttpPost("add-eppisode/{courseId}")]
        public IActionResult AddEpisode(AddEpisodeTeacherViewModel episode)
        {
            if (!ModelState.IsValid)
            {
                return View(episode);
            }

            if (string.IsNullOrEmpty(episode.EpisodeFileName))
            {
                return View(episode);
            }

            var result = _courseService.AddEpisode(episode, User.Identity.Name);
            if (result)
            {
                return RedirectToAction("TeacherListCourse", "Teacher", new { courseId = episode.CourseId });
            }

            return View(episode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> DropzoneTarget([FromQuery] int courseId)
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var course = _courseService.GetCourseByCourseId(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var userId = _userService.GetUserIdByUsername(userName);
            if (course.TeacherId != userId)
            {
                // Reject cross-course uploads before reading or buffering the multipart body.
                return Forbid();
            }

            if (!Request.HasFormContentType)
            {
                return UnsupportedMediaType();
            }

            // The antiforgery filter validates the request header before the action runs.
            // Only after authentication and course ownership succeed do we parse the form.
            var form = await Request.ReadFormAsync(HttpContext.RequestAborted);
            var files = form.Files;

            if (files.Count != 1)
            {
                return BadRequest(new { error = "Exactly one archive file is required." });
            }

            var file = files[0];
            if (file.Length == 0)
            {
                return BadRequest(new { error = "The uploaded archive is empty." });
            }

            if (file.Length > _mediaUploadOptions.MaxArchiveBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".zip" and not ".rar")
            {
                return BadRequest(new { error = "Only ZIP and RAR archives are accepted." });
            }

            // This is the legacy staging location. The private MediaAsset storage
            // pipeline will replace it in the next implementation phase.
            var fileName = $"{courseId}-{Guid.NewGuid():N}{extension}";
            var episodeDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Episode");

            Directory.CreateDirectory(episodeDirectory);

            var finalPath = Path.Combine(episodeDirectory, fileName);
            await using (var stream = new FileStream(
                             finalPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             useAsync: true))
            {
                await file.CopyToAsync(stream, HttpContext.RequestAborted);
            }

            return new JsonResult(new { data = fileName, status = "Success" });
        }
    }
}
