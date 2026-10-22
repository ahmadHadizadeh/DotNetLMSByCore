using Core.DTO;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asancode.Web.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class HomeController : Controller
    {
        #region Interface

        // interface
        private IUserService _userService;
        private IOrderService _orderService;

        public HomeController(IUserService userService, IOrderService orderService)
        {
            _userService = userService;
            _orderService = orderService;
        }

        #endregion Interface

        #region Index

        public IActionResult Index()
        {
            ViewBag.CourseSlider = _orderService.GetCourseByUserPanel();

            return View(_userService.GetUserbyUserPanel(User.Identity.Name));
        }

        #endregion Index

        #region EditeProfile

        [Route("UserPanel/EditProfile")]
        public IActionResult EditProfile()
        {
            return View(_userService.GetUserForEditeProfile(User.Identity.Name));
        }

        [Route("UserPanel/EditProfile")]
        [HttpPost]
        public IActionResult EditProfile(EditProfileViewModel profile)
        {
            if (!ModelState.IsValid)
            {
                return View(profile);
            }
            _userService.EditUser(User.Identity.Name, profile);
            //Logout
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Login?EditProfile=True");
        }

        #endregion EditeProfile

        #region ChangePassword

        [Route("UserPanel/ChangePassword")]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [Route("UserPanel/ChangePassword")]
        [HttpPost]
        public IActionResult ChangePassword(ChangPasswordviewModel chang)
        {
            string CurrentUser = User.Identity.Name;
            if (!ModelState.IsValid)
            {
                return View(chang);
            }

            if (!_userService.CompareOldPassword(CurrentUser, chang.OldPassword))
            {
                ModelState.AddModelError("OldPassword", "کلمه عبور مطابقت ندارد");
                return View(chang);
            }

            _userService.ChangePassword(CurrentUser, chang.Password);

            ViewBag.IsSuccess = true;
            return View();
        }

        #endregion ChangePassword

        #region MyCourse

        [Route("MyCourse")]
        public IActionResult MyCourse()
        {
            //ViewBag.user = _userService.GetUserIdByUsername(userName);

            return View(_orderService.GetListCourseShowPanel(User.Identity.Name));
        }

        #endregion MyCourse
    }
}