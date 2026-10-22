using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Asancode.Web.Pages.Admin.Course
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]

    public class EditCourseModel : PageModel
    {
        private ICourseGroupService _courseGroupService;

        public EditCourseModel(ICourseGroupService courseGroupService)
        {
            _courseGroupService = courseGroupService;
        }

        [BindProperty]
        public DataLayer.Entities.Course.Course Course { get; set; }
        public void OnGet(int id)
        {
            Course = _courseGroupService.GetCourseByCourseId(id);
            //mainGroup
            var MainGroup = _courseGroupService.GetMainGroup();
            ViewData["maingroup"] = new SelectList(MainGroup, "Value", "Text", Course.GroupId);

            //SubGroup
            List<SelectListItem> subgroup = new List<SelectListItem>()
            {
                new SelectListItem()
                {
                    Text = "انتخاب کنید",Value = ""
                }

            };
            subgroup.AddRange(_courseGroupService.GetSubGroup(Course.GroupId));
            string selectItemSubGroup = null;
            if (Course.SubGroup != null)
            {
                selectItemSubGroup = subgroup.ToString();
            }
            ViewData["subgroup"] = new SelectList(subgroup, "Value", "Text", selectItemSubGroup);
            //teacher

            var Techear = _courseGroupService.GetTeacher();
            ViewData["techear"] = new SelectList(Techear, "Value", "Text", Course.TeacherId);

            //Level
            var levels = _courseGroupService.GetLevels();
            ViewData["Levels"] = new SelectList(levels, "Value", "Text", Course.LevelId);

            //Status
            var statues = _courseGroupService.GetStatus();
            ViewData["Statues"] = new SelectList(statues, "Value", "Text", Course.StatusId);
        }

        public IActionResult OnPost(IFormFile UplodImg, IFormFile demoUp)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //update Course
            _courseGroupService.UpdateCourse(Course, UplodImg, demoUp);

            return RedirectToPage("Index");
        }
    }
}
