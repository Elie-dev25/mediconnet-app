# Corrige massivement les anti-patterns de logging detectes par Sonar :
#   - S2629 / CA2254 : _logger.LogX($"...{var}...")   -> template + args
#   - S6667          : catch(ex) { _logger.LogError("..."); } -> LogError(ex, "...")
#
# Strategie : regex conservateurs, multi-passes. On ne touche qu'aux cas surs.

param(
    [string]$RootPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Mediconnet-Backend"),
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$files = Get-ChildItem -Path $RootPath -Recurse -Filter *.cs -File `
    | Where-Object { $_.FullName -notmatch '\\(bin|obj|Migrations)\\' }

$totalChanges = 0
$filesChanged = 0

# Helper : convertit une expression C# en nom PascalCase valide pour un placeholder
function ConvertTo-PascalPlaceholder {
    param([string]$Expression)
    # Si c'est un simple identifiant, on met en PascalCase
    if ($Expression -match '^[a-zA-Z_][a-zA-Z0-9_]*$') {
        return [char]::ToUpper($Expression[0]) + $Expression.Substring(1)
    }
    # Si c'est expr.Member (ex: user.Name), on prend Member
    if ($Expression -match '^[a-zA-Z_][a-zA-Z0-9_]*\.([a-zA-Z_][a-zA-Z0-9_]*)$') {
        return $Matches[1]
    }
    # Indexeur, appel, etc. → pas sur. Renvoyer quelque chose de generique
    return "Arg"
}

foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw -Encoding UTF8
    if ([string]::IsNullOrEmpty($content)) { continue }
    $original = $content
    $localChanges = 0

    # -----------------------------------------------------------------
    # PASS 1 : _logger.LogXxx($"...{ex.Message}"); -> (ex, "...")
    # (simplifie et enleve l'interpolation + pousse l'exception en 1er arg)
    # -----------------------------------------------------------------
    # Capture :
    #   1 = nom methode (LogError, LogWarning, ...)
    #   2 = texte avant {ex.Message} ou {exception.Message} ou similaire
    #   3 = nom exception (ex, exception, e)
    #   4 = ); ou );
    $pattern1 = '(_logger\.(?:LogError|LogWarning|LogCritical|LogInformation|LogDebug|LogTrace))\(\$"([^"{}]*)\{([a-zA-Z_][a-zA-Z0-9_]*)\.Message\}"\);'
    $content = [regex]::Replace($content, $pattern1, {
        param($m)
        $logger = $m.Groups[1].Value
        $text = $m.Groups[2].Value.TrimEnd()
        if ($text.EndsWith(":")) { $text = $text.TrimEnd(":").TrimEnd() }
        $ex = $m.Groups[3].Value
        $script:localChanges++
        return "$logger($ex, ""$text"");"
    })

    # -----------------------------------------------------------------
    # PASS 2 : _logger.LogXxx($"text"); (interpolation sans variable)
    # -> _logger.LogXxx("text");
    # -----------------------------------------------------------------
    $pattern2 = '_logger\.(LogError|LogWarning|LogCritical|LogInformation|LogDebug|LogTrace)\(\$"([^"{}]*)"\)(;|\s*,)'
    $content = [regex]::Replace($content, $pattern2, {
        param($m)
        $script:localChanges++
        return "_logger.$($m.Groups[1].Value)(""$($m.Groups[2].Value)"")$($m.Groups[3].Value)"
    })

    # -----------------------------------------------------------------
    # PASS 3 : _logger.LogXxx($"...{simpleIdent}...");
    #       -> _logger.LogXxx("...{Ident}...", simpleIdent);
    # Limite : un seul placeholder, identifiant simple (pas de dot)
    # -----------------------------------------------------------------
    $pattern3 = '_logger\.(LogError|LogWarning|LogCritical|LogInformation|LogDebug|LogTrace)\(\$"([^"{}]*)\{([a-zA-Z_][a-zA-Z0-9_]*)\}([^"{}]*)"\);'
    $content = [regex]::Replace($content, $pattern3, {
        param($m)
        $script:localChanges++
        $method = $m.Groups[1].Value
        $prefix = $m.Groups[2].Value
        $var = $m.Groups[3].Value
        $suffix = $m.Groups[4].Value
        $placeholder = if ($var.Length -gt 0) { [char]::ToUpper($var[0]) + $var.Substring(1) } else { "Arg" }
        return "_logger.$method(""$prefix{$placeholder}$suffix"", $var);"
    })

    # -----------------------------------------------------------------
    # PASS 4 : _logger.LogXxx($"...{expr.Member}..."); (1 placeholder avec membre)
    # Prudence : on ne touche pas ex.Message (deja fait en pass 1)
    # -----------------------------------------------------------------
    $pattern4 = '_logger\.(LogError|LogWarning|LogCritical|LogInformation|LogDebug|LogTrace)\(\$"([^"{}]*)\{([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\}([^"{}]*)"\);'
    $content = [regex]::Replace($content, $pattern4, {
        param($m)
        $method = $m.Groups[1].Value
        $prefix = $m.Groups[2].Value
        $expr = $m.Groups[3].Value
        $suffix = $m.Groups[4].Value
        # Ne pas re-traiter ex.Message (deja geres en pass 1, mais securite)
        if ($expr -match '\.Message$') { return $m.Value }
        $script:localChanges++
        $member = ($expr -split '\.')[-1]
        $placeholder = [char]::ToUpper($member[0]) + $member.Substring(1)
        return "_logger.$method(""$prefix{$placeholder}$suffix"", $expr);"
    })

    # -----------------------------------------------------------------
    # PASS 5 : generique multi-placeholders
    # _logger.LogXxx($"...{expr1}...{expr2}...{exprN}...");
    # Remplace TOUTE chaine $"..." qui ne contient aucun ":" de format et aucun {{ }}
    # par template + args.
    # Les expressions supportees : identifiant ou identifiant.Member (sans parens).
    # -----------------------------------------------------------------
    $pattern5 = '_logger\.(LogError|LogWarning|LogCritical|LogInformation|LogDebug|LogTrace)\(\$"([^"]+)"\);'
    $content = [regex]::Replace($content, $pattern5, {
        param($m)
        $method = $m.Groups[1].Value
        $raw = $m.Groups[2].Value

        # Si pas d'interpolation, rien a faire
        if ($raw -notmatch '\{') { return $m.Value }
        # Refuser les formats avancees et braces echappes
        if ($raw -match '\{\{|\}\}|\{[^}]+:[^}]+\}') { return $m.Value }

        # Etape 1 : extraire et valider TOUTES les expressions {...} avant de transformer
        $exprMatches = [regex]::Matches($raw, '\{([^{}]+)\}')
        if ($exprMatches.Count -eq 0) { return $m.Value }
        $allValid = $true
        foreach ($em in $exprMatches) {
            $e = $em.Groups[1].Value.Trim()
            if ($e -notmatch '^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$') {
                $allValid = $false
                break
            }
        }
        if (-not $allValid) { return $m.Value }

        # Etape 2 : transformer
        $argExprs = New-Object System.Collections.Generic.List[string]
        $usedNames = New-Object System.Collections.Generic.HashSet[string]
        $template = [regex]::Replace($raw, '\{([^{}]+)\}', {
            param($mm)
            $expr = $mm.Groups[1].Value.Trim()
            $lastPart = ($expr -split '\.')[-1]
            $name = [char]::ToUpper($lastPart[0]) + $lastPart.Substring(1)
            $baseName = $name; $i = 1
            while ($usedNames.Contains($name)) { $i++; $name = "$baseName$i" }
            [void]$usedNames.Add($name)
            [void]$argExprs.Add($expr)
            return "{$name}"
        })

        if ($argExprs.Count -eq 0) { return $m.Value }

        $script:localChanges++
        $argList = ($argExprs -join ", ")
        return "_logger.$method(""$template"", $argList);"
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
