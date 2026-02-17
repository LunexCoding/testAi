using ModalWindows.Commands;
using ModalWindows.ViewModels;
using System;
using System.Collections.ObjectModel;



namespace OrderApprovalSystem.ViewModels.Modals
{

    public class vmApproveTechnologist : InputModalViewModel
    {
        private DateTime? _manufacturingTerm;
        private string _comment;

        public vmApproveTechnologist(string message, string title): base(message, title)
        {

        }

        // Дата выполнения ОТОИП
        public DateTime? ManufacturingTerm
        {
            get => _manufacturingTerm;
            set
            {
                _manufacturingTerm = value;
                OnPropertyChanged(nameof(ManufacturingTerm));
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
    }

}