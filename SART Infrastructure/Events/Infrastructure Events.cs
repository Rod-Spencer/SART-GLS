using System;
using Microsoft.Practices.Prism.Events;
using Segway.SART.Objects;

namespace Segway.Modules.SART_Infrastructure
{
    public class SART_Infrastructure_Clear_DealerList_Event : CompositePresentationEvent<Boolean> { }

    public class SART_Events_Add_Event : CompositePresentationEvent<SART_Events> { }
    public class SART_EventLog_Add_Event : CompositePresentationEvent<SART_Event_Log_Entry> { }
    public class SART_EventLog_Update_Event : CompositePresentationEvent<SART_Event_Log_Entry> { }

    public class SART_CU_CodeLoad_A_Event : CompositePresentationEvent<Boolean> { }
    public class SART_CU_CodeLoad_B_Event : CompositePresentationEvent<Boolean> { }
    public class SART_CU_CodeReset_A_Event : CompositePresentationEvent<Boolean> { }
    public class SART_CU_CodeReset_B_Event : CompositePresentationEvent<Boolean> { }

    public class SART_BSA_CodeLoad_Event : CompositePresentationEvent<Boolean> { }
    public class SART_BSA_CodeLoad_A_Event : CompositePresentationEvent<Boolean> { }
    public class SART_BSA_CodeLoad_B_Event : CompositePresentationEvent<Boolean> { }
    public class SART_BSA_CodeReset_A_Event : CompositePresentationEvent<Boolean> { }
    public class SART_BSA_CodeReset_B_Event : CompositePresentationEvent<Boolean> { }

    public class SART_MotorTest_Done_Event : CompositePresentationEvent<Boolean> { }
    public class SART_RiderDetectTest_Done_Event : CompositePresentationEvent<Boolean> { }
    public class SART_BSATest_Done_Event : CompositePresentationEvent<Boolean> { }
    public class SART_LEDTest_Done_Event : CompositePresentationEvent<Boolean> { }

    public class SART_Configuration_CUA_Display_Event : CompositePresentationEvent<String> { }
    public class SART_Configuration_CUB_Display_Event : CompositePresentationEvent<String> { }
    public class SART_Configuration_BSAA_Display_Event : CompositePresentationEvent<String> { }
    public class SART_Configuration_BSAB_Display_Event : CompositePresentationEvent<String> { }
    public class SART_Configuration_UISID_Display_Event : CompositePresentationEvent<String> { }
    public class SART_Configuration_UICSerial_Display_Event : CompositePresentationEvent<String> { }

    public class SART_WorkOrder_Selected_Event : CompositePresentationEvent<String> { }
    public class SART_WorkOrder_Close_Event : CompositePresentationEvent<Boolean> { }
    public class SART_WorkOrder_Cancel_Event : CompositePresentationEvent<Boolean> { }

    public class SART_Dealers_Loaded_Event : CompositePresentationEvent<Boolean> { }
    public class SART_Load_Dealers_Event : CompositePresentationEvent<Boolean> { }


    //////////////////////////////////////////////////////////////////////////
    // Regional Data
    public class SART_Load_Regional_Data_Event : CompositePresentationEvent<Boolean> { }
    public class SART_Loaded_Regional_Data_Event : CompositePresentationEvent<Boolean> { }
    public class SART_Save_Regional_Data_Event : CompositePresentationEvent<Boolean> { }
    public class SART_Saved_Regional_Data_Event : CompositePresentationEvent<Boolean> { }
    // Regional Data
    //////////////////////////////////////////////////////////////////////////


    public class SART_UserSettings_Changed_Event : CompositePresentationEvent<SART_User_Settings> { }
    public class SART_Settings_Changed_Event : CompositePresentationEvent<Boolean> { }

}

