using Core.DTO;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// using ZarinPal;





namespace Asancode.Web.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class WalletController : Controller
    {
        #region Interface

        private IUserService _userService;
        public WalletController(IUserService userService)
        {
            _userService = userService;
        }

        #endregion
        #region Wallet
        [Route("UserPanel/Wallet")]

        public IActionResult Index()
        {
            if (User.Identity != null) ViewBag.listWallet = _userService.GetInformationeWallet(User.Identity.Name);
            return View();
        }
        [Route("UserPanel/Wallet")]
        [HttpPost]
        public IActionResult Index(ChargeWalletviewModel charge)
        {
            if (!ModelState.IsValid)
            {
                if (User.Identity != null) ViewBag.listWallet = _userService.GetInformationeWallet(User.Identity.Name);
                return View(charge);
            }

            if (User.Identity != null)
            {
                if (User.Identity.Name != null)
                {
                    int walletId = _userService.ChargeWallet(User.Identity.Name, charge.Amount, "شارژ حساب");
                    // var payment = new ZarinPal.Class.Payment("cd3ddeb1-1c95-4d1a-aeb9-d87477ba5b55", charge.Amount);
                    // var res = payment.Request("شارژ کیف پول", "https://www.asancode.com/OnlinePayment/" + walletId);
                    // if (res.Result.Status == 100)
                    // {
                    //     return Redirect("https://www.zarinpal.com/pg/StartPay/" + res.Result.Authority);
                    // }
                }
            }
            /**/
            return null;
        }
        #endregion
        public IActionResult DeleteWallet(int id)
        {
            var wallet =_userService.GetWalletByWalletId(id);
            _userService.DeleteWallet(id);
            return RedirectToAction("Index");
        }
    }
}
