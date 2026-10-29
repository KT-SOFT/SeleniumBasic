
#define ASSEMBLY_PATH ".\Selenium\bin\Release\Selenium.dll"
#define ASSEMBLY_VERSION GetFileVersion(ASSEMBLY_PATH) 

#define AppName "Selenium"
#define AppID "{{0277FC34-FD1B-4616-BB19-1FDB7381B291}"
#define AppLongName "SeleniumBasic KTOOL"
#define AppPublisher "KT-SOFT (SeleniumBasic fork)"
#define AppURL "https://github.com/KT-SOFT/SeleniumBasic"
#define AppFolder "SeleniumBasic"
#define AppSetupFilename "SeleniumBasic-Core-" + ASSEMBLY_VERSION

[Setup]

AppId={#AppID}
PrivilegesRequired=lowest 
AppName={#AppLongName}
AppVersion={#ASSEMBLY_VERSION}
AppVerName={#AppLongName}
VersionInfoVersion={#ASSEMBLY_VERSION}
VersionInfoTextVersion={#ASSEMBLY_VERSION}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DisableDirPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
CloseApplications=no
RestartApplications=no
MinVersion=10.0
DefaultDirName={code:GetRootDir}
UsePreviousAppDir=no
UsePreviousSetupType=yes
DefaultGroupName={#AppLongName}
DisableProgramGroupPage=yes
LicenseFile=.\LICENSE.txt
OutputDir="."
OutputBaseFilename={#AppSetupFilename}
;Compression=zip
Compression=lzma2
SolidCompression=yes
DirExistsWarning=no
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
MsgNETFrameworkNotInstalled=Microsoft .NET Framework 4.8 installation was not detected.
MsgRegistryWriteFailure=Failed to register the library due to insufficient privileges. 
MsgFileNotFound=File Not found: %1. 
MsgCOMInvokeFailed=Installation failed. The installer was unable to call the registered library. 

[Components]
Name: "pkg_core"; Description: ".Net core libraries";                         Types: full compact custom; Flags: fixed;
Name: "pkg_doc";  Description: "Templates and examples";                      Types: full compact custom;
Name: "pkg_cons"; Description: "Enhanced console runner for VBScript files";  Types: full compact custom;
Name: "pkg_ff";   Description: "WebDriver for Firefox";                       Types: full custom;
Name: "pkg_cr";   Description: "WebDriver for Chrome";                        Types: full custom;
Name: "pkg_op";   Description: "WebDriver for Opera";                         Types: full custom;
Name: "pkg_ie";   Description: "WebDriver for Internet Explorer";             Types: full custom;
Name: "pkg_edg";  Description: "WebDriver for Microsoft Edge";                Types: full custom;
Name: "pkg_pjs";  Description: "WebDriver for PhantomJS (headless browser)";  Types: full custom;
Name: "pkg_ide";  Description: "SeleniumIDE plugin for Firefox";              Types: full custom;

[Files]                                                                                             
Source: "Selenium\bin\Release\Selenium.dll";   DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;
Source: "Selenium\bin\Release\Selenium.pdb";   DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;
Source: "Selenium\bin\Release\Selenium32.tlb"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;
Source: "Selenium\bin\Release\Selenium64.tlb"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core; Check: IsWin64;
Source: "Selenium\bin\Release\SeleniumBasic.DriverSetup.exe"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;
Source: "KTOOL\README_KTOOL.txt"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;

Source: "Selenium\bin\Help\Selenium.chm"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_core;                                                                                                    

Source: "LICENSE.txt";   DestDir: "{app}"; Flags: ignoreversion overwritereadonly ; Attribs:readonly;
Source: "CHANGELOG.txt"; DestDir: "{app}"; Flags: ignoreversion overwritereadonly ; Attribs:readonly;

Source: "VbsConsole\bin\Release\vbsc.exe"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_cons;

;drivers
Source: "References\firefoxdriver.xpi"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_ff;
Source: "References\operadriver.exe";   DestDir: "{app}"; Flags: ignoreversion; Components: pkg_op;
Source: "References\phantomjs.exe";     DestDir: "{app}"; Flags: ignoreversion; Components: pkg_pjs;
Source: "References\iedriver.exe";      DestDir: "{app}"; Flags: ignoreversion; Components: pkg_ie;
;Source: "References\iedriver64.exe";    DestDir: "{app}"; Flags: ignoreversion; Components: pkg_ie;  Check: IsWin64;

;Firefox extensions
Source: "FirefoxAddons\bin\extensions.xpi"; DestDir: "{app}"; Flags: ignoreversion; Components: pkg_ide;

;examples                                                                                                                                                     
Source: "Scripts\*.*" ;               DestDir: "{app}\Scripts";             Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_core;
Source: "Templates\*.vbs" ;           DestDir: "{app}\Templates";           Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc;
Source: "Templates\Xlbin\*.xlt";      DestDir: "{app}\Templates";           Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasOldExcel
Source: "Templates\*.xltm" ;          DestDir: "{app}\Templates";           Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasNewExcel
Source: "Templates\*.au3" ;           DestDir: "{app}\Templates";           Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasAutoIt
Source: "Examples\VBScript\*.vbs";    DestDir: "{app}\Examples\VBScript";   Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; 
Source: "Examples\JavaScript\*.js";   DestDir: "{app}\Examples\JavaScript"; Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc;
Source: "Examples\Excel\Xlbin\*.xls"; DestDir: "{app}\Examples\Excel";      Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasOldExcel
Source: "Examples\Excel\*.xlsm";      DestDir: "{app}\Examples\Excel";      Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasNewExcel
Source: "Examples\AutoIt\*.au3";      DestDir: "{app}\Examples\AutoIt";     Flags: ignoreversion overwritereadonly; Attribs:readonly; Components: pkg_doc; Check: HasAutoIt

;copy config file

[Icons]
Name: "{group}\Project Home Page";  Filename: {#AppURL};                      WorkingDir: "{app}";
Name: "{group}\Vbs Console";        Filename: "{app}\vbsc.exe";               Components: pkg_cons;
Name: "{group}\Examples";           Filename: "{app}\Examples";               
Name: "{group}\Templates";          Filename: "{app}\Templates";              
Name: "{group}\RunCleaner";         Filename: "{app}\Scripts\RunCleaner.vbs";
Name: "{group}\API documentation";  Filename: "{app}\Selenium.chm";           
Name: "{group}\ChangeLog";          Filename: "{app}\CHANGELOG.txt";    
Name: "{group}\Uninstall";          Filename: "{uninstallexe}"

Name: "{group}\Start Firefox";      Filename: "{app}\Scripts\StartFirefox.vbs";          Components: pkg_ff;
Name: "{group}\Start Chrome";       Filename: "{app}\Scripts\StartChrome.vbs";           Components: pkg_cr;
Name: "{group}\Start Chrome Debug"; Filename: "{app}\Scripts\StartChromeDebug.vbs";      Components: pkg_cr;
Name: "{group}\Start IE";           Filename: "{app}\Scripts\StartInternetExplorer.vbs"; Components: pkg_ie;
Name: "{group}\Start Opera";        Filename: "{app}\Scripts\StartOpera.vbs";            Components: pkg_op;
Name: "{group}\Start PhantomJS";    Filename: "{app}\Scripts\StartPhantomJS.vbs";        Components: pkg_pjs;


; KTOOL: registration is limited to the Selenium COM library below.

[Run]
;Filename: "{app}\RegNet.exe"; Parameters: "-r"; WorkingDir: {app}; Flags: waituntilterminated runascurrentuser runhidden; StatusMsg: "Register for COM interoperability";
Filename: "{code:GetAppPath|firefox.exe}"; Parameters: "-url ""{app}\extensions.xpi"""; WorkingDir: {app}; Flags: shellexec postinstall skipifsilent runascurrentuser; Components: pkg_ide; Check: HasFirefox; Description: "Install the Selenium IDE Addon for Firefox";

[UninstallRun]
;Filename: "{app}\RegNet.exe"; Parameters: "-u"; WorkingDir: {app}; Flags: waituntilterminated runascurrentuser runhidden; StatusMsg: "Unregister for COM interoperability"; 

; KTOOL: preserve existing profiles, examples and other user-created files.

[Code]
var
  ALL_USERS : Boolean;
  HK : Integer;
  HK32 : Integer;

Function HasPrivileges(): Boolean;
  Begin
    Result := IsAdminLoggedOn Or IsPowerUserLoggedOn
  End;

Function GetRootDir(Param: String): String;
  Begin
    If ALL_USERS Then
      Result := ExpandConstant('{pf}\{#AppFolder}')
    Else
      Result := ExpandConstant('{localappdata}\{#AppFolder}');
  End;

Procedure InitRootKeys();
  Begin
    If ALL_USERS Then Begin
      HK := HKLM;
      HK32 := HKLM32;
    End Else Begin
      HK := HKCU;
      HK32 := HKCU32;
    End
  End;

Function GetAppPath(app: String): string;
  Begin
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\' + app, '', Result);
  End;

Function HasFirefox(): Boolean;
  Begin
    Result := FileExists(GetAppPath('firefox.exe'));
  End;

Function GetAppFolder(app: String): string;
  Begin
    Result := ExtractFileDir(GetAppPath(app));
  End;

Function HasApp(app: String): Boolean;
  Begin
    Result := GetAppPath(app) <> '';
  End;

Function GetOfficeVersion(app: String): String;
  Var ver: String; i: Integer;
  Begin
    If RegQueryStringValue(HKCR, app + '.Application\CurVer', '', ver) Then Begin
      For i := 1 To Length(ver) Do Begin
        If (ver[i] >= '0') And (ver[i] <= '9') Then
          Result := Result + ver[i];
      End;
    End;
  End;

Function HasExcel(): Boolean;
  Begin
    Result := RegKeyExists(HKCR, 'Excel.Application');
  End;

Function HasWord(): Boolean;
  Begin
    Result := RegKeyExists(HKCR, 'Word.Application');
  End;

Function HasNewExcel(): Boolean;
  Begin
    Result := RegKeyExists(HKCR, '.xlsm');
  End;

Function HasOldExcel(): Boolean;
  Begin
    Result := RegKeyExists(HKCR, '.xls') And Not RegKeyExists(HKCR, '.xlsm');
  End;

Function HasAutoIt(): Boolean;
  Begin
    Result := RegKeyExists(HKCR, '.au3');
  End;

Procedure _PatchOfficeFileVersion(Const subkey: String);
  Var name, value: String;
  Begin
    name := 'Maximum File Version Number';
    If RegQueryStringValue(HKLM, subkey, name, value) Then Begin
        RegWriteStringValue(HKLM, subkey, 'Maximum File Version', value);
        RegDeleteValue(HKLM, subkey, name);
    End
  End;

Procedure PatchOfficeFileVersion();
  Begin
    If Not HasPrivileges() Then Exit;
    //Excel 11
    _PatchOfficeFileVersion('SOFTWARE\Microsoft\.NETFramework\Policy\AppPatch\v4.0.30319.00000\excel.exe\{2CCAA9FE-6884-4AF2-99DD-5217B94115DF}'); 
    _PatchOfficeFileVersion('SOFTWARE\Microsoft\.NETFramework\Policy\AppPatch\v2.0.50727.00000\excel.exe\{2CCAA9FE-6884-4AF2-99DD-5217B94115DF}');
    //Word 11
    _PatchOfficeFileVersion('SOFTWARE\Microsoft\.NETFramework\Policy\AppPatch\v4.0.30319.00000\winword.exe\{2CCAA9FE-6884-4AF2-99DD-5217B94115DF}'); 
    _PatchOfficeFileVersion('SOFTWARE\Microsoft\.NETFramework\Policy\AppPatch\v2.0.50727.00000\winword.exe\{2CCAA9FE-6884-4AF2-99DD-5217B94115DF}'); 
  End;

Procedure AssertFrameworkPresent(Const version: String);
  Begin
    If Not RegKeyExists(HKLM,'SOFTWARE\Microsoft\NET Framework Setup\NDP\v' + version) Then
        RaiseException(ExpandConstant('{cm:MsgNETFrameworkNotInstalled}'));
  End;

Procedure AssertFilePresent(Const path: String);
  Begin
      If Not FileExists(path) Then
          RaiseException(ExpandConstant('{cm:MsgFileNotFound,' + path +'}'));
  End;

Function UninstallPrevious(const appid: String): Boolean;
  Var key, name, out_cmd: String; retcode: Integer;
  Begin
    key := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + ExpandConstant(appid) + '_is1';
    name := 'UninstallString';
    If Not RegQueryStringValue(HKLM, key, name, out_cmd) Then
      If Not RegQueryStringValue(HKLM32, key, name, out_cmd) Then
        If Not RegQueryStringValue(HKCU, key, name, out_cmd) Then
          RegQueryStringValue(HKCU32, key, name, out_cmd);
    If out_cmd <> '' Then
        Exec('>', out_cmd + ' /SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_SHOW, ewWaitUntilTerminated, retcode);
    Result := retcode = 0;
  End;

Function BoolToStr(const value: Boolean): String;
  Begin
    If value Then Result := '1' Else Result := '0';
  End;

//---------------------------------------------------------------------------------------
// Registration
//---------------------------------------------------------------------------------------

type
  TNetLib = record
    Guid: String;
    FullName: String;
    Description: String;
    TypeVersion: String;
    Directory: String;
    PathDll: String;
    PathTlb32: String;
    PathTlb64: String;
    Runtime: String;
  end;

Procedure RegString(Const root: Integer; Const subkey, name, value: String);
  Begin
      //Log('REG [' + subkey + '] "' + name + '"="' + value + '"');
      If Not RegWriteStringValue(root, subkey, name, value) Then
         RaiseException(ExpandConstant('{cm:MsgRegistryWriteFailure}')); 
  End;

// Interface registration (32/64bits Independent) :
// 
// HKCU\Software\Classes\Interface\{ interface guid }
//   ""=" interface name "
//   \ProxyStubClsid32
//     ""=" interface short name "
//   \TypeLib
//     ""="{ assembly guid }"
//   \Version
//     ""=" assembly type version "
//
Procedure RegInterface_(Const lib : TNetLib; Const root: Integer; Const guid, typename, proxyStub: String);
  Var key: String;
  Begin
    key := 'Software\Classes\Interface\' + guid;
    RegDeleteKeyIncludingSubkeys(root, key);
    If Not IsUninstaller Then Begin        
      RegString(root, key                        , ''         , typename         );
      RegString(root, key + '\ProxyStubClsid32'  , ''         , proxyStub        );
      RegString(root, key + '\TypeLib'           , ''         , lib.Guid         ); 
      RegString(root, key + '\TypeLib'           , 'Version'  , lib.TypeVersion  ); 
    End
  End;

Procedure RegInterface(Const lib : TNetLib; Const guid, name, proxystub: String);
  Begin        
    RegInterface_(lib, HK, guid, name, proxystub);
    If IsWin64 Then
      RegInterface_(lib, HK32, guid, name, proxystub);
  End;

// Enumeration registration (32/64bits Shared) :
// 
// HKCU\Software\Classes\Record\{ record guid }]
//   ""="mscoree.dll"
//   "Class"="Selenium.Structures.Point"
//   "Assembly"="Selenium, Version=2.0.1.2, Culture=neutral, PublicKeyToken=null"
//   "RuntimeVersion"="v2.0.50727"
//   "CodeBase"="C:\...\SeleniumBasic\Selenium.dll"
//
Procedure RegRecord(Const lib : TNetLib; Const guid, typename: String);
  Var key: String;
  Begin
    key := 'Software\Classes\Record\' + guid;
    RegDeleteKeyIncludingSubkeys(HK, key);
    If Not IsUninstaller Then Begin
      RegString(HK, key, 'Class'           , typename     );
      RegString(HK, key, 'Assembly'        , lib.FullName );
      RegString(HK, key, 'RuntimeVersion'  , lib.Runtime  ); 
      RegString(HK, key, 'CodeBase'        , lib.PathDll  );
    End
  End;

// CLSID registration (32/64bits Independent) :
// 
// Root\Software\Classes\CLSID\{ class guid }
//   ""="Selenium.WebDriver"
//   \InprocServer32
//     ""="C:\Windows\System32\mscoree.dll"
//     "Class"="Selenium.WebDriver"
//     "Assembly"="Selenium, Version=2.0.1.2, Culture=neutral, PublicKeyToken=null"
//     "RuntimeVersion"="v2.0.50727"
//     "CodeBase"="C:\...\SeleniumBasic\Selenium.dll"
//     "ThreadingModel"="Both"
//   \Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}
//     ""=""
//   \ProgId
//     ""="Selenium.WebDriver"
//   \VersionIndependentProgID
//     ""="Selenium.WebDriver"
// 
Procedure RegClsid(Const lib : TNetLib; Const root: Integer; Const guid, progid, typename, sysdir: String);
  Var key, skey : String;
  Begin
    key := 'Software\Classes\CLSID\' + guid;
    RegDeleteKeyIncludingSubkeys(root, key);
    If Not IsUninstaller Then Begin
      RegString(root, key, '', typename);
      
      skey := key + '\InprocServer32';
      RegString(root, skey, ''               , ExpandConstant(sysdir) + '\mscoree.dll' ); 
      RegString(root, skey, 'Class'          , typename     );
      RegString(root, skey, 'Assembly'       , lib.FullName );
      RegString(root, skey, 'RuntimeVersion' , lib.Runtime  ); 
      RegString(root, skey, 'CodeBase'       , lib.PathDll  );
      RegString(root, skey, 'ThreadingModel' , 'Both'       );

      skey := key + '\Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}';
      RegString(root, skey, '', '');

      skey := key + '\ProgId';
      RegString(root, skey, '', progid);

      skey := key + '\VersionIndependentProgID';
      RegString(root, skey, '', progid);
    End
  End;

// Class registration (32/64bits Shared) :
// 
// HKCU\Software\Classes\[progid]
//   ""=[progid]
//   \CLSID
//     ""=[class guid]
//
Procedure RegClass(Const lib : TNetLib; Const guid, progid, typename: String);
  Var key, sysdir: String;
  Begin
    key := 'Software\Classes\' + progid;
    RegDeleteKeyIncludingSubkeys(HK, key);
    If Not IsUninstaller Then Begin
      RegString(HK, key, '', progid);
      RegString(HK, key + '\CLSID', '', guid);
    End;

    RegClsid(lib, HK, guid, progid, typename, '{sys}');
    If IsWin64 Then
      RegClsid(lib, HK32, guid, progid, typename, '{syswow64}');
  End;

// TypeLib registration (32/64bits Shared) :
// 
// HKCU\Software\Classes\TypeLib\[assembly guid]
//   \2.0
//     ""="App Type Library"
//     \0\win32
//       ""="C:\...\App32.tlb"
//     \0\win64
//       ""="C:\...\App64.tlb"
//     \FLAGS
//       ""="0"
//     \HELPDIR
//       ""="C:\..."
//
Procedure RegTypeLib(Const lib : TNetLib);
  Var key, skey : String;
  Begin
    key := 'Software\Classes\TypeLib\' + lib.Guid;
    RegDeleteKeyIncludingSubkeys(HK, key);
    If Not IsUninstaller Then Begin
      skey := key + '\' + lib.TypeVersion;
      RegString(HK, skey, '', lib.Description); 
      RegString(HK, skey + '\FLAGS'   , ''  , '0'           ); 
      RegString(HK, skey + '\HELPDIR' , ''  , lib.Directory );
      RegString(HK, skey + '\0\win32' , ''  , lib.PathTlb32 );
      If IsWin64 Then
        RegString(HK, skey + '\0\win64', '' , lib.PathTlb64 );
    End
  End;

Procedure RegisterAssembly();
  Var lib : TNetLib;
  Begin
    { Includes the file generated by gen-registration.ipy }
    #include 'SeleniumBasicSetup.pas'
  End;

//---------------------------------------------------------------------------------------
// Workflow
//---------------------------------------------------------------------------------------
                   
Function InitializeSetup() : Boolean;
  Begin
    ALL_USERS := False; { Same-user install remains writable for automatic driver updates. }
    InitRootKeys();
    //AssertFrameworkPresent('3.5');
    Result := True;
  End;
      
Function InitializeUninstall() : Boolean;
  var p : string;
  Begin
    ALL_USERS := GetPreviousData('AllUsers', '') = '1';
    InitRootKeys();
    Result := True;
  End;

Procedure RegisterPreviousData(PreviousDataKey: Integer);
  Begin
    SetPreviousData(PreviousDataKey, 'AllUsers', BoolToStr(ALL_USERS));
  End;

Procedure CurPageChanged(CurPageID: Integer);
  var lines : TStrings;
  Begin
    If CurPageID = wpReady Then Begin
      lines := Wizardform.ReadyMemo.Lines;
      If ALL_USERS Then
        lines.Insert(0, 'Install folder / All Users :')
      Else
        lines.Insert(0, 'Install folder / Current User :');
        
     lines.Insert(1, ExpandConstant('{app}'));
     lines.Insert(2, '');
    End;
  End;

Procedure CurStepChanged(CurStep: TSetupStep);
  Var code: Integer;
  Begin
    If CurStep = ssPostInstall Then Begin
      RegisterAssembly();
      If Not Exec(ExpandConstant('{app}\SeleniumBasic.DriverSetup.exe'), '/all', ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, code) Then
        RaiseException('Automatic browser driver preparation could not start.');
      If code <> 0 Then
        RaiseException('Some browser drivers could not be prepared. See KTOOL_DriverSetup.log in the SeleniumBasic folder.');
    End;
  End;
Procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep); 
  Begin
    If CurUninstallStep = usUninstall  Then Begin
      RegisterAssembly(); //Only deletes the main keys
    End;
  End;
