using DataLayer.Entities.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Order
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int OrderSum { get; set; }
        public bool IsFinally { get; set; }
        [Required]
        public DateTime CreateDate { get; set; }

        public bool IsDelete { get; set; }

        #region Relation

        public virtual Users User { get; set; }
        public List<OrderDetails> OrderDetail { get; set; }

        #endregion

    }
}
