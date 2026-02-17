using Fox;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;

namespace OrderApprovalSystem.ViewModels
{
    public class vmDev : VMBase
    {
        public void Go(UserRole role)
        {
            var main = (vmMain)App.Current.MainWindow.DataContext;
            main.Navigate(role);
        }
    }
}