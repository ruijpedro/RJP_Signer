#define MyAppName "RJP Signer Bridge"
#ifndef MyAppVersion
#define MyAppVersion "1.2.9"
#endif
#define MyAppPublisher "RJP"
#define MyAppExeName "RJP.Signer.Bridge.exe"

[Setup]
AppId={{D37EE3F1-ACF1-4E19-AE58-4E0643B65D41}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\RJP Signer Bridge
DefaultGroupName=RJP Signer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=RJP_Signer_Bridge_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\RJP.Signer.Bridge\bin\Release\RJP.Signer.Bridge.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\RJP.Signer.Bridge\bin\Release\RJP.Signer.Bridge.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README_BRIDGE.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\RJP Signer Bridge"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\RJP Signer Bridge"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "RJP Signer Bridge"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

[Tasks]
Name: "desktopicon"; Description: "Criar atalho no ambiente de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar RJP Signer Bridge"; Flags: nowait postinstall skipifsilent


[Code]
procedure StopRunningBridge();
var
  ResultCode: Integer;
begin
  { Garante que o EXE antigo não fica residente na área de notificação. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1000);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    StopRunningBridge();
end;
