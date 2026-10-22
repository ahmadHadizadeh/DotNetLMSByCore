using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Asancode.Web.ViewComponents
{
    public class CourseGroupComponent : ViewComponent
    {
        private ICourseGroupService _courseGroupService;

        public CourseGroupComponent(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.FromResult((IViewComponentResult)View("CourseGroup", _courseGroupService.GetAllGroup()));
        }
    }
}
