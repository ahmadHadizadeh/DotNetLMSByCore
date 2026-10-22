using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.Permission;

namespace DataLayer.Entities.User
{
    public class Roles
    {
        public Roles()
        {

        }
        [Key]
        public int RoleId { get; set; }
        [Display(Name = "عنوان نقش ها")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(200, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string RolesTitles { get; set; }

        public bool IsDelete { get; set; }

        #region Relations
        public virtual List<UsersRole> UsersRoles { get; set; }
        public List<RolePermission> RolePermissions { get; set; }
        #endregion
    }
}
