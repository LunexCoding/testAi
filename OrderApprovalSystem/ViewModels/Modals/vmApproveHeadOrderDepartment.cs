using ModalWindows.Commands;
using ModalWindows.Entities;
using ModalWindows.ViewModels;
using ModalWindows.Views;
using System;

namespace OrderApprovalSystem.ViewModels.Modals
{
    public class vmApproveHeadOrderDepartment : InputModalViewModel
    {
        private string _comment;

        public vmApproveHeadOrderDepartment(string message, string title) : base(message, title)
        {
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