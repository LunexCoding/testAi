using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderApprovalSystem.Core.Roles
{
    public sealed class User
    {
        public string Name { get; private set; }
        public bool IsDev { get; }

        // Маска (активная роль)
        public UserRole ActiveRole { get; private set; }

        // Реальные роли пользователя
        public IReadOnlyCollection<UserRole> Roles { get; }

        public User(string name, IEnumerable<UserRole> roles)
        {
            Name = name;
            Roles = roles.ToList();

            IsDev = Roles.Contains(UserRole.Dev);
            ActiveRole = IsDev ? UserRole.Dev : Roles.FirstOrDefault();
        }

        internal void SetRole(UserRole role)
        {
            ActiveRole = role;
        }

        internal void SetName(string name)
        {
            Name = name;
        }
    }
}