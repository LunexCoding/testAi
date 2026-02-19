using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Models;

namespace OrderApprovalSystem.Core.Helpers
{
    public static class ApprovalHistoryTreeBuilder
    {
        public static ObservableCollection<ApprovalHistoryNode> BuildTree(IEnumerable<OrderApprovalHistory> flatHistory)
        {
            if (flatHistory == null) return new ObservableCollection<ApprovalHistoryNode>();

            // Сортируем по дате для порядка внутри уровней
            var sorted = flatHistory.OrderBy(h => h.ReceiptDate).ToList();

            // Создаем ноды
            var nodeLookup = sorted.ToDictionary(h => h.ID, h => new ApprovalHistoryNode(h));
            var rootNodes = new ObservableCollection<ApprovalHistoryNode>();

            foreach (var record in sorted)
            {
                var currentNode = nodeLookup[record.ID];

                if (record.ParentID.HasValue && nodeLookup.TryGetValue(record.ParentID.Value, out var parentNode))
                {
                    currentNode.Parent = parentNode;
                    parentNode.Children.Add(currentNode);
                }
                else
                {
                    rootNodes.Add(currentNode);
                }
            }

            // Пересчитываем Level после построения дерева,
            // т.к. reparenting может создать ситуацию когда дочерний узел
            // обрабатывается раньше родительского (по ReceiptDate)
            RecalculateLevels(rootNodes, 0);

            return rootNodes;
        }

        private static void RecalculateLevels(IEnumerable<ApprovalHistoryNode> nodes, int level)
        {
            foreach (var node in nodes)
            {
                node.Level = level;
                if (node.Children != null && node.Children.Count > 0)
                {
                    RecalculateLevels(node.Children, level + 1);
                }
            }
        }
    }
}