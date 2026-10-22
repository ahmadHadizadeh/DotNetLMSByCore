using DataLayer.Entities.User;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Order
{
    public class UserCourse
    {
        [Key]
        public int UC_Id { get; set; }

        public int UserId { get; set; }
        public int CourseId { get; set; }

        #region relation

        public Users User { get; set; }
        public Course.Course Courses { get; set; }

        #endregion relation
    }
}