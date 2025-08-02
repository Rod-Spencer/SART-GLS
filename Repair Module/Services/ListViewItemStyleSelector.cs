using System.Windows;
using System.Windows.Controls;
using Segway.SART.Objects;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class</summary>
    public class ListViewItemStyleSelector : StyleSelector
    {
        /// <summary>Public Method - SelectStyle</summary>
        /// <param name="item">object</param>
        /// <param name="container">DependencyObject</param>
        /// <returns>Style</returns>
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var data = item as SART_WO_Components;
            var lvi = container as DataGridRow; // ListViewItem;
            Style st = null;
            if (data != null)
            {
                if (data.Installed == "Yes")
                {
                    st = lvi.FindResource(WPFResources.DataGrids.DataGridRowStyleOneGreen) as Style;
                }
                else if (data.Installed == "No")
                {
                    st = lvi.FindResource(WPFResources.DataGrids.DataGridRowStyleOneRed) as Style;
                }
                else
                {
                    st = lvi.FindResource(WPFResources.DataGrids.DataGridRowStyleOne) as Style;
                }
            }
            return st;
        }
    }
}
