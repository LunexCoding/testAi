using System;
using System.Collections.Generic;
using Fox;
using OrderApprovalSystem.Data.Entities;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.ViewModels;

namespace OrderApprovalSystem.Core.Roles
{
    public static class NavigationService
    {
        private static readonly Dictionary<UserRole, Func<VMBase>> Map =
            new Dictionary<UserRole, Func<VMBase>>()
            {
                { UserRole.Dev, () => new vmDev() },
                { UserRole.Technologist, () => {
                    var vm = new vmOrderApproval();
                    vm.Model = new mTechnologist(); // Явно устанавливаем модель
                    return vm;
                }},
                { UserRole.OrderManager, () => {
                    var vm = new vmOrderApproval();
                    vm.Model = new mOrderManager();
                    return vm;
                }},
                { UserRole.HeadOrderDepartment, () => {
                    var vm = new vmOrderApproval();
                    vm.Model = new mHeadOrderDepartment();
                    return vm;
                }},
                { UserRole.Guest, () => new vmGuest() }
            };

        public static VMBase Navigate(UserRole role)
        {
            if (Map.TryGetValue(role, out var factory))
            {
                var vm = factory();

                // Дополнительная проверка для vmOrderApproval
                if (vm is vmOrderApproval orderApproval && orderApproval.Model == null)
                {
                    switch (role)
                    {
                        case UserRole.Technologist:
                            orderApproval.Model = new mTechnologist();
                            break;
                        case UserRole.OrderManager:
                            orderApproval.Model = new mOrderManager();
                            break;
                        case UserRole.HeadOrderDepartment:
                            orderApproval.Model = new mHeadOrderDepartment();
                            break;
                    }
                }

                return vm;
            }

            return new vmGuest();
        }

        public static VMBase NavigateToHistory(TechnologicalOrder order)
        {
            return new vmApprovalHistory(order);
        }
    }
}