using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.roles
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class IndexModel : PageModel
    {

        private IPermissionService _permissionService;

        public IndexModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }
        public List<Roles> listRoles { get; set; }

        public void OnGet()
        {
            listRoles = _permissionService.GetAllRoles();
        }
    }
}
