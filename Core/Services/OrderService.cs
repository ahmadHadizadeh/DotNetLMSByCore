using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Core.DTO;
using Core.Genarate;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Entities.Order;
using DataLayer.Entities.User;

namespace Core.Services
{
    public class OrderService : IOrderService
    {
        #region Order

        private AsanwebContext _context;
        private IUserService _userService;

        public OrderService(AsanwebContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public int AddOrder(string userName, int courseId)
        {
            int UserId = _userService.GetUserIdByUsername(userName);

            Order Order = _context.Orders.SingleOrDefault(o => o.UserId == UserId && !o.IsFinally);

            var Course = _context.Course.Find(courseId);
            if (Order == null)
            {
                //First Order
                Order = new Order()
                {
                    UserId = UserId,
                    IsFinally = false,
                    CreateDate = DateTime.Now,
                    OrderSum = Course.CoursePrice,

                    OrderDetail = new List<OrderDetails>()
                    {
                        new OrderDetails()
                        {
                            CourseId = courseId,
                            Count = 1,
                            Price = Course.CoursePrice,
                        }
                    }
                };
                _context.Orders.Add(Order);
                _context.SaveChanges();
            }
            else
            {
                OrderDetails details = _context.OrderDetails.FirstOrDefault(o => o.OrderId == Order.OrderId
                                                                                 && o.CourseId == courseId);
                if (details != null)
                {
                    details.Count += 1;
                    _context.OrderDetails.Update(details);
                }
                else
                {
                    details = new OrderDetails()
                    {
                        OrderId = Order.OrderId,
                        Count = 1,
                        CourseId = courseId,
                        Price = Course.CoursePrice,
                    };
                    _context.OrderDetails.Add(details);
                }
                _context.SaveChanges();
                UpdatePriceOrder(Order.OrderId);
            }

            return Order.OrderId;
        }

        public List<ShowCourseCreateQuestionViewModel> GetCourseByUserPanel()
        {
            return _context.Course.Select(c => new ShowCourseCreateQuestionViewModel()
            {
                ImageCourse = c.CourseImageName,
                CourseId = c.CourseId,
                Title = c.CourseTitle,
            }).Take(8).ToList();
        }

        public void UpdatePriceOrder(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            order.OrderSum = _context.OrderDetails.Where(d => d.OrderId == orderId)
                .Sum(d => d.Price);
            _context.Orders.Update(order);
            _context.SaveChanges();
        }

        public Order GetOrderByShowUser(string userName, int orderId)
        {
            int userId = _userService.GetUserIdByUsername(userName);
            return _context.Orders.Include(o => o.OrderDetail).ThenInclude(o => o.Courses)
                .FirstOrDefault(o => o.UserId == userId && o.OrderId == orderId);
        }

        public bool ApplyOrder(string userName, int orderId)
        {
            int userId = _userService.GetUserIdByUsername(userName);
            var order = _context.Orders.Include(o => o.OrderDetail)
                .ThenInclude(od => od.Courses).SingleOrDefault(o => o.UserId == userId && o.OrderId == orderId);
            if (order == null || order.IsFinally)
            {
                return false;
            }

            if (_userService.BallenceWallet(userName) >= order.OrderSum)
            {
                order.IsFinally = true;
                _userService.AddWallet(new Wallet()
                {
                    Amount = order.OrderSum,
                    CreateDate = DateTime.Now,
                    Description = "شماره فاکتور #" + order.OrderId,
                    IsPay = true,
                    UserId = userId,
                    TypeId = 2,
                });
                _context.Orders.Update(order);

                foreach (var detail in order.OrderDetail)
                {
                    _context.UserCourse.Add(new UserCourse()
                    {
                        CourseId = detail.CourseId,
                        UserId = userId
                    });
                }
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public List<Order> GetListOrderByShowIndexOrder(string userName)
        {
            int userId = _userService.GetUserIdByUsername(userName);
            return _context.Orders.Where(o => o.UserId == userId).ToList();
        }

        public Order GerOrderById(int orderId)
        {
            return _context.Orders.Find(orderId);
        }

        public void UpdateOrder(Order order)
        {
            _context.Orders.Update(order);
            _context.SaveChanges();
        }

        public void UpdateSum(int orderid)
        {
            Discount discount = new Discount();
            var order = GerOrderById(orderid);
            int percent = (order.OrderSum * discount.Percent) / 100;
            order.OrderSum = order.OrderSum - percent;
            UpdateOrder(order);
            _context.SaveChanges();
        }

        public void DeleteOrder(int orderId)
        {
            var order = GerOrderById(orderId);
            order.IsDelete = true;
            _context.Update(order);
            _context.SaveChanges();
        }

        public void GetOrderDetailByDelete(int id)
        {
            var detail = GetOrderDetailsForDelete(id);

            if (detail != null && detail.Count >= 1)
            {
                var count = detail.Count -= 1;
                _context.Remove(detail);
            }
            if (detail != null && detail.Count == 0)
            {
                detail.Price = 0;
                detail.IsDelete = true;
            }
            _context.Update(detail);
            _context.SaveChanges();
        }

        public DiscountUseType UseDiscount(int orderId, string code)
        {
            var disCount = _context.Discounts.SingleOrDefault(d => d.DiscountCode == code);
            if (disCount == null)
                return DiscountUseType.NotFound;
            if (disCount.StartDate != null && disCount.StartDate > DateTime.Now)
                return DiscountUseType.NotStart;

            if (disCount.EndDate != null && DateTime.Now >= disCount.EndDate)
                return DiscountUseType.ExpierDate;

            if (disCount.Count != null && disCount.Count < 1)
                return DiscountUseType.Finished;

            var order = GerOrderById(orderId);
            if (_context.UserDiscounts.Any(d => d.UserId == order.UserId && d.DiscountId == disCount.DiscountId))
                return DiscountUseType.UserUsed;

            int percent = (order.OrderSum * disCount.Percent) / 100;

            order.OrderSum = order.OrderSum - percent;
            UpdateOrder(order);
            if (disCount.Count != null)
            {
                disCount.Count -= 1;
            }
            _context.Discounts.Update(disCount);

            _context.UserDiscounts.Add(new UserDiscount()
            {
                UserId = order.UserId,
                DiscountId = disCount.DiscountId,
            });
            _context.SaveChanges();

            return DiscountUseType.Success;
        }

        public void AddDiscount(Discount discount)
        {
            _context.Discounts.Add(discount);
            _context.SaveChanges();
        }

        public List<Discount> GetAllListByDiscount()
        {
            return _context.Discounts.ToList();
        }

        public Discount GetDiscountById(int discountId)
        {
            return _context.Discounts.Find(discountId);
        }

        public void UpdateDiscount(Discount discount)
        {
            _context.Discounts.Update(discount);
            _context.SaveChanges();
        }

        public bool IsExistCode(string code)
        {
            return _context.Discounts.Any(d => d.DiscountCode == code);
        }

        public bool IsBuyCourse(string userName, int courseId)
        {
            int userId = _userService.GetUserIdByUsername(userName);

            return _context.UserCourse.Any(c => c.UserId == userId && c.CourseId == courseId);
        }

        public bool IsBuyPlan(string userName, int planId)
        {
            int userId = _userService.GetUserIdByUsername(userName);
            return _context.UserPlans.Any(c => c.UserId == userId && c.PlanId == planId);
        }

        public bool IsExistCourseInOrder(int courseId)
        {
            return _context.OrderDetails.Any(od => od.CourseId == courseId);
        }

        public void GetIncraceOrderDetails(int id)
        {
            var details = GetOrderDetailsForDelete(id);
            if (details != null)
            {
                var count = details.Count += 1;
            }
            _context.Update(details);
            _context.SaveChanges();
        }

        public OrderDetails GetOrderDetailsForDelete(int detailsId)
        {
            return _context.OrderDetails.Find(detailsId);
        }

        public List<UserCourse> GetListCourseShowPanel(string userName)
        {
            int userId = _userService.GetUserIdByUsername(userName);
            return _context.UserCourse.Include(c => c.Courses).Where(c => c.UserId == userId).ToList();
        }

        #endregion Order
    }
}