using Core.Security.CheckPermission;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin
{
    [PermissionChecker(1)]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
