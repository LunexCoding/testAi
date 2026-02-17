using System;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Fox;
using OrderApprovalSystem.ViewModels;
using OrderApprovalSystem.Models;

namespace OrderApprovalSystem.Views
{
    public class RibbonTabBehavior : Behavior<Ribbon>
    {
        public static readonly DependencyProperty CurrentViewModelProperty =
            DependencyProperty.Register(
                nameof(CurrentViewModel),
                typeof(VMBase),
                typeof(RibbonTabBehavior),
                new PropertyMetadata(null, OnCurrentViewModelChanged)
            );

        public VMBase CurrentViewModel
        {
            get => (VMBase)GetValue(CurrentViewModelProperty);
            set => SetValue(CurrentViewModelProperty, value);
        }

        private RibbonTab _dynamicTab;

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateRibbonTab();
        }

        private static void OnCurrentViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTabBehavior)d).UpdateRibbonTab();
        }

        private void UpdateRibbonTab()
        {
            if (AssociatedObject == null)
                return;

            RemoveDynamicTab();

            if (CurrentViewModel == null)
                return;

            _dynamicTab = CreateTabForViewModel(CurrentViewModel);
            if (_dynamicTab == null)
                return;

            // Вставляем вкладку в начало
            AssociatedObject.Items.Insert(0, _dynamicTab);
            _dynamicTab.IsSelected = true;
        }

        private void RemoveDynamicTab()
        {
            if (_dynamicTab == null)
                return;

            AssociatedObject.Items.Remove(_dynamicTab);
            _dynamicTab = null;
        }

        private RibbonTab CreateTabForViewModel(VMBase viewModel)
        {
            switch (viewModel)
            {
                case vmDev dev:
                    return CreateDevTab(dev);
                case vmOrderApproval approval:
                    return CreateApprovalTab(approval);
                case vmApprovalHistory history:
                    return CreateHistoryTab(history);
                default:
                    return null;
            }
        }

        #region Создание вкладок

        private RibbonTab CreateDevTab(vmDev vmDev)
        {
            var tab = new RibbonTab
            {
                Header = "РАЗРАБОТКА",
                DataContext = vmDev
            };

            var toolsGroup = new RibbonGroup { Header = "Инструменты" };
            tab.Items.Add(toolsGroup);

            return tab;
        }

        private RibbonTab CreateApprovalTab(vmOrderApproval viewModel)
        {
            var tab = new RibbonTab
            {
                Header = GetTabHeader(viewModel.Model),
                DataContext = viewModel
            };

            tab.Items.Add(CreateApprovalGroup(viewModel));

            if (viewModel.Model is mOrderManager)
            {
                tab.Items.Add(CreateOrderGroup(viewModel));
            }

            tab.Items.Add(CreateHistoryGroup(viewModel)); // ЭТО УЖЕ БЫЛО
            tab.Items.Add(CreateSearchGroup(viewModel));
            tab.Items.Add(CreateNavigationGroup(viewModel));

            return tab;
        }

        // ДОБАВЛЕН НОВЫЙ МЕТОД
        private RibbonTab CreateHistoryTab(vmApprovalHistory viewModel)
        {
            var tab = new RibbonTab
            {
                Header = "ИСТОРИЯ",
                DataContext = viewModel
            };

            var navigationGroup = new RibbonGroup { Header = "Навигация" };

            var backButton = CreateRibbonButton(
                "Назад",
                viewModel.GoBackCommand, // ICommand из vmApprovalHistory
                Icons.Back,
                "Вернуться",
                "Вернуться к предыдущему окну",
                Brushes.Blue
            );

            navigationGroup.Items.Add(backButton);
            tab.Items.Add(navigationGroup);

            return tab;
        }

        #endregion

        #region Создание групп для вкладки согласования

        private RibbonGroup CreateApprovalGroup(vmOrderApproval viewModel)
        {
            var group = new RibbonGroup { Header = "Согласование" };

            var approveButton = CreateRibbonButton(
                "Согласовать",
                viewModel.pApprovalOrderCommand,
                Icons.Check,
                "Согласовать заказ",
                "Подтвердить согласование текущего заказа",
                Brushes.Green
            );

            var rejectButton = CreateRibbonButton(
                "Не согласовывать",
                viewModel.pRejectOrderCommand,
                Icons.Cross,
                "Отклонить заказ",
                "Отправить заказ на доработку",
                Brushes.Red
            );

            group.Items.Add(approveButton);
            group.Items.Add(rejectButton);

            return group;
        }

        private RibbonGroup CreateOrderGroup(vmOrderApproval viewModel)
        {
            var group = new RibbonGroup { Header = "Заказ" };

            var memoButton = CreateRibbonButton(
                "По СЗ",
                viewModel.pOrderByMemoCommand,
                Icons.Memo,
                "Создать заказ по служебной записке",
                "Создать новый заказ на основе служебной записки"
            );

            group.Items.Add(memoButton);
            return group;
        }

        private RibbonGroup CreateHistoryGroup(vmOrderApproval viewModel)
        {
            var group = new RibbonGroup { Header = "История" };

            var historyButton = CreateRibbonButton(
                "История",
                viewModel.pApprovalHistory,
                Icons.History,
                "История согласования",
                "Просмотреть историю согласования текущего заказа"
            );

            group.Items.Add(historyButton);
            return group;
        }

        private RibbonGroup CreateSearchGroup(vmOrderApproval viewModel)
        {
            var group = new RibbonGroup { Header = "Поиск" };

            var searchBox = new RibbonTextBox
            {
                Label = "Заказ:",
                Width = 200,
                TextBoxWidth = 140,
                Style = (Style)Application.Current.FindResource(typeof(TextBox))
            };

            searchBox.SetBinding(RibbonTextBox.TextProperty, new Binding(nameof(viewModel.SearchText))
            {
                Source = viewModel,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            });

            group.Items.Add(searchBox);
            return group;
        }

        private RibbonGroup CreateNavigationGroup(vmOrderApproval viewModel)
        {
            var group = new RibbonGroup { Header = "Навигация" };

            group.Items.Add(CreateRibbonButton(
                "Предыдущий заказ",
                viewModel.NavigatePreviousGroupCommand,
                Icons.PrevGroup,
                "Предыдущий заказ",
                "Перейти к предыдущему заказу"
            ));

            group.Items.Add(CreateRibbonButton(
                "Следующий заказ",
                viewModel.NavigateNextGroupCommand,
                Icons.NextGroup,
                "Следующий заказ",
                "Перейти к следующему заказу"
            ));

            group.Items.Add(CreateRibbonButton(
                "Предыдущий чертеж",
                viewModel.NavigatePreviousCommand,
                Icons.Prev,
                "Предыдущий чертеж",
                "Перейти к предыдущему чертежу в текущем заказе"
            ));

            group.Items.Add(CreateRibbonButton(
                "Следующий чертеж",
                viewModel.NavigateNextCommand,
                Icons.Next,
                "Следующий чертеж",
                "Перейти к следующему чертежу в текущем заказе"
            ));

            return group;
        }

        #endregion

        #region Вспомогательные методы

        private string GetTabHeader(mBaseOrderApproval model)
        {
            switch (model)
            {
                case mTechnologist _:
                    return "ТЕХНОЛОГ";
                case mOrderManager _:
                    return "МЕНЕДЖЕР ЗАКАЗОВ";
                case mHeadOrderDepartment _:
                    return "НАЧАЛЬНИК ОТДЕЛА";
                default:
                    return "СОГЛАСОВАНИЕ";
            }
        }

        private RibbonButton CreateRibbonButton(
            string label,
            CommandBase command,
            string iconGeometry,
            string toolTipTitle,
            string toolTipDescription,
            Brush iconBrush = null)
        {
            var button = new RibbonButton
            {
                Label = label,
                Command = command,
                LargeImageSource = Icons.CreateImageSource(iconGeometry, iconBrush ?? GetDefaultBrush(), 32, 32),
                SmallImageSource = Icons.CreateImageSource(iconGeometry, iconBrush ?? GetDefaultBrush(), 16, 16),
                ToolTip = CreateToolTip(toolTipTitle, toolTipDescription)
            };

            return button;
        }

        // ЭТОТ МЕТОД УЖЕ БЫЛ, ОН НУЖЕН ДЛЯ ICommand
        private RibbonButton CreateRibbonButton(
            string label,
            System.Windows.Input.ICommand command,
            string iconGeometry,
            string toolTipTitle,
            string toolTipDescription,
            Brush iconBrush = null)
        {
            var button = new RibbonButton
            {
                Label = label,
                Command = command,
                LargeImageSource = Icons.CreateImageSource(iconGeometry, iconBrush ?? GetDefaultBrush(), 32, 32),
                SmallImageSource = Icons.CreateImageSource(iconGeometry, iconBrush ?? GetDefaultBrush(), 16, 16),
                ToolTip = CreateToolTip(toolTipTitle, toolTipDescription)
            };

            return button;
        }

        private Brush GetDefaultBrush()
        {
            return (Brush)Application.Current.Resources["TextBrush"] ?? Brushes.Black;
        }

        private ToolTip CreateToolTip(string title, string description)
        {
            return new ToolTip
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 2)
                        },
                        new TextBlock
                        {
                            Text = description,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 200
                        }
                    }
                }
            };
        }

        #endregion
    }

    #region Иконки

    public static class Icons
    {
        public const string Check = "M9 16.17L4.83 12L3.41 13.41L9 19L21 7L19.59 5.59L9 16.17Z";
        public const string Cross = "M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12L19 6.41Z";
        public const string Memo = "M14,2H6C4.9,2 4,2.9 4,4V20C4,21.1 4.9,22 6,22H18C19.1,22 20,21.1 20,20V8L14,2M18,20H6V4H13V9H18V20M10,15H8V10H10V15M14,15H12V10H14V15";
        public const string PrevGroup = "M18.41,7.41L17,6L11,12L17,18L18.41,16.59L13.83,12L18.41,7.41M12.41,7.41L11,6L5,12L11,18L12.41,16.59L7.83,12L12.41,7.41Z";
        public const string NextGroup = "M5.59,7.41L7,6L13,12L7,18L5.59,16.59L10.17,12L5.59,7.41M11.59,7.41L13,6L19,12L13,18L11.59,16.59L16.17,12L11.59,7.41Z";
        public const string Prev = "M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z";
        public const string Next = "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z";
        public const string History = "M13,3A9,9 0 0,0 4,12H1L4.89,15.89L4.96,16.03L9,12H6A7,7 0 0,1 13,5A7,7 0 0,1 20,12A7,7 0 0,1 13,19C11.07,19 9.32,18.21 8.06,16.94L6.64,18.36C8.27,20 10.5,21 13,21A9,9 0 0,0 22,12A9,9 0 0,0 13,3M12,8V13L16.28,15.54L17,14.33L13.5,12.25V8H12Z";
        public const string Back = "M20,11V13H8L13.5,18.5L12.08,19.92L4.16,12L12.08,4.08L13.5,5.5L8,11H20Z"; // ДОБАВЛЕНО

        public static ImageSource CreateImageSource(string geometryData, Brush brush, double width, double height)
        {
            var geometry = Geometry.Parse(geometryData);
            var drawing = new GeometryDrawing(brush, null, geometry);
            var drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();
            return drawingImage;
        }
    }

    #endregion
}