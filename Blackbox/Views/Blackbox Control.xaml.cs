using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;



namespace Segway.Service.SART
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Blackbox\Views\Blackbox Control.xaml.cs.xaml
    /// </summary>
    public partial class BlackBox_Control : UserControl, BlackBox_Control_Interface
    {
        /// <summary>Public Member - Control_Name</summary>
        public static String Control_Name = "BlackBox_Control";

        /// <summary>Public Contructor - BlackBox_Control</summary>
        public BlackBox_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }

        /// <summary>Public Property ViewModel - IViewModel</summary>
        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
