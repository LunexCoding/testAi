using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fox;
using Fox.Core.Logging;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Data.Entities;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.Views;

namespace OrderApprovalSystem.ViewModels
{
    public class vmApprovalHistory : VMBase
    {
        #region Поля и свойства

        public ApprovalHistory View;
        
        private ICommand _goBackCommand;
        private ICommand _refreshCommand;

        public mApprovalHistory Model { get; }

        public void viewLoaded(object sender, RoutedEventArgs e)
        {

            OnPropertyChanged(nameof(WindowTitle));

            Model.LoadHistory();
        }

        /// <summary>
        /// Заголовок окна/вкладки
        /// </summary>
        public string WindowTitle => "История согласования";

        /// <summary>
        /// Команда возврата в предыдущее представление
        /// </summary>
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand == null)
                {
                    _goBackCommand = new RelayCommand(GoBack, CanGoBack);
                }
                return _goBackCommand;
            }
        }

        /// <summary>
        /// Команда обновления истории
        /// </summary>
        public ICommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new RelayCommand(RefreshHistory);
                }
                return _refreshCommand;
            }
        }

        

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор с ID заказа
        /// </summary>
        /// <param name="orderApprovalID">ID заказа для отображения истории</param>
        public vmApprovalHistory(TechnologicalOrder order)
        {
            View = new ApprovalHistory();
            Model = new mApprovalHistory(order);

        }

        #endregion

        #region Методы загрузки

        
        /// <summary>
        /// Обработчик изменений свойств модели
        /// </summary>
        private void modelPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
           
        }

        #endregion

        #region Методы навигации

        /// <summary>
        /// Возврат к предыдущему представлению
        /// </summary>
        private void GoBack()
        {
            try
            {
                LoggerManager.MainLogger.Debug("Выполняется возврат из истории согласования");

                if (Application.Current?.MainWindow?.DataContext is vmMain main)
                {
                    main.GoBack();
                }
                else
                {
                    LoggerManager.MainLogger.Warn("Не удалось получить главную ViewModel для возврата");

                    // Запасной вариант - навигация через NavigationService
                    if (Application.Current?.MainWindow?.DataContext is vmMain mainWindow)
                    {
                        mainWindow.Navigate(UserRole.Technologist); // Или другая роль по умолчанию
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при возврате из истории: {ex.Message}");

                MessageBox.Show(
                    "Не удалось вернуться к предыдущему окну",
                    "Ошибка навигации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверка возможности возврата
        /// </summary>
        private bool CanGoBack()
        {
            try
            {
                if (Application.Current?.MainWindow?.DataContext is vmMain main)
                {
                    return main.CanGoBack;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Дополнительные методы

        /// <summary>
        /// Обновить данные истории
        /// </summary>
        private void RefreshHistory()
        {
            try
            {
                if (Model != null)
                {
                    Model.LoadHistory();
                }
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при обновлении истории: {ex.Message}");
            }
        }

        #endregion
    }
}