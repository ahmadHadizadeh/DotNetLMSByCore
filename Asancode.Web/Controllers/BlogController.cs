using System;
using Core.Convertors;
using Core.Services.Interfaces;
using DataLayer.Entities.Bloger;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;

namespace Asancode.Web.Controllers
{
    public class BlogController : Controller
    {
        private IUserService _userService;
        private ICourseGroupService _courseGroupService;
        private IBlogService _blogService;

        public BlogController(IUserService userService, ICourseGroupService courseGroupService, IBlogService blogService)
        {
            _userService = userService;
            _courseGroupService = courseGroupService;
            _blogService = blogService;
        }

        public IActionResult Index(int pageId = 1, int take = 0, string filter = "", string typeShow = "all", string orderBy = "date")
        {
            ViewBag.pageid = pageId;
            return View(_blogService.GetAllBlogByShowToIndex(pageId, take = 20, filter, orderBy));
        }

        [Route("ShowBlog/{id}/{title}")]
        public IActionResult ShowBlog(int id, string title)
        {
            var blog = _blogService.GetBlogJustClickOnBlog(id);
            ViewBag.dt = blog.Date.ToShamci();
            ViewBag.slidBlog = _blogService.GetBlogSidbarToShowBlog();
            if (blog == null)
            {
                return NotFound();
            }
            return View(blog);
        }

        [HttpPost]
        public IActionResult CreateComment(CommentBlog comments)
        {
            comments.IsDelete = false;
            comments.CreateDate = DateTime.Now;

            if (User.Identity != null) comments.UserId = _userService.GetUserIdByUsername(User.Identity.Name);
            var sanitizer = new HtmlSanitizer();
            comments.Comment = sanitizer.Sanitize(comments.Comment);
            _blogService.AddCommentBlog(comments);
            return View("ShowBlogComment", _blogService.GetCommentByShowBlog(comments.BlogId));
        }

        public IActionResult ShowBlogComment(int id, int pageId = 1)
        {
            return View(_blogService.GetCommentByShowBlog(id, pageId));
        }

        [Route("b/{key}")]
        public IActionResult ShortKeyRedirectBlog(string key)
        {
            var blog = _blogService.GetShortKeyRedirectBlog(key);
            if (blog == null)
            {
                return NotFound();
            }
            Uri uri = new Uri("https://asancode.com" + "/ShowBlog/" + blog.BlogId + "/" + blog.Title.Trim().Replace(" ", "-"));
            return Redirect(uri.AbsoluteUri);
        }
    }
}