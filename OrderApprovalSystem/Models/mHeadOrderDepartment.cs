using System;
using System.Collections.Generic;
using System.Linq;
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

                // Определяем ParentID: если текущий шаг - доработка, новый шаг становится его дочерним
                // Иначе новый шаг остаётся на том же уровне (ParentID = текущий ParentID)
                int? nextParentID = thisStep.IsRework ? thisStep.ID : thisStep.ParentID;

                OrderApprovalHistory nextStep = new OrderApprovalHistory
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

        protected override (string Role, string Name) GetDefaultNextRecipient()
        {
            return ("Менеджер заказов", "Папаева");
        }
    }
}