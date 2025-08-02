using System;

namespace Segway.Modules.WorkOrder
{
    public class Component_Labels
    {
        public const String Serial_Number = "Serial Number:";
        public const String Part_Number = "Part Number:";
        public const String Part_Type = "Part Type:";
        public const String Installed = "Installed:";
        public const String Installed_By = "Installed By:";
        public const String SW_Version = "Software Version:";
        public const String Model = "Model:";
    }

    public class Configuration_Labels
    {
        public const String BSA = "BSA";
        public const String CUA = "CU-A";
        public const String CUB = "CU-B";
        public const String MotorL = "Left Motor";
        public const String MotorR = "Right Motor";
        public const String Pivot = "Pivot";
        public const String UIC = "UIC";
    }

    public class Component_Info
    {

        public String Label { get; set; }
        public String Data { get; set; }

        public Component_Info() { }

        public Component_Info(String l, String d)
        {
            Label = l;
            Data = d;
        }

        public Component_Info(Component_Info ci)
        {
            Copy(ci);
        }


        public Component_Info Copy()
        {
            Component_Info ci = new Component_Info();
            ci.Label = this.Label;
            ci.Data = this.Data;
            return ci;
        }

        public void Copy(Component_Info ci)
        {
            this.Label = ci.Label;
            this.Data = ci.Data;
        }

        public static String Format_Label(Component_Labels label)
        {
            return label.ToString().Replace("_", " ");
        }

    }
}
