using System;
using System.Windows.Controls;
using Segway.Modules.ShellControls;

namespace Segway.Modules.CU_Log_Module
{
    /// <summary>
    /// Interaction logic for CU_Log_Extraction.xaml
    /// </summary>
    public partial class CU_Log_Extraction : UserControl, CU_Log_Extraction_Interface
    {
        public CU_Log_Extraction()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        public static String Control_Name = "CU_Log_Extraction";
        public String ViewName { get; set; }


        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

    }
}
