using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.roles
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class DeleteRolesModel : PageModel
    {
        public IPermissionService _permissionService;

        public DeleteRolesModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [BindProperty]
        public Roles Roles { get; set; }
        public void OnGet(int id)
        {
            Roles = _permissionService.GetRolesByRolesId(id);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //deleteRole
            _permissionService.DeleteRole(Roles);
            return RedirectToPage("index");
        }
    }
}
