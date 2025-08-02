using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Segway.Modules.ShellControls;

namespace Segway.Service.Bugs
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Bug Tracking\Views\Bug Detail Control.xaml.cs.xaml
    /// </summary>
    public partial class Bug_Detail_Control : UserControl, Bug_Detail_Control_Interface
    {
        /// <summary>Public Member - Control_Name</summary>
        public static String Control_Name = "Bug_Detail_Control";

        /// <summary>Public Contructor - Bug_Detail_Control</summary>
        public Bug_Detail_Control()
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
