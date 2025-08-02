using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for WorkOrderSummaryView.xaml
    /// </summary>
    public partial class WorkOrderSummaryView : UserControl, IWorkOrderSummaryView
    {
        public static String Control_Name = "WorkOrderSummaryView";

        public WorkOrderSummaryView()
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
