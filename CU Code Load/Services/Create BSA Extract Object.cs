using Microsoft.Practices.Unity;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using System;
using System.Reflection;

namespace Segway.Modules.SART.CodeLoad
{
    public partial class CU_Code_ViewModel
    {
        /// <summary>Public Method - Create_BSA_Extract_Object</summary>
        /// <returns>SART_Event_Object</returns>
        public SART_Event_Object Create_BSA_Extract_Object()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                logger.Debug("Creating instance of SART_Event_Object");
                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                            Event_Types.BSA_Extract_Code_Data,
                                            Event_Statuses.In_Progress,
                                            InfrastructureModule.Token.LoginContext.UserName);

                logger.Debug("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    return null;
                }
                logger.Debug("SART Event Object ID: {0}", obj.ID);
                container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());
                aggregator.GetEvent<CU_Load_Code_EventID_Event>().Publish(obj.ID);
                return obj;
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
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
