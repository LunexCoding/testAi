using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Data.Entities;
using OrderApprovalSystem.ViewModels.Modals;


namespace OrderApprovalSystem.Validators
{

    public class CreateOrderByMemoValidator
    {
        public static Result Validate(TechnologicalOrder record)
        {

            if (!AreAllFieldsFilled(record))
            {
                return Result.Failed("Заполнены не все поля!");
            }

            if (record.IsByMemo)
            {
                if (string.IsNullOrEmpty(record.MemoNumber))
                {
                    return Result.Failed("Номер служебной не может быть пустым!");
                }

                if (string.IsNullOrEmpty(record.MemoAuthor))
                {
                    return Result.Failed("Автор служебной должен быть заполнен!");
                }
            }

            if (!record.OrderByMemo.HasValue)
            {
                return Result.Failed("Заказ не может быть пустым!");
            }

            if (!record.NumberByMemo.HasValue)
            {
                return Result.Failed("Номер заказа не может быть пустым!");
            }

            if (!record.CoreOrderByMemo.HasValue)
            {
                return Result.Failed("Основной заказ не может быть пустым!");
            }

            if (!record.CoreNumberByMemo.HasValue)
            {
                return Result.Failed("Номер основного заказа не может быть пустым!");
            }

            if (string.IsNullOrEmpty(record.OrderName))
            {
                return Result.Failed("Наименование заказа не может быть пустым!");
            }

            if (!record.OpenAtByMemo.HasValue)
            {
                return Result.Failed("Дата открытия заказа должна быть заполнена!");
            }

            if (!record.Balance.HasValue)
            {
                return Result.Failed("Балансовый счет не может быть пустым!");
            }

            if (record.NomenclatureGroup == null)
            {
                return Result.Failed("Выберите номенклатурную группу!");
            }

            if (record.EquipmentType == null)
            {
                return Result.Failed("Выберите тип оснастки!");
            }

            if (!record.DraftByMemo.HasValue)
            {
                return Result.Failed("Чертеж не может быть пустым!");
            }

            if (string.IsNullOrEmpty(record.DraftNameByMemo))
            {
                return Result.Failed("Наименование чертежа не может быть пустым!");
            }

            if (string.IsNullOrEmpty(record.WorkshopByMemo))
            {
                return Result.Failed("Цех не может быть пустым!");
            }

            if (!record.EquipmentRequiredQuantityByMemo.HasValue)
            {
                return Result.Failed("Количество не может быть пустым!");
            }

            return Result.Success(); // Все заполнено!
        }

        private static bool AreAllFieldsFilled(TechnologicalOrder record)
        {
            // Проверка строковых полей
            if (record.IsByMemo)
            {
                if (string.IsNullOrWhiteSpace(record.MemoNumber)) return false;
                if (string.IsNullOrWhiteSpace(record.MemoAuthor)) return false;
            }
            if (string.IsNullOrWhiteSpace(record.OrderName)) return false;
            if (string.IsNullOrWhiteSpace(record.DraftNameByMemo)) return false;
            if (string.IsNullOrWhiteSpace(record.WorkshopByMemo)) return false;

            // Проверка nullable-полей
            if (!record.OrderByMemo.HasValue) return false;
            if (!record.NumberByMemo.HasValue) return false;
            if (!record.CoreOrderByMemo.HasValue) return false;
            if (!record.CoreNumberByMemo.HasValue) return false;
            if (!record.OpenAtByMemo.HasValue) return false;
            if (!record.Balance.HasValue) return false;
            if (!record.DraftByMemo.HasValue) return false;
            if (!record.EquipmentRequiredQuantityByMemo.HasValue) return false;

            // Проверка reference-типов
            if (record.NomenclatureGroup == null) return false;
            if (record.EquipmentType == null) return false;

            return true; // Все поля заполнены
        }
    }

}
