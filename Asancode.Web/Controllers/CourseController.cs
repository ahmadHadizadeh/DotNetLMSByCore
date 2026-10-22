using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Core.Services.Interfaces;
using DataLayer.Entities.Course;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharpCompress.Archives;

namespace Asancode.Web.Controllers
{
    public class CourseController : Controller
    {
        #region Constroctor

        private ICourseGroupService _courseGroupService;
        private IOrderService _orderService;
        private IUserService _userService;

        public CourseController(ICourseGroupService courseGroupService, IOrderService orderService,
            IUserService userService)
        {
            _courseGroupService = courseGroupService;
            _orderService = orderService;
            _userService = userService;
        }

        #endregion Constroctor

        #region Index

        public IActionResult Index(int pageId = 1, string filter = "", string typeShow = "all", string orderBy = "date",
            int startPrice = 0, int endPrice = 0, List<int> selectedGroup = null)
        {
            ViewBag.Groups = _courseGroupService.GetAllGroup();
            ViewBag.selecteCheckBox = selectedGroup;
            ViewBag.pageid = pageId;
            return View(_courseGroupService.GetCourse(pageId, 12, filter, typeShow, orderBy, startPrice, endPrice, selectedGroup));
        }

        #endregion Index

        #region CourseDetails

        [Route("CourseDetails/{id}/{title}")]
        public IActionResult CourseDetails(int id, string title, int episode = 0)
        {
            var course = _courseGroupService.GetCourseInfoByShowCourseDetailse(id);

            if (User.Identity != null && episode != 0 && User.Identity.IsAuthenticated)
            {
                if (course.CourseEpisodes.All(e => e.EpisodeId != episode))
                {
                    return NotFound();
                }

                if (!course.CourseEpisodes.First(e => e.EpisodeId == episode).IsFree)
                {
                    if (!_orderService.IsBuyCourse(User.Identity.Name, id))
                    {
                        return NotFound();
                    }
                }

                var ep = course.CourseEpisodes.First(e => e.EpisodeId == episode);
                ViewBag.Episode = ep;

                string filePath;
                string checkFilePath;
                if (ep.IsFree)
                {
                    filePath = "/FileFree/" + ep.EpisodeFileName.Replace(".zip", ".mp4");
                    checkFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/FileFree",
                        ep.EpisodeFileName.Replace(".zip", ".mp4"));
                }
                else
                {
                    filePath = "/FilePrice/" + ep.EpisodeFileName.Replace(".zip", ".mp4");
                    checkFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/FilePrice",
                        ep.EpisodeFileName.Replace(".zip", ".mp4"));
                }

                if (!System.IO.File.Exists(checkFilePath))
                {
                    string targetPath = Directory.GetCurrentDirectory();
                    targetPath = Path.Combine(targetPath, ep.IsFree ? "wwwroot/FileFree" : "wwwroot/FilePrice");

                    string rarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode",
                        ep.EpisodeFileName);
                    var archive = ArchiveFactory.Open(rarPath);
                    var Entries = archive.Entries.OrderBy(x => x.Key.Length);
                    foreach (var en in Entries)
                    {
                        if (Path.GetExtension(en.Key) == ".mp4")
                        {
                            en.WriteTo(System.IO.File.Create(Path.Combine(targetPath, ep.EpisodeFileName.Replace(".zip", ".mp4"))));
                        }
                    }
                }

                ViewBag.filePath = filePath;
            }

            return View(course);
        }

        #endregion CourseDetails

        #region CreateComment

        [HttpPost]
        public IActionResult CreateComment(CourseComment comments)
        {
            comments.IsDelete = false;
            comments.CreateDate = DateTime.Now;
            comments.UserId = _userService.GetUserIdByUsername(User.Identity.Name);
            var sanitizer = new HtmlSanitizer();
            comments.Comment = sanitizer.Sanitize(comments.Comment);
            _courseGroupService.AddComment(comments);
            return View("ShowComment", _courseGroupService.GetListComment(comments.CourseId));
        }

        #endregion CreateComment

        #region ShowComment

        public IActionResult ShowComment(int id, int pageId = 1)
        {
            return View(_courseGroupService.GetListComment(id, pageId));
        }

        #endregion ShowComment

        #region ShortKey

        [Route("c/{key}")]
        public IActionResult ShortKeyRedirect(string key)
        {
            var course = _courseGroupService.GetShortKeyRedirect(key);
            if (course == null)
            {
                return NotFound();
            }
            Uri uri = new Uri("https://asancode.com" + "/CourseDetails/" + course.CourseId + "/" + course.CourseTitle.Trim().Replace(" ", "-"));
            return Redirect(uri.AbsoluteUri);
        }

        #endregion ShortKey

        #region ByCourse

        [Authorize]
        public ActionResult BuyCourse(int id)
        {
            int orderId = _orderService.AddOrder(User.Identity.Name, id);

            return Redirect("/UserPanel/MyOrder/ShowOrder/" + orderId);
        }

        #endregion ByCourse

        #region DownloadFile

        [Route("DownloadFile/{episodId}")]
        public IActionResult DownloadFile(int episodId)
        {
            var episod = _courseGroupService.GetEpisodeById(episodId);
            string fileName = episod.EpisodeFileName;
            string pathName = Path.Combine(Directory.GetCurrentDirectory(), "https://dl.asancode.com/downloadfile/1402/python/", episod.EpisodeFileName);

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (episod.IsFree)
                {
                    byte[] file = System.IO.File.ReadAllBytes(pathName);
                    return File(file, "application/force-download", fileName);
                }

                if (_orderService.IsBuyCourse(User.Identity.Name, episod.CourseId))
                {
                    byte[] file = System.IO.File.ReadAllBytes(pathName);
                    return File(file, "application/force-download", fileName);
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

            return Forbid();
        }

        #endregion DownloadFile

        #region CourseVote

        public IActionResult CourseVote(int id)
        {
            if (!_courseGroupService.IsFree(id))
            {
                if (User.Identity != null && !_orderService.IsBuyCourse(User.Identity.Name, id))
                {
                    ViewBag.NotAccess = true;
                }
            }

            return PartialView(_courseGroupService.GetCourseVotes(id));
        }

        #endregion CourseVote

        #region AddVote

        [Authorize]
        public IActionResult AddVote(int id, bool vote)
        {
            if (User.Identity != null)
                _courseGroupService.AddsVote(_userService.GetUserIdByUsername(User.Identity.Name), id, vote);

            return PartialView("CourseVote", _courseGroupService.GetCourseVotes(id));
        }

        #endregion AddVote
    }
}