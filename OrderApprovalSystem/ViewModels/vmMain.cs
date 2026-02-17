using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Fox;
using Fox.Core.Logging;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Core.Settings;
using OrderApprovalSystem.Data.Entities;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.Views;

namespace OrderApprovalSystem.ViewModels
{
    public class vmMain : VMBase
    {
        public Main View;
        public mMain Model;

        private VMBase _current;
        private Stack<VMBase> _navigationHistory = new Stack<VMBase>();

        /// <summary>
        /// Текущая ViewModel
        /// </summary>
        public VMBase CurrentViewModel
        {
            get => _current;
            set
            {
                _current = value;
                OnPropertyChanged(nameof(CurrentViewModel));
                OnPropertyChanged(nameof(IsHistoryMode));
                OnPropertyChanged(nameof(ShowRegularTabs));
                OnPropertyChanged(nameof(CanGoBack));
            }
        }

        /// <summary>
        /// Режим истории (текущая ViewModel - vmApprovalHistory)
        /// </summary>
        public bool IsHistoryMode => CurrentViewModel is vmApprovalHistory;

        /// <summary>
        /// Показывать обычные вкладки (когда не в режиме истории)
        /// </summary>
        public bool ShowRegularTabs => !IsHistoryMode;

        /// <summary>
        /// Можно ли выполнить возврат
        /// </summary>
        public bool CanGoBack => _navigationHistory.Count > 0;

        /// <summary>
        /// Заголовок окна
        /// </summary>
        public string WindowTitle => "Система согласования заказов";

        #region Свойства видимости меню

        public bool IsBurgerMenuVisible => RoleManager.IsDev;
        public bool IsDevMenuVisible => RoleManager.IsDev;
        public bool IsTechnologistMenuVisible => RoleManager.IsDev && ShowTechnologist;
        public bool IsOrderManagerMenuVisible => RoleManager.IsDev && ShowOrderManager;
        public bool IsHeadOrderDepartmentMenuVisible => RoleManager.IsDev && ShowHead;

        public bool ShowDevMenu => RoleManager.IsDev;
        public bool ShowTechnologist => AccessManager.CanAccess(UserRole.Technologist);
        public bool ShowOrderManager => AccessManager.CanAccess(UserRole.OrderManager);
        public bool ShowHead => AccessManager.CanAccess(UserRole.HeadOrderDepartment);

        #endregion

        #region Команды навигации

        private readonly RelayCommand _switchToDevCommand;
        private readonly RelayCommand _switchToTechnologistCommand;
        private readonly RelayCommand _switchToOrderManagerCommand;
        private readonly RelayCommand _switchToHeadOrderDepartmentCommand;

        public ICommand pSwitchToDevCommand => _switchToDevCommand;
        public ICommand pSwitchToTechnologistCommand => _switchToTechnologistCommand;
        public ICommand pSwitchToOrderManagerCommand => _switchToOrderManagerCommand;
        public ICommand pSwitchToHeadOrderDepartmentCommand => _switchToHeadOrderDepartmentCommand;

        #endregion

        #region Тема оформления

        private bool isDarkTheme;

        public bool IsDarkTheme
        {
            get { return isDarkTheme; }
            set
            {
                if (isDarkTheme != value)
                {
                    AppThemeManager.NextTheme();
                }
            }
        }

        #endregion

        #region Конструктор и инициализация

        public vmMain()
        {
            // Инициализация команд навигации
            _switchToDevCommand = new RelayCommand(
                () => Navigate(UserRole.Dev),
                () => AccessManager.CanAccess(UserRole.Dev)
            );

            _switchToTechnologistCommand = new RelayCommand(
                () => Navigate(UserRole.Technologist),
                () => AccessManager.CanAccess(UserRole.Technologist)
            );

            _switchToOrderManagerCommand = new RelayCommand(
                () => Navigate(UserRole.OrderManager),
                () => AccessManager.CanAccess(UserRole.OrderManager)
            );

            _switchToHeadOrderDepartmentCommand = new RelayCommand(
                () => Navigate(UserRole.HeadOrderDepartment),
                () => AccessManager.CanAccess(UserRole.HeadOrderDepartment)
            );

            RoleManager.RoleChanged += Refresh;
            Navigate(RoleManager.Role);
        }

        public void viewLoaded(object _sender, RoutedEventArgs _routedEventArgs)
        {
            try
            {
                View = (Main)view;
                Model = (mMain)model;

                if (Model != null)
                {
                    Model.PropertyChanged += modelPropertyChangedHandler;
                }

                UpdateThemeProperties();
                ThemeManager.ThemeManagerService.Instance.ThemeChanged += OnThemeChanged;
            }
            catch (System.Exception ex)
            {
                LoggerManager.MainLogger.Error($"Error in vmMain.viewLoaded: {ex.Message}");
            }
        }

        #endregion

        #region Методы навигации

        /// <summary>
        /// Навигация по ролям
        /// </summary>
        public void Navigate(UserRole role)
        {
            if (!AccessManager.CanAccess(role)) return;

            if (RoleManager.IsDev)
                RoleManager.SwitchMask(role);

            // Сохраняем текущий ViewModel в историю, если это не режим истории
            if (CurrentViewModel != null && !(CurrentViewModel is vmApprovalHistory))
            {
                _navigationHistory.Push(CurrentViewModel);
            }

            // Получаем ViewModel из NavigationService
            var newViewModel = NavigationService.Navigate(role);

            // Убеждаемся, что у vmOrderApproval есть модель
            if (newViewModel is vmOrderApproval orderApproval && orderApproval.Model == null)
            {
                switch (role)
                {
                    case UserRole.Technologist:
                        orderApproval.Model = new mTechnologist();
                        break;
                    case UserRole.OrderManager:
                        orderApproval.Model = new mOrderManager();
                        break;
                    case UserRole.HeadOrderDepartment:
                        orderApproval.Model = new mHeadOrderDepartment();
                        break;
                }
            }

            CurrentViewModel = newViewModel;

            // Если навигируем не в историю, очищаем историю при смене роли
            if (!(newViewModel is vmApprovalHistory))
            {
                _navigationHistory.Clear();
            }

            Refresh();
        }

        /// <summary>
        /// Навигация к истории согласования
        /// </summary>
        public void NavigateToHistory(TechnologicalOrder order)
        {
            // Сохраняем текущий ViewModel в историю
            if (CurrentViewModel != null)
            {
                _navigationHistory.Push(CurrentViewModel);
            }

            // Создаем ViewModel истории через NavigationService
            var historyViewModel = NavigationService.NavigateToHistory(order);
            CurrentViewModel = historyViewModel;
        }

        /// <summary>
        /// Возврат к предыдущему ViewModel
        /// </summary>
        public void GoBack()
        {
            if (_navigationHistory.Count > 0)
            {
                CurrentViewModel = _navigationHistory.Pop();
            }
        }

        /// <summary>
        /// Возврат в режим разработчика
        /// </summary>
        public void BackToDev()
        {
            if (!RoleManager.IsDev) return;
            Navigate(UserRole.Dev);
        }

        #endregion

        #region Обработчики событий

        private void Refresh()
        {
            OnPropertyChanged(nameof(ShowDevMenu));
            OnPropertyChanged(nameof(ShowTechnologist));
            OnPropertyChanged(nameof(ShowOrderManager));
            OnPropertyChanged(nameof(ShowHead));
            OnPropertyChanged(nameof(IsBurgerMenuVisible));
            OnPropertyChanged(nameof(IsDevMenuVisible));
            OnPropertyChanged(nameof(IsTechnologistMenuVisible));
            OnPropertyChanged(nameof(IsOrderManagerMenuVisible));
            OnPropertyChanged(nameof(IsHeadOrderDepartmentMenuVisible));
        }

        private void OnThemeChanged(string themeName)
        {
            UpdateThemeProperties();
            AppSettingsManager.Current.Theme = themeName;
            AppSettingsManager.SaveSettings();
        }

        private void UpdateThemeProperties()
        {
            isDarkTheme = ThemeManager.ThemeManagerService.Instance.IsDarkTheme;
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        /// <summary>
        /// Обработчик изменений свойств модели
        /// </summary>
        public void modelPropertyChangedHandler(object _sender, PropertyChangedEventArgs _eventArgs)
        {
            switch (_eventArgs.PropertyName)
            {
                // Если нужно обрабатывать изменения конкретных свойств модели
                default:
                    // Общая обработка
                    break;
            }
        }

        #endregion
    }
}