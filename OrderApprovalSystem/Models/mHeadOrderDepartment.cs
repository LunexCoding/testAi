using System;
using System.Collections.Generic;
using Fox.Core.Logging;
using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Data;

namespace OrderApprovalSystem.Models
{
    public class mHeadOrderDepartment : mBaseOrderApproval
    {
        public mHeadOrderDepartment()
        {
        }

        public override Result ApproveOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug($"ApproveOrder for {CurrentItem.OrderNumber}");

            try
            {
                string comment = (string)data["Comment"];

                int workingDaysCount = GetWorkingDaysCount(CurrentItem.OrderApprovalID);
                DateTime deadlineDate = CalculateDeadlineDate(DateTime.Today, workingDaysCount);

                OrderApprovalHistory thisStep = FindCurrentStepRecord();

                if (thisStep == null)
                {
                    return Result.Failed("Не найдена текущая запись согласования!");
                }

                thisStep.CompletionDate = DateTime.Now;
                thisStep.Status = "Выполнено";
                thisStep.Result = "Согласовано";
                thisStep.Comment = comment;

                Result status = db.mUpdate(thisStep);
                if (status.IsFailed)
                {
                    return Result.Failed("Не удалось обновить запись текущего согласования в БД!");
                }

                // ОПРЕДЕЛЯЕМ СЛЕДУЮЩЕГО ПОЛУЧАТЕЛЯ С УЧЕТОМ ПРИЗНАКА ДОРАБОТКИ
                (string nextRole, string nextName) = GetNextRecipientWithReworkCheck();

                OrderApprovalHistory nextStep = new OrderApprovalHistory
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
                    IsRework = false
                };

                status = db.mAdd(nextStep);
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
                OrderApprovalHistory previousStepRecord = FindLastStepRecord(
                    RoleManager.DisplayRoleName,
                    RoleManager.CurrentUser.Name);

                if (previousStepRecord is null)
                {
                    LoggerManager.MainLogger.Error("Не найдена прошлая строка согласования");
                    return Result.Failed("Не найдена предыдущая запись согласования");
                }

                previousStepRecord.Status = "Выполнено";
                previousStepRecord.Result = "Не согласовано";
                previousStepRecord.CompletionDate = DateTime.Now;

                Result updateResult = db.mUpdate(previousStepRecord);
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

                string recipientName = (string)data["SubdivisionRecipient"];
                string recipientRole = FindUserRole(recipientName);
                string comment = (string)data["Comment"];

                // Проверяем, нет ли уже активной доработки
                var activeRework = GetActiveReworkRecord(recipientRole, recipientName);
                if (activeRework != null)
                {
                    return Result.Failed($"Уже есть активная задача на доработку для {recipientRole} - {recipientName}");
                }

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
                    $"Заказ отклонен и отправлен на доработку в {recipientRole} - {recipientName}");

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
            return ("Менеджер заказов", "Папаева");
        }
    }
}