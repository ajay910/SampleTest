using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DSNY.Novas.Common;
using DSNY.Novas.Models;
using DSNY.Novas.Services;
using DSNY.Novas.ViewModels.Interfaces;
using DSNY.Novas.ViewModels.Utils;

namespace DSNY.Novas.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged, ILoadableViewModel, IVisibilityAwareViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected INavigationService NavigationService;
        protected IAlertService AlertService;
        protected readonly INovService NovService;
        protected readonly IVehicleService VehicleService;
        protected readonly IPlaceOfOccurrenceService PlaceOfOccurrenceService;
        private bool _isCancelled;
        private bool _isVoidAction;

        protected DateTime LoginTimestamp { get; set; }

        public static DateTime LastPropertyChangeTime { get; set; }
        protected static bool NeedNotCheckAutoLogOff { get; set; }
        private int AutomaticLogOffTime = 2 * 60;
        protected bool IsForgroundScreen { get; set; }
        protected static ConcreteWorkflowFactory ConcreteWorkflowFactory;

        protected bool NavigationInProgress { get; set; }

        static ViewModelBase()
        {
            ConcreteWorkflowFactory = new ConcreteWorkflowFactory();
        }

        public ViewModelBase()
        {
            NavigationService = DependencyResolver.Get<INavigationService>();
            AlertService = DependencyResolver.Get<IAlertService>();
            NovService = DependencyResolver.Get<INovService>();
            VehicleService = DependencyResolver.Get<IVehicleService>();
            PlaceOfOccurrenceService = DependencyResolver.Get<IPlaceOfOccurrenceService>();
            AutomaticLogOff();
        }

        public virtual string Title => "";

        public NovMaster NovMaster { get; set; }
        public UserSession UserSession { get; set; }

        public AffidavitOfService AffidavitOfService => NovMaster?.AffidavitOfService;
        public NovInformation NovInformation => NovMaster?.NovInformation;
        public long? NovNumber => NovInformation?.NovNumber;

        public bool IsCancelled
        {
            get => _isCancelled;
            set { _isCancelled = value; NotifyPropertyChanged(nameof(Title)); }
        }

        public bool IsVoidAction
        {
            get => _isVoidAction;
            set { _isVoidAction = value; NotifyPropertyChanged(nameof(Title)); }
        }

        public virtual ICommand BackCommand => new Command(async () =>
        {
            if (NavigationInProgress) { return; }
            NavigationInProgress = true;

            if (UserSession != null)
            {
                UserSession.TimeoutTimeStamp = DateTime.Now;
            }
            else if (NovMaster?.UserSession != null)
            {
                NovMaster.UserSession.TimeoutTimeStamp = DateTime.Now;
            }

            WriteFieldValuesToNovMaster();
            WriteValueToCrossSettings();
            await NavigationService.PopAsync();
            NavigationInProgress = false;
            //NovMaster.NovInformation.LockPlaceOfOccurrenceScreen = true;
        });

        public virtual ICommand NextCommand => new Command(async () =>
        {
            if (NavigationInProgress) { return; }
            NavigationInProgress = true;

            var alerts = await ValidateScreen();
            foreach (AlertViewModel alert in alerts)
            {
                if (!await AlertService.DisplayAlert(alert) || !alert.ShouldContinueOnOk)
                {
                    NavigationInProgress = false;
                    return;
                }
            }

            if (UserSession != null)
            {
                UserSession.TimeoutTimeStamp = DateTime.Now;
            }
            else if (NovMaster?.UserSession != null)
            {
                NovMaster.UserSession.TimeoutTimeStamp = DateTime.Now;
            }

            WriteFieldValuesToNovMaster();
            WriteValueToCrossSettings();
            if (ShouldSaveTicketOnNext)
            {
                await SaveTicket();
            }

            if (ShouldLockPreviousScreensOnNext)
            {
                NovMaster.NovInformation.LockPreviousScreens = true;
            }

            if (ShouldLockPlaceOfOccurrenceOnNext)
            {
                NovMaster.NovInformation.LockPlaceOfOccurrenceScreen = true;
            }

            var nextVM = NextViewModel;
            if (nextVM != null)
            {
                await NavigationService.PushAsync(nextVM);
            }

            NavigationInProgress = false;


        });

        public virtual Task<List<AlertViewModel>> ValidateScreen()
        {
            return Task.FromResult(new List<AlertViewModel>());
        }

        public virtual ViewModelBase NextViewModel
        {
            get
            {
                var locator = ConcreteWorkflowFactory.GetViewModel(NovMaster.ViolationGroup.TypeName);
                var nextVM = locator.NextViewModel(this.GetType().Name, NovMaster);

                return nextVM;
            }
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                OnPropertyChanged(args);
                PropertyChanged(this, args);
                LastPropertyChangeTime = DateTime.Now;
            }
        }

        protected void RaiseCanExecuteChanged(ICommand command)
        {
            var changedCommand = command as Command;

            if (changedCommand != null)
            {
                changedCommand.ChangeCanExecute();
            }
        }

        protected async Task<bool> DisplayAlert(string errorNumber, string workflowMessage, string okAction = "Ok")
        {
            AlertViewModel vm = new AlertViewModel(errorNumber, workflowMessage, okAction);
            await AlertService.DisplayAlert(vm);

            return false;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            // Fill in any behavior that needs to happen for all Property Changed events
        }

        public virtual void OnAppearing()
        {
            IsForgroundScreen = true;
            // Can be overriden in a derived class to add behavior when a view becomes active
        }

        public virtual void OnDisappearing()
        {
            IsForgroundScreen = false;
            // Can be overriden in a derived class to add behavior when a view becomes inactive
        }

        // Can be overriden in a derived class to add loading behavior when a view becomes active
        public virtual Task LoadAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void WriteFieldValuesToNovMaster()
        {
            // Override in derived classes
        }
        public virtual void WriteValueToCrossSettings()
        {
            // Override in derived classes
        }

        public virtual bool ShouldSaveTicketOnNext => true;

        public virtual bool ShouldLockPreviousScreensOnNext => false;

        public virtual bool ShouldLockPlaceOfOccurrenceOnNext => false;

        public async Task SaveTicket()
        {
            try
            {
                await NovService.SaveViolation(NovMaster);
            }
            catch (Exception exc)
            {
                MessageCenter.Send("Error saving violation.");
            }
        }

        public virtual bool ShowBackButton => true;

        public virtual bool ShowCancelButton => false;

        public virtual bool ShowNextButton => true;

        public virtual bool ShowConfirmButton => false;

        public virtual bool ShowPlusButton => false;

        public virtual ICommand PlusCommand => null;

        public virtual bool ShowMenuButton => true;

        public virtual List<string> MenuItems
        {
            get
            {
                var menuItems = new List<string>();
                if (ScreenSpecificMenuItems != null)
                {
                    menuItems.AddRange(ScreenSpecificMenuItems);
                    menuItems.Add("——————————");
                }

                if (ShowHelpMenu) { menuItems.AddRange(HelpMenuItems); }
                if (ShowActionMenu) { menuItems.AddRange(ActionMenuItems); }
                if (ShowCancelMenu) { menuItems.AddRange(CancelMenuItems); }

                return menuItems;
            }
        }

        public virtual ICommand MenuItemTappedCommand
        {
            get { return new Command(MenuItemTapped); }
        }

        public virtual bool ShowHelpMenu
        {
            get { return true; }
        }

        public virtual List<string> ScreenSpecificMenuItems
        {
            get { return null; }
        }

        public virtual List<string> HelpMenuItems
        {
            get { return new List<string> { "NOVAS Help", "About NOVAS" }; }
        }

        public virtual bool ShowActionMenu
        {
            get { return true; }
        }

        public virtual List<string> ActionMenuItems
        {
            get { return new List<string> { "Device Status" }; }
        }

        public virtual bool ShowCancelMenu
        {
            get
            {
                if (NovMaster != null && NovMaster.NovInformation?.TicketStatus != "C" && NovMaster.NovInformation?.TicketStatus != "V" && !IsCancelled && !IsVoidAction)
                {
                    return true;
                }
                return false;
            }
        }

        public virtual List<string> CancelMenuItems
        {
            get
            {
                if (NovMaster != null && (NovMaster.NovInformation != null ? !NovMaster.NovInformation.VoidSet : false))
                {
                    return new List<string> { "Cancel" };
                }
                return new List<string> { "Void" };
            }
        }

        public async virtual void MenuItemTapped(object item)
        {
            if ((string)item == "Log Off")
            {
                var alert = new AlertViewModel("11001", WorkFlowMessages.DSNYMSG_06_ConfirmLogOff, okTitle: "Yes", okAction: LogOff, cancelTitle: "No");
                await AlertService.DisplayAlert(alert);
            }
            else if ((string)item == "Cancel")
            {
                var alert = new AlertViewModel("12001", WorkFlowMessages.DSNYMSG_ConfirmCancel, okTitle: "Yes", okAction: Cancel, cancelTitle: "No");
                await AlertService.DisplayAlert(alert);
            }
            else if ((string)item == "Void")
            {
                var alert = new AlertViewModel("12002", WorkFlowMessages.DSNYMSG_ConfirmVoid, okTitle: "Yes", okAction: VoidAction, cancelTitle: "No");
                await AlertService.DisplayAlert(alert);
            }
            else if ((string)item == "About NOVAS")
            {
                AboutAction();
            }
            else if ((string)item == "Device Status")
            {
                DeviceStatusAction();
            }
            else if ((string)item == "NOVAS Help")
            {
                NovasHelp();
            }
            else if ((string)item == "Pin Tool")
            {
                PinTool();
            }
            else if ((string)item == "Main Page")
            {
                MainPage();
            }
        }

        public async Task AutomaticLogOff()
        {
            if (GetType() != typeof(LoginViewModel))
            {
                while (!NeedNotCheckAutoLogOff)
                {
                    await Task.Delay(10000);

                    if (IsForgroundScreen == true)
                    {
                        if (DateTime.Now.Subtract(LastPropertyChangeTime).TotalSeconds > AutomaticLogOffTime)
                        {
                            if (!NeedNotCheckAutoLogOff)
                            {
                                var alert = new AlertViewModel("11001", WorkFlowMessages.DSNYMSG_06_ConfirmLogOff, okTitle: "Yes", okAction: LogOff, cancelTitle: "No", cancelAction: LogOffCancel);
                                await AlertService.DisplayAlert(alert);
                            }

                            //NeedNotCheckAutoLogOff = true;
                        }
                    }

                }
            }
        }

        public virtual async void LogOffCancel()
        {
            LastPropertyChangeTime = DateTime.Now;
        }

        public virtual async void LogOff()
        {
            if (NavigationInProgress) { return; }
            NavigationInProgress = true;

            var userSession = UserSession ?? NovMaster?.UserSession;
            NeedNotCheckAutoLogOff = true;
            bool existsData = await VehicleService.isVehicleCurrent(userSession.UserId);
            if (existsData)
            {
                VehicleDataViewModel vm = new VehicleDataViewModel(existsData) { UserSession = userSession, IsLogOff = true };
                await NavigationService.PushModalAsync(vm);
                //User Session is set to null inside the Vehicle/Radio ViewModel
            }
            else
            {
                await NavigationService.PopToLoginScreenAsync();
                UserSession = null;
            }

            NavigationInProgress = false;
        }

        public virtual void Cancel()
        {
            IsCancelled = true;
        }

        public virtual void VoidAction()
        {
            //Need to override void in PoO and Violation Details
            IsVoidAction = true;
        }

        public virtual async void AboutAction()
        {
            await NavigationService.PushModalAsync(new AboutNovasViewModel()
            {
                UserSession = NovMaster?.UserSession ?? UserSession
            });
        }

        public virtual async void PinTool()
        {
            await NavigationService.PushModalAsync(new SetPinViewModel()
            {
                UserSession = NovMaster?.UserSession ?? UserSession
            });
        }

        public virtual async void MainPage()
        {
            await NavigationService.PushModalAsync(new MainPageViewModel()
            {
                UserSession = NovMaster?.UserSession ?? UserSession
            });
        }

        public virtual async void DeviceStatusAction()
        {
            await NavigationService.PushModalAsync(new DeviceStatusViewModel()
            {
                UserSession = NovMaster?.UserSession ?? UserSession
            });
        }

        public virtual async void NovasHelp()
        {
            await NavigationService.PushModalAsync(new HelpViewModel()
            {
                UserSession = NovMaster?.UserSession ?? UserSession
            });
        }

        public async Task SaveViolator()
        {
            try
            {
                await PlaceOfOccurrenceService.SaveViolator(NovMaster);
            }
            catch (Exception exc)
            {
                MessageCenter.Send("Error saving violator");
            }
        }

        public static ViewModelBase GetViewModelBase(string viewModelName)
        {
            var t = Type.GetType(viewModelName);

            if (t == null)
                return null;

            try
            {
                var instance = Activator.CreateInstance(t);

                return (ViewModelBase)instance;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
