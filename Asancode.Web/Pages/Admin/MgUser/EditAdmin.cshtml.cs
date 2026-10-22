using Core.DTO;
using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.MgUser
{

    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class EditAdminModel : PageModel
    {
        private IUserService _userService;
        private IPermissionService _permissionService;

        public EditAdminModel(IUserService userService, IPermissionService permissionService)
        {
            _userService = userService;
            _permissionService = permissionService;
        }

        [BindProperty]
        public EditAdminViewModel EditAdminViewModel { get; set; }
        public void OnGet(int id)
        {
            EditAdminViewModel = _userService.GetInfoAdminForShowEdit(id);
            ViewData["Role"] = _permissionService.GetAllRoles();
        }

        public IActionResult OnPost(List<int> selectedRoles)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //user Edit
            _userService.EditUserForAdmin(EditAdminViewModel);
            //roles Edit
            _permissionService.EditRolesForAdmin(EditAdminViewModel.UserId, selectedRoles);
            return RedirectToPage("Index");
        }
    }
}
