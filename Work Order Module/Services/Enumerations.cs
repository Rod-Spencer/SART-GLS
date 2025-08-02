
namespace Segway.Modules.WorkOrder.Services
{
    public enum PTModel
    {
        i2, x2
    }

    public enum WorkOrderStatus
    {
        ReadyToBegin,
        WorkOrderCreated,
        OpenForDiagnostics,
        RepairRequested,
        ClosedForApproval,
        ClosedForDecline,
        OpenForRepair,
        RepairCompleted,
        WorkOrderCloseRequested,
        WorkOrderComplete
        // These status states are from SART2012 Requirements Specifications

        // note that after assigning var =  WorkOrderStatus.RepairRequested, can use ToString:  var.ToString();
        // But not for clean strings to present in the GUI, need to have a mapping to user friendly strings
    }


}
