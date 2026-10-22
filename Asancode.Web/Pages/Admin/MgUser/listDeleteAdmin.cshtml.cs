using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.MgUser
{

    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class listDeleteAdminModel : PageModel
    {
        private IUserService _userService;

        public listDeleteAdminModel(IUserService userService)
        {
            _userService = userService;
        }

        public AdminUserViewModel AdminUserViewModel { get; set; }

        public void OnGet(int pageId = 1, string filterUserName = "", string filterEmail = "")
        {
            AdminUserViewModel = _userService.GetDeleteUser(pageId, filterUserName, filterEmail);

        }
    }
}
