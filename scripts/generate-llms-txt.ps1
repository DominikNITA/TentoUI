<#
.SYNOPSIS
  Generates src/TentoUI.Docs/wwwroot/llms-full.txt by bundling llms.txt, tentoui-components/SKILL.md, and tentoui-tailwind-setup/SKILL.md.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$llmsShort = Join-Path $root 'src/TentoUI.Docs/wwwroot/llms.txt'
$skillComponents = Join-Path $root '.skills/tentoui-components/SKILL.md'
$skillTailwind = Join-Path $root '.skills/tentoui-tailwind-setup/SKILL.md'
$outFull = Join-Path $root 'src/TentoUI.Docs/wwwroot/llms-full.txt'

$sections = @(
    "# TentoUI - Complete AI & LLM Reference (llms-full.txt)",
    "",
    (Get-Content $llmsShort -Raw).Trim(),
    "",
    "---",
    "",
    (Get-Content $skillComponents -Raw).Trim(),
    "",
    "---",
    "",
    (Get-Content $skillTailwind -Raw).Trim()
)

Set-Content -Path $outFull -Value ($sections -join "`n") -Encoding utf8
Write-Host "Generated $outFull"
