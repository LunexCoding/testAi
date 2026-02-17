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
        /// - Consecutive nodes with the same RecipientName under the same parent are merged to avoid duplication
        /// - Sequential rework items nest progressively deeper (representing iterative rework cycles)
        /// - Example: Approval → Rework1 → Rework2 → Rework3 (each rework is a child of the previous)
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
                        
                        // Check if a child node with the same RecipientName already exists under current parent.
                        // This prevents creating duplicate child nodes when the same person appears multiple times
                        // in the approval flow. The current record acts as a "routing" record that navigates to
                        // the existing node rather than creating a new duplicate node.
                        // Only attempt to find existing child if RecipientName is not null.
                        var existingChild = record.RecipientName != null 
                            ? FindChildByRecipientName(currentParent, record.RecipientName)
                            : null;
                        
                        if (existingChild != null)
                        {
                            // Reuse the existing node instead of creating a duplicate.
                            // Note: The current record's data is not stored separately - it serves only to
                            // direct the flow to nest subsequent records under the existing node.
                            // This design treats consecutive appearances of the same person as a single logical node.
                            parentStack.Push(existingChild);
                        }
                        else
                        {
                            // Create a new child node
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
        /// Finds an existing child node under the given parent that has the same RecipientName.
        /// Used to merge duplicate consecutive nodes for the same person.
        /// </summary>
        /// <param name="parent">Parent node to search within</param>
        /// <param name="recipientName">RecipientName to match</param>
        /// <returns>Existing child node with matching RecipientName, or null if not found.
        /// If multiple children have the same RecipientName (data inconsistency), returns the first one.</returns>
        private static ApprovalHistoryNode FindChildByRecipientName(ApprovalHistoryNode parent, string recipientName)
        {
            if (parent?.Children == null || string.IsNullOrEmpty(recipientName))
            {
                return null;
            }

            // Using FirstOrDefault is appropriate because:
            // 1. Under normal circumstances, a parent should not have multiple children with identical RecipientNames
            // 2. If such duplicates exist (data inconsistency), reusing the first occurrence is reasonable
            // 3. This fix prevents creating additional duplicates going forward
            return parent.Children.FirstOrDefault(child => 
                child.Record?.RecipientName?.Equals(recipientName, StringComparison.Ordinal) == true);
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