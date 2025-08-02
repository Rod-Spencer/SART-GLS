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
    /// Interaction logic for WorkOrderSettingsView.xaml
    /// </summary>
    public partial class WorkOrderSettingsView : UserControl
    {
        Boolean IsRightControlKeyPressed = false;
        Boolean IsRightShiftKeyPressed = false;


        public WorkOrderSettingsView()
        {
            InitializeComponent();
        }

        //private void Work_Order_Open_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        //{
        //    if (e.Key == Key.RightCtrl)
        //    {
        //        IsRightControlKeyPressed = true;
        //    }
        //    else if (e.Key == Key.RightShift)
        //    {
        //        IsRightShiftKeyPressed = true;
        //    }
        //}

        //private void Work_Order_Open_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        //{
        //    if (e.Key == Key.RightCtrl)
        //    {
        //        IsRightControlKeyPressed = false;
        //    }
        //    else if (e.Key == Key.RightShift)
        //    {
        //        IsRightShiftKeyPressed = false;
        //    }
        //    else if ((IsRightControlKeyPressed == true) && (IsRightShiftKeyPressed == true))
        //    {
        //        if (e.Key == Key.F1)
        //        {
        //        }
        //        else if (e.Key == Key.F2)
        //        {
        //            List<String> last = Last_10.Get_Most_Recent(Last_10.Last_SRO_FileName);
        //            Message_Window mw = Message_Window.Custom_List(last, "Last 10 SROs");

        //            if (Application.Current != null)
        //            {
        //                if (Application.Current.Dispatcher != null)
        //                {
        //                    Application.Current.Dispatcher.Invoke((Action)delegate ()
        //                    {
        //                        mw.ShowDialog();
        //                    });
        //                }
        //            }
        //        }
        //    }

        //}
    }
}
