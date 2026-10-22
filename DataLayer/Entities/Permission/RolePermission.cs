using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.User;

namespace DataLayer.Entities.Permission
{
    public class RolePermission
    {
        public RolePermission()
        {

        }
        [Key]
        public int RP_Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        #region ReLation

        public Roles Role { get; set; }
        public Entities.Permission.Permission Permissions { get; set; }
        #endregion
    }
}
