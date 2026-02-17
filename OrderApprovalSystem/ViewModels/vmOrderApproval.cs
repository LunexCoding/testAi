using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Fox;
using Fox.Core;
using Fox.Core.ErrorHelper;
using Fox.Core.Logging;
using Fox.DatabaseService;
using Fox.DatabaseService.Entities;
using ModalWindows.Entities;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.Services;
using OrderApprovalSystem.Validators;
using OrderApprovalSystem.ViewModels.Modals;
using OrderApprovalSystem.Views;

namespace OrderApprovalSystem.ViewModels
{
    public interface IOrderApprovalViewModel
    {
        bool CanEditOrderFields { get; }
        bool CanEditCheckBoxes { get; }
        bool ShowMemoFieldsOrView { get; }
        bool ShowReadOnlyFields { get; }
    }


    public class vmOrderApproval : VMBase, IOrderApprovalViewModel
    {
        public OrderApprovalView View;
        private mBaseOrderApproval _model;

        public mBaseOrderApproval Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged(nameof(Model));

                // Устанавливаем model для базового класса VMBase
                model = value;
            }
        }

        #region Инициализация

        public vmOrderApproval()
        {
        }

        public void viewLoaded(object _sender, RoutedEventArgs _routedEventArgs)
        {
            try
            {
                View = (OrderApprovalView)view;

                // Проверяем, что Model не null
                if (Model == null)
                {
                    // Если Model null, пытаемся получить её из model
                    if (model is mBaseOrderApproval baseModel)
                    {
                        Model = baseModel;
                    }
                    else
                    {
                        // Создаем модель по умолчанию в зависимости от роли
                        Model = CreateModelForCurrentRole();
                    }
                }

                if (Model != null)
                {
                    Model.PropertyChanged += modelPropertyChangedHandler;
                }

                // Обновляем свойства интерфейса
                OnPropertyChanged(nameof(CanEditOrderFields));
                OnPropertyChanged(nameof(CanEditCheckBoxes));
                OnPropertyChanged(nameof(ShowMemoFieldsOrView));
                OnPropertyChanged(nameof(ShowReadOnlyFields));
                OnPropertyChanged(nameof(CurrentUserInfo));
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Error in viewLoaded: {ex.Message}");
            }
        }

        private mBaseOrderApproval CreateModelForCurrentRole()
        {
            switch (RoleManager.Role)
            {
                case UserRole.Technologist:
                    return new mTechnologist();
                case UserRole.OrderManager:
                    return new mOrderManager();
                case UserRole.HeadOrderDepartment:
                    return new mHeadOrderDepartment();
                default:
                    return new mBaseOrderApproval();
            }
        }

        public void modelPropertyChangedHandler(object _sender, PropertyChangedEventArgs _eventArgs)
        {
            if (_eventArgs.PropertyName == nameof(Model.CurrentGroup) && View?.dgReport != null)
            {
                ScrollToCurrentGroup();
            }
        }

        #endregion

        /// <summary>
        /// Может ли пользователь редактировать поля заказа (только OrderManager)
        /// </summary>
        public UserRole CurrentRole => RoleManager.Role;

        // Вычисляемые свойства на основе роли
        public bool CanEditOrderFields => CurrentRole == UserRole.OrderManager;
        public bool CanEditCheckBoxes => CurrentRole == UserRole.Technologist;
        public bool ShowMemoFieldsOrView => CurrentRole == UserRole.OrderManager ||
                                            CurrentRole == UserRole.HeadOrderDepartment;
        public bool ShowReadOnlyFields => CurrentRole == UserRole.HeadOrderDepartment;

        // Добавить информацию о текущем пользователе
        public string CurrentUserInfo => $"{RoleManager.CurrentUser?.Name} - {RoleManager.DisplayRoleName}";

        #region Команды

        public CommandBase NavigatePreviousCommand => new CommandBase(() => NavigatePrevious());
        public CommandBase NavigateNextCommand => new CommandBase(() => NavigateNext());
        public CommandBase NavigatePreviousGroupCommand => new CommandBase(() => NavigatePreviousGroup());
        public CommandBase NavigateNextGroupCommand => new CommandBase(() => NavigateNextGroup());
        public CommandBase pNavigateToDevCommand => new CommandBase(() => BackToDev());
        public CommandBase pApprovalOrderCommand => new CommandBase(() => ApprovalOrder());
        public CommandBase pRejectOrderCommand => new CommandBase(() => RejectOrder());
        public CommandBase pOrderByMemoCommand => new CommandBase(() => CreateOrderByMemo());
        public CommandBase pApprovalHistory => new CommandBase(() => HistoryByOrder());

        #endregion

        #region Свойства

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    PerformSearch();
                }
            }
        }

        #endregion

        #region Методы навигации

        private void PerformSearch()
        {
            if (Model == null) return;
            Model.FindAndNavigate(SearchText);
        }

        private void NavigatePrevious()
        {
            if (Model?.HasPrevious == true)
            {
                Model.CurrentIndex--;
                Model.CurrentItem = Model.CurrentGroup.Items[Model.CurrentIndex];
            }
        }

        private void NavigateNext()
        {
            if (Model?.HasNext == true)
            {
                Model.CurrentIndex++;
                Model.CurrentItem = Model.CurrentGroup.Items[Model.CurrentIndex];
            }
        }

        private void NavigatePreviousGroup()
        {
            if (Model?.HasPreviousGroup == true)
            {
                Model.NavigatePreviousGroup();
            }
        }

        private void NavigateNextGroup()
        {
            if (Model?.HasNextGroup == true)
            {
                Model.NavigateNextGroup();
            }
        }

        public void BackToDev()
        {
            if (!RoleManager.IsDev) return;

            var main = (vmMain)App.Current.MainWindow.DataContext;
            main.Navigate(UserRole.Dev);
        }

        private void ScrollToCurrentGroup()
        {
            if (View?.dgReport != null && Model?.CurrentGroup != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        View.dgReport.ScrollIntoView(Model.CurrentGroup);
                        View.dgReport.Focus();
                    }
                    catch (Exception ex)
                    {
                        LoggerManager.MainLogger.Error($"Error scrolling to current group: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion

        #region Методы операций

        private void ApprovalOrder()
        {
            string operationName = "Согласование заказа";

            if (RoleManager.IsInRole(UserRole.OrderManager))
            {
                if (Model is mOrderManager orderManager)
                {
                    ApproveAsOrderManager(orderManager, operationName);
                }
                else
                {
                    DialogService.ShowError("Ошибка приведения типа модели", operationName);
                }
                return;
            }


            // Используем универсальный метод из DialogService
            ModalResult userAnswer = DialogService.GetModalForApproval(
                $"Согласование заказа: {Model.CurrentItem.OrderNumber}",
                "Заполните данные",
                Model,
                vm => GetValidationForModel(vm)
            );

            if (userAnswer.IsDismissed || userAnswer.IsCancelled)
            {
                DialogService.HandleResult(Result.Success("Отменено пользователем").HideMessage(), operationName);
                return;
            }

            if (userAnswer.IsValidationFailed)
            {
                DialogService.HandleResult(Result.Failed("Не пройдена валидация"), operationName);
                return;
            }

            ExecuteTransaction(() =>
            {
                var result = Model.ApproveOrder(userAnswer.Data);
                if (!result.IsSuccess)
                {
                    throw new OperationCanceledException(result.Message);
                }
                return result;
            },
            operationName,
            userAnswer.Data);
        }

        private bool GetValidationForModel(object vm)
        {
            if (Model is mTechnologist && vm is vmApproveTechnologist techVm)
            {
                return ApproveTechnologistModalValidator.Validate(techVm);
            }
            return true; // Для других ролей валидация не требуется
        }

        private void ApproveAsOrderManager(mOrderManager orderManager, string operationName)
        {
            if (Model.CurrentGroup.IsByMemo)
            {
                operationName = "Согласование заказа по СЗ";
            }

            ExecuteTransaction(() =>
            {
                var result = orderManager.ApproveOrder();
                if (!result.IsSuccess)
                {
                    throw new OperationCanceledException(result.Message);
                }
                return result;
            },
            operationName,
            null);
        }

        private void RejectOrder()
        {
            string operationName = "НЕ согласование заказа";

            // Используем универсальный метод из DialogService
            ModalResult userAnswer = DialogService.ShowRejectDialog(
                "Заполните данные",
                $"НЕ согласовывать заказ: {Model.CurrentItem.OrderNumber}",
                Model,
                vm => RejectOrderModalValidator.Validate(vm),
                Model.CurrentItem?.OrderApprovalID
            );

            if (userAnswer.IsDismissed || userAnswer.IsCancelled)
            {
                DialogService.HandleResult(Result.Success("Отменено пользователем").HideMessage(), operationName);
                return;
            }

            if (userAnswer.IsValidationFailed)
            {
                DialogService.HandleResult(Result.Failed("Не пройдена валидация"), operationName);
                return;
            }

            ExecuteTransaction(() =>
            {
                var result = Model.RejectOrder(userAnswer.Data);
                if (!result.IsSuccess)
                {
                    throw new OperationCanceledException(result.Message);
                }
                return result;
            },
            operationName,
            userAnswer.Data);
        }

        private void CreateOrderByMemo()
        {
            if (Model is mOrderManager orderManager)
            {
                orderManager.CreateOrderByMemo();
            }
        }

        private void HistoryByOrder()
        {
            if (Model?.CurrentItem?.OrderApprovalID == null)
            {
                DialogService.ShowError("Не выбран заказ для просмотра истории", "История согласования");
                return;
            }

            // Получаем главное окно и переключаем контекст
            if (App.Current.MainWindow.DataContext is vmMain main)
            {
                main.NavigateToHistory(Model.CurrentItem);
            }
        }

        #endregion

        #region Вспомогательные методы

        private void ExecuteTransaction(Func<Result> businessLogic, string operationName, object data)
        {
            Result result = null;

            TransactionHelper.ExecuteWithTransaction(
                businessLogic: () =>
                {
                    result = businessLogic();
                },
                errorHandler: errorContext =>
                {
                    errorContext.ErrorDetail["Order"] = Model.CurrentGroup.Zak1;
                    errorContext.ErrorDetail["Draft"] = Model.CurrentItem.EquipmentDraft;
                    if (data != null)
                    {
                        errorContext.ErrorDetail["ApprovalData"] = data.ToString();
                    }

                    if (errorContext.Exception is OperationCanceledException)
                    {
                        LoggerManager.MainLogger.Error($"Операция - <{operationName}> отменена: {errorContext.Exception.Message}");
                        LoggerManager.MainLogger.Debug(
                            ErrorHelper.FormatErrorMessage(
                                errorContext.Exception,
                                $"vmOrderApproval.{operationName}",
                                errorContext.ErrorDetail
                            )
                        );
                        DialogService.ShowError(errorContext.Exception.Message, operationName);
                    }
                    else
                    {
                        LoggerManager.MainLogger.Error($"Операция - <{operationName}> прервана ошибкой: {errorContext.Exception.Message}");
                        LoggerManager.MainLogger.Error(
                            ErrorHelper.FormatErrorMessage(
                                errorContext.Exception,
                                $"vmOrderApproval.{operationName}",
                                errorContext.ErrorDetail
                            )
                        );

                        if (errorContext.ShouldLogToErrorHelper)
                        {
                            ErrorHelper.SendError(
                                errorContext.Exception,
                                $"vmOrderApproval.{operationName}",
                                errorContext.ErrorDetail
                            );
                        }

                        DialogService.ShowError("Критическая ошибка сохранения", operationName);
                    }
                },
                operationName: operationName,
                model: Model,
                isTransactionOwner: !ServiceLocator.DatabaseService.HasActiveTransaction
            );

            if (result != null)
            {
                DialogService.HandleResult(result, $"{operationName}: {Model.CurrentItem.OrderNumber}");
            }
        }

        #endregion
    }
}