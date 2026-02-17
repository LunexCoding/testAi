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
        /// Builds a hierarchical tree structure from flat approval history records.
        /// Records with IsRework=false/null are parent nodes.
        /// Records with IsRework=true are child nodes under the most recent parent at the appropriate level.
        /// Supports multi-level nesting: rework can be returned for further rework, creating deeper hierarchies.
        /// 
        /// Business Logic:
        /// - Non-rework items start new root nodes and reset the nesting hierarchy
        /// - Rework items nest under the most recent item (either root or previous rework)
        /// - Rework items with the same SenderName (Отправитель) are grouped/deduplicated:
        ///   if a parent already has a child with matching SenderName, that child is reused instead of creating a duplicate
        /// - Sequential rework items nest progressively deeper (representing iterative rework cycles)
        /// - Example without deduplication: Сладков → Сладков → Сладков (creates 3 nodes)
        /// - Example with deduplication: Сладков (single node with multiple branches: Ивченко, Шуклина)
        /// 
        /// If your workflow requires rework items at the same level (siblings), additional logic
        /// would be needed to determine when to pop the stack based on business rules.
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
            // Stack to track the current path in the tree for multi-level nesting
            var parentStack = new Stack<ApprovalHistoryNode>();

            // Ensure records are sorted by ReceiptDate for correct hierarchical structure
            var sortedHistory = flatHistory.OrderBy(h => h.ReceiptDate).ToList();

            // Process records in order
            foreach (var record in sortedHistory)
            {
                if (record.IsRework == true)
                {
                    // This is a rework item - add as child to current parent
                    if (parentStack.Count > 0)
                    {
                        var currentParent = parentStack.Peek();
                        
                        // Check if parent already has a child with the same SenderName
                        // If found, reuse that child instead of creating a duplicate node
                        var existingChild = FindChildBySenderName(currentParent, record.SenderName);
                        
                        if (existingChild != null)
                        {
                            // Reuse existing child node - push it onto stack for potential further nesting
                            parentStack.Push(existingChild);
                        }
                        else
                        {
                            // Create new child node
                            var childNode = new ApprovalHistoryNode(record, currentParent.Level + 1)
                            {
                                Parent = currentParent
                            };
                            currentParent.Children.Add(childNode);

                            // Invalidate completion date cache since we added a child
                            currentParent.InvalidateCompletionDateCache();

                            // Push this child onto the stack so subsequent rework items can nest under it
                            parentStack.Push(childNode);
                        }
                    }
                    else
                    {
                        // No parent found - this is a data inconsistency
                        // Add as root node to prevent data loss
                        var node = new ApprovalHistoryNode(record, 0);
                        rootNodes.Add(node);
                        parentStack.Clear();
                        parentStack.Push(node);
                    }
                }
                else
                {
                    // This is a regular (non-rework) item - add as root node
                    var node = new ApprovalHistoryNode(record, 0);
                    rootNodes.Add(node);

                    // Reset the parent stack and set this as the current parent
                    parentStack.Clear();
                    parentStack.Push(node);
                }
            }

            return rootNodes;
        }

        /// <summary>
        /// Finds an existing child node in the parent's children with matching SenderName.
        /// Used for deduplication when processing rework items - if a child with the same sender already exists,
        /// we reuse it instead of creating a duplicate node.
        /// </summary>
        /// <param name="parent">Parent node to search children within</param>
        /// <param name="senderName">SenderName to match (case-sensitive comparison)</param>
        /// <returns>Existing child node with matching SenderName, or null if not found</returns>
        private static ApprovalHistoryNode FindChildBySenderName(ApprovalHistoryNode parent, string senderName)
        {
            if (parent?.Children == null || parent.Children.Count == 0)
            {
                return null;
            }

            // Handle null/empty senderName - use string.IsNullOrEmpty for consistency
            if (string.IsNullOrEmpty(senderName))
            {
                return null;
            }

            // Search for existing child with matching SenderName
            // Using Ordinal comparison for consistent string matching
            foreach (var child in parent.Children)
            {
                if (child.Record != null && 
                    !string.IsNullOrEmpty(child.Record.SenderName) &&
                    string.Equals(child.Record.SenderName, senderName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
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
    }
}