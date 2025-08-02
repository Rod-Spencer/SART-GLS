using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Modules.Diagnostic
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Diagnostic_Control : UserControl, Diagnostic_Control_Interface
    {
        /// <summary>Public Contructor - Diagnostic_Control</summary>
        public Diagnostic_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Member</summary>
        public static String Control_Name = "Diagnostic_Control";
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
