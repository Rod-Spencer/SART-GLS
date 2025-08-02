using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Work Order Module\Views\WorkorderUpdate Control.xaml.cs.xaml
    /// </summary>
    public partial class Work_Order_Update_Control : UserControl, Work_Order_Update_Control_Interface
    {
        public static String Control_Name = "Work_Order_Update_Control";

        public Work_Order_Update_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        public String ViewName { get; set; }

        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
