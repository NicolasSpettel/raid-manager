# Runs the Raid Manager game directly (no Godot editor): builds the C#, then launches Main.tscn.
# Usage:  .\run-game.ps1        (add -Console to get a log console alongside the window)
param([switch]$Console)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$exeName = if ($Console) { "Godot_v4.7-stable_mono_win64_console.exe" } else { "Godot_v4.7-stable_mono_win64.exe" }
$godot = "C:\Users\nicol\Godot\Godot_v4.7-stable_mono_win64\$exeName"

if (-not (Test-Path $godot)) {
    Write-Error "Godot .NET build not found at $godot - update the path in run-game.ps1 if you moved it."
    exit 1
}

Write-Host "Building C#..." -ForegroundColor Cyan
dotnet build "$root\src\App\App.csproj"

Write-Host "Launching the game..." -ForegroundColor Cyan
& $godot --path "$root\src\App"
