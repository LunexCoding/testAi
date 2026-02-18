using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Fox;
using Fox.Core;
using Fox.Core.Logging;
using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Data;
using OrderApprovalSystem.Data.Entities;


namespace OrderApprovalSystem.Models
{
    public class mBaseOrderApproval : MBase
    {
        public User CurrentUser { get; private set; }
        public string UserRoleDisplay { get; private set; }
        public string UserName { get; private set; }

        protected Fox.DatabaseService.IDatabaseService db = ServiceLocator.DatabaseService;

        #region Конструктор и общие свойства

        public mBaseOrderApproval()
        {
            CurrentUser = RoleManager.CurrentUser;
            UserRoleDisplay = RoleManager.DisplayRoleName;
            UserName = CurrentUser.Name;

            List<OrderApprovalNomenclatureGroups> dbGroups = db.mGetQuery<OrderApprovalNomenclatureGroups>().ToList();
            List<OrderApprovalTypes> dbTypes = db.mGetQuery<OrderApprovalTypes>().ToList();

            NomenclatureGroups = new ObservableCollection<CustomNomenclatureGroup>(
                dbGroups.Select(
                    group =>
                    new CustomNomenclatureGroup
                    {
                        Id = group.ID,
                        GroupName = group.GroupName,
                        Type = dbTypes.FirstOrDefault(type => type.ID == group.TypeID)
                    }
                ).ToList()
            );

            LoadData();
        }

        private void InitializeSelectedValues()
        {
            try
            {
                if (CurrentItem?.NomenclatureGroup != null)
                {
                    // 1. Устанавливаем выбранную номенклатурную группу
                    SelectedNomenclatureGroup = NomenclatureGroups?.FirstOrDefault(
                        group => group.Id == CurrentItem.NomenclatureGroup.ID
                    );
                }

                // 2. Инициализируем список типов оснастки (по умолчанию)
                if (EquipmentTypes == null || !EquipmentTypes.Any())
                {
                    EquipmentTypes = EquipmentTypesDict.Keys.ToList();
                }

                // 3. Устанавливаем выбранный тип оснастки из CurrentItem
                if (CurrentItem?.EquipmentType != null)
                {
                    if (CurrentItem.EquipmentType is OrderApprovalTypes equipmentTypeObj)
                    {
                        // Определяем какой это тип по значениям терминов
                        if (SelectedNomenclatureGroup != null)
                        {
                            if (equipmentTypeObj.NewDevelopmentTerm == SelectedNomenclatureGroup.NewDevelopmentTerm)
                                SelectedEquipmentType = "Новая разработка";
                            else if (equipmentTypeObj.DoubleTerm == SelectedNomenclatureGroup.DoubleTerm)
                                SelectedEquipmentType = "Дублер";
                            else if (equipmentTypeObj.RepairModificationTerm == SelectedNomenclatureGroup.RepairModificationTerm)
                                SelectedEquipmentType = "Ремонт/доработка";
                        }
                    }
                }

                // 4. Если ничего не выбрано, выбираем первый тип по умолчанию
                if (string.IsNullOrEmpty(SelectedEquipmentType) && EquipmentTypes?.Any() == true)
                {
                    SelectedEquipmentType = EquipmentTypes.First();
                }

                // 5. Явно обновляем значение термина
                UpdateTermValue();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error("Ошибка при инициализации выбранных значений", ex);
            }
        }


        private ObservableCollection<CustomNomenclatureGroup> _nomenclatureGroups;
        public ObservableCollection<CustomNomenclatureGroup> NomenclatureGroups
        {
            get => _nomenclatureGroups;
            set
            {
                _nomenclatureGroups = value;
                OnPropertyChanged(nameof(NomenclatureGroups));
            }
        }

        private Dictionary<string, int> _equipmentTypesDict = new Dictionary<string, int>()
        {
            { "Новая разработка", 0 },
            { "Дублер", 0 },
            { "Ремонт/доработка", 0 }
        };

        public Dictionary<string, int> EquipmentTypesDict
        {
            get => _equipmentTypesDict;
            set
            {
                _equipmentTypesDict = value;
                OnPropertyChanged(nameof(EquipmentTypesDict));
            }
        }

        private List<string> _equipmentTypes;
        public List<string> EquipmentTypes
        {
            get => _equipmentTypes;
            set
            {
                _equipmentTypes = value;
                OnPropertyChanged(nameof(EquipmentTypes));
            }
        }

        private string _selectedEquipmentType;
        public string SelectedEquipmentType
        {
            get => _selectedEquipmentType;
            set
            {
                _selectedEquipmentType = value;
                OnPropertyChanged(nameof(SelectedEquipmentType));
                UpdateTermValue();
            }
        }

        private int _selectedTerm;
        public int SelectedTerm
        {
            get => _selectedTerm;
            set
            {
                _selectedTerm = value;
                OnPropertyChanged(nameof(SelectedTerm));
            }
        }

        // Выбранная номенклатурная группа
        private CustomNomenclatureGroup _selectedNomenclatureGroup;
        public CustomNomenclatureGroup SelectedNomenclatureGroup
        {
            get => _selectedNomenclatureGroup;
            set
            {
                _selectedNomenclatureGroup = value;

                if (value != null)
                {
                    if (CurrentItem != null)
                    {
                        CurrentItem.NomenclatureGroup = new OrderApprovalNomenclatureGroups
                        {
                            ID = _selectedNomenclatureGroup.Id,
                            GroupName = _selectedNomenclatureGroup.GroupName,
                            TypeID = _selectedNomenclatureGroup.Type.ID
                        };
                    }

                    // Обновляем словарь с терминами
                    EquipmentTypesDict["Новая разработка"] = _selectedNomenclatureGroup.NewDevelopmentTerm;
                    EquipmentTypesDict["Дублер"] = _selectedNomenclatureGroup.DoubleTerm;
                    EquipmentTypesDict["Ремонт/доработка"] = _selectedNomenclatureGroup.RepairModificationTerm;

                    // Обновляем список типов оснастки
                    EquipmentTypes = EquipmentTypesDict.Keys.ToList();

                    // Обновляем значение Term
                    UpdateTermValue();
                }

                OnPropertyChanged(nameof(SelectedNomenclatureGroup));
                OnPropertyChanged(nameof(EquipmentTypesDict));
            }
        }

        private void UpdateTermValue()
        {
            if (_selectedNomenclatureGroup != null || !string.IsNullOrEmpty(_selectedEquipmentType))
            {
                SelectedTerm = _selectedNomenclatureGroup.GetTermValue(_selectedEquipmentType);

                if (CurrentItem != null)
                {
                    CurrentItem.EquipmentType = _selectedNomenclatureGroup.Type;
                }
            }
        }

        #endregion

        #region Навигация (общая для всех ролей)

        public void NavigatePrevious()
        {
            if (HasPrevious)
            {
                CurrentIndex--;
                CurrentItem = CurrentGroup.Items[CurrentIndex];
            }
        }

        public void NavigateNext()
        {
            if (HasNext)
            {
                CurrentIndex++;
                CurrentItem = CurrentGroup.Items[CurrentIndex];
            }
        }

        public void NavigateNextGroup()
        {
            if (HasNextGroup)
            {
                CurrentGroup = GroupedData[CurrentGroupIndex + 1];
            }
        }

        public void NavigatePreviousGroup()
        {
            if (HasPreviousGroup)
            {
                CurrentGroup = GroupedData[CurrentGroupIndex - 1];
            }
        }

        public void FindAndNavigate(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || GroupedData == null)
            {
                return;
            }

            string query = searchText.ToLower().Trim();

            var targetGroup = GroupedData.FirstOrDefault(
                g => g.Zak1 != null && g.Zak1.ToLower().Contains(query)
            );

            if (targetGroup != null)
            {
                if (CurrentGroup != targetGroup)
                {
                    CurrentGroup = targetGroup;
                }
                return;
            }
        }

        #endregion

        #region Операции согласования (общие методы)

        public virtual Result ApproveOrder(Dictionary<string, object> data)
        {
            // Базовый метод, переопределяется в производных классах
            throw new NotImplementedException();
        }

        public virtual Result RejectOrder(Dictionary<string, object> data)
        {
            // Базовый метод, переопределяется в производных классах
            throw new NotImplementedException();
        }

        public virtual Result CreateOrderByMemo()
        {
            // Базовый метод, переопределяется в производных классах
            throw new NotImplementedException();
        }

        #endregion

        #region Общие свойства отображения

        private ObservableCollection<TechnologistGroup> _groupedData;
        public ObservableCollection<TechnologistGroup> GroupedData
        {
            get => _groupedData;
            set
            {
                _groupedData = value;
                OnPropertyChanged(nameof(TotalGroups));
                if (_groupedData != null && _groupedData.Any())
                {
                    CurrentGroup = _groupedData.First();
                }
            }
        }

        private TechnologistGroup _currentGroup;
        public TechnologistGroup CurrentGroup
        {
            get => _currentGroup;
            set
            {
                _currentGroup = value;
                OnPropertyChanged(nameof(CurrentGroup));

                if (_groupedData != null && _currentGroup != null)
                {
                    CurrentGroupIndex = _groupedData.IndexOf(_currentGroup);
                }

                OnPropertyChanged(nameof(CurrentGroupIndexDisplay));
                OnPropertyChanged(nameof(HasPreviousGroup));
                OnPropertyChanged(nameof(HasNextGroup));

                if (_currentGroup != null && _currentGroup.Items.Any())
                {
                    CurrentItem = _currentGroup.Items.First();
                    CurrentIndex = 0;
                }
                else
                {
                    CurrentItem = null;
                    CurrentIndex = 0;
                }

                InitializeSelectedValues();
            }
        }

        private TechnologicalOrder _currentItem;
        public TechnologicalOrder CurrentItem
        {
            get => _currentItem;
            set
            {
                _currentItem = value;

                if (_currentGroup != null && _currentItem != null && _currentGroup.Items.Contains(_currentItem))
                {
                    CurrentIndex = _currentGroup.Items.IndexOf(_currentItem);
                }

                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(CurrentIndexDisplay));
                OnPropertyChanged(nameof(HasPrevious));
                OnPropertyChanged(nameof(HasNext));
            }
        }

        private int _currentGroupIndex;
        public int CurrentGroupIndex
        {
            get => _currentGroupIndex;
            set
            {
                _currentGroupIndex = value;
                OnPropertyChanged(nameof(CurrentGroupIndex));
                OnPropertyChanged(nameof(CurrentGroupIndexDisplay));
                OnPropertyChanged(nameof(HasPreviousGroup));
                OnPropertyChanged(nameof(HasNextGroup));
            }
        }

        private int _currentIndex = 0;
        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                _currentIndex = value;
                OnPropertyChanged(nameof(CurrentIndex));
                OnPropertyChanged(nameof(CurrentIndexDisplay));
                OnPropertyChanged(nameof(HasPrevious));
                OnPropertyChanged(nameof(HasNext));
            }
        }

        public string CurrentGroupIndexDisplay =>
            GroupedData != null && GroupedData.Any()
                ? $"{CurrentGroupIndex + 1} из {GroupedData.Count}"
                : "0 из 0";

        public string CurrentIndexDisplay =>
            CurrentGroup != null && CurrentGroup.Items.Any()
                ? $"{CurrentIndex + 1} из {CurrentGroup.Items.Count}"
                : "0 из 0";

        public bool HasPrevious => CurrentGroup != null && CurrentIndex > 0;
        public bool HasNext => CurrentGroup != null && CurrentIndex < CurrentGroup.Items.Count - 1;
        public bool HasPreviousGroup => GroupedData != null && CurrentGroupIndex > 0;
        public bool HasNextGroup => GroupedData != null && CurrentGroupIndex < GroupedData.Count - 1;
        public int TotalGroups => GroupedData?.Count ?? 0;

        #endregion

        #region Загрузка данных (общая для всех ролей)

        protected virtual void LoadData()
        {
            try
            {
                IQueryable<TechnologicalOrder> query =
                    from OrderApproval in db.mGetQuery<OrderApproval>()
                    join OrderApprovalDrafts in db.mGetQuery<OrderApprovalDrafts>() on OrderApproval.ID equals OrderApprovalDrafts.OrderApprovalID
                    join zayvka in db.mGetQuery<zayvka>() on OrderApproval.zayvkaID equals zayvka.id
                    join os_pro in db.mGetQuery<os_pro>() on zayvka.zak_1 equals os_pro.zak_1

                    // LEFT JOIN oborud
                    join oborud in db.mGetQuery<oborud>() on zayvka.rab_m equals oborud.rab_m into obGroup
                    from oborud in obGroup.DefaultIfEmpty()

                        // LEFT JOIN s_oper
                    join s_oper in db.mGetQuery<s_oper>() on zayvka.kodop equals s_oper.code into soGroup
                    from s_oper in soGroup.DefaultIfEmpty()

                        // LEFT JOIN prod
                    join p in db.mGetQuery<prod>()
                        on new { zayvka.zak_1, IsDrNull = (decimal?)null }
                        equals new { p.zak_1, IsDrNull = p.dr } into pGroup
                    from p in pGroup.DefaultIfEmpty()


                        // JOIN для получения NomenclatureGroup по NomenclatureGroupID
                    join nomenclatureGroup in db.mGetQuery<OrderApprovalNomenclatureGroups>()
                        on OrderApproval.NomenclatureGroupID equals nomenclatureGroup.ID into ngGroup
                    from nomenclatureGroup in ngGroup.DefaultIfEmpty()

                        // JOIN для получения EquipmentType через TypeID из NomenclatureGroup
                    join equipmentType in db.mGetQuery<OrderApprovalTypes>()
                        on nomenclatureGroup.TypeID equals equipmentType.ID into etGroup
                    from equipmentType in etGroup.DefaultIfEmpty()


                    where os_pro.d_vn_14 != null

                    orderby zayvka.zak_1

                    select new TechnologicalOrder
                    {
                        OrderNumber = zayvka.zak_1,
                        OrderApprovalID = OrderApproval.ID,
                        OrderApprovalDraftID = OrderApprovalDrafts.ID,
                        Technologist = OrderApproval.Technologist,
                        CoreDraft = (decimal)OrderApproval.CoreDraft,
                        Draft = OrderApproval.Draft,
                        CoreDraftName = OrderApproval.CoreDraftName.Trim(),
                        DraftName = OrderApproval.DraftName.Trim(),
                        ManufacturingTerm = OrderApproval.ManufacturingTerm,

                        Workshop = OrderApproval.Workshop,
                        Warehouse = OrderApproval.Warehouse,
                        Schedule = zayvka.graf,

                        Workplace = oborud != null ? (oborud.code + " " + oborud.oborud1).Trim() : null,
                        Operation = s_oper != null ? s_oper.oper.Trim() : null,

                        EquipmentDraft = OrderApprovalDrafts.EquipmentDraft,
                        EquipmentName = OrderApprovalDrafts.EquipmentName.Trim(),
                        EquipmentNameFromTechnologist = OrderApproval.EquipmentNameFromTechnologist.Trim(),
                        EquipmentQuantityForOperation = OrderApproval.EquipmentQuantityForOperation,
                        EquimentRequiredQuantity = OrderApprovalDrafts.EquimentRequiredQuantity,
                        Cooperation = OrderApprovalDrafts.Cooperation,
                        IsDeletedFromOrder = OrderApprovalDrafts.IsDeletedFromOrder,

                        Note = zayvka.prim,
                        Analog = OrderApproval.Analog,
                        DesignComment = OrderApprovalDrafts.CommentForDesign,
                        ManufacturingComment = OrderApprovalDrafts.CommentForManufacturing,
                        OpenAtByTechnologist = OrderApproval.OpenAt,
                        Comment = null,

                        IsByMemo = OrderApproval.IsByMemo,
                        MemoNumber = OrderApproval.MemoNumber,
                        MemoAuthor = OrderApproval.MemoAuthor,
                        CoreOrderByMemo = OrderApproval.CoreOrder,
                        CoreNumberByMemo = OrderApproval.CoreNumber,
                        OrderByMemo = OrderApproval.Order,
                        NumberByMemo = OrderApproval.Number,
                        OrderName = OrderApproval.OrderName,
                        OpenAtByMemo = OrderApproval.OpenAtByMemo,

                        // Получаем NomenclatureGroup через JOIN
                        NomenclatureGroup = nomenclatureGroup,

                        // Получаем EquipmentType через JOIN
                        EquipmentType = equipmentType,

                        DraftByMemo = OrderApproval.DraftByMemo,
                        DraftNameByMemo = OrderApproval.DraftNameByMemo,
                        Balance = OrderApproval.Balance,
                        WorkshopByMemo = OrderApproval.WorkshopByMemo,
                        EquipmentRequiredQuantityByMemo = OrderApproval.EquipmentRequiredQuantityByMemo
                    };

                List<TechnologicalOrder> data = query.Distinct().ToList();

                HashSet<int> reworkOrders = db.mGetList<OrderApprovalHistory>(
                   record => record.IsRework == true
               ).Data?.Select(x => x.OrderApprovalID).Distinct().ToHashSet() ?? new HashSet<int>();

                GroupedData = new ObservableCollection<TechnologistGroup>(
                    data
                    .OrderBy(x => x.EquipmentDraft)
                    .GroupBy(x => x.OrderNumber)
                    .Select(g => new TechnologistGroup
                    {
                        Zak1 = g.Key,
                        Items = new ObservableCollection<TechnologicalOrder>(
                            g.OrderBy(x => x.OpenAtByTechnologist).ToList()
                        ),
                        IsByMemo = g.First().IsByMemo,
                        HasRework = reworkOrders.Contains(g.First().OrderApprovalID) // Проверка наличия переработки
                    })
                    .OrderBy(g => g.Zak1)
                    .ToList()
                );
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при загрузке данных: {ex.Message}");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Вспомогательные методы (общие)

        protected string FindUserRole(string userName)
        {
            // Сначала ищем как получателя
            var recipientRole = db.mGetQuery<OrderApprovalHistory>()
                .Where(h => h.RecipientName == userName)
                .Select(h => h.RecipientRole)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(recipientRole))
                return recipientRole;

            // Если не нашли, ищем как отправителя
            var senderRole = db.mGetQuery<OrderApprovalHistory>()
                .Where(h => h.SenderName == userName)
                .Select(h => h.SenderRole)
                .FirstOrDefault();

            return senderRole;
        }

        protected OrderApproval FindCurrentOrderApproval(int ID)
        {
            return db.mGetSingle<OrderApproval>(
                record =>
                record.ID == ID
            ).Data;
        }

        protected OrderApprovalHistory FindCurrentStepRecord()
        {
            return db.mGetList<OrderApprovalHistory>(
                record =>
                record.OrderApprovalID == CurrentItem.OrderApprovalID
                && record.RecipientRole == RoleManager.DisplayRoleName
                && record.RecipientName == RoleManager.CurrentUser.Name
            )
            .Data
            .OrderByDescending(record => record.ID)
            .FirstOrDefault();
        }

        protected OrderApprovalHistory FindLastStepRecord(string recipientRole, string recipientName)
        {
            var maxId = db.mGetQuery<OrderApprovalHistory>()
                .Where(
                    oah =>
                    oah.OrderApprovalID == CurrentItem.OrderApprovalID
                    && oah.RecipientRole == recipientRole
                    && oah.RecipientName == recipientName
                )
                .Select(oah => oah.ID)
                .Max();

            return db.mGetSingle<OrderApprovalHistory>(record => record.ID == maxId).Data;
        }

        protected int GetWorkingDaysCount(int orderApprovalId)
        {
            return db.mGetQuery<OrderApproval>()
                .Where(order => order.ID == orderApprovalId)
                .Join(
                    db.mGetQuery<OrderApprovalTypes>(),
                    order => order.EquipmentTypeID,
                    approvalType => approvalType.ID,
                    (order, approvalType) => approvalType.NewDevelopmentTerm
                )
                .FirstOrDefault();
        }

        protected DateTime CalculateDeadlineDate(DateTime startDate, int workingDaysCount)
        {
            DateTime deadlineDate = db.mGetQuery<calend>()
                .Where(calendarDay => calendarDay.mday > startDate && calendarDay.v == true)
                .OrderBy(calendarDay => calendarDay.mday)
                .Skip(workingDaysCount - 1)
                .Take(1)
                .Select(calendarDay => calendarDay.mday)
                .FirstOrDefault();

            if (deadlineDate == default(DateTime))
            {
                deadlineDate = startDate.AddDays(workingDaysCount);
            }

            return deadlineDate;
        }

        #endregion

        #region Методы для работы с историей согласования и возвратов

        /// <summary>
        /// Получить полную историю согласования для текущего заказа
        /// </summary>
        protected List<OrderApprovalHistory> GetOrderHistory(int orderApprovalId)
        {
            return db.mGetQuery<OrderApprovalHistory>()
                .Where(h => h.OrderApprovalID == orderApprovalId)
                .OrderBy(h => h.ID)
                .ToList();
        }

        /// <summary>
        /// Проверить, был ли возврат этому получателю ранее
        /// </summary>
        protected bool HasPreviousRejection()
        {
            var history = GetOrderHistory(CurrentItem.OrderApprovalID);

            // Ищем записи, где этот пользователь был получателем И результат был "На доработку"
            return history.Any(h =>
                h.RecipientRole == RoleManager.DisplayRoleName &&
                h.RecipientName == RoleManager.CurrentUser.Name &&
                h.Result == "Не согласовано");
        }

        /// <summary>
        /// Получить информацию о последнем возврате для этого получателя
        /// </summary>
        protected OrderApprovalHistory GetLastRejectionForRecipient()
        {
            return db.mGetQuery<OrderApprovalHistory>()
                .Where(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientRole == RoleManager.DisplayRoleName &&
                    h.RecipientName == RoleManager.CurrentUser.Name &&
                    h.Result == "Не согласовано")
                .OrderByDescending(h => h.ID)
                .FirstOrDefault();
        }

        /// <summary>
        /// Определить отправителя для повторного согласования
        /// Возвращает того, кто отправил заказ этому получателю в прошлый раз
        /// </summary>
        protected (string SenderRole, string SenderName) GetPreviousSenderForReapproval()
        {
            // Получаем последний возврат для этого получателя
            var lastRejection = GetLastRejectionForRecipient();

            if (lastRejection != null)
            {
                // Возвращаем отправителя из того возврата
                return (lastRejection.SenderRole, lastRejection.SenderName);
            }

            // Если не было возвратов, возвращаем null
            return (null, null);
        }

        /// <summary>
        /// Получить следующего получателя в цепочке
        /// Если был возврат - отправляем тому, кто вернул
        /// Иначе - стандартный следующий этап
        /// </summary>
        protected (string Role, string Name) GetNextRecipient()
        {
            // Проверяем, был ли возврат этому пользователю ранее
            if (HasPreviousRejection())
            {
                // Получаем отправителя из последнего возврата
                var (senderRole, senderName) = GetPreviousSenderForReapproval();

                if (!string.IsNullOrEmpty(senderRole) && !string.IsNullOrEmpty(senderName))
                {
                    LoggerManager.MainLogger.Debug($"Повторное согласование: возврат к {senderRole} - {senderName}");
                    return (senderRole, senderName);
                }
            }

            // Стандартная логика - следующий по цепочке
            return GetDefaultNextRecipient();
        }

        /// <summary>
        /// Стандартная логика определения следующего получателя (переопределяется в наследниках)
        /// </summary>
        protected virtual (string Role, string Name) GetDefaultNextRecipient()
        {
            // Базовая реализация - заглушка
            return (null, null);
        }

        /// <summary>
        /// Проверить, является ли текущее согласование повторным после возврата
        /// </summary>
        protected bool IsReapprovalAfterRejection()
        {
            if (CurrentItem == null) return false;

            // Получаем последнюю запись для текущего получателя
            var lastRecord = db.mGetQuery<OrderApprovalHistory>()
                .Where(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientRole == RoleManager.DisplayRoleName &&
                    h.RecipientName == UserName)
                .OrderByDescending(h => h.ID)
                .FirstOrDefault();

            // Если есть запись с результатом "На доработку" - это повторное согласование
            return lastRecord != null && lastRecord.Result == "Не согласовано";
        }

        /// <summary>
        /// Получить роль текущего пользователя
        /// </summary>
        protected string GetCurrentUserRole()
        {
            return FindUserRole(UserName);
        }

        protected bool IsCurrentStepRework()
        {
            var currentStep = FindCurrentStepRecord();
            return currentStep?.IsRework == true;
        }

        protected (string SenderRole, string SenderName) GetReworkSender()
        {
            var reworkRecord = db.mGetQuery<OrderApprovalHistory>()
                .Where(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientRole == RoleManager.DisplayRoleName &&
                    h.RecipientName == RoleManager.CurrentUser.Name &&
                    h.IsRework == true)
                .OrderByDescending(h => h.ID)
                .FirstOrDefault();

            if (reworkRecord != null)
            {
                return (reworkRecord.SenderRole, reworkRecord.SenderName);
            }

            return (null, null);
        }

        protected (string Role, string Name) GetNextRecipientWithReworkCheck()
        {
            // Проверяем, является ли текущее согласование результатом доработки
            if (IsCurrentStepRework())
            {
                // Получаем отправителя из записи с IsRework = true
                var (senderRole, senderName) = GetReworkSender();

                if (!string.IsNullOrEmpty(senderRole) && !string.IsNullOrEmpty(senderName))
                {
                    LoggerManager.MainLogger.Debug(
                        $"Обнаружен признак доработки. Пропускаем дефолтного получателя. " +
                        $"Возвращаем отправителю: {senderRole} - {senderName}");

                    return (senderRole, senderName);
                }
            }

            // Стандартная логика - следующий по цепочке
            return GetDefaultNextRecipient();
        }

        // Обновленный метод создания записи
        protected OrderApprovalHistory CreateRejectionRecord(
    string recipientRole,
    string recipientName,
    string comment,
    DateTime deadlineDate,
    int? parentId) // Добавили параметр для связи
        {
            return new OrderApprovalHistory
            {
                OrderApprovalID = CurrentItem.OrderApprovalID,
                ParentID = parentId,
                ReceiptDate = DateTime.Now,
                CompletionDate = null,
                Term = deadlineDate,
                RecipientRole = recipientRole,
                RecipientName = recipientName,
                SenderRole = RoleManager.DisplayRoleName,
                SenderName = RoleManager.CurrentUser.Name,
                Status = "В работе",
                Result = null,
                Comment = comment,
                IsRework = true
            };
        }

        // Поиск текущей активной записи для связи
        protected OrderApprovalHistory GetCurrentActiveStep()
        {
            return db.mGetList<OrderApprovalHistory>(h =>
                h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                h.RecipientName == RoleManager.CurrentUser.Name &&
                h.CompletionDate == null).Data
                .OrderByDescending(h => h.ReceiptDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Получить активную запись с IsRework для конкретного получателя
        /// </summary>
        protected OrderApprovalHistory GetActiveReworkRecord(string recipientRole, string recipientName)
        {
            return db.mGetQuery<OrderApprovalHistory>()
                .FirstOrDefault(h =>
                    h.OrderApprovalID == CurrentItem.OrderApprovalID &&
                    h.RecipientRole == recipientRole &&
                    h.RecipientName == recipientName &&
                    h.IsRework == true &&
                    h.Status == "В работе");
        }

        #endregion
    }
}