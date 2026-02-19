using System;
using System.Collections.Generic;
using System.Linq;
using Fox.Core.Logging;
using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Data;

namespace OrderApprovalSystem.Models
{
    public class mTechnologist : mBaseOrderApproval
    {
        public mTechnologist()
        {
        }

        public override Result ApproveOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug($"ApproveOrder for {CurrentItem.OrderNumber}");

            try
            {
                DateTime manufacturingTerm = (DateTime)data["ManufacturingTerm"];
                string comment = (string)data["Comment"];

                int workingDaysCount = GetWorkingDaysCount(CurrentItem.OrderApprovalID);
                DateTime deadlineDate = CalculateDeadlineDate(manufacturingTerm, workingDaysCount);

                CurrentItem.ManufacturingTerm = manufacturingTerm;
                OrderApproval currentOrderApprovalRecord = FindCurrentOrderApproval(CurrentItem.OrderApprovalID);
                currentOrderApprovalRecord.ManufacturingTerm = manufacturingTerm;
                db.mUpdate(currentOrderApprovalRecord);

                OrderApprovalHistory thisStepRecord = FindCurrentStepRecord();

                if (thisStepRecord == null)
                {
                    return Result.Failed("Не найдена текущая запись согласования!");
                }

                thisStepRecord.CompletionDate = DateTime.Now;
                thisStepRecord.Status = "Выполнено";
                thisStepRecord.Result = "Согласовано";
                thisStepRecord.Comment = comment;

                Result status = db.mUpdate(thisStepRecord);
                if (status.IsFailed)
                {
                    return Result.Failed("Не удалось обновить запись текущего согласования в БД!");
                }

                // ОПРЕДЕЛЯЕМ СЛЕДУЮЩЕГО ПОЛУЧАТЕЛЯ С УЧЕТОМ ПРИЗНАКА ДОРАБОТКИ
                (string nextRole, string nextName) = GetNextRecipientWithReworkCheck();

                // Определяем ParentID для нового шага
                int? nextParentID;
                if (thisStepRecord.IsRework)
                {
                    // Если текущий шаг - доработка (IsRework=true), то при возврате:
                    // Ищем запись, которая первой отклонила и отправила на доработку к получателю nextName
                    // Это будет родительская запись для нового шага
                    nextParentID = FindOriginalRejectingRecord(thisStepRecord, nextName);
                    // Если не нашли отклоняющую запись, используем ParentID текущей записи (остаёмся на том же уровне)
                    if (!nextParentID.HasValue)
                    {
                        nextParentID = thisStepRecord.ParentID;
                    }
                }
                else
                {
                    // Обычное согласование
                    // Проверяем, находимся ли мы в цикле доработки (есть ParentID)
                    // И возвращаемся ли к тому, кто отклонил
                    if (thisStepRecord.ParentID.HasValue)
                    {
                        // Ищем, есть ли выше в цепочке запись с RecipientName = nextName и Result = "Не согласовано"
                        var rejectingRecord = FindOriginalRejectingRecord(thisStepRecord, nextName);
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

                OrderApprovalHistory nextStepRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = CurrentItem.OrderApprovalID,
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
                    IsRework = false  // Новая запись - не доработка
                };

                status = db.mAdd(nextStepRecord);
                if (status.IsFailed)
                {
                    return Result.Failed("Не удалось сохранить запись нового согласования в БД!");
                }

                db.mSaveChanges(); // Сохраняем, чтобы получить ID нового шага

                // Если текущий шаг был доработкой, обновляем его ParentID, чтобы он стал дочерним элементом нового шага
                if (thisStepRecord.IsRework)
                {
                    // Если у текущей записи есть родитель, и это тоже запись доработки (IsRework),
                    // то родитель должен стать дочерним элементом нового шага (sub-cycle reparenting)
                    if (thisStepRecord.ParentID.HasValue)
                    {
                        var parentResult = db.mGetSingle<OrderApprovalHistory>(h => h.ID == thisStepRecord.ParentID.Value);
                        if (parentResult.IsSuccess && parentResult.Data != null && parentResult.Data.IsRework)
                        {
                            // Проверяем, не создаст ли это циклическую ссылку
                            // Если новый шаг указывает на родителя как на свой ParentID, то перенос родителя создаст цикл
                            if (nextParentID.HasValue && nextParentID.Value == parentResult.Data.ID)
                            {
                                // Циклическая ссылка: nextStep встаёт на место родителя, а родитель становится дочерним nextStep
                                // 1. nextStep получает ParentID родителя (встаёт на его место в иерархии)
                                nextStepRecord.ParentID = parentResult.Data.ParentID;
                                status = db.mUpdate(nextStepRecord);
                                if (status.IsFailed)
                                {
                                    LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для нового шага: {status.Message}");
                                }
                                // 2. Родитель переносится под nextStep
                                parentResult.Data.ParentID = nextStepRecord.ID;
                                status = db.mUpdate(parentResult.Data);
                                if (status.IsFailed)
                                {
                                    LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для родительской записи: {status.Message}");
                                }
                                // 3. thisStep остаётся дочерним элементом родителя (ParentID не меняется)
                            }
                            else
                            {
                                // Безопасно переносим родителя
                                parentResult.Data.ParentID = nextStepRecord.ID;
                                status = db.mUpdate(parentResult.Data);
                                if (status.IsFailed)
                                {
                                    LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для родительской записи доработки: {status.Message}");
                                }
                                // Текущая запись остаётся дочерней элементом родителя
                            }
                        }
                        else
                        {
                            // Обычное reparenting: текущая запись становится дочерней нового шага
                            thisStepRecord.ParentID = nextStepRecord.ID;
                            status = db.mUpdate(thisStepRecord);
                            if (status.IsFailed)
                            {
                                LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для записи доработки: {status.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Нет родителя, обычное reparenting
                        thisStepRecord.ParentID = nextStepRecord.ID;
                        status = db.mUpdate(thisStepRecord);
                        if (status.IsFailed)
                        {
                            LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для записи доработки: {status.Message}");
                        }
                    }
                }
                // Если текущий шаг НЕ был доработкой, но мы возвращаемся к отклонившему (nextParentID != текущий ParentID),
                // то текущий шаг также должен стать дочерним элементом нового шага
                else if (thisStepRecord.ParentID.HasValue && nextParentID.HasValue && 
                         thisStepRecord.ParentID.Value != nextParentID.Value)
                {
                    thisStepRecord.ParentID = nextStepRecord.ID;
                    status = db.mUpdate(thisStepRecord);
                    if (status.IsFailed)
                    {
                        LoggerManager.MainLogger.Warn($"Не удалось обновить ParentID для записи: {status.Message}");
                    }
                }

                LoggerManager.MainLogger.Info($"Заказ согласован, передан {nextRole} - {nextName}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в ApproveOrder: {ex.Message}");
                return Result.Failed($"Произошла ошибка при согласовании: {ex.Message}");
            }
        }

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
            return GetDefaultNextRecipient();
        }

        public override Result RejectOrder(Dictionary<string, object> data)
        {
            try
            {
                string comment = data.ContainsKey("Comment") ? data["Comment"].ToString() : "";

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

                // 2. Создаем новую запись для Сладкова, привязывая её к ID технолога
                OrderApprovalHistory nextStepRecord = CreateRejectionRecord(
                    recipientRole,
                    recipientName,
                    comment,
                    deadlineDate,
                    currentActiveStep?.ID); // Ключевое изменение: передаем ParentID

                Result addResult = db.mAdd(nextStepRecord);

                return addResult.IsSuccess ? Result.Success() : Result.Failed("Ошибка сохранения истории");
            }
            catch (Exception ex)
            {
                return Result.Failed($"Ошибка технолога: {ex.Message}");
            }
        }

        protected override (string Role, string Name) GetDefaultNextRecipient()
        {
            return ("Начальник отдела заказов", "Дингес");
        }
    }
}