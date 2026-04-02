<#
.SYNOPSIS
    Generates PostgreSQL UPDATE statements to fix mismatched electoral districts.

.DESCRIPTION
    Reads the validated CSV output from Validate-ElectoralDistricts.ps1 and generates
    SQL UPDATE statements for rows where:
      - DistrictMatch is MISMATCH and GeocoderScore >= MinScore: SET to the expected district (main file)
      - DistrictMatch is MISMATCH and GeocoderScore < MinScore:  SET to NULL (separate file, opt-in)
      - AddressType matches the specified type (1=Physical, 2=Mailing)

.PARAMETER InputCsv
    Path to the validated CSV file (output of Validate-ElectoralDistricts.ps1).

.PARAMETER OutputSql
    Path to the high-confidence output .sql file. Defaults to <InputName>_update.sql.

.PARAMETER MinScore
    Minimum geocoder score to trust. Rows at or above get the expected district;
    rows below get NULL (if -IncludeLowConfidence is set). Default: 70.

.PARAMETER AddressType
    AddressType to filter on. 1 = Physical, 2 = Mailing. Default: 1 (Physical).

.PARAMETER IncludeLowConfidence
    When set, generates a separate .sql file for low-confidence rows (score < MinScore)
    that sets ApplicantElectoralDistrict to NULL. File is <InputName>_nullify.sql.

.EXAMPLE
    .\Generate-ElectoralDistrictFixes.ps1 `
        -InputCsv ".\data\electoral_districts_validated.csv" `
        -MinScore 70 `
        -AddressType 1 `
        -IncludeLowConfidence
#>
param(
    [Parameter(Mandatory)]
    [string]$InputCsv,

    [string]$OutputSql = "",

    [int]$MinScore = 70,

    [ValidateSet("1", "2")]
    [string]$AddressType = "1",

    [switch]$IncludeLowConfidence
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Output path defaults ──────────────────────────────────────────────
$dir  = [System.IO.Path]::GetDirectoryName((Resolve-Path $InputCsv))
$name = [System.IO.Path]::GetFileNameWithoutExtension($InputCsv)

if (-not $OutputSql) {
    $OutputSql = Join-Path $dir "${name}_update.sql"
}
$NullifySql = Join-Path $dir "${name}_nullify.sql"

# ── Read CSV ──────────────────────────────────────────────────────────
$data = Import-Csv -Path $InputCsv
Write-Host "Loaded $($data.Count) rows from $InputCsv" -ForegroundColor Cyan

# ── Resolve address type label ────────────────────────────────────────
$addressTypeLabel = switch ($AddressType) {
    "1" { "Physical" }
    "2" { "Mailing" }
}
Write-Host "Filtering for AddressType: $AddressType ($addressTypeLabel)" -ForegroundColor Cyan

# ── Filter all mismatches for this address type ──────────────────────
$allMismatches = @($data | Where-Object {
    $_.DistrictMatch -eq "MISMATCH" -and
    $_.GeocoderScore -ne "" -and
    $_.AddressType -eq $AddressType
})

# Split into high-confidence (update to expected) and low-confidence (set to NULL)
$highConfidence = @($allMismatches | Where-Object { [int]$_.GeocoderScore -ge $MinScore })
$lowConfidence  = @($allMismatches | Where-Object { [int]$_.GeocoderScore -lt $MinScore })

Write-Host "Found $($allMismatches.Count) total MISMATCH rows for AddressType = $AddressType ($addressTypeLabel)" -ForegroundColor Cyan
Write-Host "  High confidence (score >= $MinScore): $($highConfidence.Count) -> will SET to expected district" -ForegroundColor Green
$lowConfMsg = if ($IncludeLowConfidence) { " -> will SET to NULL (separate file)" } else { " (skipped, use -IncludeLowConfidence to generate)" }
Write-Host "  Low confidence  (score <  $MinScore): $($lowConfidence.Count)$lowConfMsg" -ForegroundColor Yellow

if ($highConfidence.Count -eq 0 -and (-not $IncludeLowConfidence -or $lowConfidence.Count -eq 0)) {
    Write-Host "No rows to update. Exiting." -ForegroundColor Yellow
    return
}

# ── Helper ───────────────────────────────────────────────────────────
function Escape-SqlString {
    param([string]$value)
    return $value.Replace("'", "''")
}

# ── Generate high-confidence SQL ─────────────────────────────────────
if ($highConfidence.Count -gt 0) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("-- Electoral District Fix Script (High Confidence)")
    [void]$sb.AppendLine("-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("-- Source: $InputCsv")
    [void]$sb.AppendLine("-- Filter: MISMATCH rows, AddressType = $AddressType ($addressTypeLabel), Score >= $MinScore")
    [void]$sb.AppendLine("-- Total updates: $($highConfidence.Count)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("BEGIN;")
    [void]$sb.AppendLine("")

    foreach ($row in $highConfidence) {
        $appId       = $row.ApplicationId.Trim()
        $currentED   = Escape-SqlString $row.ApplicantElectoralDistrict.Trim()
        $expectedED  = Escape-SqlString $row.ExpectedElectoralDistrict.Trim()
        $score       = $row.GeocoderScore
        $refNo       = $row.ReferenceNo

        [void]$sb.AppendLine("-- ReferenceNo: $refNo | Score: $score | '$currentED' -> '$expectedED'")
        [void]$sb.AppendLine("UPDATE ""Applications"" SET ""ApplicantElectoralDistrict"" = '$expectedED' WHERE ""Id"" = '$appId';")
        [void]$sb.AppendLine("")
    }

    [void]$sb.AppendLine("COMMIT;")
    $sb.ToString() | Out-File -FilePath $OutputSql -Encoding UTF8
    Write-Host "`nHigh-confidence SQL written to: $OutputSql" -ForegroundColor Green
}
else {
    Write-Host "`nNo high-confidence rows to write." -ForegroundColor DarkGray
}

# ── Generate low-confidence SQL (separate file, opt-in) ──────────────
if ($IncludeLowConfidence -and $lowConfidence.Count -gt 0) {
    $sbNull = [System.Text.StringBuilder]::new()
    [void]$sbNull.AppendLine("-- Electoral District Nullify Script (Low Confidence)")
    [void]$sbNull.AppendLine("-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sbNull.AppendLine("-- Source: $InputCsv")
    [void]$sbNull.AppendLine("-- Filter: MISMATCH rows, AddressType = $AddressType ($addressTypeLabel), Score < $MinScore")
    [void]$sbNull.AppendLine("-- Total updates: $($lowConfidence.Count)")
    [void]$sbNull.AppendLine("-- Action: SET ApplicantElectoralDistrict = NULL (unreliable geocoding)")
    [void]$sbNull.AppendLine("")
    [void]$sbNull.AppendLine("BEGIN;")
    [void]$sbNull.AppendLine("")

    foreach ($row in $lowConfidence) {
        $appId     = $row.ApplicationId.Trim()
        $currentED = Escape-SqlString $row.ApplicantElectoralDistrict.Trim()
        $score     = $row.GeocoderScore
        $refNo     = $row.ReferenceNo

        [void]$sbNull.AppendLine("-- ReferenceNo: $refNo | Score: $score | '$currentED' -> NULL")
        [void]$sbNull.AppendLine("UPDATE ""Applications"" SET ""ApplicantElectoralDistrict"" = NULL WHERE ""Id"" = '$appId';")
        [void]$sbNull.AppendLine("")
    }

    [void]$sbNull.AppendLine("COMMIT;")
    $sbNull.ToString() | Out-File -FilePath $NullifySql -Encoding UTF8
    Write-Host "Low-confidence SQL written to:  $NullifySql" -ForegroundColor Yellow
}
elseif ($IncludeLowConfidence) {
    Write-Host "`nNo low-confidence rows to write." -ForegroundColor DarkGray
}

# ── Summary ───────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=============================" -ForegroundColor White
Write-Host "  SQL Generation Summary"      -ForegroundColor White
Write-Host "=============================" -ForegroundColor White
Write-Host "Address type:        $AddressType ($addressTypeLabel)"
Write-Host "Score threshold:     $MinScore"
Write-Host "-----------------------------"
Write-Host "High confidence:     $($highConfidence.Count) (SET to expected)" -ForegroundColor Green
Write-Host "Low confidence:      $($lowConfidence.Count) (SET to NULL)"     -ForegroundColor Yellow
Write-Host "Total mismatches:    $($allMismatches.Count)"
Write-Host "-----------------------------"
if ($highConfidence.Count -gt 0) {
    Write-Host "Update file:  $OutputSql" -ForegroundColor White
}
if ($IncludeLowConfidence -and $lowConfidence.Count -gt 0) {
    Write-Host "Nullify file: $NullifySql" -ForegroundColor White
}
elseif ($lowConfidence.Count -gt 0) {
    Write-Host "Nullify file: (not generated - use -IncludeLowConfidence)" -ForegroundColor DarkGray
}
Write-Host ""
Write-Host "Review the SQL file(s), then execute against your PostgreSQL database." -ForegroundColor Yellow
