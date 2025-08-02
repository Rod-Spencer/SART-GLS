using System;
using System.Windows.Controls;
using Segway.Modules.ShellControls;

namespace Segway.Modules.CU_Log_Module
{
    /// <summary>
    /// Interaction logic for CU_Log_Viewer.xaml
    /// </summary>
    public partial class CU_Log_Viewer : UserControl, CU_Log_Viewer_Interface
    {
        public CU_Log_Viewer()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        public static String Control_Name = "CU_Log_Viewer";
        public String ViewName { get; set; }


        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }
    }
}
