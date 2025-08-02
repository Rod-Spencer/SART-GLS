using Segway.Modules.SART_Infrastructure;
using System;
using System.ComponentModel;

namespace Segway.Modules.WorkOrder.Services
{
    public class WorkOrderFilterSettings : INotifyPropertyChanged, IDataErrorInfo
    {
        //private Dictionary<string, WorkOrderStatus> StatusDictionary;

        public WorkOrderFilterSettings()
        {
        }


        #region Properties

        #region PTSerialNumber

        private String _PTSerialNumber;

        /// <summary>Property PTSerialNumber of type String</summary>
        public String PTSerialNumber
        {
            get { return _PTSerialNumber; }
            set
            {
                _PTSerialNumber = value;
                OnPropertyChanged("PTSerialNumber");
            }
        }

        #endregion

        #region WorkOrderNumber

        private String _WorkOrderNumber;

        /// <summary>Property WorkOrderNumber of type String</summary>
        public String WorkOrderNumber
        {
            get { return _WorkOrderNumber; }
            set
            {
                _WorkOrderNumber = SART_Common.Format_Work_Order_ID(value);
                OnPropertyChanged("WorkOrderNumber");
            }
        }

        #endregion

        //#region SelectedWorkOrderStatus

        //private WorkOrderStatus _SelectedWorkOrderStatus;

        ///// <summary>Property SelectedWorkOderStatus of type WorkOrderStatus</summary>
        //public WorkOrderStatus SelectedWorkOrderStatus
        //{
        //    get { return _SelectedWorkOrderStatus; }
        //    set
        //    {
        //        _SelectedWorkOrderStatus = value;
        //        OnPropertyChanged("SelectedWorkOrderStatus");
        //    }
        //}

        //#endregion

        //#region StatusStrings

        //private List<String> _StatusStrings;

        ///// <summary>Property StatusStrings of type List String </summary>
        //public List<String> StatusStrings
        //{
        //    get { return _StatusStrings; }
        //    set
        //    {
        //        _StatusStrings = value;
        //        OnPropertyChanged("StatusStrings");
        //    }
        //}

        //#endregion


        #region SelectedStatusString

        private String _SelectedStatusString;

        /// <summary>Property SelectedStatusString of type String, used in GUI</summary>
        public String SelectedStatusString
        {
            get { return _SelectedStatusString; }
            set
            {
                _SelectedStatusString = value;
                OnPropertyChanged("SelectedStatusString");
            }
        }

        #endregion


        #region UserName

        private String _UserName;

        /// <summary>Property UserName of type String</summary>
        public String UserName
        {
            get { return _UserName; }
            set
            {
                _UserName = value;
                OnPropertyChanged("UserName");
            }
        }

        #endregion

        #region StartDate

        private DateTime? _StartDate;

        /// <summary>Property StartDate of type DateTime</summary>
        public DateTime? StartDate
        {
            get { return _StartDate; }
            set
            {
                _StartDate = value;
                OnPropertyChanged("StartDate");
            }
        }

        #endregion

        #region EndDate

        private DateTime? _EndDate;

        /// <summary>Property EndDate of type DateTime</summary>
        public DateTime? EndDate
        {
            get { return _EndDate; }
            set
            {
                _EndDate = value;
                OnPropertyChanged("EndDate");
            }
        }

        #endregion

        #region GroupID

        private String _GroupID;

        /// <summary>Property GroupID of type String</summary>
        public String GroupID
        {
            get { return _GroupID; }
            set
            {
                _GroupID = value;
                OnPropertyChanged("GroupID");
            }
        }

        #endregion

        #region GroupVisible

        private bool _GroupVisible;

        /// <summary>Property GroupVisible of type bool</summary>
        public bool GroupVisible
        {
            get { return _GroupVisible; }
            set
            {
                _GroupVisible = value;
                OnPropertyChanged("GroupVisible");
            }
        }

        #endregion

        #region GroupOpacity

        private Double _GroupOpacity;

        /// <summary>Property GroupOpacity of type String</summary>
        public Double GroupOpacity
        {
            get { return _GroupOpacity; }
            set
            {
                _GroupOpacity = value;
                OnPropertyChanged("GroupOpacity");
            }
        }

        #endregion


        #endregion


        //public Dictionary<string, WorkOrderStatus> MapStatusEnumerations()
        //{
        //    // make a dictionary to map GUI strings to enums
        //    // call this in constructor:
        //    // var StatusList = MapStatusEnumerations(Filter);
        //    // TO DO: replace this with a single enumeration structure and class def with properties to bind on.
        //    // Good example at:  "http://www.codeproject.com/Articles/301678/Step-by-Step-WPF-Data-Binding-with-Comboboxes"

        //    var dict = new Dictionary<string, WorkOrderStatus> { 
        //        { "Ready To Begin", WorkOrderStatus.ReadyToBegin }, 
        //        { "Work Order Created", WorkOrderStatus.WorkOrderCreated }, 
        //        { "Open For Diagnostics", WorkOrderStatus.OpenForDiagnostics }, 
        //        { "Repair Requested", WorkOrderStatus.RepairRequested },
        //        { "Closed For Approval", WorkOrderStatus.ClosedForApproval },
        //        { "Closed For Decline", WorkOrderStatus.ClosedForDecline },
        //        { "Open For Repair", WorkOrderStatus.OpenForRepair },
        //        { "Repair Completed", WorkOrderStatus.RepairCompleted },
        //        { "Work Order Close Requested", WorkOrderStatus.WorkOrderCloseRequested },
        //        { "Work Order Complete", WorkOrderStatus.WorkOrderComplete }
        //    };

        //    return dict;
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string columnName]
        {
            get { throw new NotImplementedException(); }
        }
    }
}
