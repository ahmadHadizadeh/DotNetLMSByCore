using Core.DTO;
using Core.Genarate;
using DataLayer.Entities.Order;
using System.Collections.Generic;

namespace Core.Services.Interfaces
{
    public interface IOrderService
    {
        int AddOrder(string userName, int courseId);

        List<ShowCourseCreateQuestionViewModel> GetCourseByUserPanel();

        void UpdatePriceOrder(int orderId);

        Order GetOrderByShowUser(string userName, int orderId);

        bool ApplyOrder(string userName, int orderId);

        List<Order> GetListOrderByShowIndexOrder(string userName);

        Order GerOrderById(int orderId);

        void UpdateOrder(Order order);

        void UpdateSum(int orderid);

        bool IsBuyCourse(string userName, int courseId);

        bool IsBuyPlan(string userName, int planId);

        bool IsExistCourseInOrder(int courseId);

        void GetIncraceOrderDetails(int id);

        OrderDetails GetOrderDetailsForDelete(int detailsId);

        void GetOrderDetailByDelete(int id);

        void DeleteOrder(int orderId);

        List<UserCourse> GetListCourseShowPanel(string userName);

        #region Discount

        DiscountUseType UseDiscount(int orderId, string code);

        void AddDiscount(Discount discount);

        List<Discount> GetAllListByDiscount();

        Discount GetDiscountById(int discountId);

        void UpdateDiscount(Discount discount);

        bool IsExistCode(string code);

        #endregion Discount
    }
}