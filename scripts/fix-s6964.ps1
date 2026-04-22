# Fix automatique Sonar S6964 : ajoute [Required] sur les value types des DTOs
# Utilise le JSON export de l'API Sonar (issues par rule)

param(
    [string]$IssuesFile = "s6964.json",
    [string]$BackendRoot = "Mediconnet-Backend"
)

$ErrorActionPreference = 'Stop'

$data = Get-Content $IssuesFile -Raw | ConvertFrom-Json
Write-Host "Total S6964 issues: $($data.total)"

# Grouper par fichier
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

        # Detecter indentation
        $indentMatch = [regex]::Match($line, '^(\s*)')
        $indent = $indentMatch.Groups[1].Value

        # Detecter le type : propriete { get; set; } ou parametre de methode
        # On veut s'assurer qu'il s'agit bien d'une property ou un parametre de controller action
        $trim = $line.Trim()

        # Cas 1 : property "public <type> Name { get; set; }"
        $isProperty = $trim -match '^\s*(public|internal|protected)\s+'
        # Cas 2 : parametre de methode (ligne contient , ou ) et type value + nom)
        $isMethodParam = $trim -match '^\s*\[?From' -or ($trim -match '^[A-Za-z_][A-Za-z0-9_]*\??\s+[A-Za-z_]' -and ($trim -match ',$' -or $trim -match '\)\s*$'))

        # Verifier si [Required] est deja au-dessus
        $prevIdx = $idx - 1
        $alreadyRequired = $false
        while ($prevIdx -ge 0) {
            $prev = $lines[$prevIdx].Trim()
            if ($prev -eq '') { $prevIdx--; continue }
            if ($prev -match '^\[.*Required') { $alreadyRequired = $true; break }
            if ($prev -match '^\[') { $prevIdx--; continue }
            break
        }

        if ($alreadyRequired) {
            $fileSkipped++
            continue
        }

        if ($isProperty) {
            # Inserer [Required] au-dessus
            $lines.Insert($idx, "$indent[Required]")
            $fileFixed++
            $fileChanged = $true
        }
        elseif ($isMethodParam) {
            # Parametre inline : prefixer avec [Required]
            $newLine = $line -replace '^(\s*)(\[From\w+(\([^)]*\))?\]\s*)?', '$1[Required] $2'
            if ($newLine -ne $line) {
                $lines[$idx] = $newLine
                $fileFixed++
                $fileChanged = $true
            } else {
                $fileSkipped++
            }
        }
        else {
            $fileSkipped++
        }
    }

    if ($fileChanged) {
        # S'assurer que le using est present
        $hasUsing = $false
        foreach ($l in $lines) {
            if ($l -match '^\s*using\s+System\.ComponentModel\.DataAnnotations\s*;') { $hasUsing = $true; break }
            if ($l -match '^\s*namespace\b') { break }
        }
        if (-not $hasUsing) {
            # Inserer apres le dernier using existant, ou au debut
            $insertIdx = 0
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match '^\s*using\s+') { $insertIdx = $i + 1 }
                if ($lines[$i] -match '^\s*namespace\b') { break }
            }
            $lines.Insert($insertIdx, 'using System.ComponentModel.DataAnnotations;')
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
