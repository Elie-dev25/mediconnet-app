# Fix S6580 : ajouter CultureInfo.InvariantCulture aux appels DateTime.Parse / ParseExact
# sans format provider.

param(
    [string]$RootPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Mediconnet-Backend"),
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$files = Get-ChildItem -Path $RootPath -Recurse -Filter *.cs -File `
    | Where-Object { $_.FullName -notmatch '\\(bin|obj|Migrations)\\' }

$totalChanges = 0
$filesChanged = 0

foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw -Encoding UTF8
    if ([string]::IsNullOrEmpty($content)) { continue }
    $original = $content
    $localChanges = 0

    # DateTime.Parse(x)  -> DateTime.Parse(x, CultureInfo.InvariantCulture)
    # On ne touche que si un SEUL argument (pas de virgule hors parens)
    # Match : DateTime.Parse( <contenu sans virgule top-level> )
    $pattern1 = 'DateTime\.Parse\(([^(),]+)\)'
    $content = [regex]::Replace($content, $pattern1, {
        param($m)
        $script:localChanges++
        return "DateTime.Parse($($m.Groups[1].Value), System.Globalization.CultureInfo.InvariantCulture)"
    })

    # DateOnly.Parse(x) -> avec culture
    $pattern2 = 'DateOnly\.Parse\(([^(),]+)\)'
    $content = [regex]::Replace($content, $pattern2, {
        param($m)
        $script:localChanges++
        return "DateOnly.Parse($($m.Groups[1].Value), System.Globalization.CultureInfo.InvariantCulture)"
    })

    # TimeOnly.Parse(x)
    $pattern3 = 'TimeOnly\.Parse\(([^(),]+)\)'
    $content = [regex]::Replace($content, $pattern3, {
        param($m)
        $script:localChanges++
        return "TimeOnly.Parse($($m.Groups[1].Value), System.Globalization.CultureInfo.InvariantCulture)"
    })

    # TimeSpan.Parse(x)
    $pattern4 = 'TimeSpan\.Parse\(([^(),]+)\)'
    $content = [regex]::Replace($content, $pattern4, {
        param($m)
        $script:localChanges++
        return "TimeSpan.Parse($($m.Groups[1].Value), System.Globalization.CultureInfo.InvariantCulture)"
    })

    if ($localChanges -gt 0 -and $content -ne $original) {
        if (-not $DryRun) {
            Set-Content -Path $f.FullName -Value $content -Encoding UTF8 -NoNewline
        }
        $filesChanged++
        $totalChanges += $localChanges
        Write-Host "  [$($localChanges)x] $($f.FullName.Substring($RootPath.Length + 1))"
    }
}

Write-Host ""
Write-Host "=== RESUME ==="
Write-Host "Fichiers modifies : $filesChanged"
Write-Host "Occurrences fixes : $totalChanges"
if ($DryRun) { Write-Host "(DryRun : aucune ecriture)" }
