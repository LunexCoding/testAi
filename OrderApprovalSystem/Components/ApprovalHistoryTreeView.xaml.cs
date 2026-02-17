using System.Windows.Controls;

namespace OrderApprovalSystem.Components
{
    /// <summary>
    /// Reusable TreeView component for displaying hierarchical approval history.
    /// Supports expand/collapse of rework items.
    /// </summary>
    public partial class ApprovalHistoryTreeView : UserControl
    {
        public ApprovalHistoryTreeView()
        {
            InitializeComponent();
        }
    }
}