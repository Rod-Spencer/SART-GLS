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

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for WorkOrderOpenView.xaml
    /// </summary>
    public partial class WorkOrderOpenView : UserControl
    {
        public WorkOrderOpenView()
        {
            InitializeComponent();
        }

        public Object DataContext2
        {
            get
            {
                return DataContext;
            }
            set
            {
                DataContext = value;
                viewSettings.DataContext = DataContext;
            }
        }
    }
}
