using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Globalization;

namespace Asancode.Web.Pages.Admin.Discount
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class EditDiscountModel : PageModel
    {
        private IOrderService _orderService;

        public EditDiscountModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [BindProperty]
        public DataLayer.Entities.Order.Discount Discount { get; set; }
        public void OnGet(int id)
        {
            Discount = _orderService.GetDiscountById(id);
        }
        public IActionResult OnPost(string startDt = "", string endtDt = "")
        {
            if (startDt != "")
            {
                string[] sdt = startDt.Split('/');
                Discount.StartDate = new DateTime(int.Parse(sdt[0]),

                    int.Parse(sdt[1]),
                    int.Parse(sdt[2]),
                    new PersianCalendar()
                );
            }
            if (endtDt != "")
            {
                string[] edt = endtDt.Split('/');
                Discount.EndDate = new DateTime(int.Parse(edt[0]),

                    int.Parse(edt[1]),
                    int.Parse(edt[2]),
                    new PersianCalendar()
                );
            }

            if (!ModelState.IsValid)
                return Page();

            //update discount

            _orderService.UpdateDiscount(Discount);
            return RedirectToPage("index");
        }
    }
}
