using System.Collections.Generic;

namespace OrderApprovalSystem.Core.Roles
{
    public static class AccessManager
    {
        private static readonly Dictionary<UserRole, HashSet<UserRole>> Matrix =
            new Dictionary<UserRole, HashSet<UserRole>>()
            {
                { UserRole.Dev, new HashSet<UserRole>() { UserRole.Dev, UserRole.Technologist, UserRole.OrderManager, UserRole.HeadOrderDepartment } },
                { UserRole.Technologist, new HashSet<UserRole>() { UserRole.Technologist } },
                { UserRole.OrderManager, new HashSet<UserRole>() { UserRole.OrderManager } },
                { UserRole.HeadOrderDepartment, new HashSet<UserRole>() { UserRole.HeadOrderDepartment } },
            };

        public static bool CanAccess(UserRole target)
        {
            if (RoleManager.IsDev) return true;

            return Matrix.TryGetValue(RoleManager.Role, out var set)
                   && set.Contains(target);
        }
    }
}