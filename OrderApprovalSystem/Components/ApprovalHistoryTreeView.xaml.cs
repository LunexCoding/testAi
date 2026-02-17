using System.Windows.Controls;

namespace OrderApprovalSystem.Components
{
    /// <summary>
    /// Reusable TreeView component for displaying hierarchical approval history.
    /// Supports multi-level expand/collapse of rework items.
    /// 
    /// Features:
    /// - Displays approval history in hierarchical tree structure
    /// - Supports arbitrary nesting depth for rework cycles
    /// - Shows computed completion dates for parent nodes (max from children)
    /// - Column order: Executor, Deadline, State, Result, Receipt Date, Completion Date, Comment
    /// - Proper text overflow handling (ellipsis for names/dates, wrapping for comments)
    /// - Column widths accommodate indentation without text overlap
    /// </summary>
    public partial class ApprovalHistoryTreeView : UserControl
    {
        public ApprovalHistoryTreeView()
        {
            InitializeComponent();
        }
    }
}