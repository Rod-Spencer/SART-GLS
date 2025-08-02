
namespace Segway.Modules.SART_Infrastructure
{
    public enum Open_Mode
    {
        Cancel = -1,
        Close = 0,
        Read_Write,
        Read_Only,
    }

    public enum SerialValidation
    {
        Validated = 0,
        Serial_Format_Invalid,
        Serial_Not_Associated,
    }
}
