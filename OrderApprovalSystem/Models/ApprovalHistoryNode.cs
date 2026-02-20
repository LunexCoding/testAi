using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Data.Entities;

namespace OrderApprovalSystem.Models
{
    /// <summary>
    /// Represents a node in the approval history tree structure.
    /// Wraps OrderApprovalHistory to support hierarchical display with rework items as children.
    /// </summary>
    public class ApprovalHistoryNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;
        private DateTime? _cachedEffectiveCompletionDate;
        private bool _isCacheValid;

        /// <summary>
        /// The underlying approval history record
        /// </summary>
        public OrderApprovalHistory Record { get; set; }

        /// <summary>
        /// Child nodes representing rework items under this approval
        /// </summary>
        public ObservableCollection<ApprovalHistoryNode> Children { get; set; }

        /// <summary>
        /// Indicates if this node is currently expanded in the tree
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates if this node is currently selected in the tree
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The hierarchical level of this node (0 for root, 1+ for children)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Parent node reference for navigation
        /// </summary>
        public ApprovalHistoryNode Parent { get; set; }

        /// <summary>
        /// Indicates if this node has any children
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;

        /// <summary>
        /// The effective result to display for this node.
        /// Computed by ApprovalHistoryTreeBuilder after the tree is built:
        /// - Rework nodes (IsRework=1 or collapsed from an IsRework=1 child): "Не согласовано"
        /// - Non-rework nodes with rework children: "Не согласовано"
        /// - Root-level nodes with rework children but a later root continuation: "Согласовано"
        /// - Leaf nodes: own Record.Result
        /// </summary>
        public string EffectiveResult { get; set; }

        /// <summary>
        /// Internal flag set by ApprovalHistoryTreeBuilder when this node was collapsed
        /// with a same-name child that had IsRework = true.
        /// Used to derive EffectiveResult.
        /// </summary>
        internal bool EffectiveIsRework { get; set; }

        /// <summary>
        /// Gets the effective completion date for this node.
        /// For parent nodes with children, returns the maximum completion date among all child records.
        /// For leaf nodes or nodes without children, returns the record's own completion date.
        /// The value is cached for performance and recomputed when children are modified.
        /// 
        /// Thread-safety: This property is designed for single-threaded access (WPF UI thread).
        /// If multi-threaded access is required, external synchronization must be provided.
        /// </summary>
        public DateTime? EffectiveCompletionDate
        {
            get
            {
                if (!_isCacheValid)
                {
                    if (HasChildren)
                    {
                        // Find the maximum completion date among all children recursively
                        _cachedEffectiveCompletionDate = GetMaxCompletionDateRecursive(this);
                    }
                    else
                    {
                        _cachedEffectiveCompletionDate = Record?.CompletionDate;
                    }
                    _isCacheValid = true;
                }
                return _cachedEffectiveCompletionDate;
            }
        }

        /// <summary>
        /// Invalidates the cached effective completion date for this node and all its ancestors.
        /// Should be called when children are added or modified.
        /// </summary>
        public void InvalidateCompletionDateCache()
        {
            _isCacheValid = false;
            OnPropertyChanged(nameof(EffectiveCompletionDate));

            // Propagate invalidation up the tree
            Parent?.InvalidateCompletionDateCache();
        }

        /// <summary>
        /// Recursively finds the maximum completion date among this node and all its descendants.
        /// </summary>
        private DateTime? GetMaxCompletionDateRecursive(ApprovalHistoryNode node)
        {
            DateTime? maxDate = node.Record?.CompletionDate;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var childMaxDate = GetMaxCompletionDateRecursive(child);
                    if (childMaxDate.HasValue && (!maxDate.HasValue || childMaxDate.Value > maxDate.Value))
                    {
                        maxDate = childMaxDate;
                    }
                }
            }

            return maxDate;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="record">The approval history record to wrap</param>
        /// <param name="level">The hierarchical level of this node</param>
        public ApprovalHistoryNode(OrderApprovalHistory record, int level = 0)
        {
            Record = record;
            Level = level;
            Children = new ObservableCollection<ApprovalHistoryNode>();
            _isExpanded = false;
            _isSelected = false;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}