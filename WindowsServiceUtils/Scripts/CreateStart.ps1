# Paramètres à personnaliser
$serviceName = "TonitosWindowsServiceUtils"
$exePath = ".\WindowsServiceUtils.exe"

# Vérifie si le service existe
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "🛠️ Création du service '$serviceName'..."
    sc.exe create $serviceName binPath= "\"$exePath\"" start= auto
} else {
    Write-Host "Le service '$serviceName' existe déjà."
}

# Démarrage du service
Write-Host "🚀 Démarrage du service '$serviceName'..."
sc.exe start $serviceName

Write-Host "`nTerminé."
pause