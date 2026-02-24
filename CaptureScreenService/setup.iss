#define MyAppName "CaptureScreenService"
#define MyAppVersion "0.3"
#define MyAppPublisher "CaptureScreenService"
#define MyAppURL "https://github.com/example/CaptureScreenService"

[Setup]
AppId={{60683D96-A95D-4FB3-904D-0609DA9CC70C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=Output
OutputBaseFilename=CaptureScreenService_Setup
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
UninstallDisplayIcon={app}\CaptureScreenService.exe
UninstallDisplayName={#MyAppName}
UpdateUninstallLogAppName=no
CreateAppDir=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net9.0\win-x64\publish\CaptureScreenService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net9.0\win-x64\publish\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "bin\Release\net9.0\win-x64\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\CaptureScreenService.exe"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\CaptureScreenService.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CaptureScreenService.exe"; Parameters: "--install"; Flags: nowait postinstall skipifsilent; Description: "Install Windows Service"

[UninstallRun]
Filename: "{app}\CaptureScreenService.exe"; Parameters: "--uninstall"; RunWaitId: "UninstallService"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
var
  StorageModePage: TWizardPage;
  StorageModeRadioLocal: TRadioButton;
  StorageModeRadioEmail: TRadioButton;
  
  LocalPathPage: TWizardPage;
  LocalPathEdit: TEdit;
  LocalPathBrowseBtn: TButton;
  
  EmailProviderPage: TWizardPage;
  EmailProviderRadioQQ: TRadioButton;
  EmailProviderRadioNetEase: TRadioButton;
  QQAuthLinkLabel: TLabel;
  NetEaseAuthLinkLabel: TLabel;
  
  EmailConfigPage: TWizardPage;
  EmailAddressEdit: TEdit;
  AuthCodeEdit: TEdit;
  EmailAddressLabel: TLabel;
  AuthCodeLabel: TLabel;
  
  IsUpdate: Boolean;

function InitializeSetup(): Boolean;
var
  OldPath: string;
begin
  Result := True;
  IsUpdate := RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}_is1',
    'InstallLocation', OldPath);
end;

procedure InitializeWizard;
begin
  StorageModePage := CreateCustomPage(wpSelectDir,
    'Storage Mode', 'Select how to save screenshots');
  
  StorageModeRadioLocal := TRadioButton.Create(StorageModePage);
  StorageModeRadioLocal.Parent := StorageModePage.Surface;
  StorageModeRadioLocal.Caption := 'Local Storage - Save screenshots to local folder';
  StorageModeRadioLocal.Left := 0;
  StorageModeRadioLocal.Top := 10;
  StorageModeRadioLocal.Width := StorageModePage.SurfaceWidth;
  StorageModeRadioLocal.Checked := True;
  
  StorageModeRadioEmail := TRadioButton.Create(StorageModePage);
  StorageModeRadioEmail.Parent := StorageModePage.Surface;
  StorageModeRadioEmail.Caption := 'Email - Send screenshots via email';
  StorageModeRadioEmail.Left := 0;
  StorageModeRadioEmail.Top := 35;
  StorageModeRadioEmail.Width := StorageModePage.SurfaceWidth;
  
  LocalPathPage := CreateCustomPage(StorageModePage.ID,
    'Local Storage Path', 'Select folder to save screenshots');
  
  LocalPathEdit := TEdit.Create(LocalPathPage);
  LocalPathEdit.Parent := LocalPathPage.Surface;
  LocalPathEdit.Left := 0;
  LocalPathEdit.Top := 10;
  LocalPathEdit.Width := LocalPathPage.SurfaceWidth - 90;
  LocalPathEdit.Text := 'C:\temp\TempPics';
  
  LocalPathBrowseBtn := TButton.Create(LocalPathPage);
  LocalPathBrowseBtn.Parent := LocalPathPage.Surface;
  LocalPathBrowseBtn.Caption := 'Browse...';
  LocalPathBrowseBtn.Left := LocalPathPage.SurfaceWidth - 80;
  LocalPathBrowseBtn.Top := 8;
  LocalPathBrowseBtn.Width := 80;
  
  EmailProviderPage := CreateCustomPage(StorageModePage.ID,
    'Email Provider', 'Select your email provider');
  
  EmailProviderRadioQQ := TRadioButton.Create(EmailProviderPage);
  EmailProviderRadioQQ.Parent := EmailProviderPage.Surface;
  EmailProviderRadioQQ.Caption := 'QQ Mail (smtp.qq.com:587)';
  EmailProviderRadioQQ.Left := 0;
  EmailProviderRadioQQ.Top := 10;
  EmailProviderRadioQQ.Width := EmailProviderPage.SurfaceWidth;
  EmailProviderRadioQQ.Checked := True;
  
  QQAuthLinkLabel := TLabel.Create(EmailProviderPage);
  QQAuthLinkLabel.Parent := EmailProviderPage.Surface;
  QQAuthLinkLabel.Caption := 'Get QQ Mail authorization code: https://wx.mail.qq.com/list/readtemplate?name=app_intro.html#/agreement/authorizationCode';
  QQAuthLinkLabel.Left := 20;
  QQAuthLinkLabel.Top := 35;
  QQAuthLinkLabel.Width := EmailProviderPage.SurfaceWidth - 20;
  QQAuthLinkLabel.Font.Color := clBlue;
  QQAuthLinkLabel.Cursor := crHand;
  
  EmailProviderRadioNetEase := TRadioButton.Create(EmailProviderPage);
  EmailProviderRadioNetEase.Parent := EmailProviderPage.Surface;
  EmailProviderRadioNetEase.Caption := 'NetEase Mail (smtp.163.com:465)';
  EmailProviderRadioNetEase.Left := 0;
  EmailProviderRadioNetEase.Top := 60;
  EmailProviderRadioNetEase.Width := EmailProviderPage.SurfaceWidth;
  
  NetEaseAuthLinkLabel := TLabel.Create(EmailProviderPage);
  NetEaseAuthLinkLabel.Parent := EmailProviderPage.Surface;
  NetEaseAuthLinkLabel.Caption := 'Get NetEase Mail authorization code: https://help.mail.163.com/faqDetail.do?code=d7a5dc8471cd0c0e8b4b8f4f8e49998b374173cfe9171305fa1ce630d7f67ac2a5feb28b66796d3b';
  NetEaseAuthLinkLabel.Left := 20;
  NetEaseAuthLinkLabel.Top := 85;
  NetEaseAuthLinkLabel.Width := EmailProviderPage.SurfaceWidth - 20;
  NetEaseAuthLinkLabel.Font.Color := clBlue;
  NetEaseAuthLinkLabel.Cursor := crHand;
  
  EmailConfigPage := CreateCustomPage(EmailProviderPage.ID,
    'Email Configuration', 'Enter your email settings');
  
  EmailAddressLabel := TLabel.Create(EmailConfigPage);
  EmailAddressLabel.Parent := EmailConfigPage.Surface;
  EmailAddressLabel.Caption := 'Email Address:';
  EmailAddressLabel.Left := 0;
  EmailAddressLabel.Top := 10;
  
  EmailAddressEdit := TEdit.Create(EmailConfigPage);
  EmailAddressEdit.Parent := EmailConfigPage.Surface;
  EmailAddressEdit.Left := 0;
  EmailAddressEdit.Top := 30;
  EmailAddressEdit.Width := EmailConfigPage.SurfaceWidth;
  
  AuthCodeLabel := TLabel.Create(EmailConfigPage);
  AuthCodeLabel.Parent := EmailConfigPage.Surface;
  AuthCodeLabel.Caption := 'Authorization Code (not your email password):';
  AuthCodeLabel.Left := 0;
  AuthCodeLabel.Top := 60;
  
  AuthCodeEdit := TEdit.Create(EmailConfigPage);
  AuthCodeEdit.Parent := EmailConfigPage.Surface;
  AuthCodeEdit.Left := 0;
  AuthCodeEdit.Top := 80;
  AuthCodeEdit.Width := EmailConfigPage.SurfaceWidth;
  AuthCodeEdit.PasswordChar := '*';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  if PageID = LocalPathPage.ID then
    Result := StorageModeRadioEmail.Checked
  else if (PageID = EmailProviderPage.ID) or (PageID = EmailConfigPage.ID) then
    Result := StorageModeRadioLocal.Checked;
end;

function GetAppConfigPath: string;
begin
  Result := ExpandConstant('{app}\appsettings.json');
end;

procedure SaveConfigToFile;
var
  ConfigFile: string;
  ConfigContent: string;
  StorageMode: string;
  EmailProvider: string;
  SmtpServer: string;
  SmtpPort: string;
begin
  ConfigFile := GetAppConfigPath;
  
  if StorageModeRadioLocal.Checked then
    StorageMode := 'Local'
  else
    StorageMode := 'Email';
  
  if EmailProviderRadioQQ.Checked then
  begin
    EmailProvider := 'QQ';
    SmtpServer := 'smtp.qq.com';
    SmtpPort := '587';
  end
  else
  begin
    EmailProvider := 'NetEase';
    SmtpServer := 'smtp.163.com';
    SmtpPort := '465';
  end;
  
  ConfigContent := '{' + #13#10 +
    '  "Logging": {' + #13#10 +
    '    "LogLevel": {' + #13#10 +
    '      "Default": "Information"' + #13#10 +
    '    },' + #13#10 +
    '    "EventLog": {' + #13#10 +
    '      "SourceName": "ScreenCapSvc",' + #13#10 +
    '      "LogName": "Application",' + #13#10 +
    '      "LogLevel": {' + #13#10 +
    '        "Microsoft": "Warning",' + #13#10 +
    '        "Microsoft.Hosting.Lifetime": "Information",' + #13#10 +
    '        "CaptureScreenService": "Information"' + #13#10 +
    '      }' + #13#10 +
    '    }' + #13#10 +
    '  },' + #13#10 +
    '  "AppConfig": {' + #13#10 +
    '    "StorageMode": "' + StorageMode + '",' + #13#10 +
    '    "CaptureIntervalMinutes": 5,' + #13#10 +
    '    "Local": {' + #13#10 +
    '      "SavePath": "' + StringReplace(LocalPathEdit.Text, '\', '\\', [rfReplaceAll]) + '"' + #13#10 +
    '    },' + #13#10 +
    '    "Email": {' + #13#10 +
    '      "Provider": "' + EmailProvider + '",' + #13#10 +
    '      "SmtpServer": "' + SmtpServer + '",' + #13#10 +
    '      "SmtpPort": ' + SmtpPort + ',' + #13#10 +
    '      "EmailAddress": "' + EmailAddressEdit.Text + '",' + #13#10 +
    '      "EncryptedAuthCode": "' + AuthCodeEdit.Text + '"' + #13#10 +
    '    }' + #13#10 +
    '  },' + #13#10 +
    '  "Guardian": {' + #13#10 +
    '    "RestartDelayMinutes": 5' + #13#10 +
    '  }' + #13#10 +
    '}';
  
  SaveStringToFile(ConfigFile, ConfigContent, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveConfigToFile;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    if MsgBox('Do you want to remove the configuration file?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DeleteFile(GetAppConfigPath);
    end;
  end;
end;
