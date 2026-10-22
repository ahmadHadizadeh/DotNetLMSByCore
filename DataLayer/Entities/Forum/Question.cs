using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.User;

namespace DataLayer.Entities.Forum
{
   public class Question
    {
        [Key]
        public int QuestionId { get; set; }
        [Required]
        public int CourseId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Display(Name = "عنوان سوال")]
        [Required(ErrorMessage = "عنوان سوال را وارد کنید")]
        [MaxLength(400)]
        public string Title { get; set; }
        [Display(Name = "شرح سوال")]
        [Required(ErrorMessage = "متن سوال را وارد کنید")]
        public string Body { get; set; }

        [Required]
        public DateTime CreateDate { get; set; }
        [Required]
        public DateTime ModifiedDate { get; set; }

        #region Relation
        public Users User { get; set; }
        public Course.Course Courses { get; set; }
        public List<Answer> Answers { get; set; }

       
        #endregion

    }
}
