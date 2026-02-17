using System;
using System.Collections.ObjectModel;

using ModalWindows.ViewModels;
using OrderApprovalSystem.Core.Roles;


namespace OrderApprovalSystem.ViewModels.Modals.Reject
{

    public abstract class BaseRejectModal : InputModalViewModel
    {
        private string _subdivisionRecipient;
        private string _subdivision;
        private string _comment;
        private ObservableCollection<string> _subdivisions;
        private ObservableCollection<string> _subdivisionRecipients;
        private Func<BaseRejectModal, bool> _rejectValidator;

        protected BaseRejectModal(string message, string title) : base(message, title)
        {
            _subdivisionRecipients = new ObservableCollection<string>();
            _subdivisions = new ObservableCollection<string>();
        }

        // Список доступных подразделений
        public ObservableCollection<string> Subdivisions
        {
            get => _subdivisions;
            set
            {
                _subdivisions = value;
                OnPropertyChanged(nameof(Subdivisions));
            }
        }

        // Список доступных получателей
        public ObservableCollection<string> SubdivisionRecipients
        {
            get => _subdivisionRecipients;
            set
            {
                _subdivisionRecipients = value;
                OnPropertyChanged(nameof(SubdivisionRecipients));
            }
        }

        // Подразделение
        public virtual string Subdivision
        {
            get => _subdivision;
            set
            {
                _subdivision = value;
                OnPropertyChanged(nameof(Subdivision));
                UpdateRecipients();
            }
        }

        public string SubdivisionRecipient
        {
            get => _subdivisionRecipient;
            set
            {
                _subdivisionRecipient = value;
                OnPropertyChanged(nameof(SubdivisionRecipient));
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                _comment = value;
                OnPropertyChanged(nameof(Comment));
            }
        }

        // Валидатор для Reject модалки
        public Func<BaseRejectModal, bool> RejectValidator
        {
            get => _rejectValidator;
            set => _rejectValidator = value;
        }

        protected abstract void UpdateRecipients();

        // Переопределяем метод валидации
        protected override bool ValidateBeforeClose()
        {
            // Сбрасываем сообщение об ошибке
            ErrorMessage = string.Empty;

            // Вызываем RejectValidator если он установлен
            if (RejectValidator != null)
            {
                return RejectValidator(this);
            }

            return true; // Если валидатор не установлен - пропускаем
        }
    }

}
