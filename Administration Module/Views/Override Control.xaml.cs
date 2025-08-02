using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Segway.Modules.ShellControls;

namespace Segway.Modules.Administration
{
    /// <summary>
    /// Interaction logic for C:\Code\Projects\sart\implementation\SART2012\Administration Module\Views\Override Control.xaml.cs.xaml
    /// </summary>
    public partial class Override_Control : UserControl, Override_Control_Interface
    {
        public static String Control_Name = "Override_Control";

        public Override_Control()
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

    }
}
