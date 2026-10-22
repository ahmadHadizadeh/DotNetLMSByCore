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

    public class EditRolesModel : PageModel
    {
        private IPermissionService _permissionService;

        public EditRolesModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;

        }
        [BindProperty]
        public Roles Roles { get; set; }

        public void OnGet(int id)
        {
            Roles = _permissionService.GetRolesByRolesId(id);
            ViewData["SelectPermission"] = _permissionService.GetAllPermission();
            ViewData["ShowSelectPermission"] = _permissionService.GetPermissionRole(id);
        }

        public IActionResult OnPost(List<int> SelectPermission)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            _permissionService.UpdateRole(Roles);
            _permissionService.UpdatePermission(Roles.RoleId, SelectPermission);
            return RedirectToPage("index");
        }
    }
}
