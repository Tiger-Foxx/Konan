; Script professionnel pour l'installation de Konan - Version Portable
; Date de création : 2025-05-24
; Auteur : theTigerFox - Le Gestionnaire de Presse-Papiers Ninja

#define MyAppName "Konan"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Fox"
#define MyAppURL "https://konan.the-fox.tech"
#define MyAppExeName "Konan.exe"
#define MyAppDescription "Gestionnaire de Presse-Papiers Intelligent"
#define MyAppGUID "3A9C8D7E-6F2B-4C5A-8E1D-9B7F3A2C5E8D"

[Setup]
; Informations d'application
AppId={#MyAppGUID}
AppName={#MyAppName} Portable
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} Portable {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL=https://github.com/Tiger-Foxx/Konan
AppUpdatesURL={#MyAppURL}
AppCopyright=© 2025 {#MyAppPublisher} - the-fox.tech
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription} - Version Portable
VersionInfoProductName={#MyAppName} Portable

; Configuration d'installation
DefaultDirName={autopf}\{#MyAppName} Portable
DefaultGroupName={#MyAppName} Portable
DisableProgramGroupPage=yes
LicenseFile=LICENSE.TXT
InfoBeforeFile=Before.txt
OutputDir=Setup
OutputBaseFilename=Konan_Portable_Setup_v{#MyAppVersion}
SetupIconFile=Assets\Icons\fox.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression maximale - Plus solide méthode
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=ultra64
CompressionThreads=auto

; Apparence moderne
WizardStyle=modern
WizardResizable=no
DisableWelcomePage=no
WizardSizePercent=120

; Sécurité et compatibilité
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

; Configuration avancée
DisableDirPage=auto
UsePreviousAppDir=yes
AllowNoIcons=no
ShowComponentSizes=yes
CreateUninstallRegKey=yes
Uninstallable=yes

; App de démarrage Windows
ChangesEnvironment=no
ChangesAssociations=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis:"
Name: "startupicon"; Description: "Démarrer automatiquement avec Windows (Recommandé)"; GroupDescription: "Options de démarrage:"
Name: "systrayicon"; Description: "Lancer en arrière-plan dans la barre des tâches"; GroupDescription: "Options de démarrage:"

[Files]
; Application principale - Version Portable (Self-Contained)
; UN SEUL FICHIER EXÉCUTABLE + fichiers de debug optionnels
Source: "Releases\v1.0.0\Konan-Portable-x64\Konan.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Releases\v1.0.0\Konan-Portable-x64\Konan.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Releases\v1.0.0\Konan-Portable-x64\Konan.xml"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Ressources et documentation
Source: "Assets\Icons\fox.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "Assets\Icons\fox.png"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "LICENSE.TXT"; DestDir: "{app}"; Flags: ignoreversion
Source: "Before.txt"; DestDir: "{app}\docs"; DestName: "README.txt"; Flags: ignoreversion

[Registry]
; Enregistrement de l'application
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName} Portable"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName} Portable"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName} Portable"; ValueType: dword; ValueName: "InstallDate"; ValueData: "20250524"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName} Portable"; ValueType: string; ValueName: "Edition"; ValueData: "Portable"; Flags: uninsdeletekey

; Démarrage automatique (optionnel)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName} Portable"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Tasks: startupicon; Flags: uninsdeletevalue

[Icons]
Name: "{group}\{#MyAppName} Portable"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription} - Version Portable - Ctrl+Shift+V pour ouvrir"
Name: "{group}\Désinstaller {#MyAppName} Portable"; Filename: "{uninstallexe}"; Comment: "Désinstaller {#MyAppName} Portable"
Name: "{autodesktop}\{#MyAppName} Portable"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription} - Le ninja du presse-papiers (Portable)"; Tasks: desktopicon
Name: "{group}\Documentation"; Filename: "{app}\docs\README.txt"; Comment: "Guide d'utilisation et informations"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName} maintenant"; Flags: nowait postinstall skipifsilent; Parameters: "--minimized"; Tasks: systrayicon
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName} maintenant"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Fermer l'application avant désinstallation
Filename: "taskkill.exe"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillKonanPortable"

[Messages]
WelcomeLabel1=Bienvenue dans l'installation de {#MyAppName} Portable ! 🦊
WelcomeLabel2=Vous allez installer {#MyAppName} Portable version {#MyAppVersion}, le gestionnaire de presse-papiers intelligent qui révolutionne votre Ctrl+C/Ctrl+V !%n%n✅ VERSION PORTABLE - Aucun .NET requis !%n✅ Historique infini, recherche instantanée, favoris permanents%n✅ Tout inclus dans un seul exécutable de ~80MB%n%nFermez toutes les applications avant de continuer.
FinishedHeadingLabel=Installation de {#MyAppName} Portable terminée avec succès ! ✅
FinishedLabel={#MyAppName} Portable est maintenant installé et prêt à transformer votre productivité !%n%n🚀 Aucune dépendance externe requise%n🎯 Utilisez Ctrl+Shift+V pour ouvrir votre nouvel historique ninja%n🦊 Version autonome qui fonctionne partout%n%nEnjoy the power of the portable clipboard! 🦊
ClickFinish=Cliquez sur Terminer pour quitter l'installation.
SelectDirLabel3=L'installation va copier {#MyAppName} Portable dans le dossier suivant.
SelectDirBrowseLabel=Cliquez sur Suivant pour continuer, ou sur Parcourir pour choisir un autre dossier.

[Code]
// Vérifier si l'application est en cours d'exécution
function IsAppRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('tasklist.exe', '/FI "IMAGENAME eq {#MyAppExeName}" /NH', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
      Result := True;
  end;
end;

// Préparation avant installation
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  
  // Fermer l'application si elle est en cours d'exécution
  if IsAppRunning() then
  begin
    if MsgBox('Konan est actuellement en cours d''exécution.' + #13#10 + 
              'Voulez-vous le fermer automatiquement pour continuer l''installation ?', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Sleep(1500); // Attendre 1.5 secondes
    end
    else
    begin
      Result := 'Veuillez fermer Konan manuellement avant de continuer l''installation.';
    end;
  end;
end;

// Personnaliser l'apparence et afficher les avantages de la version Portable
procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel1.Font.Size := 12;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];
end;

// Message d'information sur les avantages de la version Portable
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectDir then
  begin
    // Afficher un message informatif sur la version Portable
    MsgBox('💡 AVANTAGES DE LA VERSION PORTABLE :' + #13#10 + #13#10 +
           '✅ Aucune installation de .NET requise' + #13#10 +
           '✅ Fonctionne immédiatement sur tout PC Windows 10/11' + #13#10 +
           '✅ Parfait pour les clés USB ou environnements restreints' + #13#10 +
           '✅ Un seul fichier exécutable de ~80MB' + #13#10 +
           '✅ Toutes les dépendances embarquées' + #13#10 + #13#10 +
           'Cette version est recommandée pour la plupart des utilisateurs !', 
           mbInformation, MB_OK);
  end;
end;

// Message de fin personnalisé
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // L'installation est terminée
    // On pourrait ajouter des actions post-installation ici si nécessaire
  end;
end;