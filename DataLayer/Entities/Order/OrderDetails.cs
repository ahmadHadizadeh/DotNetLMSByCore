using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Order
{
    public class OrderDetails
    {
        [Key]
        public int DetailsId { get; set; }
        [Required]
        public int OrderId { get; set; }
        [Required]
        public int CourseId { get; set; }
        public bool IsDelete { get; set; }
        [Required]
        public int Price { get; set; }
        [Required]
        public int Count { get; set; }

        #region Relation

        public virtual Order Orders { get; set; }
        public virtual Course.Course Courses { get; set; }

        #endregion
    }
}
