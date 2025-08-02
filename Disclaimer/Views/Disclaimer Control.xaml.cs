using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Service.Disclaimer
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Disclaimer\Views\Disclaimer Control.xaml.cs.xaml
    /// </summary>
    public partial class Disclaimer_Control : UserControl, Disclaimer_Control_Interface
    {
        /// <summary>Public Contructor - Disclaimer_Control</summary>
        public Disclaimer_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Member</summary>
        public static String Control_Name = "Disclaimer_Control";
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
