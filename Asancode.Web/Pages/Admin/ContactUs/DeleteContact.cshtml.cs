using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.ContactUs
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]

    public class DeleteContactModel : PageModel
    {
        private IUserService _userService;

        public DeleteContactModel(IUserService userService)
        {
            _userService = userService;
        }

        public DataLayer.Entities.User.ContactUs ContactUs { get; set; }
        public void OnGet(int id)
        {
            ViewData["contactId"] = id;
            ContactUs = _userService.GetContactIdByContact(id);
        }

        public IActionResult OnPost(int contactId)
        {
            _userService.DeleteContactUs(contactId);
            return RedirectToPage("index");
        }
    }
}
