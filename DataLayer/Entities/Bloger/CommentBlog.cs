using DataLayer.Entities.User;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Bloger
{
    public class CommentBlog
    {
        [Key]
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public int BlogId { get; set; }
        [MaxLength(700)]
        public string Comment { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDelete { get; set; }

        #region Relation
        public Users User { get; set; }
        public Blog Blogs { get; set; }
        #endregion
    }
}
