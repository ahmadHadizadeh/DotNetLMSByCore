using System;
using System.Linq;
using System.Security.Claims;
using Core.Services.Interfaces;
using DataLayer.Entities.Forum;
using DataLayer.Entities.User;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asancode.Web.Controllers
{
    public class ForumController : Controller
    {
        private IForumService _forumService;
       private ICourseGroupService _CourseService;
        
        
        public ForumController(IForumService forumService, ICourseGroupService CourseService)
        {
            _forumService = forumService;
            _CourseService= CourseService;
        }
        public IActionResult Index(int? courseId, string filter = "")
        {
            ViewBag.courseId= courseId;
            return View(_forumService.GetQuestions(courseId, filter));
        }

        #region CreateQuestion

        [Authorize]
        public IActionResult CreateQuestion(int id)
        {
            ViewBag.courseId = id;
            ViewBag.listCourse= _CourseService.GetCourseCreateQuestion();
           Question question = new Question()
            {
                CourseId = id
            };
            return View(question);
        }
        [HttpPost]
        public IActionResult CreateQuestion(Question question)
        {
            if (!ModelState.IsValid)
            {
                return View(question);
            }
            var sanitizer = new HtmlSanitizer();
            question.Body = sanitizer.Sanitize(question.Body);
            question.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            int questionId = _forumService.AddQuestion(question);
            return Redirect($"/Forum/ShowQuestion/{questionId}");
            
        }

        #endregion

        #region Show Question
        public IActionResult ShowQuestion(int id,int courseId)
        {
             var existId = _forumService.IsExistQuestionId(id);
            if (existId != true)
            {
                return Redirect("/");
            }
            ViewBag.listCoursePopular = _CourseService.GetCoursePopular();
            return View(_forumService.GetQuestionUser(id));
        }
        #endregion

        #region Answer

        public IActionResult Answer(int id, string body)
        {
            if (!string.IsNullOrEmpty(body))
            {
                var sanitizer = new HtmlSanitizer();
                body = sanitizer.Sanitize(body);
                _forumService.AddAnswer(new Answer()
                {
                    BodyAnswer = body,
                    CreateDate = DateTime.Now,
                    UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString()),
                    QuestionId = id
                });
            }
            return RedirectToAction("ShowQuestion", new { id = id });
        }

        [Authorize]
        public IActionResult SelectIsTrueAnswer(int questionId, int answerId)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            var question = _forumService.GetQuestionUser(questionId);
            if (question.Question.UserId == currentUserId)
            {
                _forumService.ChangeIsTrueAnswer(questionId, answerId);
            }
            return RedirectToAction("ShowQuestion", new { id = questionId });
        }
        #endregion

    }
}