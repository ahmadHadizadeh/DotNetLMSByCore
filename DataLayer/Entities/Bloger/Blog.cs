using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.Bloger

{
    public class Blog
    
    {
        public Blog()
        {
            
        }
        [Key]
        public int BlogId { get; set; }

        [Display(Name = "عنوان مقاله")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string Title { get; set; }

        [Display(Name = "متن مقاله")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string TextBlog { get; set; }
        [Display(Name = "چکیده")]
        [MaxLength(700)]
        public string AbstractText { get; set; }

        [Display(Name = "نام نویسنده")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string Writer { get; set; }
        [MaxLength(5)]
        public string ShortKey { get;set;}
        public DateTime Date { get; set; }

        [MaxLength(50)]
        public string BlogImageName { get; set; }

        [MaxLength(600)]
        public string Tags { get; set; }

        public bool IsDelete { get; set; }

        #region Relation
        public List<CommentBlog> CommentBlogs { get; set; }
        #endregion

    }
}
