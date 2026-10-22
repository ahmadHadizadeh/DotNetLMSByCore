using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO.Banner;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Banner
{
    public class DeleteBannerModel : PageModel
    {
        private ICourseGroupService _courseGroup;

        public DeleteBannerModel(ICourseGroupService courseGroup)
        {
            _courseGroup = courseGroup;
        }

        public BannerInfoViewModel GetBannerForDelete { get; set; }
        public void OnGet(int id)
        {
            ViewData["BannerId"] = id;
            GetBannerForDelete = _courseGroup.GetBannerForDelete(id);
        }
        public IActionResult OnPost(int bannerId)
        {
            _courseGroup.DeleteBanner(bannerId);
            return RedirectToPage("Index");
        }
    }
}
