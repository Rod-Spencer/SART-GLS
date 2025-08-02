using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using Segway.Modules.ShellControls;

namespace Segway.Modules.SART.Repair
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Repair Module\Views\Repair Control.xaml.cs.xaml
    /// </summary>
    public partial class Repair_Control : UserControl, Repair_Control_Interface
    {
        /// <summary>Public Contructor - Repair_Control</summary>
        public Repair_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Member</summary>
        public static String Control_Name = "Repair_Control";
        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }

        /// <summary>Public Property - ViewModel</summary>
        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            e.Handled = regex.IsMatch(e.Text);
        }

        private void tbNewPN_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) e.Handled = true;
            else if (e.Key == Key.LineFeed) e.Handled = true;
            else if (e.Key == Key.Tab) e.Handled = true;
        }

        private void tbNewPN_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) || (e.Key == Key.Tab))
            {
                ((Repair_ViewModel)ViewModel).Add_New_Component(tbNewPN.Text);
                e.Handled = true;
            }
            else if (e.Key == Key.LineFeed)
            {
                e.Handled = true;
            }
        }

        private void RepairControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            tbNewPN.Focus();
        }

        //private void Part_Number_PreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //}
    }
}
