using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.ContactUs
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class indexModel : PageModel
    {
        private IUserService _userService;

        public indexModel(IUserService userService)
        {
            _userService = userService;
        }

        public ContactUsViewModel ContactUsViewModel { get; set; }

        public void OnGet(int pageId = 1, string filterUserName = "", string filterEmail = "")
        {
            ContactUsViewModel = _userService.GetContactUs(pageId, filterUserName, filterEmail);

        }
    }
}
