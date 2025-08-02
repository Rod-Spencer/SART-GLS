using System;
using System.Windows.Controls;

using Segway.Modules.ShellControls;
using System.Windows;

namespace Segway.Modules.SART.CodeLoad
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CU_Code_Control : UserControl, CU_Code_Control_Interface
    {
        /// <summary>Public Contructor - CU_Code_Control</summary>
        public CU_Code_Control()
        {
            InitializeComponent();
            ViewName = Control_Name;
        }

        /// <summary>Public Member</summary>
        public static String Control_Name = "CU_Code_Control";
        /// <summary>Public Property - ViewName</summary>
        public String ViewName { get; set; }


        /// <summary>Public Property - ViewModel</summary>
        public IViewModel ViewModel
        {
            get { return (IViewModel)DataContext; }
            set { DataContext = value; }
        }


        //#region JTagVisibility

        ///// <summary>ViewModel property: JTagVisibility of type: Visibility points to ((CU_Code_ViewModel)View).JTag_Visibility</summary>
        //public Visibility JTagVisibility
        //{
        //    get
        //    {
        //        if (((CU_Code_ViewModel)ViewModel) == null) return Visibility.Collapsed;
        //        return ((CU_Code_ViewModel)ViewModel).JTag_Visibility;
        //    }
        //    set
        //    {
        //        if (((CU_Code_ViewModel)ViewModel) == null) return;
        //        ((CU_Code_ViewModel)ViewModel).JTag_Visibility = value;
        //    }
        //}

        //#endregion

    }
}
