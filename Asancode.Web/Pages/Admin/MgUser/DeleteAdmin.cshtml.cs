using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.MgUser
{

    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class DeleteAdminModel : PageModel
    {
        private IUserService _userService;

        public DeleteAdminModel(IUserService userService)
        {
            _userService = userService;
        }
        public InformationUserPanelViewModel InformationUserPanelViewModel { get; set; }
        public void OnGet(int id)
        {
            ViewData["UserId"] = id;
            InformationUserPanelViewModel = _userService.GetUserbyUserPanel(id);
        }

        public IActionResult OnPost(int UserId)
        {
            _userService.DeleteUser(UserId);

            return RedirectToPage("Index");
        }
    }
}
