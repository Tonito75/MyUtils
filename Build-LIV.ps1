<#
.SYNOPSIS
    Script de build local - Genere les livrables dans le dossier LIV.

.DESCRIPTION
    Ce script publie tous les projets deployables (services et webapps)
    dans un dossier LIV a la racine du projet.
    Chaque projet est publie en mode single-file (quand possible) pour
    minimiser le nombre de DLLs.

.PARAMETER Projects
    Liste des projets a publier. Si vide, publie tous les projets.

.PARAMETER OutputDir
    Dossier de sortie (defaut: LIV)

.PARAMETER Clean
    Nettoie le dossier de sortie avant la publication.

.EXAMPLE
    .\Build-LIV.ps1
    .\Build-LIV.ps1 -Projects CameraWatcher,LanWatcher
    .\Build-LIV.ps1 -Clean
#>

param(
    [string[]]$Projects = @(),
    [string]$OutputDir = "LIV",
    [switch]$Clean
)

# Configuration des projets
$ProjectConfigs = @{
    # Services (Workers)
    "CameraWatcher" = @{
        ProjectPath = "CameraWatcher\CameraWatcher.csproj"
        Framework   = "net8.0"
        SingleFile  = $true
    }
    "MinecraftLogsToDiscord" = @{
        ProjectPath = "MinecraftLogsToDiscord\MinecraftLogsToDiscord.csproj"
        Framework   = "net8.0"
        SingleFile  = $true
    }
    "TimelapseCreator" = @{
        ProjectPath = "TimelapseCreator\TimelapseCreator.csproj"
        Framework   = "net8.0"
        SingleFile  = $true   # DLLs natives extraites au runtime via IncludeNativeLibrariesForSelfExtract
    }
    "MinecraftWorldToNAS" = @{
        ProjectPath = "MinecraftWorldToNAS\MinecraftWorldToNAS.csproj"
        Framework   = "net8.0"
        SingleFile  = $true
    }
    "DiscordBot" = @{
        ProjectPath = "DiscordBot\DiscordBot.csproj"
        Framework   = "net10.0"
        SingleFile  = $true
    }
    # WebApps
    "BlazorPortalCamera" = @{
        ProjectPath = "PortalCameras\BlazorPortalCamera.csproj"
        Framework   = "net10.0"
        SingleFile  = $false  # WebApps avec wwwroot ne supportent pas bien le single-file
    }
    "EndPoints" = @{
        ProjectPath = "ApiFreeBoxCore\EndPoints\EndPoints.csproj"
        Framework   = "net10.0"
        SingleFile  = $false  # WebApps avec wwwroot ne supportent pas bien le single-file
    }
}

# Couleurs pour l'affichage
function Write-Step { param([string]$Message) Write-Host "`n>> $Message" -ForegroundColor Cyan }
function Write-Success { param([string]$Message) Write-Host "   [OK] $Message" -ForegroundColor Green }
function Write-Warning { param([string]$Message) Write-Host "   [!] $Message" -ForegroundColor Yellow }
function Write-Err { param([string]$Message) Write-Host "   [X] $Message" -ForegroundColor Red }
function Write-Info { param([string]$Message) Write-Host "   $Message" -ForegroundColor Gray }

# Chemin du script (racine du projet)
$ScriptRoot = $PSScriptRoot
$LivBaseDir = Join-Path $ScriptRoot $OutputDir

# Filtrer les projets si specifie
if ($Projects.Count -eq 0) {
    $ProjectsToProcess = $ProjectConfigs.Keys
} else {
    $ProjectsToProcess = $Projects | Where-Object { $ProjectConfigs.ContainsKey($_) }
    $InvalidProjects = $Projects | Where-Object { -not $ProjectConfigs.ContainsKey($_) }
    if ($InvalidProjects.Count -gt 0) {
        Write-Warning "Projets inconnus ignores: $($InvalidProjects -join ', ')"
    }
}

if ($ProjectsToProcess.Count -eq 0) {
    Write-Err "Aucun projet valide a publier."
    Write-Host ""
    Write-Host "Projets disponibles:" -ForegroundColor Yellow
    $ProjectConfigs.Keys | Sort-Object | ForEach-Object { Write-Host "  - $_" }
    exit 1
}

Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  BUILD LOCAL - GENERATION DES LIVRABLES" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Dossier de sortie: $LivBaseDir"
Write-Host "Projets: $($ProjectsToProcess -join ', ')"
Write-Host ""

# Nettoyer le dossier LIV si demande
if ($Clean -and (Test-Path $LivBaseDir)) {
    Write-Step "Nettoyage du dossier $OutputDir"
    Remove-Item $LivBaseDir -Recurse -Force
    Write-Success "Dossier supprime"
}

# Creer le dossier LIV s'il n'existe pas
if (-not (Test-Path $LivBaseDir)) {
    New-Item -ItemType Directory -Path $LivBaseDir -Force | Out-Null
}

# Compteurs
$SuccessCount = 0
$FailCount = 0

# Fonction pour publier un projet
function Publish-Project {
    param(
        [string]$ProjectPath,
        [string]$OutputDir,
        [bool]$SingleFile = $true
    )

    $FullProjectPath = Join-Path $ScriptRoot $ProjectPath

    # Arguments de publication
    $PublishArgs = @(
        "publish"
        $FullProjectPath
        "-c", "Release"
        "-o", $OutputDir
        "-r", "win-x64"
        "--self-contained", "true"
        "/p:PublishSingleFile=$SingleFile"
        "/p:IncludeNativeLibrariesForSelfExtract=true"
        "/p:DebugType=none"
        "/p:DebugSymbols=false"
    )

    Write-Info "dotnet $($PublishArgs -join ' ')"

    $Process = Start-Process -FilePath "dotnet" -ArgumentList $PublishArgs -NoNewWindow -Wait -PassThru
    return $Process.ExitCode -eq 0
}

# Traitement de chaque projet
foreach ($ProjectKey in ($ProjectsToProcess | Sort-Object)) {
    $Config = $ProjectConfigs[$ProjectKey]
    $ProjectPath = $Config.ProjectPath
    $SingleFile = if ($Config.ContainsKey('SingleFile')) { $Config.SingleFile } else { $true }

    Write-Step "Publication de $ProjectKey"

    # Dossier de sortie pour ce projet
    $ProjectOutputDir = Join-Path $LivBaseDir $ProjectKey

    # Nettoyer le dossier de publication
    if (Test-Path $ProjectOutputDir) {
        Remove-Item $ProjectOutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $ProjectOutputDir -Force | Out-Null

    Write-Info "Publication en cours..."
    $PublishSuccess = Publish-Project -ProjectPath $ProjectPath -OutputDir $ProjectOutputDir -SingleFile $SingleFile

    if (-not $PublishSuccess) {
        Write-Err "Echec de la publication de $ProjectKey"
        $FailCount++
        continue
    }

    # Compter les fichiers generes
    $Files = Get-ChildItem -Path $ProjectOutputDir -File -Recurse
    $ExeCount = ($Files | Where-Object { $_.Extension -eq ".exe" }).Count
    $DllCount = ($Files | Where-Object { $_.Extension -eq ".dll" }).Count
    $TotalCount = $Files.Count

    Write-Success "Publication terminee ($ExeCount exe, $DllCount dll, $TotalCount fichiers total)"
    $SuccessCount++
}

# Resume
Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  BUILD TERMINE" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Resultats:" -ForegroundColor Yellow
Write-Host "  Succes: $SuccessCount" -ForegroundColor Green
if ($FailCount -gt 0) {
    Write-Host "  Echecs: $FailCount" -ForegroundColor Red
}
Write-Host ""
Write-Host "Les livrables sont disponibles dans: $LivBaseDir" -ForegroundColor Cyan
Write-Host ""

# Afficher le contenu du dossier LIV
Write-Host "Contenu du dossier $OutputDir :" -ForegroundColor Yellow
Get-ChildItem -Path $LivBaseDir -Directory | ForEach-Object {
    $DirPath = $_.FullName
    $Files = Get-ChildItem -Path $DirPath -File -Recurse
    $Size = ($Files | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "  $($_.Name) - $([math]::Round($Size, 2)) MB ($($Files.Count) fichiers)"
}
