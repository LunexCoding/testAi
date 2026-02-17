using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fox.Core;
using Fox.Core.Logging;
using Fox.DatabaseService.Entities;
using OrderApprovalSystem.Services;
using OrderApprovalSystem.Views.Modals;

namespace OrderApprovalSystem.Data.Entities
{
    public class TechnologicalOrder : INotifyPropertyChanged
    {
        #region Инициализация

        public void Dispose()
        {

        }

        public override string ToString()
        {
            return string.Empty;
        }

        #endregion Инициализация

        #region Переменные и свойства

        private string _orderNumber;
        private int _orderApprovalID;
        private int _orderApprovalDraftID;
        private string _technologist;
        private decimal _coreDraft;
        private decimal? _draft;
        private string _coreDraftName;
        private string _draftName;
        private string _workshop;
        private decimal? _warehouse;
        private string _schedule;
        private string _workplace;
        private string _operation;
        private decimal? _equipmentDraft;
        private string _equipmentName;
        private string _equipmentNameFromTechnologist;
        private decimal? _equipmentQuantityForOperation;
        private decimal? _equipmentRequiredQuantity;
        private bool _cooperation;
        private bool _isDeletedFromOrder;
        private string _note;
        private string _analog;
        private DateTime? _manufacturingTerm;
        private string _designComment;
        private string _manufacturingComment;
        private DateTime? _openAtByTechnologist;
        private string _comment;

        private bool _isByMemo;
        private string _memoNumber;
        private string _memoAuthor;
        private int? _orderByMemo;
        private int? _numberByMemo;
        private string _orderName;
        private int? _coreOrderByMemo;
        private int? _coreNumberByMemo;
        private OrderApprovalNomenclatureGroups _nomenclatureGroup;
        private OrderApprovalTypes _equipmentType;
        private decimal? _draftByMemo;
        private string _draftNameByMemo;
        private int? _balance;
        private DateTime? _openAtByMemo;
        private string _workshopByMemo;
	    private decimal? _equipmentRequiredQuantityByMemo;

        public string WorkshopByMemo
        {
            get => _workshopByMemo;
            set
            {
                if (_workshopByMemo == value) return;
                _workshopByMemo = value;
                OnPropertyChanged();
            }
        }

        public decimal? EquipmentRequiredQuantityByMemo
        {
            get => _equipmentRequiredQuantityByMemo;
            set
            {
                if (_equipmentRequiredQuantityByMemo == value) return;
                _equipmentRequiredQuantityByMemo = value;
                OnPropertyChanged();
            }
        }

        public string OrderNumber
        {
            get => _orderNumber;
            set
            {
                if (_orderNumber == value) return;
                _orderNumber = value;
                OnPropertyChanged();
            }
        }

        public int OrderApprovalID
        {
            get => _orderApprovalID;
            set
            {
                if (_orderApprovalID == value) return;
                _orderApprovalID = value;
                OnPropertyChanged();
            }
        }

        public int OrderApprovalDraftID
        {
            get => _orderApprovalDraftID;
            set
            {
                if (_orderApprovalDraftID == value) return;
                _orderApprovalDraftID = value;
                OnPropertyChanged();
            }
        }

        public string Technologist
        {
            get => _technologist;
            set
            {
                if (_technologist == value) return;
                _technologist = value;
                OnPropertyChanged();
            }
        }

        public decimal CoreDraft
        {
            get => _coreDraft;
            set
            {
                if (_coreDraft == value) return;
                _coreDraft = value;
                OnPropertyChanged();
            }
        }

        public decimal? Draft
        {
            get => _draft;
            set
            {
                if (_draft == value) return;
                _draft = value;
                OnPropertyChanged();
            }
        }

        public string CoreDraftName
        {
            get => _coreDraftName;
            set
            {
                if (_coreDraftName == value) return;
                _coreDraftName = value;
                OnPropertyChanged();
            }
        }

        public string DraftName
        {
            get => _draftName;
            set
            {
                if (_draftName == value) return;
                _draftName = value;
                OnPropertyChanged();
            }
        }

        public string Workshop
        {
            get => _workshop;
            set
            {
                if (_workshop == value) return;
                _workshop = value;
                OnPropertyChanged();
            }
        }

        public decimal? Warehouse
        {
            get => _warehouse;
            set
            {
                if (_warehouse == value) return;
                _warehouse = value;
                OnPropertyChanged();
            }
        }

        public string Schedule
        {
            get => _schedule;
            set
            {
                if (_schedule == value) return;
                _schedule = value;
                OnPropertyChanged();
            }
        }

        public string Workplace
        {
            get => _workplace;
            set
            {
                if (_workplace == value) return;
                _workplace = value;
                OnPropertyChanged();
            }
        }

        public string Operation
        {
            get => _operation;
            set
            {
                if (_operation == value) return;
                _operation = value;
                OnPropertyChanged();
            }
        }

        public decimal? EquipmentDraft
        {
            get => _equipmentDraft;
            set
            {
                if (_equipmentDraft == value) return;
                _equipmentDraft = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentName
        {
            get => _equipmentName;
            set
            {
                if (_equipmentName == value) return;
                _equipmentName = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentNameFromTechnologist
        {
            get => _equipmentNameFromTechnologist;
            set
            {
                if (_equipmentNameFromTechnologist == value) return;
                _equipmentNameFromTechnologist = value;
                OnPropertyChanged();
            }
        }

        public decimal? EquipmentQuantityForOperation
        {
            get => _equipmentQuantityForOperation;
            set
            {
                if (_equipmentQuantityForOperation == value) return;
                _equipmentQuantityForOperation = value;
                OnPropertyChanged();
            }
        }

        public decimal? EquimentRequiredQuantity
        {
            get => _equipmentRequiredQuantity;
            set
            {
                if (_equipmentRequiredQuantity == value) return;
                _equipmentRequiredQuantity = value;
                OnPropertyChanged();
            }
        }

        public bool Cooperation
        {
            get => _cooperation;
            set
            {
                if (_cooperation == value) return;
                _cooperation = value;
                OnPropertyChanged();

                // Обновление в базе данных
                UpdateCooperation();
            }
        }

        public bool IsDeletedFromOrder
        {
            get => _isDeletedFromOrder;
            set
            {
                if (_isDeletedFromOrder == value) return;
                _isDeletedFromOrder = value;
                OnPropertyChanged();

                // Обновление в базе данных
                UpdateIsDeletedFromOrder();
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (_note == value) return;
                _note = value;
                OnPropertyChanged();
            }
        }

        public string Analog
        {
            get => _analog;
            set
            {
                if (_analog == value) return;
                _analog = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ManufacturingTerm
        {
            get => _manufacturingTerm;
            set
            {
                if (_manufacturingTerm == value) return;
                _manufacturingTerm = value;
                OnPropertyChanged();
            }
        }

        public string DesignComment
        {
            get => _designComment;
            set
            {
                if (_designComment == value) return;
                _designComment = value;
                OnPropertyChanged();
            }
        }

        public string ManufacturingComment
        {
            get => _manufacturingComment;
            set
            {
                if (_manufacturingComment == value) return;
                _manufacturingComment = value;
                OnPropertyChanged();
            }
        }

        public DateTime? OpenAtByTechnologist
        {
            get => _openAtByTechnologist;
            set
            {
                if (_openAtByTechnologist == value) return;
                _openAtByTechnologist = value;
                OnPropertyChanged();
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment == value) return;
                _comment = value;
                OnPropertyChanged();
            }
        }

        public bool IsByMemo
        {
            get => _isByMemo;
            set
            {
                if (_isByMemo == value) return;
                _isByMemo = value;
                OnPropertyChanged();
            }
        }

        public string MemoNumber
        {
            get => _memoNumber;
            set
            {
                if (_memoNumber == value) return;
                _memoNumber = value;
                OnPropertyChanged();
            }
        }

        public string MemoAuthor
        {
            get => _memoAuthor;
            set
            {
                if (_memoAuthor == value) return;
                _memoAuthor = value;
                OnPropertyChanged();
            }
        }

        public int? OrderByMemo
        {
            get => _orderByMemo;
            set
            {
                if (_orderByMemo == value) return;
                _orderByMemo = value;
                OnPropertyChanged();
            }
        }

        public int? NumberByMemo
        {
            get => _numberByMemo;
            set
            {
                if (_numberByMemo == value) return;
                _numberByMemo = value;
                OnPropertyChanged();
            }
        }

        public string OrderName
        {
            get => _orderName;
            set
            {
                if (_orderName == value) return;
                _orderName = value;
                OnPropertyChanged();
            }
        }

        public int? CoreOrderByMemo
        {
            get => _coreOrderByMemo;
            set
            {
                if (_coreOrderByMemo == value) return;
                _coreOrderByMemo = value;
                OnPropertyChanged();
            }
        }

        public int? CoreNumberByMemo
        {
            get => _coreNumberByMemo;
            set
            {
                if (_coreNumberByMemo == value) return;
                _coreNumberByMemo = value;
                OnPropertyChanged();
            }
        }

        public OrderApprovalNomenclatureGroups NomenclatureGroup
        {
            get => _nomenclatureGroup;
            set
            {
                if (_nomenclatureGroup == value) return;
                _nomenclatureGroup = value;
                OnPropertyChanged();
            }
        }

        public OrderApprovalTypes EquipmentType
        {
            get => _equipmentType;
            set
            {
                if (_equipmentType == value) return;
                _equipmentType = value;
                OnPropertyChanged();
            }
        }

        public decimal? DraftByMemo
        {
            get => _draftByMemo;
            set
            {
                if (_draftByMemo == value) return;
                _draftByMemo = value;
                OnPropertyChanged();
            }
        }

        public string DraftNameByMemo
        {
            get => _draftNameByMemo;
            set
            {
                if (_draftNameByMemo == value) return;
                _draftNameByMemo = value;
                OnPropertyChanged();
            }
        }

        public int? Balance
        {
            get => _balance;
            set
            {
                if (_balance == value) return;
                _balance = value;
                OnPropertyChanged();
            }
        }

        public DateTime? OpenAtByMemo
        {
            get => _openAtByMemo;
            set
            {
                if (_openAtByMemo == value) return;
                _openAtByMemo = value;
                OnPropertyChanged();
            }
        }

        #endregion Переменные и свойства

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged Implementation

        #region Методы обновления

        private void UpdateCooperation()
        {
            try
            {
                OrderApprovalDrafts record = ServiceLocator.DatabaseService.mGetSingle<OrderApprovalDrafts>(
                    rec => rec.OrderApprovalID == OrderApprovalID
                    && rec.EquipmentDraft == EquipmentDraft
                ).Data;

                if (record != null && record.Cooperation != _cooperation)
                {
                    record.Cooperation = _cooperation;
                    Result status = ServiceLocator.DatabaseService.mUpdate(record);
                    ServiceLocator.DatabaseService.mSaveChanges();

                    if (status.IsFailed)
                    {
                        DialogService.HandleResult(status);
                        // Откат значения при ошибке
                        _cooperation = !_cooperation;
                        OnPropertyChanged(nameof(Cooperation));
                    }
                }
            }
            catch (Exception ex)
            {
                _cooperation = !_cooperation;
                OnPropertyChanged(nameof(Cooperation));
                LoggerManager.MainLogger.Error("Ошибка обновления поля 'Кооперация'", ex);
                DialogService.ShowError($"Ошибка обновления поля Кооперация");
            }
        }

        private void UpdateIsDeletedFromOrder()
        {
            try
            {
                OrderApprovalDrafts record = ServiceLocator.DatabaseService.mGetSingle<OrderApprovalDrafts>(
                    rec => rec.OrderApprovalID == OrderApprovalID
                    && rec.EquipmentDraft == EquipmentDraft
                ).Data;

                if (record != null && record.IsDeletedFromOrder != _isDeletedFromOrder)
                {
                    record.IsDeletedFromOrder = _isDeletedFromOrder;
                    Result status = ServiceLocator.DatabaseService.mUpdate(record);
                    ServiceLocator.DatabaseService.mSaveChanges();

                    if (status.IsFailed)
                    {
                        DialogService.HandleResult(status);
                        // Откат значения при ошибке
                        _isDeletedFromOrder = !_isDeletedFromOrder;
                        OnPropertyChanged(nameof(IsDeletedFromOrder));
                    }
                }
            }
            catch (Exception ex)
            {
                _isDeletedFromOrder = !_isDeletedFromOrder;
                OnPropertyChanged(nameof(IsDeletedFromOrder));
                LoggerManager.MainLogger.Error("Ошибка обновления поля 'Удалить из заказа'", ex);
                DialogService.ShowError($"Ошибка обновления поля Удалить из заказа");
            }
        }

        #endregion Методы обновления
    }


    public class TechnologistGroup : INotifyPropertyChanged
    {
        private string Order;
        private ObservableCollection<TechnologicalOrder> _items = new ObservableCollection<TechnologicalOrder>();
        private bool _isByMemo;

        public string Zak1
        {
            get => Order;
            set
            {
                if (Order == value) return;
                Order = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TechnologicalOrder> Items
        {
            get => _items;
            set
            {
                if (_items == value) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public bool IsByMemo
        {
            get => _isByMemo;
            set
            {
                if (_isByMemo == value) return;
                _isByMemo = value;
                OnPropertyChanged();
            }
        }

        private bool _hasRework;
        public bool HasRework
        {
            get => _hasRework;
            set
            {
                _hasRework = value;
                OnPropertyChanged(nameof(HasRework));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}