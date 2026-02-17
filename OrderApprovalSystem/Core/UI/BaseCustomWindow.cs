using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shell;

using Fox.Core.Settings;
using OrderApprovalSystem.Core.UI;


namespace OrderApprovalSystem.Views
{

    public class BaseCustomWindow : Window
    {
        public BaseCustomWindow(bool isChild = false)
        {
            IsChild = isChild;

            // Настройки для кастомного окна
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Инициализация WindowChrome
            WindowChrome chrome = new WindowChrome
            {
                CaptionHeight = 40,
                ResizeBorderThickness = new Thickness(5),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            // Обработчики событий
            SourceInitialized += CustomWindow_SourceInitialized;

            // Подписка на смену темы
            ThemeManager.ThemeManagerService.Instance.ThemeChanged += OnThemeChanged;
        }

        public void SetTitle(string title)
        {
            if (titleTextBlock != null)
            {
                titleTextBlock.Text = title;
            }
            // Также обновляем свойство Title окна
            Title = title;
        }

        public bool IsChild
        {
            get => isChild;
            set
            {
                if (isChild != value)
                {
                    isChild = value;
                }
            }
        }

        protected void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        protected void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected UIElement CreateWindowHeader(string title, bool includeThemeToggle = false)
        {
            Grid grid = new Grid
            {
                Height = 40
            };
            headerGrid = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Заголовок
            TextBlock titleTextBlock = new TextBlock
            {
                Text = title,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            this.titleTextBlock = titleTextBlock;

            WindowChrome.SetIsHitTestVisibleInChrome(titleTextBlock, false);
            Grid.SetColumn(titleTextBlock, 0);

            // Кнопки управления
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            this.buttonsPanel = buttonsPanel;

            WindowChrome.SetIsHitTestVisibleInChrome(buttonsPanel, true);
            Grid.SetColumn(buttonsPanel, includeThemeToggle ? 1 : 2);

            if (includeThemeToggle)
            {
                ToggleButton themeToggle = new ToggleButton
                {
                    Style = (Style)TryFindResource("FoxToggleButton")
                };
                themeToggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDarkTheme") { Mode = BindingMode.TwoWay });
                WindowChrome.SetIsHitTestVisibleInChrome(themeToggle, true);
                buttonsPanel.Children.Add(themeToggle);
            }

            // Кнопки управления окном
            Button minimizeButton = CreateWindowButton("_", MinimizeWindow);
            Button maximizeButton = CreateWindowButton("□", MaximizeWindow);
            Button closeButton = CreateWindowButton("×", CloseWindow);

            buttonsPanel.Children.Add(minimizeButton);
            buttonsPanel.Children.Add(maximizeButton);
            buttonsPanel.Children.Add(closeButton);

            grid.Children.Add(titleTextBlock);
            grid.Children.Add(buttonsPanel);

            return grid;
        }

        protected StatusBar CreateStatusBar(string debugText = "РЕЖИМ ОТЛАДКИ (DEBUG)")
        {
            StatusBar statusBar = new StatusBar();
            this.statusBar = statusBar;

            StatusBarItem statusBarItem = new StatusBarItem();
            TextBlock textBlock = new TextBlock
            {
                Text = debugText,
                FontWeight = FontWeights.Bold
            };

            statusBarItem.Content = textBlock;
            statusBar.Items.Add(statusBarItem);

            // Применяем текущую тему
            UpdateStatusBarColors();

            return statusBar;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Проверяем, что текущее содержимое окна - это Grid
            if (Content is Grid mainGrid)
            {
                // Для дочерних окон добавляем Border вокруг всего Grid
                if (IsChild)
                {
                    // 1. Сначала отсоединяем Grid от окна
                    Content = null;

                    // 2. Создаем новый Border как контейнер
                    var border = new Border
                    {
                        BorderBrush = (Brush)TryFindResource("BorderBrush"),
                        BorderThickness = new Thickness(3),
                        Child = mainGrid // Теперь mainGrid отсоединен, можно использовать
                    };

                    // 3. Устанавливаем Border как новое содержимое
                    Content = border;
                }

                // Перемещаем элементы в Grid
                UIElement header = CreateWindowHeader(Title.ToString(), this is Window);
                Grid.SetRow(header, 0);
                mainGrid.Children.Add(header);

                if (mainGrid.RowDefinitions.Count > 3 && SettingsManager.BuildConfiguration == "Debug")
                {
                    StatusBar statusBar = CreateStatusBar(SettingsManager.BuildInfo);
                    Grid.SetRow(statusBar, 3);
                    mainGrid.Children.Add(statusBar);
                }
            }

            UpdateThemeColors();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Отписываемся от событий при закрытии окна
            ThemeManager.ThemeManagerService.Instance.ThemeChanged -= OnThemeChanged;

            base.OnClosed(e);
        }

        protected void UpdateThemeColors()
        {
            // Обновляем заголовок
            if (headerGrid != null)
            {
                headerGrid.Background = (Brush)TryFindResource("PrimaryBackgroundBrush");
            }

            if (titleTextBlock != null)
            {
                titleTextBlock.Foreground = (Brush)TryFindResource("TextBrush");
            }

            if (buttonsPanel != null)
            {
                foreach (object child in buttonsPanel.Children)
                {
                    if (child is Button button)
                    {
                        button.Foreground = (Brush)TryFindResource("TextBrush");
                        button.Background = Brushes.Transparent;
                    }
                    else if (child is ToggleButton toggleButton)
                    {
                        toggleButton.Foreground = (Brush)TryFindResource("TextBrush");
                    }
                }
            }

            // Обновляем StatusBar
            if (statusBar != null)
            {
                UpdateStatusBarColors();
            }

            // Обновляем фон и передний план окна
            Background = (Brush)TryFindResource("PrimaryBackgroundBrush");
            Foreground = (Brush)TryFindResource("TextBrush");
        }

        private void CustomWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Инициализация для поддержки максимизации
            WindowSizing.WindowInitialized(this);
        }

        private void OnThemeChanged(string obj)
        {
            // Обновляем цвета при смене темы
            UpdateThemeColors();
        }

        private Button CreateWindowButton(string content, RoutedEventHandler clickHandler)
        {
            Button button = new Button
            {
                Content = content,
                Width = 46,
                Height = 30,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2, 0, 0, 0)
            };
            button.Click += clickHandler;
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private void UpdateStatusBarColors()
        {
            if (statusBar != null)
            {
                statusBar.Background = (Brush)TryFindResource("PrimaryBackgroundBrush");
                statusBar.Foreground = (Brush)TryFindResource("TextBrush");

                foreach (object item in statusBar.Items)
                {
                    if (item is StatusBarItem statusBarItem)
                    {
                        statusBarItem.Foreground = (Brush)TryFindResource("TextBrush");

                        if (statusBarItem.Content is TextBlock textBlock)
                        {
                            textBlock.Foreground = (Brush)TryFindResource("TextBrush");
                        }
                    }
                }
            }
        }

        private bool isChild;

        private Grid headerGrid;
        private TextBlock titleTextBlock;
        private StackPanel buttonsPanel;
        private StatusBar statusBar;
    }

}
