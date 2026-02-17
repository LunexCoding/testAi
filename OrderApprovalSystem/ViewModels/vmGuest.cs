using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fox;
using OrderApprovalSystem.Models;
using OrderApprovalSystem.Views;

namespace OrderApprovalSystem.ViewModels
{
    public class vmGuest : VMBase
    {
        public Guest View;
        public mGuest Model;

        public vmGuest()
        {

        }

        public void viewLoaded(object _sender, RoutedEventArgs _routedEventArgs)
        {
            View = (Guest)view;
            Model = (mGuest)model;

            Model.PropertyChanged += modelPropertyChangedHandler;
        }

        public void modelPropertyChangedHandler(object _sender, PropertyChangedEventArgs _eventArgs)
        {

        }
    }
}
