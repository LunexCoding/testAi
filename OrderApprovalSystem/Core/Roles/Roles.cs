using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderApprovalSystem.Core.Roles
{
    public enum UserRole
    {
        Guest = 0,
        Dev = 1,
        Technologist = 2,
        OrderManager = 3,
        HeadOrderDepartment = 4
    }


    public static class UserRoleExtensions
    {
        public static string ToDisplayName(this UserRole role)
        {
            switch (role)
            {
                case UserRole.Dev: return "Разработчик";
                case UserRole.Technologist: return "Технолог";
                case UserRole.OrderManager: return "Менеджер заказов";
                case UserRole.HeadOrderDepartment: return "Начальник отдела заказов";
                case UserRole.Guest: return "Гость";
                default: return role.ToString();
            }
        }
    }
}
