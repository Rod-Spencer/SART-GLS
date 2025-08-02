using System;
using Microsoft.Practices.Prism.Events;

namespace Segway.Service.Disclaimer
{
    /// <summary>Public Class - SART_Disclaimer_Reject_Event</summary>
    public class SART_Disclaimer_Reject_Event : CompositePresentationEvent<String> { }
    /// <summary>Public Class - SART_Disclaimer_Accept_Event</summary>
    public class SART_Disclaimer_Accept_Event : CompositePresentationEvent<String> { }
    /// <summary>Public Class - SART_Disclaimer_Reject_Navigate_Event</summary>
    public class SART_Disclaimer_Reject_Navigate_Event : CompositePresentationEvent<String> { }
    /// <summary>Public Class - SART_Disclaimer_Accept_Navigate_Event</summary>
    public class SART_Disclaimer_Accept_Navigate_Event : CompositePresentationEvent<String> { }
}
