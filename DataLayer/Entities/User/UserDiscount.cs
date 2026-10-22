using DataLayer.Entities.Order;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.User
{
    public class UserDiscount
    {
        [Key]
        public int UD_Id { get; set; }
        public int UserId { get; set; }
        public int DiscountId { get; set; }

        #region Relation

        public Users User { get; set; }
        public Discount Discounts { get; set; }

        #endregion
    }
}
