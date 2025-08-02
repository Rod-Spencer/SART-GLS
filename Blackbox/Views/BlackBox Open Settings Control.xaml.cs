using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Segway.Service.SART
{
    /// <summary>
    /// Interaction logic for C:\Code\Branches\Black Box\sart\implementation\SART2012\Blackbox\Views\Blackbox Open Settings Control.xaml.cs.xaml
    /// </summary>
    public partial class BlackBox_Open_Settings_Control : UserControl
    {
        /// <summary>Public Member - Control_Name</summary>
        public static String Control_Name = "BlackBox_Open_Settings_Control";

        /// <summary>Public Contructor - BlackBox_Open_Settings_Control</summary>
        public BlackBox_Open_Settings_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }

        //public IViewModel ViewModel
        //{
        //    get { return (IViewModel)DataContext; }
        //    set { DataContext = value; }
        //}


        #region dpWorkOrder

        /// <summary>Public Member - dpWorkOrderProperty</summary>
        public static DependencyProperty dpWorkOrderProperty = DependencyProperty.Register("dpWorkOrder", typeof(String), typeof(BlackBox_Open_Settings_Control),
                  new FrameworkPropertyMetadata((String)null, new PropertyChangedCallback(OndpWorkOrderChanged)));

        /// <summary>Property dpWorkOrder of type String</summary>
        public String dpWorkOrder
        {
            get { return (String)GetValue(dpWorkOrderProperty); }
            set { SetValue(dpWorkOrderProperty, value); }
        }

        private static void OndpWorkOrderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlackBox_Open_Settings_Control propOwner = (BlackBox_Open_Settings_Control)sender;
            String newdpWorkOrder = (String)e.NewValue;
            propOwner.dpWorkOrder = newdpWorkOrder;
        }

        #endregion


        #region dpPTSerial

        /// <summary>Public Member - dpPTSerialProperty</summary>
        public static DependencyProperty dpPTSerialProperty = DependencyProperty.Register("dpPTSerial", typeof(String), typeof(BlackBox_Open_Settings_Control),
                  new FrameworkPropertyMetadata((String)null, new PropertyChangedCallback(OndpPTSerialChanged)));

        /// <summary>Property dpPTSerial of type String</summary>
        public String dpPTSerial
        {
            get { return (String)GetValue(dpPTSerialProperty); }
            set { SetValue(dpPTSerialProperty, value); }
        }

        private static void OndpPTSerialChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlackBox_Open_Settings_Control propOwner = (BlackBox_Open_Settings_Control)sender;
            String newdpPTSerial = (String)e.NewValue;
            propOwner.dpPTSerial = newdpPTSerial;
        }

        #endregion


        #region dpBSASerial

        /// <summary>Public Member - dpBSASerialProperty</summary>
        public static DependencyProperty dpBSASerialProperty = DependencyProperty.Register("dpBSASerial", typeof(String), typeof(BlackBox_Open_Settings_Control),
                  new FrameworkPropertyMetadata((String)null, new PropertyChangedCallback(OndpBSASerialChanged)));

        /// <summary>Property dpBSASerial of type String</summary>
        public String dpBSASerial
        {
            get { return (String)GetValue(dpBSASerialProperty); }
            set { SetValue(dpBSASerialProperty, value); }
        }

        private static void OndpBSASerialChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlackBox_Open_Settings_Control propOwner = (BlackBox_Open_Settings_Control)sender;
            String newdpBSASerial = (String)e.NewValue;
            propOwner.dpBSASerial = newdpBSASerial;
        }

        #endregion



        #region dpApplyCommand

        /// <summary>Public Member - dpApplyCommandProperty</summary>
        public static DependencyProperty dpApplyCommandProperty = DependencyProperty.Register("dpApplyCommand", typeof(ICommand), typeof(BlackBox_Open_Settings_Control),
                  new FrameworkPropertyMetadata((ICommand)null, new PropertyChangedCallback(OndpApplyCommandChanged)));

        /// <summary>Property dpApplyCommand of type ICommand</summary>
        public ICommand dpApplyCommand
        {
            get { return (ICommand)GetValue(dpApplyCommandProperty); }
            set { SetValue(dpApplyCommandProperty, value); }
        }

        private static void OndpApplyCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlackBox_Open_Settings_Control propOwner = (BlackBox_Open_Settings_Control)sender;
            ICommand newdpApplyCommand = (ICommand)e.NewValue;
            propOwner.dpApplyCommand = newdpApplyCommand;
        }

        #endregion



        #region dpClearCommand

        /// <summary>Public Member - dpClearCommandProperty</summary>
        public static DependencyProperty dpClearCommandProperty = DependencyProperty.Register("dpClearCommand", typeof(ICommand), typeof(BlackBox_Open_Settings_Control),
                  new FrameworkPropertyMetadata((ICommand)null, new PropertyChangedCallback(OndpClearCommandChanged)));

        /// <summary>Property dpClearCommand of type ICommand</summary>
        public ICommand dpClearCommand
        {
            get { return (ICommand)GetValue(dpClearCommandProperty); }
            set { SetValue(dpClearCommandProperty, value); }
        }

        private static void OndpClearCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlackBox_Open_Settings_Control propOwner = (BlackBox_Open_Settings_Control)sender;
            ICommand newdpClearCommand = (ICommand)e.NewValue;
            propOwner.dpClearCommand = newdpClearCommand;
        }

        #endregion

    }
}
