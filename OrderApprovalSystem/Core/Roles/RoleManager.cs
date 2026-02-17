using System;

namespace OrderApprovalSystem.Core.Roles
{
    public static class RoleManager
    {
        public static User CurrentUser { get; private set; }

        public static event Action RoleChanged;

        public static void Login(User user)
        {
            CurrentUser = user;
            RoleChanged?.Invoke();
        }

        public static bool IsDev => CurrentUser?.IsDev == true;
        public static UserRole Role => CurrentUser?.ActiveRole ?? UserRole.Guest;

        public static string DisplayRoleName =>
            Role.ToDisplayName();

        public static void SwitchMask(UserRole role)
        {
            if (!IsDev) return;

            CurrentUser.SetRole(role);
            SetUserNameByRole(role);
            RoleChanged?.Invoke();
        }

        public static bool IsInRole(UserRole role)
        {
            return Role == role;
        }

        private static void SetUserNameByRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Technologist:
                    CurrentUser.SetName("Рагульский");
                    break;

                case UserRole.OrderManager:
                    CurrentUser.SetName("Папаева");
                    break;

                case UserRole.HeadOrderDepartment:
                    CurrentUser.SetName("Дингес");
                    break;

                case UserRole.Dev:
                    CurrentUser.SetName("Разработчик");
                    break;

                case UserRole.Guest:
                default:
                    CurrentUser.SetName("Гость");
                    break;
            }
        }
    }
}