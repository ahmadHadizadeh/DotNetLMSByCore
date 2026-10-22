using Core.Security.CheckPermission;
using Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;


namespace Asancode.Web.Pages.Admin.Discount
{
    [PermissionChecker(1)]
    [PermissionChecker(2)]
    public class indexModel : PageModel
    {
        private IOrderService _orderService;

        public indexModel(IOrderService orderService)
        {
            _orderService = orderService;
        }
        public List<DataLayer.Entities.Order.Discount> Discounts { get; set; }
        public void OnGet()
        {
            Discounts = _orderService.GetAllListByDiscount();

        }
    }
}
