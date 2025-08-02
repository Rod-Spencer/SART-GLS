using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Work_Order_Configuration_Control : UserControl, Work_Order_Configuration_Control_Interface
    {
        public static String Control_Name = "Work_Order_Configuration_Control";

        public Work_Order_Configuration_Control()
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((Work_Order_Configuration_ViewModel)ViewModel).AuthorizeCommand.RaiseCanExecuteChanged();
        }

        private void TextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            ((Work_Order_Configuration_ViewModel)ViewModel).Set_ContextMenu((TextBox)sender);
        }


        // For testing purposes - delete when no longer needed
        //private void TestPopButton_Click(object sender, RoutedEventArgs e)
        //{
        //    UIC_Serial_Popup.IsOpen = true;
        //    UIC_Serial_Popup.StaysOpen = false;
        //    CUAPopup.IsOpen = true;
        //    CUAPopup.StaysOpen = false;
        //    CUBPopup.IsOpen = true;
        //    CUBPopup.StaysOpen = false;
        //    BSA_A_Popup.IsOpen = true;
        //    BSA_A_Popup.StaysOpen = false;
        //    BSA_B_Popup.IsOpen = true;
        //    BSA_B_Popup.StaysOpen = false;
        //    UI_SID_Popup.IsOpen = true;
        //    UI_SID_Popup.StaysOpen = false;
        //    Pivot_Serial_Popup.IsOpen = true;
        //    Pivot_Serial_Popup.StaysOpen = false;
        //    //MotorLeft_Serial_Popup.IsOpen = true;
        //    //MotorLeft_Serial_Popup.StaysOpen = false;
        //    //MotorRight_Serial_Popup.IsOpen = true;
        //}
    }
}
