using DataLayer.Entities.Bloger;
using DataLayer.Entities.Course;
using DataLayer.Entities.Order;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.Forum;

namespace DataLayer.Entities.User
{
    public class Users
    {
        public Users()
        {
        }

        [Key]
        public int UserId { get; set; }

        [Display(Name = "نام کاربری")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(200, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string UserName { get; set; }

        [Display(Name = "ایمیل")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(200, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نمی باشد")]
        public string Email { get; set; }

        [Display(Name = "کلمه عبور")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(200, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string Password { get; set; }

        [Display(Name = "آوتار")]
        [MaxLength(200, ErrorMessage = "{ 0 } نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string UserImage { get; set; }

        [Display(Name = "کد فعال سازی")]
        [MaxLength(50, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string ActiveCode { get; set; }

        [Display(Name = "تاریخ")]
        public DateTime RegisterDate { get; set; }

        [Display(Name = "وضعیت")]
        public bool IsActive { get; set; }

        public bool IsDelete { get; set; }

        #region Relations

        public virtual List<UsersRole> UserRoles { get; set; }
        public virtual List<Wallet> Wallets { get; set; }
        public virtual List<Plan> Plans { get; set; }
        public virtual List<Order.Order> Orders { get; set; }
        public virtual List<UserCourse> UserCourses { get; set; }

        public List<UserDiscount> UserDiscounts { get; set; }
        public List<CourseComment> CourseComments { get; set; }
        public List<CommentBlog> CommentBlogs { get; set; }
        public virtual List<CourseVote> CourseVotes { get; set; }
        public virtual List<Question> Questions { get; set; }

        #endregion Relations
    }
}