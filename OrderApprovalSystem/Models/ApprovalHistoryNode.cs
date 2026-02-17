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
        private DateTime? _maxCompletionDate;
        private bool _maxCompletionDateComputed;

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
        /// Gets the maximum completion date across this node and all its descendants.
        /// Returns null if no completion dates are set in the entire subtree.
        /// Value is cached for performance and recomputed when needed.
        /// </summary>
        public DateTime? MaxCompletionDate
        {
            get
            {
                if (!_maxCompletionDateComputed)
                {
                    _maxCompletionDate = ComputeMaxCompletionDate();
                    _maxCompletionDateComputed = true;
                }
                return _maxCompletionDate;
            }
        }

        /// <summary>
        /// Recursively computes the maximum completion date across this node and all its descendants.
        /// </summary>
        private DateTime? ComputeMaxCompletionDate()
        {
            DateTime? maxDate = Record?.CompletionDate;

            if (Children != null && Children.Count > 0)
            {
                foreach (var child in Children)
                {
                    var childMaxDate = child.MaxCompletionDate;
                    if (childMaxDate.HasValue)
                    {
                        if (!maxDate.HasValue || childMaxDate.Value > maxDate.Value)
                        {
                            maxDate = childMaxDate;
                        }
                    }
                }
            }

            return maxDate;
        }

        /// <summary>
        /// Invalidates the cached MaxCompletionDate value, forcing it to be recomputed on next access.
        /// Call this when the completion date or tree structure changes.
        /// </summary>
        public void InvalidateMaxCompletionDateCache()
        {
            _maxCompletionDateComputed = false;
            _maxCompletionDate = null;
            
            // Notify UI that the property has changed
            OnPropertyChanged(nameof(MaxCompletionDate));
            
            // Recursively invalidate parent nodes up the tree
            Parent?.InvalidateMaxCompletionDateCache();
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