# TRM V4.1 Status JSON Update Helper
# Purpose: Generate or update wwwroot/data/trm-v4-1-status.json
# Usage: powershell -File tools/update-trm-app-status.ps1

param(
    [string]$StatusMdPath = "docsV4_1/TRM_V4_1_Current_Status_And_Test_Evidence.md",
    [string]$JsonPath = "TRM.App/wwwroot/data/trm-v4-1-status.json",
    [string]$DocsDest = "TRM.App/wwwroot/documents"
)

Push-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)
Push-Location ..

Write-Host "TRM V4.1 Status JSON Update Helper" -ForegroundColor Cyan

$sourceDocs = @(
    "docsV4_1/TRM_V4_1_Current_Status_And_Test_Evidence.md",
    "docsV4_1/theory/TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md",
    "docsV4_1/TRM_V4_1_Zenodo_Release_Notes.md"
)
$docNames = @(
    "TRM_V4_1_Current_Status_And_Test_Evidence.md",
    "TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md",
    "TRM_V4_1_Zenodo_Release_Notes.md"
)

# Copy docs
if (Test-Path $DocsDest) {
    for ($i = 0; $i -lt $sourceDocs.Count; $i++) {
        if (Test-Path $sourceDocs[$i]) {
            Copy-Item $sourceDocs[$i] (Join-Path $DocsDest $docNames[$i]) -Force
            Write-Host "  Copied: $($sourceDocs[$i])" -ForegroundColor Green
        }
    }
}

# Load JSON
$json = $null
if (Test-Path $JsonPath) {
    $json = Get-Content $JsonPath -Raw | ConvertFrom-Json
    Write-Host "  Loaded JSON" -ForegroundColor Green
}

# Extract test counts from status md
if (Test-Path $StatusMdPath) {
    $md = Get-Content $StatusMdPath -Raw
    if ($md -match 'total.*?(\d+)' -and $null -ne $json) { $json.testSummary.total = [int]$Matches[1] }
    if ($md -match 'passed.*?(\d+)' -and $null -ne $json) { $json.testSummary.passed = [int]$Matches[1] }
    
    # Update doc availability
    if ($null -ne $json -and $json.documents) {
        for ($i = 0; $i -lt $json.documents.Count; $i++) {
            $dest = Join-Path $DocsDest $docNames[$i]
            $json.documents[$i].available = Test-Path $dest
            if ($json.documents[$i].available) { $json.documents[$i].url = "documents/$($docNames[$i])" }
        }
    }
    Write-Host "  Updated from $StatusMdPath" -ForegroundColor Green
}

# Save
if ($null -ne $json) {
    $json | ConvertTo-Json -Depth 4 | Set-Content $JsonPath -NoNewline
    Write-Host "  Saved JSON" -ForegroundColor Green
}

Pop-Location
Pop-Location
Write-Host "Done. Run: dotnet build TRM.App" -ForegroundColor Cyan
