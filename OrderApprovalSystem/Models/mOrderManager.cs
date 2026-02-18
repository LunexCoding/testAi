using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Fox.Core.Logging;
using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Data.Entities;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Validators;
using OrderApprovalSystem.Core.Roles;

namespace OrderApprovalSystem.Models
{
    public class mOrderManager : mBaseOrderApproval
    {
        public mOrderManager()
        {
        }

        #region Основные операции

        public Result ApproveOrder()
        {
            LoggerManager.MainLogger.Debug($"ApproveOrder in mOrderManager for {CurrentItem?.OrderNumber}");

            Result validationResult = CreateOrderByMemoValidator.Validate(CurrentItem);
            if (validationResult.IsFailed)
            {
                return validationResult;
            }

            if (CurrentGroup.IsByMemo)
            {
                return ApproveOrderByMemo();
            }

            return ApproveTechnologicalOrder();
        }

        public override Result RejectOrder(Dictionary<string, object> data)
        {
            try
            {
                string comment = data.ContainsKey("Comment") ? data["Comment"].ToString() : "";

                // Находим запись, которая сейчас "висит" на текущем пользователе
                var currentActiveStep = db.mGetList<OrderApprovalHistory>(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientName == RoleManager.CurrentUser.Name &&
                    h.CompletionDate == null).Data
                    .OrderByDescending(h => h.ReceiptDate)
                    .FirstOrDefault();

                if (currentActiveStep != null)
                {
                    currentActiveStep.CompletionDate = DateTime.Now;
                    currentActiveStep.Result = "Не согласовано";
                    currentActiveStep.Status = "Выполнено";
                    db.mUpdate(currentActiveStep);
                }

                // 3. Получаем данные из параметров
                string recipientName = data.ContainsKey("SubdivisionRecipient")
                    ? (string)data["SubdivisionRecipient"]
                    : null;

                // 4. Определяем ПОЛУЧАТЕЛЯ для возврата
                (string recipientRole, string recipientName_resolved) = GetRecipientForRejection(recipientName);

                // 6. Рассчитываем срок
                int workingDaysCount = GetWorkingDaysCount(CurrentItem.OrderApprovalID);
                if (workingDaysCount <= 0)
                {
                    workingDaysCount = SelectedTerm > 0 ? SelectedTerm : 1; // Значение по умолчанию
                    LoggerManager.MainLogger.Warn($"Не найден срок, используется значение по умолчанию: {workingDaysCount}");
                }

                DateTime deadlineDate = CalculateDeadlineDate(DateTime.Today, workingDaysCount);

                // Создаем новую запись, указывая ID текущей как ParentID
                OrderApprovalHistory nextStepRecord = CreateRejectionRecord(
                    recipientRole,
                    recipientName,
                    comment,
                    deadlineDate,
                    currentActiveStep?.ID); // Вот здесь создается вложенность

                db.mAdd(nextStepRecord);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failed(ex.Message);
            }
        }

        public override Result CreateOrderByMemo()
        {
            try
            {
                TechnologistGroup newGroup = new TechnologistGroup
                {
                    Zak1 = $"СЗ-{DateTime.Now:yyyyMMdd-HHmmss}",
                    IsByMemo = true,
                    Items = new ObservableCollection<TechnologicalOrder>
                    {
                        new TechnologicalOrder
                        {
                            IsByMemo = true,
                            MemoNumber = $"СЗ-{DateTime.Now:yyyyMMdd}",
                            OpenAtByMemo = DateTime.Now
                        }
                    }
                };

                GroupedData.Add(newGroup);
                CurrentGroup = newGroup;

                LoggerManager.MainLogger.Info($"Создана новая служебная записка: {newGroup.Zak1}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при создании заказа по СЗ: {ex.Message}", ex);
                return Result.Failed($"Ошибка при создании заказа: {ex.Message}");
            }
        }

        #endregion

        #region Приватные методы согласования

        /// <summary>
        /// Согласование заказа по служебной записке
        /// </summary>
        private Result ApproveOrderByMemo()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentItem.MemoNumber))
                {
                    CurrentGroup.Zak1 = CurrentItem.MemoNumber;
                }

                Result validationResult = CreateOrderByMemoValidator.Validate(CurrentItem);
                if (validationResult.IsFailed)
                {
                    return validationResult;
                }

                // 1. Создаем новый заказ
                var newOrderApproval = new OrderApproval
                {
                    IsByMemo = CurrentGroup.IsByMemo,
                    MemoNumber = CurrentItem.MemoNumber,
                    MemoAuthor = CurrentItem.MemoAuthor,
                    Order = CurrentItem.OrderByMemo,
                    Number = CurrentItem.NumberByMemo,
                    OrderName = CurrentItem.OrderName,
                    CoreOrder = CurrentItem.CoreOrderByMemo,
                    CoreNumber = CurrentItem.CoreNumberByMemo,
                    OpenAtByMemo = CurrentItem.OpenAtByMemo,
                    NomenclatureGroupID = CurrentItem.NomenclatureGroup?.ID,
                    EquipmentTypeID = CurrentItem.EquipmentType?.ID,
                    DraftByMemo = CurrentItem.DraftByMemo,
                    DraftNameByMemo = CurrentItem.DraftNameByMemo,
                    Balance = CurrentItem.Balance,
                    WorkshopByMemo = CurrentItem.WorkshopByMemo,
                    EquipmentRequiredQuantityByMemo = CurrentItem.EquipmentRequiredQuantityByMemo
                };

                Result addResult = db.mAdd(newOrderApproval);
                if (addResult.IsFailed)
                {
                    return Result.Failed("Не удалось создать заказ по служебной записке");
                }

                db.mSaveChanges();

                // 2. Получаем созданную запись
                OrderApproval addedRecord = db.mGetQuery<OrderApproval>()
                    .OrderByDescending(record => record.ID)
                    .FirstOrDefault();

                if (addedRecord == null)
                {
                    return Result.Failed("Не удалось получить созданный заказ");
                }

                // 3. Создаем запись в истории
                var historyRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = addedRecord.ID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = DateTime.Now,
                    Term = DateTime.Now.Date.AddDays(1),
                    RecipientRole = "Технолог",
                    RecipientName = "Рагульский",
                    SenderRole = RoleManager.DisplayRoleName,
                    SenderName = RoleManager.CurrentUser.Name,
                    Status = "В работе",
                    Result = null,
                    IsRework = false
                };

                addResult = db.mAdd(historyRecord);
                if (addResult.IsFailed)
                {
                    return Result.Failed("Не удалось создать запись в истории согласования");
                }

                db.mSaveChanges();

                LoggerManager.MainLogger.Info($"Создан новый заказ по СЗ: {addedRecord.ID}, передан технологу");
                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в ApproveOrderByMemo: {ex.Message}", ex);
                return Result.Failed($"Ошибка при создании заказа: {ex.Message}");
            }
        }

        /// <summary>
        /// Согласование технологического заказа
        /// </summary>
        private Result ApproveTechnologicalOrder()
        {
            try
            {
                // 1. Обновляем данные заказа
                OrderApproval thisRecord = db.mGetSingle<OrderApproval>(
                    record => record.ID == CurrentItem.OrderApprovalID
                ).Data;

                if (thisRecord == null)
                {
                    return Result.Failed("Не найден заказ для согласования!");
                }

                thisRecord.Order = CurrentItem.OrderByMemo;
                thisRecord.Number = CurrentItem.NumberByMemo;
                thisRecord.OrderName = CurrentItem.OrderName;
                thisRecord.CoreOrder = CurrentItem.CoreOrderByMemo;
                thisRecord.CoreNumber = CurrentItem.CoreNumberByMemo;
                thisRecord.OpenAtByMemo = CurrentItem.OpenAtByMemo;
                thisRecord.NomenclatureGroupID = CurrentItem.NomenclatureGroup?.ID;
                thisRecord.EquipmentTypeID = CurrentItem.EquipmentType?.ID;
                thisRecord.DraftByMemo = CurrentItem.DraftByMemo;
                thisRecord.DraftNameByMemo = CurrentItem.DraftNameByMemo;
                thisRecord.Balance = CurrentItem.Balance;
                thisRecord.WorkshopByMemo = CurrentItem.WorkshopByMemo;
                thisRecord.EquipmentRequiredQuantityByMemo = CurrentItem.EquipmentRequiredQuantityByMemo;

                Result updateResult = db.mUpdate(thisRecord);
                if (updateResult.IsFailed)
                {
                    return Result.Failed("Не удалось обновить данные заказа");
                }

                // 2. Завершаем текущий шаг согласования
                OrderApprovalHistory thisStep = FindCurrentStepRecord();

                if (thisStep == null)
                {
                    return Result.Failed("Не найдена текущая запись согласования!");
                }

                thisStep.CompletionDate = DateTime.Now;
                thisStep.Status = "Выполнено";
                thisStep.Result = "Согласовано";
                // IsRework не меняем - это завершенная запись

                updateResult = db.mUpdate(thisStep);
                if (updateResult.IsFailed)
                {
                    return Result.Failed("Не удалось обновить запись согласования");
                }

                // 3. Определяем следующего получателя с учетом признака доработки
                int workingDaysCount = SelectedTerm > 0 ? SelectedTerm : GetWorkingDaysCount(CurrentItem.OrderApprovalID);
                DateTime deadlineDate = CalculateDeadlineDate(thisStep.ReceiptDate, workingDaysCount);

                var (nextRole, nextName) = GetNextRecipientForManager();

                // Определяем ParentID для нового шага
                int? nextParentID;
                if (thisStep.IsRework)
                {
                    // Если текущий шаг - доработка (IsRework=true), то при возврате:
                    // Ищем запись, которая первой отклонила и отправила на доработку к получателю nextName
                    // Это будет родительская запись для нового шага
                    nextParentID = FindOriginalRejectingRecord(thisStep, nextName);
                    // Если не нашли отклоняющую запись, используем ParentID текущей записи (остаёмся на том же уровне)
                    if (!nextParentID.HasValue)
                    {
                        nextParentID = thisStep.ParentID;
                    }
                }
                else
                {
                    // Обычное согласование
                    // Проверяем, находимся ли мы в цикле доработки (есть ParentID)
                    // И возвращаемся ли к тому, кто отклонил
                    if (thisStep.ParentID.HasValue)
                    {
                        // Ищем, есть ли выше в цепочке запись с RecipientName = nextName и Result = "Не согласовано"
                        var rejectingRecord = FindOriginalRejectingRecord(thisStep, nextName);
                        if (rejectingRecord.HasValue)
                        {
                            // Возвращаемся к отклонившему - остаёмся в его цикле
                            nextParentID = rejectingRecord;
                        }
                        else
                        {
                            // Не возвращаемся к отклонившему - выходим на корневой уровень
                            nextParentID = null;
                        }
                    }
                    else
                    {
                        // Уже на корневом уровне - остаёмся там
                        nextParentID = null;
                    }
                }

                // 4. Создаем следующий шаг согласования
                OrderApprovalHistory nextStep = new OrderApprovalHistory
                {
                    OrderApprovalID = thisRecord.ID,
                    ParentID = nextParentID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = null,
                    Term = deadlineDate,
                    RecipientRole = nextRole,
                    RecipientName = nextName,
                    SenderRole = RoleManager.DisplayRoleName,
                    SenderName = RoleManager.CurrentUser.Name,
                    Status = "В работе",
                    Result = null,
                    IsRework = false // Новая запись - не доработка
                };

                Result addResult = db.mAdd(nextStep);
                if (addResult.IsFailed)
                {
                    return Result.Failed("Не удалось создать запись следующего согласования");
                }

                db.mSaveChanges();

                // Если текущий шаг был доработкой, обновляем его ParentID, чтобы он стал дочерним элементом нового шага
                if (thisStep.IsRework)
                {
                    thisStep.ParentID = nextStep.ID;
                    updateResult = db.mUpdate(thisStep);
                    if (updateResult.IsFailed)
                    {
                        LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для записи доработки: {updateResult.Message}");
                    }
                }

                LoggerManager.MainLogger.Info(
                    $"Заказ {CurrentItem.OrderNumber} согласован менеджером. " +
                    $"Передан: {nextRole} - {nextName}");

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в ApproveTechnologicalOrder: {ex.Message}", ex);
                return Result.Failed($"Ошибка при согласовании: {ex.Message}");
            }
        }

        #endregion

        #region Логика определения получателей

        /// <summary>
        /// Определяет следующего получателя при СОГЛАСОВАНИИ заказа
        /// </summary>
        private (string Role, string Name) GetNextRecipientForManager()
        {
            // 1. ПРОВЕРЯЕМ ПРИЗНАК ДОРАБОТКИ - если он есть, пропускаем дефолтного получателя
            if (IsCurrentStepRework())
            {
                var (senderRole, senderName) = GetReworkSender();
                if (!string.IsNullOrEmpty(senderRole) && !string.IsNullOrEmpty(senderName))
                {
                    LoggerManager.MainLogger.Debug(
                        $"Обнаружен признак доработки. Пропускаем дефолтного получателя. " +
                        $"Возвращаем отправителю: {senderRole} - {senderName}");
                    return (senderRole, senderName);
                }
            }

            // 2. Проверяем, является ли текущее согласование ПОВТОРНЫМ ПОСЛЕ ВОЗВРАТА (для обратной совместимости)
            if (IsReapprovalAfterRejection())
            {
                var lastRejection = GetLastRejectionForRecipient();
                if (lastRejection != null)
                {
                    LoggerManager.MainLogger.Debug(
                        $"Повторное согласование после возврата. " +
                        $"Возвращаем к: {lastRejection.SenderRole} - {lastRejection.SenderName}");
                    return (lastRejection.SenderRole, lastRejection.SenderName);
                }
            }

            // 3. Стандартный маршрут
            LoggerManager.MainLogger.Debug("Стандартный маршрут: кто-то после менеджера");
            return ("кто-то после менеджера", "кто-то после менеджера");
        }

        /// <summary>
        /// Определяет получателя при ОТКЛОНЕНИИ заказа
        /// </summary>
        private (string Role, string Name) GetRecipientForRejection(string selectedRecipientName)
        {
            // 1. Если пользователь явно выбрал конкретного получателя - используем его
            if (!string.IsNullOrEmpty(selectedRecipientName))
            {
                string role = FindUserRole(selectedRecipientName);
                if (!string.IsNullOrEmpty(role))
                {
                    LoggerManager.MainLogger.Debug($"Выбран конкретный получатель: {role} - {selectedRecipientName}");
                    return (role, selectedRecipientName);
                }
            }

            // 2. По умолчанию: возвращаем тому, от кого пришел заказ
            var previousStep = db.mGetQuery<OrderApprovalHistory>()
                .Where(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientRole == RoleManager.DisplayRoleName &&
                    h.RecipientName == RoleManager.CurrentUser.Name &&
                    h.Result == null)
                .OrderByDescending(h => h.ID)
                .FirstOrDefault();

            if (previousStep != null)
            {
                LoggerManager.MainLogger.Debug(
                    $"Возврат отправителю: {previousStep.SenderRole} - {previousStep.SenderName}");
                return (previousStep.SenderRole, previousStep.SenderName);
            }
            return ("Кто-то после менеджера", "Кто-то после менеджера");
        }

        #endregion

        #region Переопределение базовых методов

        /// <summary>
        /// Стандартный получатель по умолчанию
        /// </summary>
        protected override (string Role, string Name) GetDefaultNextRecipient()
        {
            return ("Кто-то после менеджера", "Кто-то после менеджера");
        }

        #endregion
    }
}