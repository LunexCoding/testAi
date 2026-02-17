using System;
using System.Collections.ObjectModel;

namespace OrderApprovalSystem.ViewModels.Modals.Reject
{
    public interface IRejectModal
    {
        string InputText { get; }
        string Title { get; }
        string SubdivisionRecipient { get; set; }
        string Subdivision { get; set; }
        string Comment { get; set; }
        string ErrorMessage { get; set; }

        ObservableCollection<string> Subdivisions { get; set; }
        ObservableCollection<string> SubdivisionRecipients { get; set; }

        Func<IRejectModal, bool> ExternalValidator { get; set; }
    }
}