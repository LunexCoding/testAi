using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

using Fox;
using Fox.Core;
using Fox.Core.Logging;
using Fox.Core.Settings;
using OrderApprovalSystem.Core;
using OrderApprovalSystem.Core.Roles;
using OrderApprovalSystem.Core.Settings;
using OrderApprovalSystem.Services;
using OrderApprovalSystem.ViewModels;
using OrderApprovalSystem.Views;


namespace OrderApprovalSystem
{

    partial class App
    {
        /// <summary>
        /// Обработчик события при загрузке App
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_e"></param>
        void App_OnStartup(object _sender, StartupEventArgs _e)
        {
            try
            {
                SettingsManager.Initialize();
                AppSettingsManager.Initialize();
                AppThemeManager.InitializeThemes();
                LoggerManager.EnableDebugLogging(SettingsManager.BuildConfiguration == "Debug");

                VMLocator.InitializeVMLocator(Assembly.GetExecutingAssembly());

                if (CheckStartUp.CheckRun())
                {
                    ServiceLocator.Initialize();

                    // var userRoles = RoleParser.ParseRoles(CheckStartUp.RolesList);
                    // var userRoles = RoleParser.ParseRoles(new List<string> { "Dev" });
                    User user = new User(CheckStartUp.UserFIO, new List<UserRole>() { UserRole.Dev });
                    RoleManager.Login(user);

                    string name = typeof(Main).Name;
                    int index = VMLocator.CreateViewModel(name);
                    ((Main)VMLocator.VMs[name][index].view).Loaded += ((vmMain)VMLocator.VMs[name][index]).viewLoaded;
                    VMLocator.VMs[name][index].view.Show();
                }
                else Shutdown();
            }
            catch (Exception _ex) 
            { 
                ExceptionHandler.ShowException(_ex, Current); 
            }
        }
    }

}
