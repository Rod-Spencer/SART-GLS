using System;
//using System.Text;
using Microsoft.Practices.Prism.Events;
//using Microsoft.Practices.Prism.Modularity;
//using Microsoft.Practices.Prism.Regions;
//using Microsoft.Practices.Unity;

namespace Segway.Modules.WorkOrder
{
    public class ToolBarItemManager
    {
        private IEventAggregator eventAggregator;

        public ToolBarItemManager(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
        
        public void HideToolBarItems(Boolean isHidden)
        {
            //eventAggregator.GetEvent<ListBoxToolBarItem_Hide_Event>().Publish(new KeyValuePair<String, Boolean>("Load Code", isHidden));
            //eventAggregator.GetEvent<ListBoxToolBarItem_Hide_Event>().Publish(new KeyValuePair<String, Boolean>("Diagnostics", isHidden));
            //eventAggregator.GetEvent<ListBoxToolBarItem_Hide_Event>().Publish(new KeyValuePair<String, Boolean>("Repairs", isHidden));
        }

        public void HideAdminView(Boolean isHidden)
        {
            //eventAggregator.GetEvent<ListBoxToolBarItem_Hide_Event>().Publish(new KeyValuePair<String, Boolean>("Administration", isHidden));
        }

    }
}
