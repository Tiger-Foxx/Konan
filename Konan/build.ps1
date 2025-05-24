<#
.SYNOPSIS
    Script de build professionnel pour Konan - VERSION CORRIGÉE
.DESCRIPTION
    Génère les builds Self-Contained et Framework-Dependent avec optimisations
.AUTHOR
    Fox (theTigerFox)
#>

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Clean = $false,
    [switch]$Verbose = $false,
    [switch]$SingleFileOnly = $false  # Nouveau paramètre
)

# Configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Couleurs pour output
function Write-Success { param($Message) Write-Host "✅ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ️  $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠️  $Message" -ForegroundColor Yellow }
function Write-ErrorMsg { param($Message) Write-Host "❌ $Message" -ForegroundColor Red }

# Banner
Write-Host @"
🦊 ===================================
   KONAN BUILD SYSTEM v1.1
   Clipboard Manager Pro Builder
=================================== 🦊
"@ -ForegroundColor Red

Write-Info "Configuration: $Configuration"
Write-Info "Version: $Version"
Write-Info "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Info "PowerShell: $($PSVersionTable.PSVersion)"
Write-Info "Single File Only: $SingleFileOnly"

# Nettoyage si demandé
if ($Clean) {
    Write-Info "🧹 Nettoyage des builds précédents..."
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }
    if (Test-Path "Releases") { Remove-Item -Path "Releases" -Recurse -Force }
    Write-Success "Nettoyage terminé"
}

# Vérifications préalables
Write-Info "🔍 Vérifications préalables..."

# Vérifier .NET 8
try {
    $dotnetVersion = & dotnet --version 2>$null
    if (-not $dotnetVersion) {
        Write-ErrorMsg ".NET SDK non trouvé. Veuillez installer .NET 8 SDK"
        exit 1
    }
    if (-not $dotnetVersion.StartsWith("8.")) {
        Write-Warning ".NET 8 recommandé. Version détectée: $dotnetVersion"
    } else {
        Write-Success ".NET $dotnetVersion détecté"
    }
} catch {
    Write-ErrorMsg "Erreur lors de la vérification de .NET: $_"
    exit 1
}

# Vérifier les fichiers requis
$requiredFiles = @("Konan.csproj")
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-ErrorMsg "Fichier manquant: $file"
        exit 1
    }
}
Write-Success "Tous les fichiers requis sont présents"

# Créer le dossier de sortie
$outputDir = "Releases\v$Version"
if (-not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}
Write-Info "📁 Dossier de sortie: $outputDir"

# Fonction de build
function Build-Version {
    param(
        [string]$Type,
        [string]$Runtime,
        [bool]$SelfContained,
        [string]$OutputName,
        [bool]$UseSingleFile = $true
    )
    
    Write-Info "🔨 Build $Type en cours..."
    
    $buildArgs = @(
        "publish"
        "-c", $Configuration
        "-r", $Runtime
        "--self-contained", $SelfContained.ToString().ToLower()
        "-p:AssemblyVersion=$Version.0"
        "-p:FileVersion=$Version.0"
        "-p:ProductVersion=$Version"
        "-o", "$outputDir\$OutputName"
        "--verbosity", $(if ($Verbose) { "detailed" } else { "minimal" })
    )
    
    # Ajouter Single File seulement si demandé
    if ($UseSingleFile) {
        $buildArgs += "-p:PublishSingleFile=true"
        $buildArgs += "-p:PublishReadyToRun=true"
        $buildArgs += "-p:EnableCompressionInSingleFile=true"
        if ($SelfContained) {
            $buildArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
            $buildArgs += "-p:PublishTrimmed=false"  # Éviter les problèmes avec WPF
        }
    } else {
        # Build standard avec tous les fichiers
        $buildArgs += "-p:PublishReadyToRun=true"
    }
    
    try {
        & dotnet @buildArgs
        
        if ($LASTEXITCODE -eq 0) {
            $exePath = "$outputDir\$OutputName\Konan.exe"
            if (Test-Path $exePath) {
                $fileInfo = Get-Item $exePath
                $size = [math]::Round($fileInfo.Length / 1MB, 2)
                Write-Success "$Type build réussi - Taille: ${size}MB"
                
                # Compter les fichiers dans le dossier
                $fileCount = (Get-ChildItem "$outputDir\$OutputName" -File).Count
                Write-Info "   📁 Fichiers générés: $fileCount"
                Write-Info "   📅 Date: $($fileInfo.LastWriteTime)"
                Write-Info "   📏 Taille exacte: $($fileInfo.Length) bytes"
                
                return $true
            } else {
                Write-ErrorMsg "Fichier exécutable introuvable: $exePath"
                return $false
            }
        } else {
            Write-ErrorMsg "$Type build échoué (Exit Code: $LASTEXITCODE)"
            return $false
        }
    } catch {
        Write-ErrorMsg "$Type build échoué: $_"
        return $false
    }
}

# Restaurer les dépendances
Write-Info "📦 Restauration des packages NuGet..."
try {
    & dotnet restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "Échec de la restauration des packages"
        exit 1
    }
    Write-Success "Packages restaurés"
} catch {
    Write-ErrorMsg "Erreur lors de la restauration: $_"
    exit 1
}

# Builds
$builds = @(
    @{
        Type = "Self-Contained (Portable)"
        Runtime = "win-x64"
        SelfContained = $true
        OutputName = "Konan-Portable-x64"
        Description = "Version portable Single File, ne nécessite pas .NET installé"
        UseSingleFile = $true
    },
    @{
        Type = "Framework-Dependent (Léger)"
        Runtime = "win-x64"
        SelfContained = $false
        OutputName = "Konan-Lightweight-x64"
        Description = "Version légère $(if ($SingleFileOnly) {'Single File'} else {'Multi-files'}), nécessite .NET 8 installé"
        UseSingleFile = $SingleFileOnly
    }
)

$successCount = 0
foreach ($build in $builds) {
    Write-Info "🎯 $($build.Type) - $($build.Description)"
    
    if (Build-Version -Type $build.Type -Runtime $build.Runtime -SelfContained $build.SelfContained -OutputName $build.OutputName -UseSingleFile $build.UseSingleFile) {
        $successCount++
    }
    
    Write-Host ""
}

# Créer les archives ZIP (CORRIGÉ)
Write-Info "📦 Création des archives ZIP..."

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($build in $builds) {
    $sourcePath = "$outputDir\$($build.OutputName)"
    $zipPath = "$outputDir\$($build.OutputName).zip"
    
    if (Test-Path $sourcePath) {
        try {
            # S'assurer que le dossier parent existe
            $zipDir = Split-Path $zipPath -Parent
            if (-not (Test-Path $zipDir)) {
                New-Item -Path $zipDir -ItemType Directory -Force | Out-Null
            }
            
            # Supprimer le ZIP existant s'il existe
            if (Test-Path $zipPath) {
                Remove-Item $zipPath -Force
            }
            
            # Créer l'archive
            [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcePath, $zipPath)
            
            $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
            Write-Success "Archive créée: $($build.OutputName).zip (${zipSize}MB)"
        } catch {
            Write-ErrorMsg "Erreur création ZIP pour $($build.OutputName): $_"
        }
    } else {
        Write-Warning "Dossier source introuvable pour ZIP: $sourcePath"
    }
}

# Créer un fichier de version
$versionInfo = @{
    Version = $Version
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
    Builder = $env:USERNAME
    Machine = $env:COMPUTERNAME
    DotNetVersion = $dotnetVersion
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    Configuration = $Configuration
    SingleFileOnly = $SingleFileOnly
    Builds = @()
}

foreach ($build in $builds) {
    $versionInfo.Builds += @{
        Name = $build.OutputName
        Type = $build.Type
        Runtime = $build.Runtime
        SelfContained = $build.SelfContained
        UseSingleFile = $build.UseSingleFile
    }
}

$versionInfo | ConvertTo-Json -Depth 3 | Out-File -FilePath "$outputDir\build-info.json" -Encoding UTF8
Write-Success "Informations de build sauvegardées"

# Générer les checksums
Write-Info "🔐 Génération des checksums SHA256..."
$checksums = @()

Get-ChildItem "$outputDir\*.zip" | ForEach-Object {
    try {
        $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
        $checksums += "SHA256 ($($_.Name)): $($hash.Hash.ToLower())"
        Write-Info "   ✅ $($_.Name): $($hash.Hash.ToLower().Substring(0,16))..."
    } catch {
        Write-Warning "   ⚠️  Erreur checksum pour $($_.Name): $_"
    }
}

if ($checksums.Count -gt 0) {
    $checksums | Out-File -FilePath "$outputDir\checksums.txt" -Encoding UTF8
    Write-Success "Checksums sauvegardés dans checksums.txt"
}

# Résumé final
Write-Host @"

🦊 ===================================
   BUILD TERMINÉ
=================================== 🦊
"@ -ForegroundColor Green

Write-Success "Builds réussis: $successCount/$($builds.Count)"
Write-Info "📁 Dossier de sortie: $outputDir"
Write-Info "📋 Fichiers générés:"

Get-ChildItem $outputDir | ForEach-Object {
    $size = if ($_.PSIsContainer) { 
        $fileCount = (Get-ChildItem $_.FullName -File -Recurse).Count
        "DIR ($fileCount fichiers)" 
    } else { 
        "$([math]::Round($_.Length / 1MB, 2))MB" 
    }
    Write-Host "   📄 $($_.Name) ($size)" -ForegroundColor White
}

Write-Host @"

🚀 Prêt pour la distribution !
   - Version Portable: Ne nécessite pas .NET (Single File)
   - Version Lightweight: Nécessite .NET 8 Runtime $(if ($SingleFileOnly) {'(Single File)'} else {'(Multi-files)'})

📋 Checksums disponibles dans: $outputDir\checksums.txt
📋 Infos build dans: $outputDir\build-info.json
"@ -ForegroundColor Cyan

# Ouvrir le dossier de sortie
try {
    Invoke-Item $outputDir
    Write-Info "📁 Dossier de sortie ouvert dans l'Explorateur"
} catch {
    Write-Info "📁 Dossier de sortie: $(Resolve-Path $outputDir)"
}