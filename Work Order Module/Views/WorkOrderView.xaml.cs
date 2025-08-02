using System;
using System.Windows.Controls;
using Segway.Modules.ShellControls;
using ActiproSoftware.Windows.Controls.Views;
using System.Windows.Media.Imaging;

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for NewWorkOrderCtrl.xaml
    /// </summary>
    public partial class WorkOrderView : UserControl, IWorkOrderView
    {
        public static String Control_Name = "WorkOrderView";

        public WorkOrderView()
        {
            InitializeComponent();
            //listBox.ItemsSource = new ListBoxItem[3];
            listBox.ItemsSource = new WorkOrderViewConfig[] {
                                                            new WorkOrderViewConfig("New Work Order", new BitmapImage(new Uri("../Images/new.png", UriKind.RelativeOrAbsolute)), new WorkOrderNewView()),
                                                            new WorkOrderViewConfig("Open Work Order", new BitmapImage(new Uri("../Images/open.png", UriKind.RelativeOrAbsolute)), new WorkOrderOpenView()),
                                                          };
            ViewName = Control_Name;
        }

        public String ViewName { get; set; }

        public IViewModel ViewModel
        {
            get
            {
                return (IViewModel)DataContext;
            }
            set
            {
                DataContext = value;
                ((WorkOrderViewConfig)listBox.Items[0]).Content.DataContext = DataContext;
                ((WorkOrderOpenView)(((WorkOrderViewConfig)listBox.Items[1]).Content)).DataContext2 = DataContext;
            }
        }
    }
}
