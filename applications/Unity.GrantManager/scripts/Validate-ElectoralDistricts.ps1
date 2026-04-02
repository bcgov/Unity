<#
.SYNOPSIS
    Cross-references electoral districts in a CSV against the BC Geocoder API.

.DESCRIPTION
    Reads a CSV with address data, looks up each unique address via the BC Geocoder
    location API to get coordinates, then queries the electoral district WFS endpoint.
    Deduplicates addresses so identical Street+Street2+City combinations are only
    looked up once. Handles rate limiting with exponential backoff.
    Outputs a new CSV with all original columns plus the expected electoral district.

.PARAMETER InputCsv
    Path to the input CSV file.

.PARAMETER OutputCsv
    Path to the output CSV file. Defaults to <InputName>_validated.csv.

.PARAMETER GeocoderLocationBase
    Base URL for the geocoder location API (GEOCODER_LOCATION_API_BASE).
    Example: https://geocoder.api.gov.bc.ca

.PARAMETER GeocoderApiBase
    Base URL for the geocoder WFS API (GEOCODER_API_BASE).
    Example: https://openmaps.gov.bc.ca/geo/pub/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName=

.PARAMETER InitialDelayMs
    Initial delay between API calls in milliseconds. Default: 250.

.EXAMPLE
    .\Validate-ElectoralDistricts.ps1 `
        -InputCsv ".\data\electoral_districts.csv" `
        -GeocoderLocationBase "https://geocoder.api.gov.bc.ca" `
        -GeocoderApiBase "https://openmaps.gov.bc.ca/geo/pub/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName="
#>
param(
    [Parameter(Mandatory)]
    [string]$InputCsv,

    [string]$OutputCsv = "",

    [Parameter(Mandatory)]
    [string]$GeocoderLocationBase,

    [Parameter(Mandatory)]
    [string]$GeocoderApiBase,

    [int]$InitialDelayMs = 250
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Output path defaults ──────────────────────────────────────────────
if (-not $OutputCsv) {
    $dir  = [System.IO.Path]::GetDirectoryName((Resolve-Path $InputCsv))
    $name = [System.IO.Path]::GetFileNameWithoutExtension($InputCsv)
    $OutputCsv = Join-Path $dir "${name}_validated.csv"
}

# ── Read CSV ──────────────────────────────────────────────────────────
$data = Import-Csv -Path $InputCsv
Write-Host "Loaded $($data.Count) rows from $InputCsv" -ForegroundColor Cyan

# ── Helpers ───────────────────────────────────────────────────────────
function Test-NullOrEmpty {
    param([string]$value)
    return [string]::IsNullOrWhiteSpace($value) -or $value.Trim() -eq 'NULL'
}

function Get-CleanValue {
    param([string]$value)
    if (Test-NullOrEmpty $value) { return "" }
    return $value.Trim()
}

function Get-AddressKey {
    param($row)
    $street  = Get-CleanValue $row.Street
    $street2 = Get-CleanValue $row.Street2
    $city    = Get-CleanValue $row.City
    return "$street|$street2|$city".ToLowerInvariant()
}

function Build-AddressString {
    param($street, $street2, $city)
    $parts = @($street, $street2, $city) | ForEach-Object { Get-CleanValue $_ } | Where-Object { $_ -ne "" }
    return ($parts -join ", ")
}

function Get-AddressTypeName {
    param([string]$value)
    $clean = Get-CleanValue $value
    switch ($clean) {
        "1" { return "Physical" }
        "2" { return "Mailing" }
        default { return "" }
    }
}

# ── Deduplicate addresses ────────────────────────────────────────────
$addressLookup = [ordered]@{}
$skippedCount  = 0

foreach ($row in $data) {
    $street  = Get-CleanValue $row.Street
    $street2 = Get-CleanValue $row.Street2
    $city    = Get-CleanValue $row.City

    # Skip rows where both street fields and city are empty/NULL
    if ($street -eq "" -and $street2 -eq "" -and $city -eq "") {
        $skippedCount++
        continue
    }

    $key = Get-AddressKey $row
    if (-not $addressLookup.Contains($key)) {
        $addressLookup[$key] = @{
            Street      = $row.Street
            Street2     = $row.Street2
            City        = $row.City
            ExpectedED  = $null
            Score       = $null
            FullAddress = $null
            Error       = $null
        }
    }
}

$uniqueCount = $addressLookup.Count
Write-Host "Unique addresses to look up: $uniqueCount  (skipped $skippedCount rows with empty address)" -ForegroundColor Cyan

# ── Rate-limited HTTP caller with exponential backoff ─────────────────
$script:currentDelayMs = $InitialDelayMs
$maxDelayMs = 30000
$minDelayMs = 100

function Invoke-GeocoderRequest {
    param(
        [string]$Uri,
        [int]$MaxRetries = 6
    )

    for ($attempt = 1; $attempt -le ($MaxRetries + 1); $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri $Uri -Method Get -ErrorAction Stop

            # On success, gently reduce the inter-call delay
            $script:currentDelayMs = [Math]::Max($minDelayMs, [int]([Math]::Floor($script:currentDelayMs * 0.95)))

            return $response
        }
        catch {
            $statusCode = 0
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }

            if (($statusCode -eq 429 -or $statusCode -eq 503) -and $attempt -le $MaxRetries) {
                # Exponential backoff
                $script:currentDelayMs = [Math]::Min($maxDelayMs, $script:currentDelayMs * 2)
                $backoffMs = $script:currentDelayMs * $attempt
                Write-Host "    Rate limited (HTTP $statusCode). Backing off ${backoffMs}ms (attempt $attempt/$MaxRetries)..." -ForegroundColor Yellow
                Start-Sleep -Milliseconds $backoffMs
            }
            else {
                throw
            }
        }
    }
}

# ── Process each unique address ───────────────────────────────────────
$processed = 0
$errorCount = 0

# Electoral district WFS parameters (from appsettings.json Geocoder:ElectoralDistrict)
$edFeature   = "pub:WHSE_ADMIN_BOUNDARIES.EBC_PROV_ELECTORAL_DIST_SVW"
$edProperty  = "ED_NAME"
$edQueryType = "SHAPE"

foreach ($entry in $addressLookup.GetEnumerator()) {
    $processed++
    $addr = $entry.Value
    $addressString = Build-AddressString $addr.Street $addr.Street2 $addr.City

    Write-Host "[$processed/$uniqueCount] $addressString" -ForegroundColor Cyan

    try {
        # Step 1: Geocode address → coordinates
        $encodedAddress = [System.Uri]::EscapeDataString($addressString)
        $locationUri = "$GeocoderLocationBase/addresses.json?outputSRS=3005&addressString=$encodedAddress"

        Start-Sleep -Milliseconds $script:currentDelayMs
        $locationResult = Invoke-GeocoderRequest -Uri $locationUri

        if (-not $locationResult.features -or $locationResult.features.Count -eq 0) {
            Write-Host "    No location results" -ForegroundColor Yellow
            $addr.Error = "No location results"
            $errorCount++
            continue
        }

        $coords    = $locationResult.features[0].geometry.coordinates
        $latitude  = $coords[0]   # Mirrors C# ResultMapper: coordinates[0] → Latitude
        $longitude = $coords[1]   # Mirrors C# ResultMapper: coordinates[1] → Longitude
        $score     = $locationResult.features[0].properties.score
        $fullAddr  = $locationResult.features[0].properties.fullAddress

        $addr.Score       = $score
        $addr.FullAddress = $fullAddr
        Write-Host "    Resolved: $fullAddr (score: $score)" -ForegroundColor DarkGray

        # Step 2: Look up electoral district from coordinates
        $edUri = "${GeocoderApiBase}${edFeature}" +
                 "&srsname=EPSG:4326" +
                 "&propertyName=${edProperty}" +
                 "&outputFormat=application/json" +
                 "&cql_filter=INTERSECTS(${edQueryType},POINT($latitude $longitude))"

        Start-Sleep -Milliseconds $script:currentDelayMs
        $edResult = Invoke-GeocoderRequest -Uri $edUri

        if (-not $edResult.features -or $edResult.features.Count -eq 0) {
            Write-Host "    No electoral district found for coordinates" -ForegroundColor Yellow
            $addr.ExpectedED = ""
            $addr.Error = "No electoral district for coordinates ($latitude, $longitude)"
            $errorCount++
            continue
        }

        $expectedED     = $edResult.features[0].properties.ED_NAME
        $addr.ExpectedED = $expectedED
        Write-Host "    Electoral District: $expectedED" -ForegroundColor Green
    }
    catch {
        Write-Host "    ERROR: $_" -ForegroundColor Red
        $addr.Error = $_.ToString()
        $errorCount++
    }
}

# ── Build output CSV ──────────────────────────────────────────────────
Write-Host "`nBuilding output CSV..." -ForegroundColor Cyan

$output = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($row in $data) {
    $key = Get-AddressKey $row

    $expectedED  = ""
    $lookupError = ""
    $geoScore    = ""
    $geoFullAddr = ""
    $matchResult = "SKIPPED"

    if ($addressLookup.Contains($key)) {
        $lookup      = $addressLookup[$key]
        $expectedED  = if ($lookup.ExpectedED)  { $lookup.ExpectedED }  else { "" }
        $lookupError = if ($lookup.Error)        { $lookup.Error }       else { "" }
        $geoScore    = if ($lookup.Score -ne $null) { $lookup.Score }    else { "" }
        $geoFullAddr = if ($lookup.FullAddress)  { $lookup.FullAddress } else { "" }

        if ($expectedED -and $row.ApplicantElectoralDistrict) {
            if ($expectedED.Trim() -eq $row.ApplicantElectoralDistrict.Trim()) {
                $matchResult = "MATCH"
            }
            else {
                $matchResult = "MISMATCH"
            }
        }
        else {
            $matchResult = "UNKNOWN"
        }
    }

    $outRow = [ordered]@{}
    foreach ($prop in $row.PSObject.Properties) {
        $outRow[$prop.Name] = $prop.Value
    }
    $outRow["AddressTypeName"]           = Get-AddressTypeName $row.AddressType
    $outRow["ExpectedElectoralDistrict"] = $expectedED
    $outRow["DistrictMatch"]             = $matchResult
    $outRow["GeocoderScore"]             = $geoScore
    $outRow["GeocoderFullAddress"]       = $geoFullAddr
    $outRow["LookupError"]               = $lookupError

    $output.Add([PSCustomObject]$outRow)
}

$output | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

# ── Summary ───────────────────────────────────────────────────────────
$matchCount    = @($output | Where-Object { $_.DistrictMatch -eq "MATCH" }).Count
$mismatchCount = @($output | Where-Object { $_.DistrictMatch -eq "MISMATCH" }).Count
$unknownCount  = @($output | Where-Object { $_.DistrictMatch -eq "UNKNOWN" }).Count
$skippedRows   = @($output | Where-Object { $_.DistrictMatch -eq "SKIPPED" }).Count

Write-Host ""
Write-Host "=============================" -ForegroundColor White
Write-Host "  Electoral District Audit"     -ForegroundColor White
Write-Host "=============================" -ForegroundColor White
Write-Host "Total rows:       $($data.Count)"
Write-Host "Unique addresses: $uniqueCount"
Write-Host "API errors:       $errorCount"   -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host "-----------------------------"
Write-Host "MATCH:            $matchCount"    -ForegroundColor Green
Write-Host "MISMATCH:         $mismatchCount" -ForegroundColor $(if ($mismatchCount -gt 0) { "Red" } else { "Green" })
Write-Host "UNKNOWN:          $unknownCount"  -ForegroundColor Yellow
Write-Host "SKIPPED:          $skippedRows"   -ForegroundColor DarkGray
Write-Host "-----------------------------"
Write-Host "Output: $OutputCsv" -ForegroundColor White
