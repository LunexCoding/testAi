using System.Collections.ObjectModel;
using System.Linq;

using Fox.Core;

using OrderApprovalSystem.Data;
using OrderApprovalSystem.ViewModels.Modals.Reject;

namespace OrderApprovalSystem.ViewModels.Modals
{
    public class RejectOrderManagerModal : BaseRejectModal
    {
        private int _orderApprovalId;
        // Константа для исключения конкретного пользователя
        private const string ExcludedUser = "Папаев";

        public RejectOrderManagerModal(string message, string title, int? orderApprovalID) : base(message, title)
        {
            _orderApprovalId = orderApprovalID ?? 0;
            LoadSubdivisionRecipients();
        }

        /// <summary>
        /// Загружает список уникальных людей из истории (отправителей и получателей),
        /// исключая "Менеджера заказов" (если нужно по логике) и Папаева.
        /// </summary>
        private void LoadSubdivisionRecipients()
        {
            if (_orderApprovalId <= 0)
            {
                SubdivisionRecipients = new ObservableCollection<string>();
                return;
            }

            var histories = ServiceLocator.DatabaseService.mGetList<OrderApprovalHistory>(
                record => record.OrderApprovalID == _orderApprovalId
            ).Data;

            if (histories == null || !histories.Any())
            {
                SubdivisionRecipients = new ObservableCollection<string>();
                return;
            }

            // Собираем всех уникальных людей (и отправителей, и получателей)
            var participants = histories
                .SelectMany(h => new[] { h.RecipientName, h.SenderName })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => !name.Contains(ExcludedUser)) // Удаляем Папаева
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            SubdivisionRecipients = new ObservableCollection<string>(participants);
        }

        /// <summary>
        /// Переопределяем метод, так как теперь нам не нужно разделение на подразделения/роли.
        /// Если BaseRejectModal требует этот метод, оставляем его пустым или обновляем коллекцию.
        /// </summary>
        protected override void UpdateRecipients()
        {

        }
    }

}