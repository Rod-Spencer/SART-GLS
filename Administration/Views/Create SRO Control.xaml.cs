using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.Service.Common;
using Segway.Service.Modules.AddWindow;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Segway.Modules.Administration
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Administration\Views\Create Sro Control.xaml.cs.xaml
    /// </summary>
    public partial class Create_SRO_Control : UserControl, Create_SRO_Control_Interface
    {
        /// <summary>Public Member - Control_Name</summary>
        public static String Control_Name = "Create_SRO_Control";
        Boolean IsRightControlKeyPressed = false;
        Boolean IsRightShiftKeyPressed = false;


        /// <summary>Public Contructor - Create_SRO_Control</summary>
        public Create_SRO_Control()
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


        private void Create_SRO_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void Create_SRO_KeyUp(object sender, KeyEventArgs e)
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
                        ((Create_SRO_ViewModel)ViewModel).SerialNumber = (String)mw.dp_ComboBoxSelected;
                    }
                }
            }
        }

    }
}
