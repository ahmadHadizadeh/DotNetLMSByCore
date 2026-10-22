using DataLayer.Entities.Permission;
using DataLayer.Entities.User;
using System.Collections.Generic;

namespace Core.Services.Interfaces
{
    public interface IPermissionService
    {
        #region Roles

        List<Roles> GetAllRoles();
        void AddRolesForAdmin(List<int> rolesIds, int userId);
        void EditRolesForAdmin(int userId, List<int> roleId);
        int AddRoles(Roles roles);

        Roles GetRolesByRolesId(int rolesId);
        void UpdateRole(Roles roles);
        void DeleteRole(Roles roles);

        #endregion

        #region Permission

        List<Permission> GetAllPermission();
        void AddPermissionInRole(int roleId, List<int> permission);

        List<int> GetPermissionRole(int roleId);
        void UpdatePermission(int roleId, List<int> permission);
        bool CheckAcsessPermissionRole(int permissionId, string userName);

        #endregion
    }
}
