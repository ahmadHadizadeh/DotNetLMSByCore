using DataLayer.Entities.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Order
{
    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }
        [Display(Name = "کد")]
        [MaxLength(150, ErrorMessage = "تعداد کاراکتر{0} نمی تواند بیشتر از{1} کاراکتر باشد")]

        public string DiscountCode { get; set; }
        [Display(Name = "درصد")]
        [Required]
        public int Percent { get; set; }
        [Display(Name = "تعداد")]
        public int? Count { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        #region Relation

        public List<UserDiscount> UserDiscounts { get; set; }

        #endregion
    }
}
