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

            // Вычисляем эффективный результат для каждого узла:
            // - Узлы с IsRework=1 (или поглотившие IsRework=1 при свёртке): "Не согласовано"
            // - Не-корневые узлы с дочерними элементами: "Не согласовано"
            // - Корневые узлы с дочерними элементами, за которыми следует более поздний корневой узел: "Согласовано"
            // - Листовые узлы: собственный Result из Record
            ComputeEffectiveResults(rootNodes);

            return rootNodes;
        }

        /// <summary>
        /// Убирает промежуточные узлы, у которых ровно 1 ребёнок с тем же RecipientName.
        /// Дети такого узла переносятся к его родителю.
        /// Например: Дингес → Дингес → Рагульский становится Дингес → Рагульский.
        /// Если поглощаемый ребёнок имеет IsRework=true или сам помечен как EffectiveIsRework,
        /// результирующему узлу выставляется EffectiveIsRework=true.
        /// </summary>
        private static void CollapseSameNameSingleChildNodes(ObservableCollection<ApprovalHistoryNode> nodes)
        {
            foreach (var node in nodes)
            {
                while (node.Children.Count == 1 &&
                       node.Record?.RecipientName == node.Children[0].Record?.RecipientName)
                {
                    var singleChild = node.Children[0];

                    if (singleChild.Record?.IsRework == true || singleChild.EffectiveIsRework)
                        node.EffectiveIsRework = true;

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
        /// Вычисляет и присваивает <see cref="ApprovalHistoryNode.EffectiveResult"/> каждому узлу дерева.
        ///
        /// Правила (применяются в порядке приоритета):
        /// 1. Узел поглотил IsRework=1 ребёнка при свёртке (EffectiveIsRework=true) → "Не согласовано"
        /// 2. Корневой узел (Parent==null) с дочерними элементами и более поздним корневым узлом → "Согласовано"
        /// 3. Не-корневой узел с дочерними элементами → "Не согласовано"
        /// 4. Листовой узел → собственный Record.Result
        /// </summary>
        private static void ComputeEffectiveResults(ObservableCollection<ApprovalHistoryNode> rootNodes)
        {
            ComputeEffectiveResultsRecursive(rootNodes, rootNodes);
        }

        private static void ComputeEffectiveResultsRecursive(
            IEnumerable<ApprovalHistoryNode> nodes,
            ObservableCollection<ApprovalHistoryNode> rootNodes)
        {
            foreach (var node in nodes)
            {
                if (node.EffectiveIsRework)
                {
                    node.EffectiveResult = "Не согласовано";
                }
                else if (node.HasChildren)
                {
                    if (node.Parent == null)
                    {
                        // Корневой узел: "Согласовано" если есть более поздний корневой узел,
                        // иначе "Не согласовано" (доработка не завершена)
                        bool hasLaterRoot = node.Record != null &&
                                           rootNodes.Any(r => r.Record != null &&
                                                              r.Record.ReceiptDate > node.Record.ReceiptDate);
                        node.EffectiveResult = hasLaterRoot ? "Согласовано" : "Не согласовано";
                    }
                    else
                    {
                        // Не-корневой узел с дочерними элементами — итерация доработки
                        node.EffectiveResult = "Не согласовано";
                    }
                }
                else
                {
                    node.EffectiveResult = node.Record?.Result;
                }

                if (node.HasChildren)
                {
                    ComputeEffectiveResultsRecursive(node.Children, rootNodes);
                }
            }
        }
    }
}