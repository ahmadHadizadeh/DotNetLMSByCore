using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Asancode.Web.ExterNalProvider;
using Core.Convertors;
using Core.DTO;
using Core.Genarate;
using Core.Security;
using Core.Security.Sender;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Core.DTO.Recatch;
using Microsoft.AspNetCore.Http;

namespace Asancode.Web.Controllers
{
    public class AccountController : Controller
    {
        #region InterFace

        private IUserService _userService;
        private AsanwebContext _context;
        private IViewRenderService _renderService;
        private SignInManager<ApplicationUser> _signInManager;

        public AccountController(IUserService userService, AsanwebContext context, IViewRenderService renderService, SignInManager<ApplicationUser> signInManager)
        {
            _userService = userService;
            _context = context;
            _renderService = renderService;
            _signInManager = signInManager;
        }

        #endregion InterFace

        #region Register

        [Route("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("Register")]
        public IActionResult Register(RegisterViewModel register)
        {
            if (!ModelState.IsValid)
            {
                return View(register);
            }
            if (_userService.IsExistUserName(register.UserName))
            {
                ModelState.AddModelError("UserName", "نام کاربری معتبر نمی باشد");
                return View(register);
            }

            if (_userService.IsExistEmail(FixedTexd.FixedEmail(register.Email)))
            {
                ModelState.AddModelError("Email", "ایمیل معتبر نمی باشد");
                return View(register);
            }
            DataLayer.Entities.User.Users user = new Users()
            {
                ActiveCode = CreateUniqCode.buildUniqCod(),
                Email = FixedTexd.FixedEmail(register.Email),
                IsActive = false,
                Password = PasswordEncript.EncriptPassword(register.Password),
                RegisterDate = DateTime.Now,
                UserImage = "Defult.jpg",
                UserName = register.UserName
            };
            _userService.AddUser(user);
            string body = _renderService.RenderToStringAsync("_ActiveEmail", user);
            SendEmail.Send(user.Email, "ایمیل فعال سازی", body);
            return View("SeuccessRegister", user);
        }

        #endregion Register

        #region Login

        [Route("login", Name = "LoginGet")]
        public ActionResult Login(bool EditProfile = false)
        {
            ViewBag.EditProfile = EditProfile;

            return View();
        }

        [HttpPost]
        [Route("login", Name = "LoginPost")]
        public ActionResult Login(LoginViewModel login, IFormCollection form, string ReturnUrl = "/")
        {
            #region CaptchaCode

            //string urlToPost = "https://www.google.com/recaptcha/api/siteverify";
            //string secretKey = "6Ld5m5YdAAAAAI9g6_s-VlF8T_sHRd7C66_F4nw6"; // change this
            //string gRecaptchaResponse = form["g-recaptcha-response"];
            //var postData = "secret=" + secretKey + "&response=" + gRecaptchaResponse;
            //// send post data
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlToPost);
            //request.Method = "POST";
            //request.ContentLength = postData.Length;
            //request.ContentType = "application/x-www-form-urlencoded";
            //using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            //{
            //    streamWriter.Write(postData);
            //}
            //// receive the response now
            //string result = string.Empty;
            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            //{
            //    using (var reader = new StreamReader(response.GetResponseStream()))
            //    {
            //        result = reader.ReadToEnd();
            //    }
            //}
            //// validate the response from Google reCaptcha

            //var captChaesponse = JsonConvert.DeserializeObject<ReCaptchaResponse>(result);

            //if (!captChaesponse.Success)
            //{
            //    ViewBag.Message = "Sorry, please validate the reCAPTCHA";
            //    return View();
            //}
            //// go ahead and write code to validate username password against database

            #endregion CaptchaCode

            #region LoginPost

            if (!ModelState.IsValid)
            {
                return View(login);
            }

            var user = _userService.LoginUser(login);

            if (user != null)
            {
                if (user.IsActive)
                {
                    var claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),
                        new Claim(ClaimTypes.Name,user.UserName)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    var properties = new AuthenticationProperties
                    {
                        IsPersistent = login.RememberMe
                    };
                    HttpContext.SignInAsync(principal, properties);

                    ViewBag.IsSuccess = true;
                    if (ReturnUrl != "/")
                    {
                        return Redirect(ReturnUrl);
                    }
                    return View();
                }
                else
                {
                    ModelState.AddModelError("Email", "حساب کاربری شما فعال نمی باشد");
                }
            }
            ModelState.AddModelError("Email", "کاربری با مشخصات وارد شده یافت نشد");
            return View(login);

            #endregion LoginPost
        }

        #endregion Login

        #region ActiveCode

        public IActionResult ActiveCode(string id)
        {
            ViewBag.IsActive = _userService.ActiveCodeAccount(id);
            return View();
        }

        #endregion ActiveCode

        #region Logout

        [Route("Logout")]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Login");
        }

        #endregion Logout

        #region ForgotPassword

        [Route("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [Route("ForgotPassword")]
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel forgot)
        {
            if (!ModelState.IsValid)
            {
                return View(forgot);
            }
            string fixemail = FixedTexd.FixedEmail(forgot.Email);
            var user = _userService.GetUserByEmail(fixemail);
            if (user == null)
            {
                ModelState.AddModelError("Email", "کاربری یافت نشد");
                return View(forgot);
            }
            String bodyEmail = _renderService.RenderToStringAsync("_ForgotPassword", user);
            SendEmail.Send(user.Email, "بازیابی کلمه عبور", bodyEmail);
            ViewBag.IsSuccess = true;
            return View();
        }

        #endregion ForgotPassword

        #region ResetPassword

        public IActionResult ResetPassword(string id)
        {
            return View(new ResetPasswordViewModel()
            {
                ActiveCode = id,
            });
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel reset)
        {
            if (!ModelState.IsValid)
            {
                return View(reset);
            }

            var user = _userService.GetUserActiveCode(reset.ActiveCode);
            if (user == null)

                return NotFound();

            string ConvertPass = PasswordEncript.EncriptPassword(reset.Password);
            user.Password = ConvertPass;
            _userService.UpdateUser(user);
            ViewBag.IsSuccessResstPass = true;
            return View(reset);
        }

        #endregion ResetPassword

        #region MyRegion

        [Route("provider/{provider}")]
        public IActionResult GetProvider(string provider)
        {
            var redirectUrl = Url.RouteUrl("ExternalLogin", Request.Scheme);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [Route("external-login", Name = "ExternalLogin")]
        public IActionResult ExternalLogin()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.SingleOrDefault(u => u.Email == userEmail);
            if (user != null)
            {
                var claims = new List<Claim>()
                {
                    new(ClaimTypes.NameIdentifier,user.UserId.ToString()),
                    new(ClaimTypes.Name,user.UserName)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                var properties = new AuthenticationProperties
                {
                    IsPersistent = true
                };
                HttpContext.SignInAsync(principal, properties);
            }
            return RedirectToRoute("LoginGet");
        }

        #endregion
    }
}