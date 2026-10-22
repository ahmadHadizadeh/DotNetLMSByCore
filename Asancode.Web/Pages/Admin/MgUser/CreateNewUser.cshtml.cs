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
    public class CreateNewUserModel : PageModel
    {
        private IUserService _userService;
        private IPermissionService _permissionService;

        public CreateNewUserModel(IUserService userService, IPermissionService permissionService)
        {
            _userService = userService;
            _permissionService = permissionService;
        }

        [BindProperty]
        public CreateNewUserViewModel CreateNewUserViewModel { get; set; }
        public void OnGet()
        {
            ViewData["Role"] = _permissionService.GetAllRoles();
        }
        public IActionResult OnPost(List<int> selectedRoles)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //user add
            int userId = _userService.AddUserForAdmin(CreateNewUserViewModel);
            //Roles User
            _permissionService.AddRolesForAdmin(selectedRoles, userId);

            return Redirect("/Admin/MgUser");

        }

    }
}
