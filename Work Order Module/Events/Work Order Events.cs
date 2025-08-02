using System;

using Microsoft.Practices.Prism.Events;

using Segway.SART.Objects;

namespace Segway.Modules.WorkOrder
{
    public class WorkOrder_Opened_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_Read_CU_Log_Event : CompositePresentationEvent<WorkOrder_Events> { }
    public class WorkOrder_Clear_List_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_Save_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_AutoSave_Event : CompositePresentationEvent<Boolean> { }
    //public class WorkOrder_AutoSave_Delete_Event : CompositePresentationEvent<Boolean> { }

    public class WO_Config_Clear_Event : CompositePresentationEvent<Boolean> { }
    public class WO_Config_EventID_Event : CompositePresentationEvent<Int32> { }

    public class WorkOrder_Configuration_Start_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_Configuration_Final_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_Configuration_Final_UpdateDB_Event : CompositePresentationEvent<SART_PT_Configuration> { }
    public class WorkOrder_Configuration_ClearLog_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_Configuration_Event : CompositePresentationEvent<SART_PT_Configuration> { }
    public class WorkOrder_Configuration_Refresh_Event : CompositePresentationEvent<Boolean> { }
    public class WorkOrder_ConfigurationType_Event : CompositePresentationEvent<ConfigurationTypes> { }
    public class WorkOrder_Status_Change_Event : CompositePresentationEvent<String> { }
    public class WorkOrder_RideTest_Event : CompositePresentationEvent<Boolean> { }

    public class WorkOrder_AuditUpdate_Request_Event : CompositePresentationEvent<Boolean> { }
}
