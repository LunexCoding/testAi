using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Fox.Core;

using OrderApprovalSystem.Data;


namespace OrderApprovalSystem.ViewModels.Modals.Reject
{

    public class RejectTechnologistModal : BaseRejectModal
    {
        private string _orderNumber;

        public RejectTechnologistModal(string message, string title) : base(message, title)
        {
            try
            {
                _orderNumber = title.Contains(':')
                    ? title.Split(':')[1].Trim()
                    : title.Trim();
            }
            catch
            {
                _orderNumber = title.Trim();
            }

            Subdivisions = new ObservableCollection<string>
            {
                "Технолог",
                "Конструктор"
            };
        }

        protected override void UpdateRecipients()
        {
            if (string.IsNullOrEmpty(Subdivision))
            {
                SubdivisionRecipients = new ObservableCollection<string>();
                return;
            }

            List<string> recipients = new List<string>();

            if (Subdivision == "Технолог")
            {
                recipients = ServiceLocator.DatabaseService.mGetList<zayvka>(
                    record => record.zak_1 == _orderNumber
                ).Data.SelectMany(
                    record => new[] { record.imyts, record.imyt }
                )
                .Distinct()
                .Where(str => !string.IsNullOrEmpty(str))
                .OrderBy(str => str)
                .ToList();
            }
            else if (Subdivision == "Конструктор")
            {
                recipients = ServiceLocator.DatabaseService.mGetList<zayvka>(
                    record => record.zak_1 == _orderNumber
                ).Data.SelectMany(
                    record => new[] { record.imyk, record.ispolk }
                )
                .Distinct()
                .Where(str => !string.IsNullOrEmpty(str))
                .OrderBy(str => str)
                .ToList();
            }

            SubdivisionRecipients = new ObservableCollection<string>(recipients);

            if (!string.IsNullOrEmpty(SubdivisionRecipient) && !recipients.Contains(SubdivisionRecipient))
            {
                SubdivisionRecipient = null;
            }
        }
    }

}
