using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Service.SART.Email
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Email Segway\Views\EmailSegway Control.xaml.cs.xaml
    /// </summary>
    public partial class EmailSegway_Control : UserControl, EmailSegway_Control_Interface
    {
        /// <summary>Public Member</summary>
        public static String Control_Name = "EmailSegway_Control";

        /// <summary>Public Contructor - EmailSegway_Control</summary>
        public EmailSegway_Control()
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
