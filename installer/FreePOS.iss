; ============================================
; FreePOS Inno Setup Installer Script
; ============================================
; Build command: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" FreePOS.iss

#define MyAppName "FreePOS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "FreePOS"
#define MyAppExeName "MyWinFormsApp.exe"
#define MyAppURL "https://freepos.app"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=FreePOS-Setup-{#MyAppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked
Name: "installpostgres"; Description: "Install PostgreSQL 17 (required if not already installed)"; GroupDescription: "Database:"; Flags: checked

[Files]
; FreePOS application files (self-contained publish output)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Database setup files
Source: "schema.sql"; DestDir: "{app}\db"; Flags: ignoreversion
Source: "setup-db.bat"; DestDir: "{app}\db"; Flags: ignoreversion

; PostgreSQL installer (download separately, place in installer/ folder)
Source: "postgresql-17-windows-x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Tasks: installpostgres; Check: not IsPostgreSQLInstalled

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Install PostgreSQL silently (only if checkbox selected and not already installed)
Filename: "{tmp}\postgresql-17-windows-x64.exe"; \
    Parameters: "--mode unattended --superpassword postgres --servicename postgresql-17 --servicepassword postgres --serverport 5432"; \
    StatusMsg: "Installing PostgreSQL 17..."; \
    Flags: waituntilterminated; \
    Tasks: installpostgres; \
    Check: not IsPostgreSQLInstalled

; Wait for PostgreSQL service to start
Filename: "cmd.exe"; \
    Parameters: "/c timeout /t 5 /nobreak >nul"; \
    StatusMsg: "Waiting for PostgreSQL service to start..."; \
    Flags: waituntilterminated runhidden

; Run database setup
Filename: "cmd.exe"; \
    Parameters: "/c ""{app}\db\setup-db.bat"""; \
    StatusMsg: "Setting up FreePOS database..."; \
    Flags: waituntilterminated runhidden

; Launch FreePOS after install
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch {#MyAppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
// Check if PostgreSQL is already installed
function IsPostgreSQLInstalled: Boolean;
var
  ResultCode: Integer;
begin
  Result := False;

  // Check for PostgreSQL service
  if Exec('sc.exe', 'query postgresql-17', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
      Result := True;
  end;

  // Also check common install locations
  if not Result then
  begin
    if DirExists('C:\Program Files\PostgreSQL\17') or
       DirExists('C:\Program Files\PostgreSQL\18') then
      Result := True;
  end;
end;

// Check if the PostgreSQL installer file exists (warn if not)
function InitializeSetup: Boolean;
begin
  Result := True;

  if IsTaskSelected('installpostgres') and not IsPostgreSQLInstalled then
  begin
    if not FileExists(ExpandConstant('{src}\postgresql-17-windows-x64.exe')) then
    begin
      if MsgBox('PostgreSQL installer not found in the setup directory.' + #13#10 +
                 'The database will not be set up automatically.' + #13#10#13#10 +
                 'You can install PostgreSQL manually later.' + #13#10#13#10 +
                 'Continue with installation?', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;
