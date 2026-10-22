using DataLayer.Entities.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Course
{
    public class CourseComment
    {
        [Key]
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        [MaxLength(700)]
        public string Comment { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDelete { get; set; }
        public bool IsReadAdmin { get; set; }

        #region Relation
        public Users User { get; set; }
        public Course Courses { get; set; }
        #endregion


    }
}
