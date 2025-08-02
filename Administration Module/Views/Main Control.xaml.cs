using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Modules.Administration
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Main_Control : UserControl, Main_Control_Interface
    {
        /// <summary>Public Contructor - Main_Control</summary>
        public Main_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Member</summary>
        public static String Control_Name = "Main_Control";
        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }


        /// <summary>Public Property - ViewModel</summary>
        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
