using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.Subscribe
{
    [PermissionChecker(1)]
   
    public class IndexModel : PageModel
    {
        private IUserService _userService;

        public IndexModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<SubscribeToNewsLetter> SubscribeToNewsLetter { get; set; }
        public void OnGet()
        {
            SubscribeToNewsLetter = _userService.GetSubscribeByShow();
        }
    }
}
