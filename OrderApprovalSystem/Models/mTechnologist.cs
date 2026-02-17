using System;
using System.Collections.Generic;
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

                OrderApprovalHistory nextStepRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = CurrentItem.OrderApprovalID,
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

                LoggerManager.MainLogger.Info($"Заказ согласован, передан {nextRole} - {nextName}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в ApproveOrder: {ex.Message}");
                return Result.Failed($"Произошла ошибка при согласовании: {ex.Message}");
            }
        }

        public override Result RejectOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug("Call RejectOrder");

            try
            {
                string recipientRole = (string)data["Subdivision"];
                string recipientName = (string)data["SubdivisionRecipient"];
                string comment = (string)data["Comment"];

                // Проверяем, нет ли уже активной доработки для этого получателя
                var activeRework = GetActiveReworkRecord(recipientRole, recipientName);
                if (activeRework != null)
                {
                    return Result.Failed($"Уже есть активная задача на доработку для {recipientRole} - {recipientName}");
                }

                OrderApprovalHistory thisStep = FindLastStepRecord(RoleManager.DisplayRoleName, RoleManager.CurrentUser.Name);

                if (thisStep is null)
                {
                    LoggerManager.MainLogger.Error("Не найдена прошлая строка согласования");
                    return Result.Failed("Не найдена предыдущая запись согласования");
                }

                thisStep.Status = "Выполнено";
                thisStep.Result = "Не согласовано";
                thisStep.CompletionDate = DateTime.Now;
                thisStep.Comment = comment;
                // IsRework у текущей записи не меняем - она завершена

                Result updateResult = db.mUpdate(thisStep);
                if (updateResult.IsFailed)
                {
                    LoggerManager.MainLogger.Error($"Ошибка при обновлении записи: {updateResult.Message}");
                    return Result.Failed("Не удалось обновить запись согласования");
                }

                int workingDaysCount = GetWorkingDaysCount(CurrentItem.OrderApprovalID);
                if (workingDaysCount <= 0)
                {
                    LoggerManager.MainLogger.Error($"Not found term for approvalType:");
                    return Result.Failed("Не найден срок");
                }

                DateTime deadlineDate = CalculateDeadlineDate(DateTime.Today, workingDaysCount);

                // СОЗДАЕМ ЗАПИСЬ С ПРИЗНАКОМ ДОРАБОТКИ
                OrderApprovalHistory nextStepRecord = CreateRejectionRecord(
                    recipientRole,
                    recipientName,
                    comment,
                    deadlineDate);

                Result addResult = db.mAdd(nextStepRecord);
                if (addResult.IsFailed)
                {
                    LoggerManager.MainLogger.Error($"Ошибка при добавлении записи: {addResult.Message}");
                    return Result.Failed("Не удалось создать новую запись согласования");
                }

                LoggerManager.MainLogger.Info(
                    $"Заказ отклонен и отправлен на доработку в {recipientRole} - {recipientName}. " +
                    $"IsRework=true");

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в RejectOrder: {ex.Message}", ex);
                return Result.Failed($"Произошла ошибка при отклонении заказа: {ex.Message}");
            }
        }

        protected override (string Role, string Name) GetDefaultNextRecipient()
        {
            return ("Начальник отдела заказов", "Дингес");
        }
    }
}