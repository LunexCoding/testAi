using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Models;

namespace OrderApprovalSystem.Core.Helpers
{
    public static class ApprovalHistoryTreeBuilder
    {
        private const string PipeIndent = "│   ";
        private const string EmptyIndent = "    ";
        private const string MiddleChildConnector = "├── ";
        private const string LastChildConnector = "└── ";

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

            // Вычисляем IsLastChild и строковый префикс коннекторов дерева (├──, └──, │)
            SetIsLastChild(rootNodes);
            ComputeTreeConnectorPrefixes(rootNodes);

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

        /// <summary>
        /// Устанавливает IsLastChild для каждого узла в дереве.
        /// </summary>
        private static void SetIsLastChild(ObservableCollection<ApprovalHistoryNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].IsLastChild = (i == nodes.Count - 1);
                if (nodes[i].Children.Count > 0)
                {
                    SetIsLastChild(nodes[i].Children);
                }
            }
        }

        /// <summary>
        /// Вычисляет строковый префикс коннекторов дерева (├──, └──, │) для каждого узла.
        /// </summary>
        private static void ComputeTreeConnectorPrefixes(ObservableCollection<ApprovalHistoryNode> rootNodes)
        {
            foreach (var node in rootNodes)
            {
                ComputeTreeConnectorPrefixForNode(node);
            }
        }

        private static void ComputeTreeConnectorPrefixForNode(ApprovalHistoryNode node)
        {
            var sb = new StringBuilder();

            // Собираем предков от корня до родителя
            var ancestors = new List<ApprovalHistoryNode>();
            var current = node.Parent;
            while (current != null)
            {
                ancestors.Add(current);
                current = current.Parent;
            }
            ancestors.Reverse();

            // Для каждого предка: │ если не последний, пробелы если последний
            foreach (var ancestor in ancestors)
            {
                sb.Append(ancestor.IsLastChild ? EmptyIndent : PipeIndent);
            }

            // Для текущего узла: └── если последний, ├── если нет
            sb.Append(node.IsLastChild ? LastChildConnector : MiddleChildConnector);

            node.TreeConnectorPrefix = sb.ToString();

            foreach (var child in node.Children)
            {
                ComputeTreeConnectorPrefixForNode(child);
            }
        }
    }
}