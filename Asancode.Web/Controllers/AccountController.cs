using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Convertors;
using Core.DTO;
using Core.Genarate;
using Core.Security;
using Core.Security.Sender;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        private IViewRenderService _renderService;
        private readonly IEmailSender _emailSender;
        private readonly IAuthenticationRateLimiter _authenticationRateLimiter;

        public AccountController(
            IUserService userService,
            IViewRenderService renderService,
            IEmailSender emailSender,
            IAuthenticationRateLimiter authenticationRateLimiter)
        {
            _userService = userService;
            _renderService = renderService;
            _emailSender = emailSender;
            _authenticationRateLimiter = authenticationRateLimiter;
        }

        #endregion InterFace

        #region Register

        [Route("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                Password = string.Empty,
                RegisterDate = DateTime.Now,
                UserImage = "Defult.jpg",
                UserName = register.UserName
            };
            user.Password = _userService.HashPassword(user, register.Password);
            _userService.AddUser(user);
            string body = _renderService.RenderToStringAsync("_ActiveEmail", user);
            _emailSender.Send(user.Email, "ایمیل فعال سازی", body);
            return View("SeuccessRegister", user);
        }

        #endregion Register

        #region Login

        [Route("login", Name = "LoginGet")]
        public ActionResult Login(bool EditProfile = false, string? ReturnUrl = null)
        {
            ViewBag.EditProfile = EditProfile;
            ViewBag.ReturnUrl = ReturnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("login", Name = "LoginPost")]
        public async Task<ActionResult> Login(LoginViewModel login, string? ReturnUrl = null)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(login);
            }

            var clientIpAddress = HttpContext.Connection.RemoteIpAddress;
            var currentLimit = _authenticationRateLimiter.GetStatus(login.Email, clientIpAddress);
            if (currentLimit.IsLimited)
            {
                return ReturnRateLimitedLogin(login, ReturnUrl, currentLimit);
            }

            var user = _userService.LoginUser(login);

            if (user == null || !user.IsActive)
            {
                var failedAttempt = _authenticationRateLimiter.RecordFailedAttempt(
                    login.Email,
                    clientIpAddress);

                if (failedAttempt.IsLimited)
                {
                    return ReturnRateLimitedLogin(login, ReturnUrl, failedAttempt);
                }

                ModelState.AddModelError("Email", "اطلاعات ورود نامعتبر است یا حساب کاربری فعال نیست");
                return View(login);
            }

            // Re-check after password verification so concurrent failed attempts cannot
            // establish a lock while this request proceeds to issue a cookie.
            var finalLimit = _authenticationRateLimiter.GetStatus(login.Email, clientIpAddress);
            if (finalLimit.IsLimited)
            {
                return ReturnRateLimitedLogin(login, ReturnUrl, finalLimit);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, user.UserName),
                new(UserSessionClaims.SecurityStamp, user.SecurityStamp.ToString("N"))
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = login.RememberMe
            };

            _authenticationRateLimiter.ClearAfterSuccessfulLogin(login.Email, clientIpAddress);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);

            var destination = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";
            return LocalRedirect(destination);
        }

        private ActionResult ReturnRateLimitedLogin(
            LoginViewModel login,
            string? returnUrl,
            LoginRateLimitStatus status)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.LoginRateLimitSeconds = status.RetryAfterSeconds;
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers["Retry-After"] = status.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            return View("Login", login);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
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
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel forgot)
        {
            if (!ModelState.IsValid)
            {
                return View(forgot);
            }
            string fixemail = FixedTexd.FixedEmail(forgot.Email);
            var user = _userService.GetUserByEmail(fixemail);
            if (user != null)
            {
                var resetToken = _userService.CreatePasswordResetToken(user.UserId);
                var emailModel = new PasswordResetEmailViewModel
                {
                    UserName = user.UserName,
                    ResetToken = resetToken
                };

                String bodyEmail = _renderService.RenderToStringAsync("_ForgotPassword", emailModel);
                _emailSender.Send(user.Email, "بازیابی کلمه عبور", bodyEmail);
            }

            // Keep the response identical for known and unknown addresses.
            ViewBag.IsSuccess = true;
            return View();
        }

        #endregion ForgotPassword

        #region ResetPassword

        public IActionResult ResetPassword(string id)
        {
            if (!_userService.IsPasswordResetTokenValid(id))
            {
                return NotFound();
            }

            return View(new ResetPasswordViewModel { ResetToken = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel reset)
        {
            if (!ModelState.IsValid)
            {
                return View(reset);
            }

            if (!_userService.TryResetPassword(reset.ResetToken, reset.Password))
            {
                return NotFound();
            }

            reset.ResetToken = string.Empty;
            ViewBag.IsSuccessResstPass = true;
            return View(reset);
        }

        #endregion ResetPassword
    }
}