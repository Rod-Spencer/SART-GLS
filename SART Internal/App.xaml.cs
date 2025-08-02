using NLog;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;


namespace Segway.SART2012
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        private static Mutex mutex = new Mutex(true, "c9cb4f7e-367b-409d-8084-f055daabb8e1");


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (mutex.WaitOne(TimeSpan.Zero, true))
                {

                    base.OnStartup(e);
                    SART_Internal_Bootstrapper bootstrapper = new SART_Internal_Bootstrapper();
                    bootstrapper.Run();
                }
                else
                {
                    logger.Debug("Instance already running");
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            logger.Debug("Moving previous instance to foreground");
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }
                    logger.Debug("Shutting down this process");
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
    }
}
