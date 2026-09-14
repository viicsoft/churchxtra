[Setup]
AppName=ChurchXtra AI
AppVersion=1.0.0
DefaultDirName={autopf}\ChurchXtra AI
DefaultGroupName=ChurchXtra AI
UninstallDisplayIcon={app}\ChurchAI.App.exe
SetupIconFile=C:\Churchxtra\ChurchAI.App\Assets\app_icon.ico
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir=C:\Churchxtra\installer\output
OutputBaseFilename=ChurchXtraAI_Setup
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "C:\Churchxtra\InstallerBuild\PublishOutput\*"; DestDir: "{app}"; Flags: ignoreversion recurseSubdirs createallsubdirs
Source: "C:\Churchxtra\ChurchAI.App\churchai_bible.db"; DestDir: "{localappdata}\ChurchAI"; Flags: ignoreversion
Source: "C:\keys\google-key.json"; DestDir: "C:\keys"; Flags: onlyifdoesntexist uninsneveruninstall

[Dirs]
Name: "{localappdata}\ChurchAI"
Name: "C:\keys"

[Icons]
Name: "{group}\ChurchXtra AI"; Filename: "{app}\ChurchAI.App.exe"
Name: "{autodesktop}\ChurchXtra AI"; Filename: "{app}\ChurchAI.App.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
; Register Windows Firewall rules for NDI across all network profiles (Public & Private)
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""ChurchXtra AI NDI Inbound"" dir=in action=allow program=""{app}\ChurchAI.App.exe"" enable=yes profile=any"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""ChurchXtra AI NDI Outbound"" dir=out action=allow program=""{app}\ChurchAI.App.exe"" enable=yes profile=any"; Flags: runhidden
; Run NDI 5 Runtime setup if present
Filename: "{app}\Dependencies\NDI 5 Runtime.exe"; Parameters: "/SILENT"; StatusMsg: "Installing NDI 5 Runtime..."; Check: HasNdiInstaller
; Launch app after install
Filename: "{app}\ChurchAI.App.exe"; Description: "Launch ChurchXtra AI"; Flags: nowait postinstall skipifsilent shellexec

[Code]
function HasNdiInstaller(): Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\Dependencies\NDI 5 Runtime.exe'));
end;
