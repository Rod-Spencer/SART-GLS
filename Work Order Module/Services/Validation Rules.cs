using System;
using System.Windows.Controls;

namespace Segway.Modules.WorkOrder.Services
{
    public class PT_Serial_Rule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            String serial = (String)value;
            if (serial.Length != 12) return new ValidationResult(false, "Invalid number of characters");
            if ((serial[2] != '2') || (serial[5] != '1')) return new ValidationResult(false, "Invalid format");
            return new ValidationResult(true, null);
        }
    }

    public class Work_Order_Rule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            String serial = (String)value;
            if (serial.Length != 7) return new ValidationResult(false, "Invalid number of characters");
            return new ValidationResult(true, null);
        }
    }

}
