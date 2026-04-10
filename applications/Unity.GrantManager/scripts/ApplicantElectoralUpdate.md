# Applicant Electoral District Update

## Overview

This process cross-references application electoral districts against the BC Geocoder API and generates SQL fix scripts for any mismatches.

## Steps

### 1. Extract Data from the Database

Run `GetElectoralDistrictData.sql` against the database and save the results as a CSV file in the `data/` folder:

```
data/electoral_districts.csv
```

The query returns all applications joined with their applicant addresses, including `ApplicationId`, `ReferenceNo`, `ApplicantElectoralDistrict`, `Street`, `Street2`, `City`, and `AddressType`.

### 2. Validate Electoral Districts

Run `Validate-ElectoralDistricts.ps1` with the extracted CSV. This geocodes each unique address via the BC Geocoder API, looks up the electoral district from the WFS endpoint, and compares it to the value stored in the database.

```powershell
.\Validate-ElectoralDistricts.ps1 `
    -InputCsv ".\data\electoral_districts.csv" `
    -GeocoderLocationBase "https://geocoder.api.gov.bc.ca" `
    -GeocoderApiBase "https://openmaps.gov.bc.ca/geo/pub/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName="
```

This produces a validated CSV with match results:

```
data/electoral_districts_validated.csv
```

Each row includes the expected electoral district, geocoder score, and a `DistrictMatch` column (`MATCH`, `MISMATCH`, `UNKNOWN`, or `SKIPPED`).

### 3. Generate SQL Fix Scripts

Run `Generate-ElectoralDistrictFixes.ps1` against the validated CSV. This produces SQL UPDATE statements for mismatched rows.

```powershell
.\Generate-ElectoralDistrictFixes.ps1 `
    -InputCsv ".\data\electoral_districts_validated.csv" `
    -MinScore 70 `
    -AddressType 1 `
    -IncludeLowConfidence
```

This generates two SQL scripts:

| File | Description |
|------|-------------|
| `electoral_districts_validated_update.sql` | High-confidence updates (score >= 70) — sets the district to the expected value |
| `electoral_districts_validated_nullify.sql` | Low-confidence updates (score < 70) — sets the district to NULL |

Both scripts use conditional updates that only apply when the current database value still matches the value at extract time, preventing overwrites of legitimate changes.

### 4. Apply the Fix Scripts

Review the generated SQL files, then execute them against the database:

1. **Always apply the high-confidence script first** (`_update.sql`).
2. **Optionally apply the low-confidence script** (`_nullify.sql`) if you want to clear unreliable district values.
