using Microsoft.Practices.Unity;
using NLog;
using Segway.Login.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Segway.Modules.WorkOrder.Services
{
    public class Common
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();

        public static Dictionary<String, String> Get_Statuses(AuthenticationToken token)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (InfrastructureModule.Container.IsRegistered<Dictionary<String, String>>(WorkOrderModule.StatusCodesName) == false)
                {
                    Dictionary<String, String> _Status_Codes = Syteline_FSStat_Web_Service_Client_REST.Get_Statuses(InfrastructureModule.Token);
                    InfrastructureModule.Container.RegisterInstance<Dictionary<String, String>>(WorkOrderModule.StatusCodesName, _Status_Codes, new ContainerControlledLifetimeManager());
                    return _Status_Codes;
                }
                else
                {
                    return InfrastructureModule.Container.Resolve<Dictionary<String, String>>(WorkOrderModule.StatusCodesName);
                }
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                InfrastructureModule.Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                return null;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
    }
}
