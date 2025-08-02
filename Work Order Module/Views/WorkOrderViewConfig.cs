using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Segway.Modules.WorkOrder
{
    public class WorkOrderViewConfig
    {
        public WorkOrderViewConfig() { }

        public WorkOrderViewConfig(String title, ImageSource img, UserControl content)
        {
            Title = title;
            Image = img;
            Content = content;
        }

        public String Title { get; set; }

        public ImageSource Image { get; set; }

        public UserControl Content { get; set; }
    }
}
