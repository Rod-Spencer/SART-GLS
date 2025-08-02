using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;


namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Model_ViewModel</summary>
    public class Model_ViewModel : ViewModelBase, Model_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Model_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Model_ViewModel(Model_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            ChangeModelCommand = new DelegateCommand(CommandChangeModel, CanCommandChangeModel);

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


        #region Models

        private List<Items> _Models;

        /// <summary>Property Models of type List&lt;Items&gt;</summary>
        public List<Items> Models
        {
            get
            {
                if (_Models == null) _Models = Syteline_Items_Web_Service_Client_REST.Select_Gen2_Models(Token);
                return _Models;
            }
            set { _Models = value; }
        }

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region Change Model Popup Controls

        #region Change_Model_PopupMessage

        private String _Change_Model_PopupMessage;

        /// <summary>ViewModel Property: Change_Model_PopupMessage of type: String</summary>
        public String Change_Model_PopupMessage
        {
            get { return _Change_Model_PopupMessage; }
            set
            {
                _Change_Model_PopupMessage = value;
                OnPropertyChanged("Change_Model_PopupMessage");
            }
        }

        #endregion

        #region Change_Model PopupOpen

        private Boolean _Change_Model_PopupOpen;

        /// <summary>ViewModel Property: Change_Model_PopupOpen of type: Boolean</summary>
        public Boolean Change_Model_PopupOpen
        {
            get { return _Change_Model_PopupOpen; }
            set
            {
                _Change_Model_PopupOpen = value;
                OnPropertyChanged("Change_Model_PopupOpen");
            }
        }

        #endregion

        #region Change_Model PopupColor

        private Brush _Change_Model_PopupColor;

        /// <summary>ViewModel Property: Change_Model_PopupColor of type: Brush</summary>
        public Brush Change_Model_PopupColor
        {
            get { return _Change_Model_PopupColor; }
            set
            {
                _Change_Model_PopupColor = value;
                OnPropertyChanged("Change_Model_PopupColor");
            }
        }

        #endregion

        #endregion


        #region Change Model Controls

        #region PT_Serial_Number

        private String _PT_Serial_Number;

        /// <summary>Property PT_Serial_Number of type String</summary>
        public String PT_Serial_Number
        {
            get { return _PT_Serial_Number; }
            set
            {
                try
                {
                    logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                    _PT_Serial_Number = value;
                    if (Serial_Validation.Is_PT_Serial(_PT_Serial_Number) == true)
                    {
                        var unit = Syteline_Units_Web_Service_Client_REST.Select_FS_Unit_SER_NUM(Token, _PT_Serial_Number);
                        if (unit == null)
                        {
                            Change_Model_PopupMessage = String.Format("Unable to find unit: {0}", _PT_Serial_Number);
                            Change_Model_PopupColor = Brushes.LightGoldenrodYellow;
                            Change_Model_PopupOpen = true;

                            Current_Part_Number = "";
                            Current_Description = "";
                        }
                        else
                        {
                            Current_Part_Number = unit.Item;
                            if (String.IsNullOrEmpty(unit.Description) == false)
                            {
                                Current_Description = unit.Description;
                            }
                            else
                            {
                                foreach (var model in Models)
                                {
                                    if (model.Item == unit.Item)
                                    {
                                        Current_Description = model.Description;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Current_Description = null;
                        Current_Part_Number = null;
                        Selected_Model = null;
                    }
                }
                catch (Exception ex)
                {
                    String msg = Exception_Helper.FormatExceptionString(ex);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                }
                finally
                {
                    OnPropertyChanged("PT_Serial_Number");
                    ChangeModelCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion


        #region Current_Part_Number

        private String _Current_Part_Number;

        /// <summary>Property Current_Part_Number of type String</summary>
        public String Current_Part_Number
        {
            get { return _Current_Part_Number; }
            set
            {
                _Current_Part_Number = value;
                OnPropertyChanged("Current_Part_Number");
            }
        }

        #endregion


        #region Current_Description

        private String _Current_Description;

        /// <summary>Property Current_Description of type String</summary>
        public String Current_Description
        {
            get { return _Current_Description; }
            set
            {
                _Current_Description = value;
                OnPropertyChanged("Current_Description");
            }
        }

        #endregion


        #region Model_List

        private ObservableCollection<Items> _Model_List;

        /// <summary>Property Model_List of type ObservableCollection&lt;SLItems&gt;</summary>
        public ObservableCollection<Items> Model_List
        {
            get
            {
                if (_Model_List == null)
                {
                    if (Token != null)
                    {
                        Models = Syteline_Items_Web_Service_Client_REST.Select_Gen2_Models(Token);
                        if (Models == null) _Model_List = new ObservableCollection<Items>();
                        else _Model_List = new ObservableCollection<Items>(Models);
                    }
                }
                return _Model_List;
            }
            set
            {
                _Model_List = value;
                OnPropertyChanged("Model_List");
            }
        }

        #endregion


        #region Selected_Model

        private Items _Selected_Model;

        /// <summary>Property Selected_Model of type SLItems</summary>
        public Items Selected_Model
        {
            get { return _Selected_Model; }
            set
            {
                _Selected_Model = value;
                OnPropertyChanged("Selected_Model");
                ChangeModelCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers


        #region ChangeModelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ChangeModelCommand</summary>
        public DelegateCommand ChangeModelCommand { get; set; }
        private Boolean CanCommandChangeModel()
        {
            if (String.IsNullOrEmpty(PT_Serial_Number) == true) return false;
            return Selected_Model != null;
        }

        private void CommandChangeModel()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread back = new Thread(new ThreadStart(CommandChangeModel_Back));
                back.IsBackground = true;
                back.Start();
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Change_Model_PopupColor = Brushes.Pink;
                Change_Model_PopupMessage = msg;
                Change_Model_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String obj)
        {
            if (Token != null)
            {
                var models = Syteline_Items_Web_Service_Client_REST.Select_Gen2_Models(Token);

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    if (models == null) _Model_List = new ObservableCollection<Items>();
                    else _Model_List = new ObservableCollection<Items>(models);
                });
            }
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
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(false);
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            OnPropertyChanged("Model_List");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void CommandChangeModel_Back()
        {

            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);


                //logger.Debug("Retrieving Syteline Item - {0}", Selected_Model.Item);
                //SLItems item = SART_2012_Web_Service_Client.Get_Syteline_Item(Token, Selected_Model.Item);
                //if (item == null)
                //{
                //    String msg = String.Format("Unable to retrieve Syteline Item: {0}", Selected_Model.Item);
                //    logger.Error(msg);
                //    Change_Model_PopupColor = Brushes.Pink;
                //    Change_Model_PopupMessage = msg;
                //    Change_Model_PopupOpen = true;
                //    return;
                //}
                //else if (String.IsNullOrEmpty(item.Charfld4) == true)
                //{
                //    String msg = String.Format("Item: {0} does not have the flight code indicator", Selected_Model.Item);
                //    logger.Warn(msg);
                //    Change_Model_PopupColor = Brushes.LightGoldenrodYellow;
                //    Change_Model_PopupMessage = msg;
                //    Change_Model_PopupOpen = true;
                //    return;
                //}

                logger.Debug("Retrieving Security records - {0}", PT_Serial_Number);
                var secList = Manufacturing_Security_Web_Service_Client_REST.Select_Security_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PT_Serial_Number);
                if ((secList == null) || (secList.Count == 0))
                {
                    String msg = String.Format("No Security record was found for Unit: {0}", PT_Serial_Number);
                    logger.Warn(msg);
                    Change_Model_PopupColor = Brushes.LightGoldenrodYellow;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }

                /////////////////////////////////////////////////////////////////////////////////////
                // Update Security Table
                Security sec = new Security();
                foreach (Security S in secList)
                {
                    sec.Update(S);
                }
                if (sec.Unit_ID_Part_Number != Selected_Model.Charfld4)
                {
                    sec.Unit_ID_Part_Number = Selected_Model.Charfld4;
                    sec.ID = 0;
                    logger.Debug("Adding Security record");
                    if (Manufacturing_Security_Web_Service_Client_REST.Insert_Security_Object(InfrastructureModule.Token, sec) == null)
                    {
                        String msg = String.Format("Unable to update Security table for Unit: {0}", PT_Serial_Number);
                        logger.Error(msg);
                        Change_Model_PopupColor = Brushes.Pink;
                        Change_Model_PopupMessage = msg;
                        Change_Model_PopupOpen = true;
                        return;
                    }
                }
                // Update Security Table
                /////////////////////////////////////////////////////////////////////////////////////


                /////////////////////////////////////////////////////////////////////////////////////
                // Update Stage1_Part_Number Table
                logger.Debug("Retrieving Stage1 Part Number records - {0}", PT_Serial_Number);
                var partList = Manufacturing_S1PN_Web_Service_Client_REST.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PT_Serial_Number);
                if ((partList == null) || (partList.Count == 0))
                {
                    String msg = String.Format("No \"Part Number\" record was found for Unit: {0}", PT_Serial_Number);
                    logger.Warn(msg);
                    Change_Model_PopupColor = Brushes.LightGoldenrodYellow;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }

                partList = partList.OrderBy(x => x.Date_Time_Entered).ToList();
                Stage1_Partnum part = new Stage1_Partnum();
                foreach (var p in partList)
                {
                    part.Update(p);
                }
                if (part.Unit_ID_Partnumber != Selected_Model.Charfld4)
                {
                    part.Unit_ID_Partnumber = Selected_Model.Charfld4;
                    logger.Debug("Adding Stage1 Part Number record");
                    if (Manufacturing_S1PN_Web_Service_Client_REST.Insert_Stage1_Partnum_Object(InfrastructureModule.Token, part) == null)
                    {
                        String msg = String.Format("Unable to update Stage1_PartNum table for Unit: {0}", PT_Serial_Number);
                        logger.Error(msg);
                        Change_Model_PopupColor = Brushes.Pink;
                        Change_Model_PopupMessage = msg;
                        Change_Model_PopupOpen = true;
                        return;
                    }
                }
                // Update Stage1_Part_Number Table
                /////////////////////////////////////////////////////////////////////////////////////

                /////////////////////////////////////////////////////////////////////////////////////
                // Update Syteline Unit
                logger.Debug("Retrieving Unit record - {0}/{1}", PT_Serial_Number, Current_Part_Number);
                var unit = Syteline_Units_Web_Service_Client_REST.Select_FS_Unit_SER_NUM(Token, PT_Serial_Number, Current_Part_Number);
                if (unit == null)
                {
                    String msg = String.Format("Unable to retrieve Syteline Unit: {0} ({1})", PT_Serial_Number, Current_Part_Number);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }

                if (unit.Item != Selected_Model.Item)
                {
                    logger.Debug("Retrieving Item record - {0}", Selected_Model.Item);
                    unit.Item = Selected_Model.Item;
                    unit.Description = Selected_Model.Description;
                    if (Syteline_Units_Web_Service_Client_REST.Update_FS_Unit_Object(InfrastructureModule.Token, unit) == false)
                    {
                        String msg = String.Format("Unable to update Syteline Unit: {0} ({1})", PT_Serial_Number, Selected_Model.Item);
                        logger.Error(msg);
                        Change_Model_PopupColor = Brushes.Pink;
                        Change_Model_PopupMessage = msg;
                        Change_Model_PopupOpen = true;
                        return;
                    }
                }
                // Update Syteline Unit
                /////////////////////////////////////////////////////////////////////////////////////


                /////////////////////////////////////////////////////////////////////////////////////
                // Update Syteline Serial
                logger.Debug("Adding Serial record - {0}/{1}", PT_Serial_Number, Selected_Model.Item);
                Serial s = new Serial() { Ser_Num = PT_Serial_Number, Item = Selected_Model.Item, Created_Date = DateTime.Now, Created_BY = LoginContext.UserName, Updated_BY = LoginContext.UserName };
                if (Syteline_Serial_Web_Service_Client_REST.Insert_Serial_Object(InfrastructureModule.Token, s) == null)
                {
                    String msg = String.Format("Unable to add Syteline Serial for: {0} ({1})", PT_Serial_Number, Selected_Model.Item);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }
                // Update Syteline Serial
                /////////////////////////////////////////////////////////////////////////////////////


                /////////////////////////////////////////////////////////////////////////////////////
                // Update Production Line Assembly table
                logger.Debug("Retrieving Production Line Assembly record - {0}", PT_Serial_Number);
                var parent = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, PT_Serial_Number);
                if (parent == null)
                {
                    String msg = String.Format("Unable to retrieve Production Unit: {0}", PT_Serial_Number);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }

                logger.Debug("Terminating Production Line Assembly record - {0}/{1}", parent.ID, parent.Serial_Number);
                parent.End_Date = DateTime.Now;
                if (Manufacturing_PLA_Web_Service_Client_REST.Update_Production_Line_Assembly_Object(InfrastructureModule.Token, parent) == false)
                {
                    String msg = String.Format("Unable to update/terminate Production Unit: {0}", PT_Serial_Number);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }


                parent.End_Date = null;
                parent.ID = 0;
                parent.Part_Number = InfrastructureModule.Service_Part_Numbers_XRef[Selected_Model.Item];
                parent.Model = InfrastructureModule.Part_Numbers_Models[parent.Part_Number].Model;

                logger.Debug("Adding new Production Line Assembly record - {0}/{1}/{2}", parent.Serial_Number, parent.Part_Number, parent.Model);
                parent = Manufacturing_PLA_Web_Service_Client_REST.Insert_Production_Line_Assembly_Object(InfrastructureModule.Token, parent);
                if (parent == null)
                {
                    String msg = String.Format("Unable to insert Production Unit: {0} - New part number", PT_Serial_Number);
                    logger.Error(msg);
                    Change_Model_PopupColor = Brushes.Pink;
                    Change_Model_PopupMessage = msg;
                    Change_Model_PopupOpen = true;
                    return;
                }



                logger.Debug("Retrieving children Production Line Assembly record - {0}/{1}", parent.ID, parent.Serial_Number);
                var plaChildren = Manufacturing_PLA_Web_Service_Client_REST.Select_Production_Line_Assembly_PARENT_SERIAL_Active(InfrastructureModule.Token, parent.Serial_Number);

                logger.Debug("Updating children records to point to new parent record");
                foreach (var child in plaChildren)
                {
                    child.Parent_ID = parent.ID;
                    child.Parent_Serial = parent.Serial_Number;
                    if (Manufacturing_PLA_Web_Service_Client_REST.Update_Production_Line_Assembly_Object(InfrastructureModule.Token, child) == false)
                    {
                        String msg = String.Format("Unable to remove child assembly: {0} ({1})", child.Serial_Number, child.Part_Type);
                        logger.Error(msg);
                        Change_Model_PopupColor = Brushes.Pink;
                        Change_Model_PopupMessage = msg;
                        Change_Model_PopupOpen = true;
                        return;
                    }
                }

                logger.Debug("Retrieving children Production Line Assembly record - {0}/{1}", parent.ID, parent.Serial_Number);
                plaChildren = Manufacturing_PLA_Web_Service_Client_REST.Select_Production_Line_Assembly_MASTER_SERIAL_Active(InfrastructureModule.Token, parent.Serial_Number);

                logger.Debug("Updating descendancy records to point to new master record");
                foreach (var child in plaChildren)
                {
                    child.Master_ID = parent.ID;
                    child.Master_Serial = parent.Serial_Number;
                    if (Manufacturing_PLA_Web_Service_Client_REST.Update_Production_Line_Assembly_Object(InfrastructureModule.Token, child) == false)
                    {
                        String msg = String.Format("Unable to remove child assembly: {0} ({1})", child.Serial_Number, child.Part_Type);
                        logger.Error(msg);
                        Change_Model_PopupColor = Brushes.Pink;
                        Change_Model_PopupMessage = msg;
                        Change_Model_PopupOpen = true;
                        return;
                    }
                }


                String m = String.Format("Successfully converted the model of unit: {0} from: {1} to: {2}", PT_Serial_Number, Current_Part_Number, Selected_Model.Item);
                logger.Debug(m);
                Change_Model_PopupColor = Brushes.LightGreen;
                Change_Model_PopupMessage = m;
                Change_Model_PopupOpen = true;
                return;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Change_Model_PopupColor = Brushes.Pink;
                Change_Model_PopupMessage = msg;
                Change_Model_PopupOpen = true;
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
