using System;
using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.User;

namespace DataLayer.Entities.Forum
{
   public class Answer
    {
        [Key]
        public int AnswerId { get; set; }
        [Required]
        public int QuestionId { get; set; }
        public  Question Question{ get; set; }
        [Required]
        public int UserId { get; set; }
        public Users User{ get; set; }
        public bool IsTrue { get; set; } = false;
        [Required]
        public string BodyAnswer { get; set; }
        [Required]
        public DateTime CreateDate { get; set; }

    }
}
