using Microsoft.Practices.Prism.Events;
using Segway.Modules.ShellControls;
using Segway.Service.Common;
using Segway.Service.Modules.AddWindow;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Segway.Modules.WorkOrder
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Work Order Module\Views\WorkOrderOpen Control.xaml.cs.xaml
    /// </summary>
    public partial class Work_Order_Open_Control : UserControl, Work_Order_Open_Control_Interface
    {
        public static String Control_Name = "Work_Order_Open_Control";

        Boolean IsRightControlKeyPressed = false;
        Boolean IsRightShiftKeyPressed = false;



        public Work_Order_Open_Control(IEventAggregator aggregator)
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

        private void Work_Order_Open_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.RightCtrl)
            {
                IsRightControlKeyPressed = true;
            }
            else if (e.Key == Key.RightShift)
            {
                IsRightShiftKeyPressed = true;
            }
        }

        private void Work_Order_Open_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.RightCtrl)
            {
                IsRightControlKeyPressed = false;
            }
            else if (e.Key == Key.RightShift)
            {
                IsRightShiftKeyPressed = false;
            }
            else if ((IsRightControlKeyPressed == true) && (IsRightShiftKeyPressed == true))
            {
                if (e.Key == Key.F1)
                {
                    List<String> last = Last_10.Get_Most_Recent(Last_10.Last_PT_FileName);
                    Message_Window mw = Message_Window.Custom_List(last, "Last 10 PT Serials");

                    if (Application.Current != null)
                    {
                        if (Application.Current.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                mw.ShowDialog();
                            });
                        }
                    }

                    if (mw.dp_DialogResult == MessageButtons.Enter)
                    {
                        ((Work_Order_Open_ViewModel)ViewModel).WorkOrderNumber = (String)mw.dp_ComboBoxSelected;
                    }
                }
                else if (e.Key == Key.F4)
                {
                    List<String> last = Last_10.Get_Most_Recent(Last_10.Last_SRO_FileName);
                    Message_Window mw = Message_Window.Custom_List(last, "Last 10 SROs");

                    if (Application.Current != null)
                    {
                        if (Application.Current.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                mw.ShowDialog();
                            });
                        }
                    }

                    if (mw.dp_DialogResult == MessageButtons.Enter)
                    {
                        ((Work_Order_Open_ViewModel)ViewModel).WorkOrderNumber = (String)mw.dp_ComboBoxSelected;
                    }
                }
            }
        }
    }
}
