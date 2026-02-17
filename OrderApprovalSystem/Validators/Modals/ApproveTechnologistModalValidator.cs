using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderApprovalSystem.ViewModels.Modals;

namespace OrderApprovalSystem.Validators
{

    public static class ApproveTechnologistModalValidator
    {
        public static bool Validate(vmApproveTechnologist vm)
        {
            if (!vm.ManufacturingTerm.HasValue)
            {
                vm.ErrorMessage = "Срок не может быть пустым!";
                return false;
            }
            return true;
        }
    }

}
