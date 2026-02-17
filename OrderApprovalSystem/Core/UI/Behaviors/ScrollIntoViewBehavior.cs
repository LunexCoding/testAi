using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows;


namespace OrderApprovalSystem.Behaviors
{

    public class ScrollIntoViewBehavior : Behavior<DataGrid>
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object),
                typeof(ScrollIntoViewBehavior),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as ScrollIntoViewBehavior;
            if (behavior?.AssociatedObject != null && e.NewValue != null)
            {
                behavior.ScrollToItem(e.NewValue);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
            base.OnDetaching();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject.SelectedItem != null)
            {
                ScrollToItem(AssociatedObject.SelectedItem);
            }
        }

        private void ScrollToItem(object item)
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Dispatcher.InvokeAsync(() =>
                {
                    AssociatedObject.ScrollIntoView(item);

                    // Также обновляем фокус на строке
                    if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                    {
                        row.Focus();
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }

}
