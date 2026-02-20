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

            // Убираем промежуточные узлы с одним ребёнком и тем же RecipientName,
            // т.к. они не добавляют информации (например, Дингес → Дингес → Рагульский → Дингес → Рагульский)
            CollapseSameNameSingleChildNodes(rootNodes);

            // Пересчитываем Level после построения дерева,
            // т.к. reparenting может создать ситуацию когда дочерний узел
            // обрабатывается раньше родительского (по ReceiptDate)
            RecalculateLevels(rootNodes, 0);

            return rootNodes;
        }

        /// <summary>
        /// Убирает промежуточные узлы, у которых ровно 1 ребёнок с тем же RecipientName.
        /// Дети такого узла переносятся к его родителю.
        /// Например: Дингес → Дингес → Рагульский становится Дингес → Рагульский.
        /// </summary>
        private static void CollapseSameNameSingleChildNodes(ObservableCollection<ApprovalHistoryNode> nodes)
        {
            foreach (var node in nodes)
            {
                while (node.Children.Count == 1 &&
                       node.Record?.RecipientName == node.Children[0].Record?.RecipientName)
                {
                    var singleChild = node.Children[0];
                    node.Children.Clear();
                    foreach (var grandchild in singleChild.Children)
                    {
                        grandchild.Parent = node;
                        node.Children.Add(grandchild);
                    }
                }

                if (node.Children.Count > 0)
                {
                    CollapseSameNameSingleChildNodes(node.Children);
                }
            }
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