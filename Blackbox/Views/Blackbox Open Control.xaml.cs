using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;



namespace Segway.Service.SART
{
    /// <summary>
    /// Interaction logic for C:\Code\Branches\Black Box\sart\implementation\SART2012\Blackbox\Views\Blackbox Open Control.xaml.cs.xaml
    /// </summary>
    public partial class BlackBox_Open_Control : UserControl, BlackBox_Open_Control_Interface
    {
        /// <summary>Public Member - Control_Name</summary>
        public static String Control_Name = "BlackBox_Open_Control";

        /// <summary>Public Contructor - BlackBox_Open_Control</summary>
        public BlackBox_Open_Control()
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

        private void BB_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((BlackBox_Open_ViewModel)ViewModel).BlackBoxMergeCommand.RaiseCanExecuteChanged();
        }
    }
}
