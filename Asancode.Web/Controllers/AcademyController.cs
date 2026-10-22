using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Ganss.Xss;


namespace Asancode.Web.Controllers
{
    public class AcademyController : Controller
    {
        private IUserService _userService;

        public AcademyController(IUserService userService)
        {
            _userService = userService;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Route("AboutMe")]
        public IActionResult AboutMe()
        {
            return View();
        }
        [Route("Service")]
        public IActionResult Service()
        {
            return View();
        } 
        [Route("History")]
        public IActionResult History()
        {
            return View();
        }
        [Route("Team")]
        public IActionResult Team()
        {
            return View();
        }
        [HttpGet]
        [Route("HelpCenter")]
        public IActionResult HelpCenter()
        {
            return View();
        }
        [HttpPost]
        [Route("HelpCenter")]
        public IActionResult HelpCenter(ContactUs contactUs)
        {
            if (!ModelState.IsValid)
                return View();
            var sanitizer = new HtmlSanitizer();
            contactUs.Description = sanitizer.Sanitize(contactUs.Description);
            _userService.AddContactUs(contactUs);
            ViewBag.IsSuccess = true;
            return View();
        } 
        [Route("Faqs")]
        public IActionResult Faqs()
        {
            return View();
        }
      
        [Route("Contact")]
        public IActionResult Contact()
        {
            return View();
        }

        [Route("Cooperation")]
        public IActionResult Cooperation()
        {
            return View();
        }
        
        [Route("Education")]
        public IActionResult Education()
        {
            return View();
        } 
        [Route("Bloger")]
        public IActionResult Bloger()
        {
            return View();
        }
        [Route("Office")]
        public IActionResult Office()
        {
            return View();
        }

        [Route("Terms")]
        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult SubscribeLetters(SubscribeToNewsLetter subscribe)
        {
            _userService.AddSubscribe(subscribe);
            return Redirect("/");
        }
    }
}
