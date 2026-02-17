using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.ViewModels.Modals;
using OrderApprovalSystem.ViewModels.Modals.Reject;


namespace OrderApprovalSystem.Validators
{

    public static class RejectOrderModalValidator
    {
        public static bool Validate(BaseRejectModal vm)
        {
            // Сбрасываем предыдущее сообщение об ошибке
            vm.ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(vm.Subdivision) && (string.IsNullOrWhiteSpace(vm.SubdivisionRecipient) && string.IsNullOrWhiteSpace(vm.Comment)))
            {
                vm.ErrorMessage = "Заполните все поля!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(vm.Subdivision))
            {
                if (RoleManager.IsInRole(UserRole.Technologist))
                {
                    vm.ErrorMessage = "Выберите подразделение!";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(vm.SubdivisionRecipient))
            {
                vm.ErrorMessage = "Выберите получателя!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(vm.Comment))
            {
                vm.ErrorMessage = "Напишите Комментарий!";
                return false;
            }

            return true;
        }        
    }

}
