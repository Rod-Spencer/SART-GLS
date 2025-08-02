using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.SART.Reports
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\SART Reports\Views\Report Control.xaml.cs.xaml
    /// </summary>
    public partial class Report_Control : UserControl, Report_Control_Interface
    {
        /// <summary>Public Member</summary>
        public static String Control_Name = "Report_Control";

        /// <summary>Public Contructor - Report_Control</summary>
        public Report_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }

        /// <summary>Public Iviewmodel - ViewModel</summary>
        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
