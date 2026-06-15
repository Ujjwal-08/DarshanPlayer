; ================================
; Darshan Player Installer
; Professional Production Version
; Works with Velopack auto-updater
; ================================

[Setup]
; ----- App Info -----
AppId={{7F4B7A11-8C33-4E55-A111-DARSHANPLAYER}}
AppName=Darshan Player
AppVersion=1.1.0
AppPublisher=Ujjwal Dadhich
AppPublisherURL=https://chapterchase.com
AppSupportURL=https://chapterchase.com
AppUpdatesURL=https://chapterchase.com

; ----- Installation -----
DefaultDirName={autopf}\Darshan Player
DefaultGroupName=Darshan Player
DisableProgramGroupPage=yes

; ----- Output -----
OutputDir=C:\darshan\DarshanPlayer\Installer
OutputBaseFilename=DarshanPlayer-Setup

; ----- Installer UI -----
WizardStyle=modern
SetupIconFile=C:\darshan\DarshanPlayer\icon.ico

; ----- Compression -----
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMADictionarySize=65536

; ----- Architecture -----
ArchitecturesInstallIn64BitMode=x64compatible

; ----- Permissions -----
PrivilegesRequired=admin

; ----- Add/Remove Programs icon -----
UninstallDisplayIcon={app}\DarshanPlayer.exe

; ----- Version Info -----
VersionInfoVersion=1.1.0
VersionInfoCompany=Ujjwal Dadhich
VersionInfoDescription=Darshan Player Installer
VersionInfoProductName=Darshan Player

; ----- Misc -----
DisableDirPage=no
DisableReadyMemo=no
DisableWelcomePage=no
ChangesAssociations=yes

; ================================
; OPTIONAL TASKS (user chooses)
; ================================

[Tasks]
Name: "desktopicon";  Description: "Create a &desktop shortcut";          GroupDescription: "Shortcuts:";       Flags: unchecked
Name: "autostart";    Description: "Start Darshan Player with &Windows";  GroupDescription: "Startup:";         Flags: unchecked
Name: "contextmenu";  Description: """Play with Darshan Player"" right-click menu on all files"; GroupDescription: "Integration:"; Flags: checked

; ================================
; FILES
; ================================

[Files]
; Main app — Velopack will replace this on updates
Source: "C:\darshan\DarshanPlayer\publish\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; ================================
; SHORTCUTS
; ================================

[Icons]
; Start Menu
Name: "{autoprograms}\Darshan Player"; Filename: "{app}\DarshanPlayer.exe"

; Desktop shortcut (optional — user decides in task)
Name: "{autodesktop}\Darshan Player"; Filename: "{app}\DarshanPlayer.exe"; Tasks: desktopicon

; ================================
; REGISTRY — File Associations
; ================================

[Registry]
; ---- ProgID ----
Root: HKCR; Subkey: "DarshanPlayer.AssocFile";                          ValueType: string; ValueName: ""; ValueData: "Media File";                         Flags: uninsdeletekey
Root: HKCR; Subkey: "DarshanPlayer.AssocFile\DefaultIcon";              ValueType: string; ValueName: ""; ValueData: "{app}\DarshanPlayer.exe,0"
Root: HKCR; Subkey: "DarshanPlayer.AssocFile\shell\open\command";       ValueType: string; ValueName: ""; ValueData: """{app}\DarshanPlayer.exe"" ""%1"""

; ---- Video formats ----
Root: HKCR; Subkey: ".mp4\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".mkv\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".avi\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".mov\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".wmv\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".flv\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".webm\OpenWithProgids"; ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".m4v\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".ts\OpenWithProgids";   ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".m2ts\OpenWithProgids"; ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".3gp\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue

; ---- Audio formats ----
Root: HKCR; Subkey: ".mp3\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".aac\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".flac\OpenWithProgids"; ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".wav\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".ogg\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".m4a\OpenWithProgids";  ValueType: string; ValueName: "DarshanPlayer.AssocFile"; ValueData: ""; Flags: uninsdeletevalue

; ---- Default Programs registration (Windows Settings → Default Apps) ----
Root: HKLM; Subkey: "Software\DarshanPlayer";                                          Flags: uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities";                             ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Beautiful media player for Indian audiences"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities";                             ValueType: string; ValueName: "ApplicationName";        ValueData: "Darshan Player"
Root: HKLM; Subkey: "Software\RegisteredApplications";                                 ValueType: string; ValueName: "DarshanPlayer";          ValueData: "Software\DarshanPlayer\Capabilities"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".mp4";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".mkv";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".avi";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".mov";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".wmv";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".webm"; ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".mp3";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".flac"; ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".wav";  ValueData: "DarshanPlayer.AssocFile"
Root: HKLM; Subkey: "Software\DarshanPlayer\Capabilities\FileAssociations";            ValueType: string; ValueName: ".aac";  ValueData: "DarshanPlayer.AssocFile"

; ---- Right-click "Play with Darshan Player" on ALL files (optional task) ----
Root: HKCR; Subkey: "*\shell\Play with Darshan Player";              ValueType: string; ValueName: "";          ValueData: "Play with Darshan Player"; Flags: uninsdeletekey;  Tasks: contextmenu
Root: HKCR; Subkey: "*\shell\Play with Darshan Player";              ValueType: string; ValueName: "Icon";      ValueData: "{app}\DarshanPlayer.exe,0";                        Tasks: contextmenu
Root: HKCR; Subkey: "*\shell\Play with Darshan Player\command";      ValueType: string; ValueName: "";          ValueData: """{app}\DarshanPlayer.exe"" ""%1"""";              Tasks: contextmenu

; ---- Auto-start with Windows (optional task) ----
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DarshanPlayer"; ValueData: """{app}\DarshanPlayer.exe"""; Tasks: autostart; Flags: uninsdeletevalue

; ================================
; RUN AFTER INSTALL
; ================================

[Run]
; Launch app after install (user can uncheck)
Filename: "{app}\DarshanPlayer.exe"; \
    Description: "Launch Darshan Player now"; \
    Flags: nowait postinstall skipifsilent

; ================================
; UNINSTALL CLEANUP
; ================================

[UninstallDelete]
; Remove user data folder on uninstall
Type: filesandordirs; Name: "{localappdata}\DarshanPlayer"
