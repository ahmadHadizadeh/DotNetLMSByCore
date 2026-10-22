using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Asancode.Web.ViewComponents
{
    public class CourseCacheComponent : ViewComponent
    {
        private ICourseGroupService _courseGroupService;

        public CourseCacheComponent(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.FromResult((IViewComponentResult)View("CoursCache", _courseGroupService.GetCourse().Item1));
        }
    }
}