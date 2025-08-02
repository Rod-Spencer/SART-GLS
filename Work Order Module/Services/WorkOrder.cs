using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Segway.Service.Common.LoggerHelp;
using System.Reflection;
using Segway.Service.Common;
using Segway.Service.Objects;

namespace Segway.Modules.WorkOrder.Services
{
    public class WorkOrder : INotifyPropertyChanged, IDataErrorInfo
    {
        // keep track of all errors for enabling or disabling commands
        private readonly List<string> errors = new List<string>();
        private static NLog.Logger Logger = LoggerHelper.GetCurrentLogger();

        // AD is being retrieved in the ViewModel's OnNavigatedTo().  Make an instance constructor for the other purposes

        public WorkOrder()
        {
            //AuthenticationDetails currentLogin = GetAuthenticationDetails();
            Logger.Debug("In WorkOrder constructor");
            //Logger.Debug("user: {0}, password: {1}, tool: {2}", currentLogin.UserName, currentLogin.Password, currentLogin.ServiceTool);

        }

                
        // likewise for the work order information that will be stored by this module, but used by other modules.  
        // Or perhaps all we need to store is the work order number, and that can be a simple property added to app.config.
        
        
        # region Properties

        private String _workOrderID;
        public String WorkOrderID
        {
            get { return _workOrderID; }
            set
            {
                _workOrderID = value;
                OnPropertyChanged("WorkOrderID");
            }
        }

        private String _PTSerialNumber;
        public String PTSerialNumber
        {
            get { return _PTSerialNumber; }
            set
            {
                _PTSerialNumber = value;
                OnPropertyChanged("PTSerialNumber");
            }
        }

        // PTModel  {i2, x2, for now} 
        //private PTModel _PTModel;
        //public PTModel PTModel
        //{
        //    get { return _PTModel; }
        //    set
        //    {
        //        _PTModel = value;
        //        OnPropertyChanged("PTModel");
        //    }
        //}

        //// WorkOrderStatus     
        //private WorkOrderStatus _workOrderStatus;
        //public WorkOrderStatus WorkOrderStatus
        //{
        //    get { return _workOrderStatus; }
        //    set
        //    {
        //        _workOrderStatus = value;
        //        OnPropertyChanged("WorkOrderStatus");
        //    }
        //}
        
        // Observations
        private String _observations;
        public String Observations
        {
            get { return _observations; }
            set
            {
                _observations = value;
                OnPropertyChanged("Observations");
            }
        }
        
        // InfoKeyErrorCode     { nullable, user may report issue not displaying }
        private String _infoKeyErrorCode;
        public String InfoKeyErrorCode
        {
            get { return _infoKeyErrorCode; }
            set
            {
                _infoKeyErrorCode = value;
                OnPropertyChanged("InfoKeyErrorCode");
            }
        }

        // GroupID  {dealer, distributor, Segway}
        private String _groupID;
        public String GroupID
        {
            get { return _groupID; }
            set
            {
                _workOrderID = value;
                OnPropertyChanged("GroupID");
            }
        }

        // CreateTimestamp    {only set when create action occurs}
        private DateTime _createTime;
        public DateTime CreateTime
        {
            get { return _createTime; }
            set
            {
                _createTime = value;
                OnPropertyChanged("CreateTime");
            }
        }

        // UpdateTimestamp
        private DateTime _updateTime;
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set
            {
                _updateTime = value;
                OnPropertyChanged("UpdateTime");
            }
        }

        // CreateUser    {AD.UserName when creating new from current login}
        private String _createUser;
        public String CreateUser
        {
            get { return _createUser; }
            set
            {
                _createUser = value;
                OnPropertyChanged("CreateUser");
            }
        }

        // UpdateUser    {AD.UserName when creating new from current login}
        private String _updateUser;
        public String UpdateUser
        {
            get { return _updateUser; }
            set
            {
                _updateUser = value;
                OnPropertyChanged("UpdateUser");
            }
        }

        //private AuthenticationDetails _ad;
        //public AuthenticationDetails AD
        //{
        //    get { return _ad; }
        //    set
        //    {
        //        _ad = value;
        //    }
        //}
        
        # endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion //INotifyPropertyChanged

        #region IDataErrorInfo

        private String _Error;
        public String Error
        {
            get { return _Error; }
            private set
            {
                _Error = value;
                OnPropertyChanged("Error");
            }
        }
        #endregion

        
        

        public String this[string columnName]
        {
            get
            {
                String error = null;

                switch (columnName)
                {
                    //case "UserName":
                    //    if (string.IsNullOrEmpty(_userName))
                    //    {
                    //        error = "A username is required";
                    //        this.AddError("UserNameMissing");
                    //    }
                    //    else
                    //    {
                    //        this.RemoveError("UserNameMissing");
                    //    }
                    //    break;
                    //case "Password":
                    //    if (string.IsNullOrEmpty(_password))
                    //    {
                    //        error = "A password is required";
                    //        this.AddError("PasswordMissing");
                    //    }
                    //    else
                    //    {
                    //        this.RemoveError("PasswordMissing");
                    //    }
                    //    break;
                }
                Error = error;
                return (Error);
            }
        }

        private void AddError(String ruleName)
        {
            if (!this.errors.Contains(ruleName))
            {
                this.errors.Add(ruleName);
            }
        }

        private void RemoveError(String ruleName)
        {
            if (this.errors.Contains(ruleName))
            {
                this.errors.Remove(ruleName);
            }
        }

        public List<String> Errors
        {
            get { return errors; }
        }

        private bool CanSubmit(object parameter)
        {
            return this.errors.Count == 0;
        }

        //public static AuthenticationDetails GetAuthenticationDetails()
        //{
        //    String classClientString = Application_Helper.GetConfigurationValue("ClientClass");
        //    String baseclassClientString = Application_Helper.GetConfigurationValue("ClientBaseClass");
        //    String classAssemblyString = Application_Helper.GetConfigurationValue("ClientClassAssembly");
        //    String namespaceString = Application_Helper.GetConfigurationValue("ClientClassNameSpace");
        //    Reflection_Helper.Invoke(classAssemblyString, namespaceString, classClientString, "Initialize", null);

        //    //String toolstr = Application_Helper.GetConfigurationValue("ToolName");
        //    Logger.Debug("Retrieving AuthenticationDetails information from global class");

        //    classAssemblyString = Application_Helper.GetConfigurationValue("AuthenticationStaticAssembly");
        //    namespaceString = Application_Helper.GetConfigurationValue("AuthenticationStaticNameSpace");
        //    classClientString = Application_Helper.GetConfigurationValue("AuthenticationStaticClass");
        //    String fieldName = Application_Helper.GetConfigurationValue("AuthenticationStaticField");

        //    //Reflection_Helper.SetValueStaticProperty(classAssemblyString, namespaceString, classClientString, fieldName, ad);

        //    AuthenticationDetails ad = (AuthenticationDetails)Reflection_Helper.GetValueStaticProperty(classAssemblyString, namespaceString, classClientString, fieldName);
        //    Logger.Debug("Returned AD data - user: {0}, password: {1}, tool: {2}", ad.UserName, ad.Password, ad.ServiceTool);
        //    //AuthenticationDetails ad = new AuthenticationDetails(Login.UserName, Login.Password, tool);
            
        //    return ad;
        //}

    }
    
}
