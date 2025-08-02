using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Modules.CU_Log_Module
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CU_Log_Control : UserControl, CU_Log_Control_Interface
    {
        public CU_Log_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        public static String Control_Name = "CU_Log_Control";
        public String ViewName { get; set; }


        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
