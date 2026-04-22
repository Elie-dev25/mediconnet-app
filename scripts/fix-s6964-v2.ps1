# Fix automatique Sonar S6964 : ajoute [JsonRequired] sur les value types des DTOs
# [JsonRequired] est la seule option qui bloque reellement l'under-posting JSON
# (contrairement a [Required] qui ne detecte pas les value types a leur valeur par defaut)

param(
    [string]$IssuesFile = "s6964.json",
    [string]$BackendRoot = "Mediconnet-Backend"
)

$ErrorActionPreference = 'Stop'

$data = Get-Content $IssuesFile -Raw | ConvertFrom-Json
Write-Host "Total S6964 issues: $($data.total)"

$byFile = @{}
foreach ($issue in $data.issues) {
    $relPath = ($issue.component -split ':', 2)[1]
    if (-not $byFile.ContainsKey($relPath)) {
        $byFile[$relPath] = New-Object System.Collections.Generic.List[int]
    }
    $byFile[$relPath].Add([int]$issue.line)
}

$totalFixed = 0
$totalSkipped = 0
$skippedDetails = @()

foreach ($relPath in $byFile.Keys) {
    $fullPath = Join-Path $BackendRoot $relPath
    if (-not (Test-Path $fullPath)) {
        Write-Warning "Introuvable : $fullPath"
        continue
    }

    $lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $fullPath)
    $issueLines = $byFile[$relPath] | Sort-Object -Unique -Descending

    $fileChanged = $false
    $fileFixed = 0
    $fileSkipped = 0

    foreach ($lineNum in $issueLines) {
        $idx = $lineNum - 1
        if ($idx -lt 0 -or $idx -ge $lines.Count) { continue }

        $line = $lines[$idx]
        $indentMatch = [regex]::Match($line, '^(\s*)')
        $indent = $indentMatch.Groups[1].Value
        $trim = $line.Trim()

        $isProperty = $trim -match '^\s*(public|internal|protected)\s+'

        # Verifier si [JsonRequired] existe deja sur une ligne d'attribut au-dessus
        $prevIdx = $idx - 1
        $alreadyRequired = $false
        while ($prevIdx -ge 0) {
            $prev = $lines[$prevIdx].Trim()
            if ($prev -eq '') { $prevIdx--; continue }
            if ($prev -match '\bJsonRequired\b') { $alreadyRequired = $true; break }
            if ($prev -match '^\[') { $prevIdx--; continue }
            break
        }

        if ($alreadyRequired) {
            $fileSkipped++
            continue
        }

        if ($isProperty) {
            $lines.Insert($idx, "$indent[JsonRequired]")
            $fileFixed++
            $fileChanged = $true
        }
        else {
            $fileSkipped++
            $skippedDetails += "$relPath : $lineNum : $trim"
        }
    }

    if ($fileChanged) {
        # Ajout du using System.Text.Json.Serialization s'il manque
        $hasUsing = $false
        foreach ($l in $lines) {
            if ($l -match '^\s*using\s+System\.Text\.Json\.Serialization\s*;') { $hasUsing = $true; break }
            if ($l -match '^\s*namespace\b') { break }
        }
        if (-not $hasUsing) {
            $insertIdx = 0
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match '^\s*using\s+') { $insertIdx = $i + 1 }
                if ($lines[$i] -match '^\s*namespace\b') { break }
            }
            $lines.Insert($insertIdx, 'using System.Text.Json.Serialization;')
        }

        Set-Content -LiteralPath $fullPath -Value $lines -Encoding UTF8
        Write-Host "  OK $relPath : +$fileFixed fixes, $fileSkipped skipped"
        $totalFixed += $fileFixed
        $totalSkipped += $fileSkipped
    }
    else {
        Write-Host "  -- $relPath : nothing changed ($fileSkipped skipped)"
        $totalSkipped += $fileSkipped
    }
}

Write-Host ""
Write-Host "=== TOTAL ==="
Write-Host "Fixed  : $totalFixed"
Write-Host "Skipped: $totalSkipped"
if ($skippedDetails.Count -gt 0) {
    Write-Host ""
    Write-Host "=== Skipped details ==="
    $skippedDetails | ForEach-Object { Write-Host "  $_" }
}
