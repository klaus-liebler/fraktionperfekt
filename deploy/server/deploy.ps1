<#
.SYNOPSIS
    FraktionPerfekt auf liebler.iui.hs-osnabrueck.de ausrollen (gleiches Muster wie VibeVHS): framework-abhängiger Linux-Build (Runtime einmalig auf dem Server, VibeVHS-Schritt 09),
    Kit und Archiv hochladen, auf dem Server mit sudo einspielen. Übertragen wird NUR das Programm (dotnet publish) –
    die Datenbank und alle Daten bleiben auf dem Server in /var/lib/fraktionperfekt und werden nie überschrieben.
.EXAMPLE
    .\deploy\server\deploy.ps1                # bauen + hochladen + einspielen (fragt nach dem sudo-Passwort)
.EXAMPLE
    .\deploy\server\deploy.ps1 -NurKit        # nur das Kit nach /tmp/fraktionperfekt-kit hochladen (für Schritt 01)
.EXAMPLE
    .\deploy\server\deploy.ps1 -NurBauen      # nur bauen (deploy\ausgabe\fraktionperfekt.tar.gz), keine Verbindung
#>
param(
    [string] $Ziel = "srvadmin@liebler.iui.hs-osnabrueck.de",
    [string] $Schluessel = "$env:USERPROFILE\.ssh\id_ed25519_liebler_iui_hs_osnabrueck",
    [switch] $NurKit,
    [switch] $NurBauen,
    # Paket mit eigener .NET-Runtime (self-contained, ~70 statt ~20 MB) – nur, falls die gemeinsame Runtime fehlt
    [switch] $MitRuntime
)

$ErrorActionPreference = "Stop"
$wurzel = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$ausgabe = Join-Path $wurzel "deploy\ausgabe"
$sshArgs = @()
if (Test-Path $Schluessel) { $sshArgs = @("-i", $Schluessel) }
# Windows-eigenes tar (bsdtar) verwenden – ein GNU-tar aus Git Bash im PATH deutet „C:\…“ als entfernten Rechner
$tar = Join-Path $env:SystemRoot "System32\tar.exe"
if (-not (Test-Path $tar)) { $tar = "tar" }

# Pfad und Host aus der gemeinsamen Konfiguration (eine Quelle für Skripte, Unit und nginx)
$konfig = @{}
Get-Content (Join-Path $PSScriptRoot "konfiguration.env") | Where-Object { $_ -match '^\s*([A-Z_]+)="([^"]*)"' } |
    ForEach-Object { if ($_ -match '^\s*([A-Z_]+)="([^"]*)"') { $konfig[$Matches[1]] = $Matches[2] } }

function Kit-Hochladen {
    Write-Host "» Kit nach ${Ziel}:/tmp/fraktionperfekt-kit" -ForegroundColor Cyan
    $kit = Join-Path $ausgabe "fraktionperfekt-kit.tar.gz"
    New-Item -ItemType Directory -Force $ausgabe | Out-Null
    # tar statt scp -r: Zielordner wird jedes Mal sauber ersetzt; Zeilenenden bleiben LF (.gitattributes)
    & $tar -czf $kit -C (Join-Path $wurzel "deploy\server") .
    scp @sshArgs $kit "${Ziel}:/tmp/fraktionperfekt-kit.tar.gz"
    ssh @sshArgs $Ziel "rm -rf /tmp/fraktionperfekt-kit && mkdir -p /tmp/fraktionperfekt-kit && tar -xzf /tmp/fraktionperfekt-kit.tar.gz -C /tmp/fraktionperfekt-kit && rm /tmp/fraktionperfekt-kit.tar.gz && chmod +x /tmp/fraktionperfekt-kit/schritte/*.sh /tmp/fraktionperfekt-kit/werkzeuge/*.sh"
}

if ($NurKit) { Kit-Hochladen; return }

$eigeneRuntime = if ($MitRuntime) { "true" } else { "false" }
Write-Host ("» dotnet publish (linux-x64, {0})" -f $(if ($MitRuntime) { "mit eigener Runtime" } else { "framework-abhängig" })) -ForegroundColor Cyan
$build = Join-Path $ausgabe "fraktionperfekt"
if (Test-Path $build) { Remove-Item $build -Recurse -Force }
# Eigener Artefakt-Ordner: Die lokal laufende Instanz (bin\) bleibt unberührt
dotnet publish (Join-Path $wurzel "src\FraktionPerfekt.Web") -c Release -r linux-x64 --self-contained $eigeneRuntime -o $build -p:DebugType=None `
    --artifacts-path (Join-Path $ausgabe "artefakte")
if ($LASTEXITCODE -ne 0) { throw "Build fehlgeschlagen." }
Remove-Item (Join-Path $build "appsettings.Development.json") -ErrorAction SilentlyContinue

# Sicherheitsnetz: niemals Daten, Datenbanken oder Schlüssel mit ausliefern
$verboten = Get-ChildItem $build -Recurse -File | Where-Object { $_.Name -match '\.db(-wal|-shm)?$' -or $_.FullName -match '[\\/](data|logs)[\\/]' }
if ($verboten) { throw "Build enthält Daten-Dateien (z. B. $($verboten[0].FullName)) – abgebrochen." }

$archiv = Join-Path $ausgabe "fraktionperfekt.tar.gz"
& $tar -czf $archiv -C $ausgabe fraktionperfekt
Write-Host ("✓ gebaut: {0} ({1:N1} MB)" -f $archiv, ((Get-Item $archiv).Length / 1MB)) -ForegroundColor Green
if ($NurBauen) { return }

Kit-Hochladen
Write-Host "» Archiv hochladen" -ForegroundColor Cyan
scp @sshArgs $archiv "${Ziel}:/tmp/fraktionperfekt.tar.gz"
Write-Host "» einspielen (sudo fragt nach dem Passwort von srvadmin)" -ForegroundColor Cyan
ssh @sshArgs -t $Ziel "sudo bash /tmp/fraktionperfekt-kit/schritte/ausrollen.sh /tmp/fraktionperfekt.tar.gz"
if ($LASTEXITCODE -ne 0) { throw "Einspielen fehlgeschlagen – Meldung oben (eine vorhandene Version läuft unverändert weiter)." }
Write-Host ("✓ ausgerollt: https://{0}{1}/" -f $konfig["HOST"], $konfig["PFAD"]) -ForegroundColor Green
