using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.roles
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class CreateRoleModel : PageModel
    {
        private IPermissionService _permissionService;

        public CreateRoleModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }
        [BindProperty]
        public Roles Roles { get; set; }
        public void OnGet()
        {
            ViewData["Permission"] = _permissionService.GetAllPermission();
        }

        public IActionResult OnPost(List<int> SelectPermission)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            int RoleId = _permissionService.AddRoles(Roles);
            Roles.IsDelete = false;
            _permissionService.AddPermissionInRole(RoleId, SelectPermission);
            return RedirectToPage("Index");
        }
    }
}
