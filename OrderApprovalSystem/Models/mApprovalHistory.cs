using System;
using System.Collections.ObjectModel;
using System.Linq;
using Fox;
using Fox.Core;
using OrderApprovalSystem.Core.Helpers;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Data.Entities;

namespace OrderApprovalSystem.Models
{
    public class mApprovalHistory : MBase
    {
        private TechnologicalOrder _order;

        protected Fox.DatabaseService.IDatabaseService db = ServiceLocator.DatabaseService;

        private OrderApprovalHistory _selectedOrderHistoryRecord;
        public OrderApprovalHistory SelectedOrderHistoryRecord
        {
            get => _selectedOrderHistoryRecord;
            set
            {
                _selectedOrderHistoryRecord = value;
                OnPropertyChanged(nameof(SelectedOrderHistoryRecord));
            }
        }

        private ObservableCollection<OrderApprovalHistory> _orderHistory;
        public ObservableCollection<OrderApprovalHistory> OrderHistory
        {
            get => _orderHistory;
            set
            {
                _orderHistory = value;
                OnPropertyChanged(nameof(OrderHistory));
            }
        }

        private ObservableCollection<ApprovalHistoryNode> _historyTree;
        /// <summary>
        /// Hierarchical tree structure of approval history for TreeView binding
        /// </summary>
        public ObservableCollection<ApprovalHistoryNode> HistoryTree
        {
            get => _historyTree;
            set
            {
                _historyTree = value;
                OnPropertyChanged(nameof(HistoryTree));
            }
        }

        public TechnologicalOrder Order
        {
            get => _order;
            set
            {
                _order = value;
                OnPropertyChanged(nameof(Order));
                OnPropertyChanged(nameof(OrderInfo));
            }
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public mApprovalHistory()
        {
        }

        /// <summary>
        /// Конструктор с ID заказа
        /// </summary>
        /// <param name="orderApprovalID">ID заказа для отображения истории</param>
        public mApprovalHistory(TechnologicalOrder order)
        {

            Order = order;
        }

        public void LoadHistory()
        {
            OrderHistory = new ObservableCollection<OrderApprovalHistory>(
                db.mGetList<OrderApprovalHistory>(
                    record =>
                    record.OrderApprovalID == Order.OrderApprovalID
                ).Data
            );

            // Build hierarchical tree structure from flat history
            HistoryTree = ApprovalHistoryTreeBuilder.BuildTree(OrderHistory);
        }

        public string OrderInfo => $"Заказ: {Order.OrderNumber}";
    }
}