using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataLayer.Entities.User
{
    public class Plan

    {
        public Plan()
        {
        }

        [Key]
        public int PlanId { get; set; }

        [Display(Name = "کاربر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int UserId { get; set; }

        [Display(Name = "اشتراک "), MaxLength(150)]
        [Required]
        public string PlanLevel { get; set; }

        [Display(Name = "هزینه اشتراک"), MaxLength(150)]
        public int PlanPrice { get; set; }

        [Display(Name = "شرح"), MaxLength(500, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDelete { get; set; }
        public virtual Users Users { get; set; }
        public List<Course.Course> Courses { get; set; }
    }
}