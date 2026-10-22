using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
  
    public class CreateCourseModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public CreateCourseModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        [BindProperty]
        public DataLayer.Entities.Course.Course Course { get; set; }

        public void OnGet()
        {
            //mainGroup
            var MainGroup = _courseGroupService.GetMainGroup();
            ViewData["maingroup"] = new SelectList(MainGroup, "Value", "Text");

            //SubGroup
            var SubGroup = _courseGroupService.GetSubGroup(int.Parse(MainGroup.First().Value));
            ViewData["subgroup"] = new SelectList(SubGroup, "Value", "Text");
            //teacher

            var Techear = _courseGroupService.GetTeacher();
            ViewData["techear"] = new SelectList(Techear, "Value", "Text");

            //Level
            var levels = _courseGroupService.GetLevels();
            ViewData["Levels"] = new SelectList(levels, "Value", "Text");

            //plan
            // var planList = _courseGroupService.GetPlan();
            // ViewData["plan"] = new SelectList(planList, "Value", "Text");

            //Status
            var statues = _courseGroupService.GetStatus();
            ViewData["Statues"] = new SelectList(statues, "Value", "Text");
        }

        public IActionResult OnPost(IFormFile UplodImg, IFormFile demoUp)
        {
            if (!ModelState.IsValid)
                return Page();

            Course.ShortKey = _courseGroupService.GenerateShortKey();

            _courseGroupService.AddCourse(Course, UplodImg, demoUp);

            return RedirectToPage("Index");
        }
    }
}