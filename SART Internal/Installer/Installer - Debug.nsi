; Script generated with the Venis Install Wizard

; Define your application name
!system "GetVersionDebug.exe"
!include "Version.txt"
!define APPNAME "SART Internal"
!define APPNAMEANDVERSION "${APPNAME} ${VERSION}"

; Main Install settings
Name "${APPNAMEANDVERSION}"
InstallDir "c:\Segway\${APPNAME}"
InstallDirRegKey HKLM "Software\${APPNAME}" ""
OutFile "${APPNAME}.Upgrade - Debug.${VERSION}.N.exe"

; Modern interface settings
!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPNAME}.exe"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Set languages (first is default language)
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL

Section "SART Internal"

    ; Set Section properties
    SetOverwrite on

    ; Set Section Files and Shortcuts
    SetOutPath "$INSTDIR\"
    File "..\bin\Debug\${APPNAME}.exe"

    File "..\bin\Debug\ActiproSoftware.Editors.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.Gauge.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.Legacy.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.PropertyGrid.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.Shared.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.Themes.Office.Wpf.dll"
    File "..\bin\Debug\ActiproSoftware.Views.Wpf.dll"
    File "..\bin\Debug\Add Window.dll"
    File "..\bin\Debug\Administration.dll"
    File "..\bin\Debug\All_Diagnostics.dll"
    File "..\bin\Debug\Assembly Line Web Service Client.dll"
    File "..\bin\Debug\Authentication Objects.dll"
    File "..\bin\Debug\Authentication Web Service Client.dll"
    File "..\bin\Debug\Battery Info Module.dll"
    File "..\bin\Debug\Battery Objects.dll"
    File "..\bin\Debug\Black Box Web Service Client.dll"
    File "..\bin\Debug\BlackBox.dll"
    File "..\bin\Debug\BlowFish.dll"
    File "..\bin\Debug\Bug Objects.dll"
    File "..\bin\Debug\Bug Tracking Module.dll"
    File "..\bin\Debug\BugZilla Web Service Client.dll"
    File "..\bin\Debug\CAN Objects.dll"
    File "..\bin\Debug\CAN Systec.dll"
    File "..\bin\Debug\CAN.dll"
    File "..\bin\Debug\CAN2 Commands.dll"
    File "..\bin\Debug\CAN2 Messages.dll"
    File "..\bin\Debug\COFF.dll"
    File "..\bin\Debug\CommentModule.dll"
    File "..\bin\Debug\Common.dll"
    File "..\bin\Debug\CU Code Load.dll"
    File "..\bin\Debug\CU Log Module.dll"
    File "..\bin\Debug\DatabaseHelper.dll"
    File "..\bin\Debug\DateTimePicker.dll"
    File "..\bin\Debug\Diagnostics Helper.dll"
    File "..\bin\Debug\Email Segway Module.dll"
    File "..\bin\Debug\HeaderBar.dll"
    File "..\bin\Debug\IDOProtocol.dll"
    File "..\bin\Debug\Image Helper.dll"
    File "..\bin\Debug\InfoKey Objects.dll"
    File "..\bin\Debug\JTagsModule.dll"
    File "..\bin\Debug\List Box Vertical Tool Bar.dll"
    File "..\bin\Debug\Manufacturing Objects.dll"
    File "..\bin\Debug\Manufacturing Tables Web Service Client.dll"
    File "..\bin\Debug\Manufacturing Web Service Client.dll"
    File "..\bin\Debug\MGShared.dll"
    File "..\bin\Debug\Microsoft.Expression.Controls.dll"
    File "..\bin\Debug\Microsoft.Expression.Drawing.dll"
    File "..\bin\Debug\Microsoft.Practices.Prism.dll"
    File "..\bin\Debug\Microsoft.Practices.Prism.Interactivity.dll"
    File "..\bin\Debug\Microsoft.Practices.Prism.UnityExtensions.dll"
    File "..\bin\Debug\Microsoft.Practices.ServiceLocation.dll"
    File "..\bin\Debug\Microsoft.Practices.Unity.dll"
    File "..\bin\Debug\MotorTests.dll"
    File "..\bin\Debug\MultiLevelToolBar.dll"
    File "..\bin\Debug\NHibernateBase.dll"
    File "..\bin\Debug\NLog.dll"
    File "..\bin\Debug\PDF.dll"
    File "..\bin\Debug\PdfSharp.dll"
    File "..\bin\Debug\ProgressionLogViewer.dll"
    File "..\bin\Debug\Progression_Window.dll"
    File "..\bin\Debug\PT_BSA.dll"
    File "..\bin\Debug\PT_LED.dll"
    File "..\bin\Debug\Repair Module.dll"
    File "..\bin\Debug\Reports Module.dll"
    File "..\bin\Debug\Ride Test Module.dll"
    File "..\bin\Debug\RiderDetect.dll"
    File "..\bin\Debug\Runtime Logs Web Service Client.dll"
    File "..\bin\Debug\SART 2012 Web Service Client.dll"
    File "..\bin\Debug\SART Diagnostic.dll"
    File "..\bin\Debug\SART Disclaimer Module.dll"
    File "..\bin\Debug\SART Objects.dll"
    File "..\bin\Debug\SARTInfrastructure.dll"
    File "..\bin\Debug\Segway Objects.dll"
    File "..\bin\Debug\SegwayShellControls.dll"
    File "..\bin\Debug\Segway_Login_Module.dll"
    File "..\bin\Debug\SQL Reporting Objects.dll"
    File "..\bin\Debug\Status Bar Control.dll"
    File "..\bin\Debug\System.Windows.Interactivity.dll"
    File "..\bin\Debug\Syteline External Web Service Client.dll"
    File "..\bin\Debug\Syteline Objects.dll"
    File "..\bin\Debug\Title Bar Control.dll"
    File "..\bin\Debug\UcanDotNET.dll"
    File "..\bin\Debug\UnderConstruction.dll"
    File "..\bin\Debug\Updater Objects.dll"
    File "..\bin\Debug\Updater Web Service Client.dll"
    File "..\bin\Debug\USBCAN32.dll"
    File "..\bin\Debug\WorkOrderModule.dll"
    File "..\bin\Debug\WPFResources.dll"
    File "..\bin\Debug\WPFVisifire.Charts.dll"
    File "..\bin\Debug\WPFVisifire.Gauges.dll"
    File "..\bin\Debug\WSEnums.dll"
    File "..\bin\Debug\WSFormServerProtocol.dll"

    File "..\bin\Debug\Add Window.pdb"
    File "..\bin\Debug\Administration.pdb"
    File "..\bin\Debug\All_Diagnostics.pdb"
    File "..\bin\Debug\Assembly Line Web Service Client.pdb"
    File "..\bin\Debug\Authentication Objects.pdb"
    File "..\bin\Debug\Authentication Web Service Client.pdb"
    File "..\bin\Debug\Battery Info Module.pdb"
    File "..\bin\Debug\Battery Objects.pdb"
    File "..\bin\Debug\Black Box Web Service Client.pdb"
    File "..\bin\Debug\BlackBox.pdb"
    File "..\bin\Debug\BlowFish.pdb"
    File "..\bin\Debug\Bug Objects.pdb"
    File "..\bin\Debug\Bug Tracking Module.pdb"
    File "..\bin\Debug\BugZilla Web Service Client.pdb"
    File "..\bin\Debug\CAN Objects.pdb"
    File "..\bin\Debug\CAN Systec.pdb"
    File "..\bin\Debug\CAN.pdb"
    File "..\bin\Debug\CAN2 Commands.pdb"
    File "..\bin\Debug\CAN2 Messages.pdb"
    File "..\bin\Debug\COFF.pdb"
    File "..\bin\Debug\CommentModule.pdb"
    File "..\bin\Debug\Common.pdb"
    File "..\bin\Debug\CU Code Load.pdb"
    File "..\bin\Debug\CU Log Module.pdb"
    File "..\bin\Debug\DatabaseHelper.pdb"
    File "..\bin\Debug\DateTimePicker.pdb"
    File "..\bin\Debug\Diagnostics Helper.pdb"
    File "..\bin\Debug\Email Segway Module.pdb"
    File "..\bin\Debug\HeaderBar.pdb"
    File "..\bin\Debug\Image Helper.pdb"
    File "..\bin\Debug\InfoKey Objects.pdb"
    File "..\bin\Debug\JTagsModule.pdb"
    File "..\bin\Debug\List Box Vertical Tool Bar.pdb"
    File "..\bin\Debug\Manufacturing Objects.pdb"
    File "..\bin\Debug\Manufacturing Tables Web Service Client.pdb"
    File "..\bin\Debug\Manufacturing Web Service Client.pdb"
    File "..\bin\Debug\Microsoft.Practices.Prism.Interactivity.pdb"
    File "..\bin\Debug\Microsoft.Practices.Prism.pdb"
    File "..\bin\Debug\Microsoft.Practices.Prism.UnityExtensions.pdb"
    File "..\bin\Debug\MotorTests.pdb"
    File "..\bin\Debug\MultiLevelToolBar.pdb"
    File "..\bin\Debug\NHibernateBase.pdb"
    File "..\bin\Debug\PDF.pdb"
    File "..\bin\Debug\PdfSharp.pdb"
    File "..\bin\Debug\ProgressionLogViewer.pdb"
    File "..\bin\Debug\Progression_Window.pdb"
    File "..\bin\Debug\PT_BSA.pdb"
    File "..\bin\Debug\PT_LED.pdb"
    File "..\bin\Debug\Repair Module.pdb"
    File "..\bin\Debug\Reports Module.pdb"
    File "..\bin\Debug\Ride Test Module.pdb"
    File "..\bin\Debug\RiderDetect.pdb"
    File "..\bin\Debug\Runtime Logs Web Service Client.pdb"
    File "..\bin\Debug\SART 2012 Web Service Client.pdb"
    File "..\bin\Debug\SART Diagnostic.pdb"
    File "..\bin\Debug\SART Disclaimer Module.pdb"
    File "..\bin\Debug\SART Internal.pdb"
    File "..\bin\Debug\SART Objects.pdb"
    File "..\bin\Debug\SARTInfrastructure.pdb"
    File "..\bin\Debug\Segway Objects.pdb"
    File "..\bin\Debug\SegwayShellControls.pdb"
    File "..\bin\Debug\Segway_Login_Module.pdb"
    File "..\bin\Debug\SQL Reporting Objects.pdb"
    File "..\bin\Debug\Status Bar Control.pdb"
    File "..\bin\Debug\Syteline External Web Service Client.pdb"
    File "..\bin\Debug\Syteline Objects.pdb"
    File "..\bin\Debug\Title Bar Control.pdb"
    File "..\bin\Debug\UcanDotNET.pdb"
    File "..\bin\Debug\UnderConstruction.pdb"
    File "..\bin\Debug\Updater Objects.pdb"
    File "..\bin\Debug\Updater Web Service Client.pdb"
    File "..\bin\Debug\WorkOrderModule.pdb"
    File "..\bin\Debug\WPFResources.pdb"
    File "..\bin\Debug\WSEnums.pdb"

    File "..\bin\Debug\${APPNAME}.exe.Config"

    SetOutPath "$INSTDIR\Images"
    File "..\bin\Debug\Images\Close-Icon.png"
    File "..\bin\Debug\Images\Maximize-Icon.png"
    File "..\bin\Debug\Images\Minimize-Icon.png"
    File "..\bin\Debug\Images\Restore-Icon.png"
    File "..\bin\Debug\Images\TitleBarImage.png"

    SetOutPath "$INSTDIR\"
    CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${APPNAME}.exe"
    CreateDirectory "$SMPROGRAMS\Segway\${APPNAME}"
    CreateShortCut "$SMPROGRAMS\Segway\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPNAME}.exe"
    CreateShortCut "$SMPROGRAMS\Segway\${APPNAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe"

SectionEnd

Section -FinishSection

    WriteRegStr HKLM "Software\${APPNAME}" "" "$INSTDIR"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteUninstaller "$INSTDIR\uninstall.exe"

SectionEnd

; Modern install component descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${Section1} ""
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;Uninstall section
Section Uninstall

    ;Remove from registry...
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
    DeleteRegKey HKLM "SOFTWARE\${APPNAME}"

    ; Delete self
    Delete "$INSTDIR\uninstall.exe"

    ; Delete Shortcuts
    Delete "$DESKTOP\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\Segway\${APPNAME}\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\Segway\${APPNAME}\Uninstall.lnk"

    ; Remove remaining directories
    RMDir "$SMPROGRAMS\Segway\${APPNAME}"
    RMDir /r "$INSTDIR\"

SectionEnd

; eof
