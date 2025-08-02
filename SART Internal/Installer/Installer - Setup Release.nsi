; Script generated with the Venis Install Wizard

; Define your application name
!system "GetVersionRelease.exe"
!include "Version.txt"
!define APPNAME "SART Internal"
!define APPNAMEANDVERSION "${APPNAME} ${VERSION}"

; Main Install settings
Name "${APPNAMEANDVERSION}"
InstallDir "c:\Segway\${APPNAME}"
InstallDirRegKey HKLM "Software\${APPNAME}" ""
OutFile "${APPNAME}.Setup - Release.${VERSION}.N.exe"

; Modern interface settings
!include "MUI.nsh"
!include LogicLib.nsh
!include x64.nsh

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

    RMDir /r "$INSTDIR\App_Data\"
    RMDir /r "$INSTDIR\App Data\"
    RMDir /r "$INSTDIR\Settings\"

    RMDir /r "$INSTDIR\Drivers"
    CreateDirectory "$INSTDIR\Drivers"
    SetOutPath "$INSTDIR\Drivers"
    File "..\..\..\..\..\tools\3rd Party Libraries\Systec CAN Driver\SO-387_V5.09\setup.exe" 

  ; Set Section Files and Shortcuts
  SetOutPath "$INSTDIR\"
  File "..\bin\Release\${APPNAME}.exe"

    File "..\bin\Release\ActiproSoftware.Editors.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.Gauge.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.Legacy.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.PropertyGrid.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.Shared.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.Themes.Office.Wpf.dll"
    File "..\bin\Release\ActiproSoftware.Views.Wpf.dll"
    File "..\bin\Release\Add Window.dll"
    File "..\bin\Release\Administration Module.dll"
    File "..\bin\Release\All_Diagnostics.dll"
    File "..\bin\Release\Assembly Line Web Service Client.dll"
    File "..\bin\Release\Authentication Objects.dll"
    File "..\bin\Release\Authentication Web Service Client.dll"
    File "..\bin\Release\Battery Info Module.dll"
    File "..\bin\Release\Battery Objects.dll"
    File "..\bin\Release\BlackBox.dll"
    File "..\bin\Release\BlowFish.dll"
    File "..\bin\Release\CAN Objects.dll"
    File "..\bin\Release\CAN Systec.dll"
    File "..\bin\Release\CAN.dll"
    File "..\bin\Release\CAN2 Commands.dll"
    File "..\bin\Release\CAN2 Messages.dll"
    File "..\bin\Release\COFF.dll"
    File "..\bin\Release\Common.dll"
    File "..\bin\Release\CU Code Load.dll"
    File "..\bin\Release\CU Log Module.dll"
    File "..\bin\Release\DatabaseHelper.dll"
    File "..\bin\Release\DateTimePicker.dll"
    File "..\bin\Release\Diagnostics Helper.dll"
    File "..\bin\Release\Email Segway Module.dll"
    File "..\bin\Release\HeaderBar.dll"
    File "..\bin\Release\JTagsModule.dll"
    File "..\bin\Release\List Box Vertical Tool Bar.dll"
    File "..\bin\Release\Manufacturing Tables Web Service Client.dll"
    File "..\bin\Release\Microsoft.Expression.Controls.dll"
    File "..\bin\Release\Microsoft.Expression.Drawing.dll"
    File "..\bin\Release\Microsoft.Practices.Prism.dll"
    File "..\bin\Release\Microsoft.Practices.Prism.Interactivity.dll"
    File "..\bin\Release\Microsoft.Practices.Prism.UnityExtensions.dll"
    File "..\bin\Release\Microsoft.Practices.ServiceLocation.dll"
    File "..\bin\Release\Microsoft.Practices.Unity.dll"
    File "..\bin\Release\MotorTests.dll"
    File "..\bin\Release\MultiLevelToolBar.dll"
    File "..\bin\Release\NHibernateBase.dll"
    File "..\bin\Release\NLog.dll"
    File "..\bin\Release\PDF.dll"
    File "..\bin\Release\PdfSharp.dll"
    File "..\bin\Release\ProgressionLogViewer.dll"
    File "..\bin\Release\Progression_Window.dll"
    File "..\bin\Release\PT_BSA.dll"
    File "..\bin\Release\PT_LED.dll"
    File "..\bin\Release\Repair Module.dll"
    File "..\bin\Release\Reports Module.dll"
    File "..\bin\Release\Ride Test Module.dll"
    File "..\bin\Release\RiderDetect.dll"
    File "..\bin\Release\Runtime Logs Web Service Client.dll"
    File "..\bin\Release\SART 2012 Web Service Client.dll"
    File "..\bin\Release\SART Diagnostic.dll"
    File "..\bin\Release\SART Disclaimer Module.dll"
    File "..\bin\Release\SART Objects.dll"
    File "..\bin\Release\SARTInfrastructure.dll"
    File "..\bin\Release\Segway Objects.dll"
    File "..\bin\Release\SegwayShellControls.dll"
    File "..\bin\Release\Segway_Login_Module.dll"
    File "..\bin\Release\SQL Reporting Objects.dll"
    File "..\bin\Release\Status Bar Control.dll"
    File "..\bin\Release\System.Windows.Interactivity.dll"
    File "..\bin\Release\Syteline Objects.dll"
    File "..\bin\Release\Title Bar Control.dll"
    File "..\bin\Release\UcanDotNET.dll"
    File "..\bin\Release\UnderConstruction.dll"
    File "..\bin\Release\Updater Web Service Client.dll"
    File "..\bin\Release\USBCAN32.dll"
    File "..\bin\Release\WorkOrderModule.dll"
    File "..\bin\Release\WPFResources.dll"
    File "..\bin\Release\WPFVisifire.Charts.dll"
    File "..\bin\Release\WPFVisifire.Gauges.dll"

    File "..\bin\Release\Add Window.pdb"
    File "..\bin\Release\Administration Module.pdb"
    File "..\bin\Release\All_Diagnostics.pdb"
    File "..\bin\Release\Assembly Line Web Service Client.pdb"
    File "..\bin\Release\Authentication Objects.pdb"
    File "..\bin\Release\Authentication Web Service Client.pdb"
    File "..\bin\Release\Battery Info Module.pdb"
    File "..\bin\Release\Battery Objects.pdb"
    File "..\bin\Release\BlackBox.pdb"
    File "..\bin\Release\BlowFish.pdb"
    File "..\bin\Release\CAN Objects.pdb"
    File "..\bin\Release\CAN Systec.pdb"
    File "..\bin\Release\CAN.pdb"
    File "..\bin\Release\CAN2 Commands.pdb"
    File "..\bin\Release\CAN2 Messages.pdb"
    File "..\bin\Release\COFF.pdb"
    File "..\bin\Release\Common.pdb"
    File "..\bin\Release\CU Code Load.pdb"
    File "..\bin\Release\CU Log Module.pdb"
    File "..\bin\Release\DatabaseHelper.pdb"
    File "..\bin\Release\DateTimePicker.pdb"
    File "..\bin\Release\Diagnostics Helper.pdb"
    File "..\bin\Release\Email Segway Module.pdb"
    File "..\bin\Release\HeaderBar.pdb"
    File "..\bin\Release\JTagsModule.pdb"
    File "..\bin\Release\List Box Vertical Tool Bar.pdb"
    File "..\bin\Release\Manufacturing Tables Web Service Client.pdb"
    File "..\bin\Release\Microsoft.Practices.Prism.Interactivity.pdb"
    File "..\bin\Release\Microsoft.Practices.Prism.pdb"
    File "..\bin\Release\Microsoft.Practices.Prism.UnityExtensions.pdb"
    File "..\bin\Release\MultiLevelToolBar.pdb"
    File "..\bin\Release\NHibernateBase.pdb"
    File "..\bin\Release\PDF.pdb"
    File "..\bin\Release\PdfSharp.pdb"
    File "..\bin\Release\ProgressionLogViewer.pdb"
    File "..\bin\Release\Progression_Window.pdb"
    File "..\bin\Release\PT_BSA.pdb"
    File "..\bin\Release\PT_LED.pdb"
    File "..\bin\Release\Repair Module.pdb"
    File "..\bin\Release\Reports Module.pdb"
    File "..\bin\Release\Ride Test Module.pdb"
    File "..\bin\Release\RiderDetect.pdb"
    File "..\bin\Release\Runtime Logs Web Service Client.pdb"
    File "..\bin\Release\SART 2012 Web Service Client.pdb"
    File "..\bin\Release\SART Diagnostic.pdb"
    File "..\bin\Release\SART Disclaimer Module.pdb"
    File "..\bin\Release\SART Internal.pdb"
    File "..\bin\Release\SART Objects.pdb"
    File "..\bin\Release\SARTInfrastructure.pdb"
    File "..\bin\Release\Segway Objects.pdb"
    File "..\bin\Release\SegwayShellControls.pdb"
    File "..\bin\Release\Segway_Login_Module.pdb"
    File "..\bin\Release\SQL Reporting Objects.pdb"
    File "..\bin\Release\Status Bar Control.pdb"
    File "..\bin\Release\Syteline Objects.pdb"
    File "..\bin\Release\Title Bar Control.pdb"
    File "..\bin\Release\UnderConstruction.pdb"
    File "..\bin\Release\Updater Web Service Client.pdb"
    File "..\bin\Release\WorkOrderModule.pdb"
    File "..\bin\Release\WPFResources.pdb"

    File "..\bin\Release\${APPNAME}.exe.Config"

    SetOutPath "$INSTDIR\App Data"
    Delete "$INSTDIR\App Data\ToolBar Permissions.bf"
    Delete "$INSTDIR\App Data\ToolBar Groups.bf"
    Delete "$INSTDIR\App Data\ToolBar Groups.xml"
    Delete "$INSTDIR\App Data\ToolBar Permissions.xml"

    SetOutPath "$INSTDIR\Images"
    File "..\bin\Release\Images\Close-Icon.png"
    File "..\bin\Release\Images\Maximize-Icon.png"
    File "..\bin\Release\Images\Minimize-Icon.png"
    File "..\bin\Release\Images\Restore-Icon.png"
    File "..\bin\Release\Images\TitleBarImage.png"


    SetOutPath "$INSTDIR\"
    CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${APPNAME}.exe"
    CreateDirectory "$SMPROGRAMS\Segway\${APPNAME}"
    CreateShortCut "$SMPROGRAMS\Segway\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPNAME}.exe"
    CreateShortCut "$SMPROGRAMS\Segway\${APPNAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe"

	ExecWait '"$INSTDIR\Drivers\setup.exe"'

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
