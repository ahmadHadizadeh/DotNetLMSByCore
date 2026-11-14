using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Genarate;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Asancode.Web.Controllers
{
    public class HomeController : Controller
    {
        private ICourseGroupService _courseGroupService;
        private IBlogService _blogService;
        private IUserService _userservice;

        public HomeController(ICourseGroupService courseGroupService, IBlogService blogService, IUserService userservice)
        {
            _courseGroupService = courseGroupService;
            _blogService = blogService;
            _userservice = userservice;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            ViewBag.listCoursePopular = _courseGroupService.GetCoursePopular();
            ViewBag.listBlog = _blogService.GetBlogShowMainIndex();
            ViewBag.listBanner = _courseGroupService.GetBannerForIndex();
            return View(_courseGroupService.GetCourse().Item1);

        }


        [Route("OnlinePayment/{id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult OnlinePayment(int id)
        {
            // The legacy sandbox gateway integration has been removed because its
            // dependency graph is vulnerable and its callback flow is not verifiable.
            // This endpoint remains fail-closed until the payment adapter is
            // redesigned with server-side request/verification and idempotency.
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "درگاه پرداخت تا تکمیل بازطراحی امن پرداخت موقتاً غیرفعال است.");
        }
        public ActionResult GetSubGroup(int id)
        {
            //select
            List<SelectListItem> list = new List<SelectListItem>()
            {
                new SelectListItem()
                {
                    Text = "انتخاب کنید",
                    Value = ""
                }
            };
            list.AddRange(_courseGroupService.GetSubGroup(id));

            return Json(new SelectList(list, "Value", "Text"));
        }
        [HttpPost]
        [Route("file-upload")]
        public IActionResult UploadImage(IFormFile upload, string ckEditorFuncNum, string CKEditor, string langCode)
        {
            if (upload.Length <= 0) return null;

            //Image Name
            var fileName = CreateUniqCode.buildUniqCod() + System.IO.Path.GetExtension(upload.FileName);

            //Image Path
            var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/CkImage", fileName);

            //save
            using (var stream = new FileStream(Path, FileMode.Create))
            {
                upload.CopyTo(stream);
            }

            var url = $"{"/CkImage/"}{fileName}";
            return Json(new { uploaded = true, url });
        }
        public IActionResult SubscribeLetters(SubscribeToNewsLetter subscribe)
        {
            _userservice.AddSubscribe(subscribe);
            return Redirect("index");
        }
        public IActionResult Error404()
        {
            return View();
        }

    }
}