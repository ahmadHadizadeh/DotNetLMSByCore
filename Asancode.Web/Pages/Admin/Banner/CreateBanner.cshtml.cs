using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services.Interfaces;
using DataLayer.Entities.Banner;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Banner
{
    public class CreateBannerModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public CreateBannerModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        [BindProperty] public BannerInformation BannerInformation { get; set; }
        public void OnGet()
        {

        }
        public IActionResult OnPost(IFormFile UplodImg)
        {
            if (!ModelState.IsValid)
                return Page();
            _courseGroupService.AddBanner(BannerInformation, UplodImg);
            return RedirectToPage("Index");
        }

    }
}
