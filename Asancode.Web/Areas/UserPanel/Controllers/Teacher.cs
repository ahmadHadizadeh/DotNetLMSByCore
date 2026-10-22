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

namespace Asancode.Web.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    [PermissionChecker(2 )]
    public class Teacher : Controller
    {
        private ICourseGroupService _courseService;
        private IUserService _userService;

        public Teacher(ICourseGroupService courseService, IUserService userService)
        {
            _courseService = courseService;
            _userService = userService;
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
            if (course==null)
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
        public IActionResult DropzoneTarget(List<IFormFile> files, int courseId)
        {
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    var fileName = $"{courseId}-{Guid.NewGuid().ToString()}" + Path.GetExtension(file.FileName);

                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode/");

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var finalPath = path + fileName;

                    using (var stream = new FileStream(finalPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    return new JsonResult(new { data = fileName, status = "Success" });
                }
            }

            return new JsonResult(new { status = "Error" });
        }

    }
}
