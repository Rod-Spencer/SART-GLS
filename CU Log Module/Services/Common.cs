using Segway.SART.CULog.Objects;
using Segway.SART.Objects;
using Segway.Service.Common;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Tools.PDFHelper;
using System;
using System.IO;

namespace Segway.Modules.CU_Log_Module
{
    public class Common
    {
        public static void Display_CU_Log(SART_CU_Logs log, G2_Report_Types type, Boolean isReverseOrder, Boolean showRaw)
        {
            DirectoryInfo di = new DirectoryInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Reports"));
            if (di.Exists == false) di.Create();
            String repPath = Write_CU_Log(log, type, isReverseOrder, showRaw, di);
            //CU_Log_Reporter report = new CU_Log_Reporter();
            //PDF_Helper pdf = report.Generate(log, showRaw, type, isReverseOrder);
            FileInfo fi = new FileInfo(repPath);
            //pdf.Save(fi.FullName);
            ProcessHelper.Run(fi.FullName, wait: 0, redirectOutput: false, redirectError: false, checkexist: false);
        }


        public static String Write_CU_Log(SART_CU_Logs log, G2_Report_Types type, Boolean isReverseOrder, Boolean showRaw, DirectoryInfo di)
        {
            CU_Log_Reporter report = new CU_Log_Reporter();
            PDF_Helper pdf = report.Generate(log, showRaw, type, isReverseOrder);
            String path = Path.Combine(di.FullName, String.Format("{0}_{1}_{2}.pdf", log.PT_Serial, log.CU_Side, log.Date_Time_Extracted.Value.ToString("yyyyMMdd_HHmmss")));
            FileInfo fi = new FileInfo(path);
            if (fi.Exists)
            {
                while (true)
                {
                    try
                    {
                        fi.Delete();
                        break;
                    }
                    catch
                    {
                        String msg = $"The PDF file: {path} could not be updated.\n\nThis may be the result of the file being opened. Please close and then click on OK";
                        Message_Window.Warning(msg, height: Window_Sizes.Medium, width: Window_Sizes.Medium, buttons: MessageButtons.OK).ShowDialog();
                    }
                }
            }
            pdf.Save(fi.FullName);
            return fi.FullName;
        }

    }
}
