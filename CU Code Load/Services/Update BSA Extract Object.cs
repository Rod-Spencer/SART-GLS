using Segway.Modules.SART_Infrastructure;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using System;
using System.Reflection;

namespace Segway.Modules.SART.CodeLoad
{
    public partial class CU_Code_ViewModel
    {
        /// <summary>Public Method - Update_BSA_Extract_Object</summary>
        /// <param name="obj">SART_Event_Object</param>
        /// <param name="status">Boolean</param>
        /// <returns>Boolean</returns>
        public Boolean Update_BSA_Extract_Object(SART_Event_Object obj, Boolean status)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                SART_Common.Create_Event(WorkOrder_Events.BSA_Code_Compare, status ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                aggregator.GetEvent<CU_Load_Code_EventID_Event>().Publish(0);
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                return true;
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
    }
}
