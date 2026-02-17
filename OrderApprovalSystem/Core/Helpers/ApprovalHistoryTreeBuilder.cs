using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Models;

namespace OrderApprovalSystem.Core.Helpers
{
    /// <summary>
    /// Helper class for building hierarchical tree structure from flat OrderApprovalHistory data.
    /// Converts flat approval history records into a tree where rework items appear as children.
    /// </summary>
    public static class ApprovalHistoryTreeBuilder
    {
        /// <summary>
        /// Builds a hierarchical tree structure from flat approval history records with iteration-based grouping.
        /// 
        /// NEW Business Logic (Iteration-Based Grouping for Repeated Approvals):
        /// - "Повторное согласование" (repeated approval) occurs when the same RecipientName appears multiple times
        ///   in the history, regardless of Sender or IsRework flag.
        /// - When a RecipientName appears for the first time, it creates a root-level node that acts as the active
        ///   approval record.
        /// - When the SAME RecipientName appears again later (repeated approval), a container is created or used
        ///   to group all iterations of that recipient, with each occurrence as a separate iteration node (sibling).
        /// - Between iterations of the same recipient, approvals by OTHER recipients become children of the 
        ///   current iteration node, creating proper parent-child relationships.
        /// - This prevents consecutive duplicate nodes like "Сладков → Сладков → Сладков" and instead produces:
        ///   Container: Сладков
        ///     ├─ Iteration 1: Сладков
        ///     │    └─ Child: Рагульский
        ///     ├─ Iteration 2: Сладков
        ///     │    └─ Child: Дингес
        ///     └─ Iteration 3: Сладков
        ///          └─ Child: Дингес
        ///               └─ Child: Рагульский
        /// 
        /// Algorithm:
        /// 1. Sort all records by ReceiptDate (chronological order)
        /// 2. First pass: Identify which recipients have repeated approvals
        /// 3. Second pass: Build tree structure
        ///    - For recipients with NO repeats: create simple root node
        ///    - For recipients with repeats: create container + iteration nodes for each occurrence
        ///    - For other recipients between iterations: add as children of current parent
        /// 4. Maintain proper Level values and cache invalidation
        /// </summary>
        /// <param name="flatHistory">Flat collection of approval history records.
        /// Should be sorted by ReceiptDate ascending for correct parent-child relationships.</param>
        /// <returns>Collection of root-level tree nodes with children populated</returns>
        public static ObservableCollection<ApprovalHistoryNode> BuildTree(IEnumerable<OrderApprovalHistory> flatHistory)
        {
            if (flatHistory == null)
            {
                return new ObservableCollection<ApprovalHistoryNode>();
            }

            var rootNodes = new ObservableCollection<ApprovalHistoryNode>();
            
            // Ensure records are sorted by ReceiptDate for correct chronological structure
            var sortedHistory = flatHistory.OrderBy(h => h.ReceiptDate).ToList();
            
            if (sortedHistory.Count == 0)
            {
                return rootNodes;
            }

            // First pass: identify which RecipientNames have multiple occurrences (repeated approvals)
            var recipientOccurrences = new Dictionary<string, int>();
            foreach (var record in sortedHistory)
            {
                string recipientName = record.RecipientName ?? "";
                if (!recipientOccurrences.ContainsKey(recipientName))
                {
                    recipientOccurrences[recipientName] = 0;
                }
                recipientOccurrences[recipientName]++;
            }

            // Track container nodes by RecipientName for recipients with multiple occurrences
            var containersByRecipient = new Dictionary<string, ApprovalHistoryNode>();
            
            // Track the current parent node for adding children
            ApprovalHistoryNode currentParent = null;
            
            // Track the last processed RecipientName to detect when we switch recipients
            string lastRecipientName = null;
            
            // Track which recipients we've seen to determine if this is first or repeated occurrence
            var seenRecipients = new HashSet<string>();

            foreach (var record in sortedHistory)
            {
                string recipientName = record.RecipientName ?? "";
                bool hasMultipleOccurrences = recipientOccurrences[recipientName] > 1;
                bool isFirstOccurrence = !seenRecipients.Contains(recipientName);
                
                // Same recipient as previous record - add as child (continuation of flow)
                if (recipientName == lastRecipientName && currentParent != null)
                {
                    var childNode = new ApprovalHistoryNode(record, currentParent.Level + 1)
                    {
                        Parent = currentParent
                    };
                    currentParent.Children.Add(childNode);
                    currentParent.InvalidateCompletionDateCache();
                    currentParent = childNode;
                }
                // Recipient with multiple occurrences
                else if (hasMultipleOccurrences)
                {
                    if (isFirstOccurrence)
                    {
                        // First occurrence of a recipient that will have repeats
                        // Create container at root level using this record
                        var containerNode = new ApprovalHistoryNode(record, 0);
                        rootNodes.Add(containerNode);
                        containersByRecipient[recipientName] = containerNode;
                        seenRecipients.Add(recipientName);
                        
                        // Container serves as current parent for subsequent records
                        currentParent = containerNode;
                        lastRecipientName = recipientName;
                    }
                    else
                    {
                        // Repeated occurrence - create iteration node under container
                        var containerNode = containersByRecipient[recipientName];
                        var iterationNode = new ApprovalHistoryNode(record, containerNode.Level + 1)
                        {
                            Parent = containerNode
                        };
                        containerNode.Children.Add(iterationNode);
                        containerNode.InvalidateCompletionDateCache();
                        
                        currentParent = iterationNode;
                        lastRecipientName = recipientName;
                    }
                }
                // Recipient with single occurrence - simple root node
                else
                {
                    if (currentParent == null)
                    {
                        // No current parent - create root node
                        var rootNode = new ApprovalHistoryNode(record, 0);
                        rootNodes.Add(rootNode);
                        currentParent = rootNode;
                        lastRecipientName = recipientName;
                        seenRecipients.Add(recipientName);
                    }
                    else
                    {
                        // Has current parent - add as child
                        var childNode = new ApprovalHistoryNode(record, currentParent.Level + 1)
                        {
                            Parent = currentParent
                        };
                        currentParent.Children.Add(childNode);
                        currentParent.InvalidateCompletionDateCache();
                        currentParent = childNode;
                        lastRecipientName = recipientName;
                        seenRecipients.Add(recipientName);
                    }
                }
            }

            return rootNodes;
        }

        /// <summary>
        /// Flattens a hierarchical tree structure back into a flat list.
        /// Useful for searching or exporting data.
        /// </summary>
        /// <param name="treeNodes">Collection of root-level tree nodes</param>
        /// <returns>Flat list of all nodes in depth-first order</returns>
        public static List<ApprovalHistoryNode> FlattenTree(IEnumerable<ApprovalHistoryNode> treeNodes)
        {
            var flatList = new List<ApprovalHistoryNode>();

            if (treeNodes == null)
            {
                return flatList;
            }

            foreach (var node in treeNodes)
            {
                FlattenTreeRecursive(node, flatList);
            }

            return flatList;
        }

        /// <summary>
        /// Recursive helper for flattening tree structure
        /// </summary>
        private static void FlattenTreeRecursive(ApprovalHistoryNode node, List<ApprovalHistoryNode> flatList)
        {
            flatList.Add(node);

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    FlattenTreeRecursive(child, flatList);
                }
            }
        }

        /// <summary>
        /// Expands all nodes in the tree to show all children
        /// </summary>
        /// <param name="treeNodes">Collection of tree nodes to expand</param>
        public static void ExpandAll(IEnumerable<ApprovalHistoryNode> treeNodes)
        {
            if (treeNodes == null) return;

            foreach (var node in treeNodes)
            {
                ExpandNodeRecursive(node);
            }
        }

        /// <summary>
        /// Recursive helper for expanding all nodes
        /// </summary>
        private static void ExpandNodeRecursive(ApprovalHistoryNode node)
        {
            if (node == null) return;

            node.IsExpanded = true;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    ExpandNodeRecursive(child);
                }
            }
        }

        /// <summary>
        /// Collapses all nodes in the tree to hide all children
        /// </summary>
        /// <param name="treeNodes">Collection of tree nodes to collapse</param>
        public static void CollapseAll(IEnumerable<ApprovalHistoryNode> treeNodes)
        {
            if (treeNodes == null) return;

            foreach (var node in treeNodes)
            {
                CollapseNodeRecursive(node);
            }
        }

        /// <summary>
        /// Recursive helper for collapsing all nodes
        /// </summary>
        private static void CollapseNodeRecursive(ApprovalHistoryNode node)
        {
            if (node == null) return;

            node.IsExpanded = false;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    CollapseNodeRecursive(child);
                }
            }
        }

        #region Test Helpers (Non-Production)

        /// <summary>
        /// Test harness method to validate the BuildTree logic with sample data.
        /// This method is for development/testing purposes only and should not be used in production code.
        /// 
        /// Test scenarios:
        /// 1. Simple repeated approval: Same recipient appears multiple times
        /// 2. Interleaved approvals: Different recipients between iterations
        /// 3. Complex nested scenarios: Multiple recipients with multiple iterations each
        /// </summary>
        public static string TestBuildTreeLogic()
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine("=== Testing BuildTree Logic ===\n");

            // Test Scenario 1: Simple repeated approval
            output.AppendLine("Scenario 1: Simple Repeated Approval (Сладков appears 3 times)");
            var history1 = new List<OrderApprovalHistory>
            {
                CreateTestRecord(1, 1, "Сладков", new DateTime(2024, 1, 1, 10, 0, 0)),
                CreateTestRecord(2, 1, "Сладков", new DateTime(2024, 1, 2, 10, 0, 0)),
                CreateTestRecord(3, 1, "Сладков", new DateTime(2024, 1, 3, 10, 0, 0))
            };
            var tree1 = BuildTree(history1);
            output.AppendLine(PrintTree(tree1));

            // Test Scenario 2: Repeated approval with interleaved different recipients
            output.AppendLine("Scenario 2: Repeated Approval with Interleaved Recipients");
            var history2 = new List<OrderApprovalHistory>
            {
                CreateTestRecord(1, 1, "Сладков", new DateTime(2024, 1, 1, 10, 0, 0)),
                CreateTestRecord(2, 1, "Рагульский", new DateTime(2024, 1, 2, 10, 0, 0)),
                CreateTestRecord(3, 1, "Сладков", new DateTime(2024, 1, 3, 10, 0, 0)),
                CreateTestRecord(4, 1, "Дингес", new DateTime(2024, 1, 4, 10, 0, 0)),
                CreateTestRecord(5, 1, "Сладков", new DateTime(2024, 1, 5, 10, 0, 0)),
                CreateTestRecord(6, 1, "Дингес", new DateTime(2024, 1, 6, 10, 0, 0)),
                CreateTestRecord(7, 1, "Рагульский", new DateTime(2024, 1, 7, 10, 0, 0))
            };
            var tree2 = BuildTree(history2);
            output.AppendLine(PrintTree(tree2));

            // Test Scenario 3: Multiple recipients with their own iterations
            output.AppendLine("Scenario 3: Multiple Recipients with Iterations");
            var history3 = new List<OrderApprovalHistory>
            {
                CreateTestRecord(1, 1, "Иванов", new DateTime(2024, 1, 1, 10, 0, 0)),
                CreateTestRecord(2, 1, "Петров", new DateTime(2024, 1, 2, 10, 0, 0)),
                CreateTestRecord(3, 1, "Иванов", new DateTime(2024, 1, 3, 10, 0, 0)),
                CreateTestRecord(4, 1, "Сидоров", new DateTime(2024, 1, 4, 10, 0, 0)),
                CreateTestRecord(5, 1, "Петров", new DateTime(2024, 1, 5, 10, 0, 0))
            };
            var tree3 = BuildTree(history3);
            output.AppendLine(PrintTree(tree3));

            output.AppendLine("\n=== Test Complete ===");
            return output.ToString();
        }

        /// <summary>
        /// Helper method to create test OrderApprovalHistory records
        /// </summary>
        private static OrderApprovalHistory CreateTestRecord(int id, int orderApprovalId, string recipientName, DateTime receiptDate)
        {
            return OrderApprovalHistory.CreateOrderApprovalHistory(
                id: id,
                orderApprovalID: orderApprovalId,
                receiptDate: receiptDate,
                term: receiptDate.AddDays(7),
                recipientRole: "Test Role",
                recipientName: recipientName,
                senderRole: "Test Sender Role",
                senderName: "Test Sender"
            );
        }

        /// <summary>
        /// Helper method to print the tree structure for debugging
        /// </summary>
        private static string PrintTree(ObservableCollection<ApprovalHistoryNode> nodes, int indent = 0)
        {
            var output = new System.Text.StringBuilder();
            foreach (var node in nodes)
            {
                var indentStr = new string(' ', indent * 2);
                output.AppendLine($"{indentStr}Level {node.Level}: {node.Record.RecipientName} (ID: {node.Record.ID}, Date: {node.Record.ReceiptDate:yyyy-MM-dd})");
                
                if (node.Children != null && node.Children.Count > 0)
                {
                    output.Append(PrintTree(node.Children, indent + 1));
                }
            }
            return output.ToString();
        }

        #endregion
    }
}