using System;
using System.Net.Mime;
using Core.DTO;
using Core.Services.Interfaces;
using DataLayer.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;

namespace Asancode.Web.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class MyOrderController : Controller
    {
        private IOrderService _orderService;
        private AsanwebContext _context;

        public MyOrderController(IOrderService orderService, AsanwebContext context)
        {
            _orderService = orderService;
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.CourseSlider = _orderService.GetCourseByUserPanel();
            return View(_orderService.GetListOrderByShowIndexOrder(User.Identity.Name));
        }

        public IActionResult ShowOrder(int id, int detailsid, bool apply = false, string type = "")
        {
            ViewBag.CourseSlider = _orderService.GetCourseByUserPanel();
            var order = _orderService.GetOrderByShowUser(User.Identity.Name, id);
            if (order == null)
            {
                return NotFound();
            }
            ViewBag.AlertName = type;
            ViewBag.Apply = apply;
            ViewBag.NotOrder = order;
           
            return View(order);
        }
        public IActionResult DeleteOrder(int id)
        {
            var order = _orderService.GerOrderById(id);
            _orderService.DeleteOrder(id);
            return RedirectToAction("Index");
        }

        public IActionResult ApplyOrder(int id)
        {

            if (User.Identity != null && _orderService.ApplyOrder(User.Identity.Name, id))
            {
                return Redirect("/UserPanel/MyOrder/ShowOrder/" + id + "?apply=true");
            }
            return BadRequest();
        }
        public IActionResult Discount(int orderId, string code)
        {
            DiscountUseType type = _orderService.UseDiscount(orderId, code);
            return Redirect("/UserPanel/MyOrder/ShowOrder/" + orderId + "?type=" + type.ToString());
        }

    }
}
