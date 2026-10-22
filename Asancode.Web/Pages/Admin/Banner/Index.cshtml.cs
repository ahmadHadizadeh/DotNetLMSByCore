using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Core.Services.Interfaces;
using DataLayer.Entities.Banner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asancode.Web.Pages.Admin.Banner
{
    public class IndexModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public IndexModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }
        public List<BannerInformation> BannerInformations { get; set; }
        public void OnGet()
        {
            BannerInformations = _courseGroupService.GetAllBannerForIndex();
        }
    }
}
