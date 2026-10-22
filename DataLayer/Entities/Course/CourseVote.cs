using DataLayer.Entities.User;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Course
{
    public class CourseVote
    {
        [Key]
        public int VoteId { get; set; }
        [Required]
        public int CourseId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public bool Vote { get; set; }
        public DateTime VoteDate { get; set; } = DateTime.Now;


        public Users User { get; set; }
        public Course Course { get; set; }
    }
}
