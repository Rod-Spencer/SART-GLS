using Microsoft.Practices.Prism.Events;
using Segway.Database.Objects;
using System;

namespace Segway.Modules.CU_Log_Module
{
    //public class CU_Log_Add_SARTEvent_Event : CompositePresentationEvent<SART_Event_Log_Entry> { }
    //public class CU_Log_Update_SARTEvent_Event : CompositePresentationEvent<SART_Event_Log_Entry> { }
    public class CU_Log_EventID_Event : CompositePresentationEvent<Int32> { }

    public class SelectLogPanelEvent : CompositePresentationEvent<Int32> { }
    public class OpenExtractPanelEvent : CompositePresentationEvent<Boolean> { }
    public class ApplyLogFilterEvent : CompositePresentationEvent<SqlBooleanCriteria> { }
}
