using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Entities.Permission;
using DataLayer.Entities.User;
using System.Collections.Generic;
using System.Linq;

namespace Core.Services
{
    public class PermissionService : IPermissionService
    {
        #region Permissian

        private AsanwebContext _context;
        public PermissionService(AsanwebContext context)
        {
            _context = context;
        }
        public List<Roles> GetAllRoles()
        {
            return _context.Roles.ToList();
        }

        public void AddRolesForAdmin(List<int> rolesIds, int userId)
        {
            foreach (int roleId in rolesIds)
            {
                _context.UsersRoles.Add(new UsersRole()
                {
                    RoleId = roleId,
                    UserId = userId,
                });
                _context.SaveChanges();
            }

        }

        public void EditRolesForAdmin(int userId, List<int> roleId)
        {
            //Delete
            _context.UsersRoles.Where(r => r.UserId == userId).ToList()
                .ForEach(r => _context.UsersRoles.Remove(r));

            //add role
            AddRolesForAdmin(roleId, userId);

        }

        public int AddRoles(Roles roles)
        {
            _context.Add(roles);
            _context.SaveChanges();
            return roles.RoleId;
        }

        public Roles GetRolesByRolesId(int rolesId)
        {
            return _context.Roles.Find(rolesId);
        }

        public void UpdateRole(Roles roles)
        {
            _context.Update(roles);
            _context.SaveChanges();
        }

        public void DeleteRole(Roles roles)
        {
            roles.IsDelete = true;
            UpdateRole(roles);
        }

        public List<Permission> GetAllPermission()
        {
            return _context.Permission.ToList();
        }

        public void AddPermissionInRole(int roleid, List<int> permission)
        {
            foreach (var p in permission)
            {
                _context.RolePermission.Add(new RolePermission()
                {
                    PermissionId = p,
                    RoleId = roleid
                });
            }
            _context.SaveChanges();
        }
        public List<int> GetPermissionRole(int roleId)
        {
            return _context.RolePermission.Where
                (r => r.RoleId == roleId)
                .Select(r => r.PermissionId).ToList();
        }

        public void UpdatePermission(int roleId, List<int> permission)
        {
            _context.RolePermission.Where(p => p.RoleId == roleId).ToList()
                .ForEach(p => _context.RolePermission.Remove(p));
            AddPermissionInRole(roleId, permission);
        }

        public bool CheckAcsessPermissionRole(int permissionId, string userName)
        {
            int userId = _context.Users.Single(u => u.UserName == userName).UserId;

            //GetAllrolse
            List<int> UserRoles = _context.UsersRoles
                .Where(u => u.UserId == userId)
                .Select(r => r.RoleId).ToList();

            if (!UserRoles.Any())
            {
                return false;
            }
            // getAccess Role
            List<int> RolePermission = _context.RolePermission
                .Where(p => p.PermissionId == permissionId)
                .Select(r => r.RoleId).ToList();

            return RolePermission.Any(p => UserRoles.Contains(p));
        }
        #endregion
    }
}
