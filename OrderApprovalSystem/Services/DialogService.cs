using System.Windows;

using ModalWindows.Entities;
using ModalWindows.Services;

using ModalWindows.Views;

using OrderApprovalSystem.Views.Modals;
using OrderApprovalSystem.ViewModels.Modals;
using System;
using OrderApprovalSystem.Data.Entities;
using Fox.DatabaseService.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Fox.Core;
using OrderApprovalSystem.Data;
using System.Linq;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.ViewModels.Modals.Reject;
using OrderApprovalSystem.Validators;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.Core.Roles;

namespace OrderApprovalSystem.Services
{
    public enum MessageDisplayBehavior
    {
        /// <summary>
        /// Показать все типы сообщений (поведение по умолчанию).
        /// </summary>
        ShowAll,
        /// <summary>
        /// Показать только сообщения об ошибках и вопросы, требующие ответа.
        /// </summary>
        ShowFailedAndQuestion,
        /// <summary>
        /// Не показывать никаких сообщений (только логирование).
        /// </summary>
        ShowNone
    }

    public enum ApprovalRoleType
    {
        Technologist,
        HeadOrderDepartment,
        OrderManager
    }

    static public class DialogService
    {
        public delegate bool ApprovalValidation(vmApproveTechnologist viewModel);

        #region Общие методы диалогов

        static public ModalResult ShowMessage(string message, string title = "Сообщение", bool withCheckBox = false)
        {
            ModalResult result = null;
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    result = ModalWindows.Services.ModalDialogService.ShowInfo(message, title, withCheckBox);
                }
            );
            return result;
        }

        /// <summary>
        /// Показ окна с ошибкой.
        /// </summary>
        static public ModalResult ShowError(string message, string title = "Ошибка")
        {
            ModalResult result = null;
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    result = ModalWindows.Services.ModalDialogService.ShowError(message, title);
                }
            );
            return result;
        }

        /// <summary>
        /// Показ окна с вопросом (Да/Нет).
        /// </summary>
        static public ModalResult ShowQuestion(string message, string title = "Подтверждение", bool withCheckBox = false)
        {
            ModalResult result = null;
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    result = ModalWindows.Services.ModalDialogService.ShowConfirm(message, title, withCheckBox);
                }
            );
            return result;
        }

        #endregion

        #region Методы для согласования заказов

        /// <summary>
        /// Показывает модальное окно для согласования заказа технологом
        /// </summary>
        static public ModalResult ShowApproveTechnologist(
            string message,
            string title,
            Func<vmApproveTechnologist, bool> validation = null
        )
        {
            ModalResult result = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                InputModal window = new InputModal();
                vmApproveTechnologist viewModel = new vmApproveTechnologist(message, title);

                if (validation != null)
                {
                    // Передаем логику в VM
                    viewModel.ExternalValidator = (baseVm) =>
                    {
                        vmApproveTechnologist vmApproval = (vmApproveTechnologist)baseVm;
                        bool ok = validation(vmApproval);
                        if (!ok && string.IsNullOrEmpty(vmApproval.ErrorMessage))
                        {
                            vmApproval.ErrorMessage = "Проверьте правильность заполнения полей!";
                        }
                        result = ModalResult.Input(viewModel.InputText, message, false);
                        return ok;
                    };
                }

                window.DataContext = viewModel;

                if (window.ShowDialog() == true)
                {
                    result = ModalResult.Input(viewModel.InputText, message, true);
                    result.Data.Add("ManufacturingTerm", viewModel.ManufacturingTerm);
                    result.Data.Add("Comment", viewModel.Comment);
                }
                else
                {
                    result = ModalResult.Cancel();
                }
            });
            return result;
        }

        /// <summary>
        /// Показывает модальное окно для согласования заказа начальником отдела заказов
        /// </summary>
        static public ModalResult ShowApproveHeadOrder(string message, string title)
        {
            ModalResult result = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                InputModal window = new InputModal();
                vmApproveHeadOrderDepartment viewModel = new vmApproveHeadOrderDepartment(message, title);

                // Валидация не нужна, так как поле необязательное

                window.DataContext = viewModel;
                // Нужно убедиться, что ContentControl в InputModal подхватит ApproveHeadOrder
                // Обычно это делается через DataTemplate в ресурсах приложения

                if (window.ShowDialog() == true)
                {
                    result = ModalResult.Input(viewModel.InputText, message, true);
                    // Возвращаем только комментарий
                    result.Data.Add("Comment", viewModel.Comment);
                }
                else
                {
                    result = ModalResult.Cancel();
                }
            });
            return result;
        }

        /// <summary>
        /// Универсальный метод для показа модального окна согласования
        /// </summary>
        static public ModalResult ShowApprovalDialog(
            string message,
            string title,
            ApprovalRoleType roleType,
            Func<object, bool> validation = null
        )
        {
            ModalResult result = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                InputModal window = new InputModal();
                object viewModel = null;

                switch (roleType)
                {
                    case ApprovalRoleType.Technologist:
                        var techViewModel = new vmApproveTechnologist(message, title);
                        viewModel = techViewModel;

                        if (validation != null)
                        {
                            techViewModel.ExternalValidator = (baseVm) =>
                            {
                                var vmApproval = (vmApproveTechnologist)baseVm;
                                bool ok = validation(vmApproval);
                                if (!ok && string.IsNullOrEmpty(vmApproval.ErrorMessage))
                                {
                                    vmApproval.ErrorMessage = "Проверьте правильность заполнения полей!";
                                }
                                return ok;
                            };
                        }
                        break;

                    case ApprovalRoleType.HeadOrderDepartment:
                        var headViewModel = new vmApproveHeadOrderDepartment(message, title);
                        viewModel = headViewModel;
                        break;

                    case ApprovalRoleType.OrderManager:
                        // Для менеджера заказов используем другую логику
                        result = ShowOrderManagerApproval(message, title);
                        return; // Выходим из лямбды
                }

                if (viewModel == null)
                {
                    result = ModalResult.Cancel();
                    return;
                }

                window.DataContext = viewModel;

                if (window.ShowDialog() == true)
                {
                    result = ModalResult.Input("", message, true);

                    // Заполняем данные в зависимости от типа ViewModel
                    if (viewModel is vmApproveTechnologist techVm)
                    {
                        result.Data.Add("ManufacturingTerm", techVm.ManufacturingTerm);
                        result.Data.Add("Comment", techVm.Comment);
                    }
                    else if (viewModel is vmApproveHeadOrderDepartment headVm)
                    {
                        result.Data.Add("Comment", headVm.Comment);
                    }
                }
                else
                {
                    result = ModalResult.Cancel();
                }
            });

            return result;
        }

        /// <summary>
        /// Метод для согласования заказа менеджером заказов
        /// </summary>
        static private ModalResult ShowOrderManagerApproval(string message, string title)
        {
            // Для менеджера заказов согласование происходит без модального окна
            // Просто возвращаем успешный результат
            return ModalResult.Input("", message, true);
        }

        /// <summary>
        /// Получает модальное окно для согласования на основе типа модели
        /// </summary>
        static public ModalResult GetModalForApproval(
            string message,
            string title,
            object model,
            Func<object, bool> validation = null
        )
        {
            if (model == null)
            {
                return ModalResult.Cancel();
            }

            var modelType = model.GetType();

            if (modelType == typeof(mTechnologist))
            {
                // Используем валидатор для технолога
                Func<vmApproveTechnologist, bool> techValidator = null;
                if (validation != null)
                {
                    techValidator = (vm) => validation(vm);
                }

                return ShowApproveTechnologist(message, title, techValidator);
            }
            else if (modelType == typeof(mHeadOrderDepartment))
            {
                return ShowApproveHeadOrder(message, title);
            }
            else if (modelType == typeof(mOrderManager))
            {
                // Для менеджера заказов согласование происходит без модального окна
                return ModalResult.Input("", message, true);
            }

            return ModalResult.Cancel();
        }

        #endregion

        #region Методы для отклонения заказов

        /// <summary>
        /// Показывает модальное окно для отклонения заказа
        /// </summary>
        public static ModalResult ShowReject(
            string message,
            string title,
            Func<BaseRejectModal, bool> validation = null,
            int? orderApprovalId = null
        )
        {
            ModalResult result = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                InputModal window = new InputModal();
                BaseRejectModal viewModel;

                // Создаем ViewModel в зависимости от роли
                if (RoleManager.IsInRole(UserRole.OrderManager) || RoleManager.IsInRole(UserRole.HeadOrderDepartment))
                {
                    viewModel = new RejectOrderManagerModal(message, title, orderApprovalId);
                }
                else
                {
                    viewModel = new RejectTechnologistModal(message, title);
                }

                // Настраиваем валидаторы
                if (validation != null)
                {
                    // Устанавливаем переданный валидатор
                    viewModel.RejectValidator = validation;
                }
                else
                {
                    // Или стандартный валидатор
                    viewModel.RejectValidator = RejectOrderModalValidator.Validate;
                }

                // Также настраиваем ExternalValidator для совместимости
                viewModel.ExternalValidator = (vm) =>
                {
                    // Просто возвращаем true, так как валидация уже выполняется в ValidateBeforeClose
                    return true;
                };

                window.DataContext = viewModel;

                bool? dialogResult = window.ShowDialog();

                if (dialogResult == true)
                {
                    // Создаем успешный результат
                    result = ModalResult.Input(viewModel.InputText, message, true);
                    result.Data.Add("SubdivisionRecipient", viewModel.SubdivisionRecipient);
                    result.Data.Add("Subdivision", viewModel.Subdivision);
                    result.Data.Add("Comment", viewModel.Comment);
                }
                else
                {
                    result = ModalResult.Cancel();
                }
            });

            return result;
        }

        /// <summary>
        /// Универсальный метод для показа модального окна отклонения
        /// </summary>
        public static ModalResult ShowRejectDialog(
            string message,
            string title,
            object model,
            Func<BaseRejectModal, bool> validation = null,
            int? orderApprovalId = null
        )
        {
            // Для менеджера заказов проверяем, не является ли это заказом по СЗ
            if (model is mOrderManager orderManagerModel)
            {
                var currentItem = orderManagerModel.CurrentItem;
                if (currentItem?.IsByMemo == true)
                {
                    // Нельзя отклонять заказ по СЗ
                    ShowError("Нельзя НЕ согласовывать заказ по СЗ!");
                    return ModalResult.Cancel();
                }
            }

            // Используем существующий метод ShowReject
            return ShowReject(message, title, validation, orderApprovalId);
        }

        #endregion

        #region Обработка результатов

        static public bool HandleResult(Result result, string title = null, MessageDisplayBehavior displayBehavior = MessageDisplayBehavior.ShowAll)
        {
            //ModalWindows.Entities.ModalResult
            // Не показывать сообщения, если выбран режим ShowNone или если в самом результате стоит флаг не показывать.
            if (displayBehavior == MessageDisplayBehavior.ShowNone || !result.ShowMessage)
            {
                // Для Question важно вернуть правильный результат, даже если окно не показывалось.
                // В этом случае, если ответа пользователя нет, логично считать это отказом.
                if (result.Status == ResultStatus.Question)
                {
                    return false;
                }
                return result.Status == ResultStatus.Success;
            }

            switch (result.Status)
            {
                case ResultStatus.Success:
                    // Показываем сообщение об успехе, только если выбран режим ShowAll
                    if (displayBehavior == MessageDisplayBehavior.ShowAll)
                    {
                        ShowMessage(result.Message, title);
                    }
                    return true;

                case ResultStatus.Question:
                    // Вопросы показываем всегда, кроме режима ShowNone (уже обработан выше)
                    bool userAnswer = ShowQuestion(result.Message, title).IsConfirmed;
                    return userAnswer;

                case ResultStatus.Failed:
                    // Ошибки показываем всегда, кроме режима ShowNone (уже обработан выше)
                    ShowError(result.Message, title);
                    return false;

                default:
                    // Неизвестный статус тоже считаем ошибкой и показываем
                    ShowError("Неизвестный статус операции", title);
                    return false;
            }
        }

        #endregion
    }
}