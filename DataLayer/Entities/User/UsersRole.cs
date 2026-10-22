using System.ComponentModel.DataAnnotations;

namespace DataLayer.Entities.User
{
    public class UsersRole
    {
        public UsersRole()
        {

        }

        [Key]
        public int UR_Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        #region Relations

        public virtual Users User { get; set; }
        public virtual Roles Role { get; set; }

        #endregion

    }
}
