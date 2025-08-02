using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;


namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Parts_ViewModel</summary>
    public class Parts_ViewModel : ViewModelBase, Parts_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Parts_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Parts_ViewModel(Parts_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            PNTM_AddCommand = new DelegateCommand(CommandPNTM_Add, CanCommandPNTM_Add);
            PNTM_SaveCommand = new DelegateCommand(CommandPNTM_Save, CanCommandPNTM_Save);
            PNTM_DeleteCommand = new DelegateCommand(CommandPNTM_Delete, CanCommandPNTM_Delete);
            PNTM_RefreshCommand = new DelegateCommand(CommandPNTM_Refresh, CanCommandPNTM_Refresh);
            PNTM_SaveAllCommand = new DelegateCommand(CommandPNTM_SaveAll, CanCommandPNTM_SaveAll);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Token

        private AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public AuthenticationToken Token
        {
            get
            {
                if (_Token == null)
                {
                    if (container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                    {
                        _Token = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    }
                }
                return _Token;
            }
        }

        #endregion

        #region LoginContext

        private Login_Context _LoginContext = null;
        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (_LoginContext == null)
                {
                    if (Token != null) _LoginContext = Token.LoginContext;
                }
                return _LoginContext;
            }
        }
        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region PNPT Popup Controls

        #region PNPT_PopupMessage

        private String _PNPT_PopupMessage;

        /// <summary>ViewModel Property: PNPT_PopupMessage of type: String</summary>
        public String PNPT_PopupMessage
        {
            get { return _PNPT_PopupMessage; }
            set
            {
                _PNPT_PopupMessage = value;
                OnPropertyChanged("PNPT_PopupMessage");
            }
        }

        #endregion

        #region PNPT_PopupOpen

        private Boolean _PNPT_PopupOpen;

        /// <summary>ViewModel Property: PNPT_PopupOpen of type: Boolean</summary>
        public Boolean PNPT_PopupOpen
        {
            get { return _PNPT_PopupOpen; }
            set
            {
                _PNPT_PopupOpen = value;
                OnPropertyChanged("PNPT_PopupOpen");
            }
        }

        #endregion

        #region PNPT_PopupColor

        private Brush _PNPT_PopupColor;

        /// <summary>ViewModel Property: PNPT_PopupColor of type: Brush</summary>
        public Brush PNPT_PopupColor
        {
            get { return _PNPT_PopupColor; }
            set
            {
                _PNPT_PopupColor = value;
                OnPropertyChanged("PNPT_PopupColor");
            }
        }

        #endregion

        #endregion


        #region Part Number/Part Type Cross Reference Management


        #region Part_List

        private ObservableCollection<Segway_Part_Type_Xref> _Part_List;

        /// <summary>Property Part_List of type ObservableCollection(Segway_Part_Type_Xref)</summary>
        public ObservableCollection<Segway_Part_Type_Xref> Part_List
        {
            get
            {
                if (_Part_List == null)
                {
                    if (InfrastructureModule.Token == null) return new ObservableCollection<Segway_Part_Type_Xref>();

                    if (InfrastructureModule.Assembly_Table_Parts == null) _Part_List = new ObservableCollection<Segway_Part_Type_Xref>();
                    else
                    {
                        _Part_List = new ObservableCollection<Segway_Part_Type_Xref>(InfrastructureModule.Assembly_Table_Parts.Values);
                    }

                    //List<Segway_Part_Type_Xref> _Part_Types = SART_2012_Web_Service_Client.Select_Segway_Part_Type_Xref_Criteria(InfrastructureModule.Token, null);
                    //if (_Part_Types == null) _Part_List = new ObservableCollection<Segway_Part_Type_Xref>();
                    //else _Part_List = new ObservableCollection<Segway_Part_Type_Xref>(_Part_Types);
                }
                return _Part_List;
            }
            set
            {
                _Part_List = value;
                OnPropertyChanged("Part_List");
            }
        }

        #endregion


        #region Selected_Part

        private Segway_Part_Type_Xref _Selected_Part;

        /// <summary>Property Selected_Part of type Segway_Part_Type_Xref</summary>
        public Segway_Part_Type_Xref Selected_Part
        {
            get { return _Selected_Part; }
            set
            {
                _Selected_Part = value;
                OnPropertyChanged("Selected_Part");
                PNTM_SaveCommand.RaiseCanExecuteChanged();
                PNTM_DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region Part Number/Type Management Commands


        #region PNTM_AddCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PNTM_AddCommand</summary>
        public DelegateCommand PNTM_AddCommand { get; set; }
        private Boolean CanCommandPNTM_Add() { return true; }
        private void CommandPNTM_Add()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Entry_Window ew = new Entry_Window();
                if (ew.ShowDialog() == true)
                {
                    if (String.IsNullOrEmpty(ew.dp_TextBoxText) == false)
                    {
                        String pn = ew.dp_TextBoxText.Replace("-", "");
                        pn = pn.Insert(5, "-");

                        foreach (Segway_Part_Type_Xref xref in Part_List)
                        {
                            if (xref.Service_Part_Number == pn)
                            {
                                PNPT_PopupColor = Brushes.LightGoldenrodYellow;
                                PNPT_PopupMessage = String.Format("Part Numer: {0} already exists", pn);
                                PNPT_PopupOpen = true;
                                return;
                            }
                        }

                        Segway_Part_Type_Xref part = new Segway_Part_Type_Xref()
                        {
                            Service_Part_Number = pn,
                            Date_Time_Entered = DateTime.Now,
                            Date_Time_Updated = DateTime.Now,
                        };

                        part = Manufacturing_SPTX_Web_Service_Client_REST.Insert_Segway_Part_Type_Xref_Key(InfrastructureModule.Token, part);
                        if (part == null)
                        {
                            PNPT_PopupColor = Brushes.Pink;
                            PNPT_PopupMessage = String.Format("An Error occurred while trying to add the new part. Please contact Segway Technical Support for assistance", pn);
                            PNPT_PopupOpen = true;
                            return;
                        }

                        Part_List.Add(part);
                        Selected_Part = part;
                        OnPropertyChanged("Part_List");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region PNTM_SaveCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PNTM_SaveCommand</summary>
        public DelegateCommand PNTM_SaveCommand { get; set; }
        private Boolean CanCommandPNTM_Save() { return Selected_Part != null; }

        private void CommandPNTM_Save()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Selected_Part == null) return;

                if (Manufacturing_SPTX_Web_Service_Client_REST.Update_Segway_Part_Type_Xref_Key(InfrastructureModule.Token, Selected_Part) == false)
                {
                    PNPT_PopupColor = Brushes.Pink;
                    PNPT_PopupMessage = String.Format("An Error occurred while trying to update the selected part: {0}. Please contact Segway Technical Support for assistance", Selected_Part.Service_Part_Number);
                    PNPT_PopupOpen = true;
                    return;
                }

                InfrastructureModule.Assembly_Table_Parts = null;
                PNPT_PopupColor = Brushes.LightGreen;
                PNPT_PopupMessage = String.Format("The selected part ({0}) was successfully updated.", Selected_Part.Service_Part_Number);
                PNPT_PopupOpen = true;
                return;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PNPT_PopupColor = Brushes.Pink;
                PNPT_PopupMessage = msg;
                PNPT_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region PNTM_DeleteCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PNTM_DeleteCommand</summary>
        public DelegateCommand PNTM_DeleteCommand { get; set; }
        private Boolean CanCommandPNTM_Delete() { return Selected_Part != null; }
        private void CommandPNTM_Delete()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Selected_Part == null) return;

                if (Manufacturing_SPTX_Web_Service_Client_REST.Delete_Segway_Part_Type_Xref_Key(InfrastructureModule.Token, Selected_Part.ID) == false)
                {
                    PNPT_PopupColor = Brushes.Pink;
                    PNPT_PopupMessage = String.Format("An Error occurred while trying to delete the selected part: {0}. Please contact Segway Technical Support for assistance", Selected_Part.Service_Part_Number);
                    PNPT_PopupOpen = true;
                    return;
                }
                var store = Selected_Part;
                Part_List.Remove(Selected_Part);
                PNPT_PopupColor = Brushes.LightGreen;
                PNPT_PopupMessage = String.Format("The selected part ({0}) was successfully deleted.", store.Service_Part_Number);
                PNPT_PopupOpen = true;
                return;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PNPT_PopupColor = Brushes.Pink;
                PNPT_PopupMessage = msg;
                PNPT_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }

        }

        /////////////////////////////////////////////
        #endregion


        #region PNTM_RefreshCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PNTM_RefreshCommand</summary>
        public DelegateCommand PNTM_RefreshCommand { get; set; }
        private Boolean CanCommandPNTM_Refresh() { return true; }
        private void CommandPNTM_Refresh()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                var store = Selected_Part;

                List<Segway_Part_Type_Xref> _Part_Types = Manufacturing_SPTX_Web_Service_Client_REST.Select_Segway_Part_Type_Xref_Criteria(InfrastructureModule.Token, null);
                if (_Part_Types == null) Part_List = new ObservableCollection<Segway_Part_Type_Xref>();
                else Part_List = new ObservableCollection<Segway_Part_Type_Xref>(_Part_Types);

                if (store != null)
                {
                    foreach (var xref in Part_List)
                    {
                        if (store.ID == xref.ID)
                        {
                            Selected_Part = xref;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PNPT_PopupColor = Brushes.Pink;
                PNPT_PopupMessage = msg;
                PNPT_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region PNTM_SaveAllCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PNTM_SaveAllCommand</summary>
        public DelegateCommand PNTM_SaveAllCommand { get; set; }
        private Boolean CanCommandPNTM_SaveAll() { return true; }
        private void CommandPNTM_SaveAll()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                List<Segway_Part_Type_Xref> _Part_Types = new List<Segway_Part_Type_Xref>(Part_List);
                if (Manufacturing_SPTX_Web_Service_Client_REST.Update_Segway_Part_Type_Xref_ObjList(InfrastructureModule.Token, _Part_Types) == false)
                {
                    PNPT_PopupColor = Brushes.Pink;
                    PNPT_PopupMessage = String.Format("An Error occurred while trying to save all of the parts.", Selected_Part.Service_Part_Number);
                    PNPT_PopupOpen = true;
                    return;
                }
                PNPT_PopupColor = Brushes.LightGreen;
                PNPT_PopupMessage = String.Format("All of the parts were successfully saved.", Selected_Part.Service_Part_Number);
                PNPT_PopupOpen = true;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String name)
        {
            _Token = null;
            _LoginContext = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        /// <summary>Public Method - IsNavigationTarget</summary>
        /// <param name="navigationContext">NavigationContext</param>
        /// <returns>bool</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        /// <summary>Public Method - OnNavigatedFrom</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

 
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
