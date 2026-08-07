# Downloads attachments listed in the eval CSV from OpenShift dev via a
# port-forward to the Unity web pod. Uses the [AllowAnonymous] endpoint
# /api/app/attachment/chefs/{submissionId}/download/{fileId}/{fileName}
# so no auth token is needed once the port-forward is up.
#
# Prereq: `oc login` already done (e.g. via ocUnityDbConnect.ps1).
# ponytail: reuses the anonymous chefs download route rather than hitting CHEFS
# directly, so tenant-scoped CHEFS creds live in the pod, not on your laptop.

param(
    [string]$CsvPath   = (Join-Path $PSScriptRoot '..\data\attachment-summary-eval.csv'),
    [string]$OutDir    = (Join-Path $PSScriptRoot '..\dataset\attachments'),
    [string]$Namespace = 'd18498-dev',
    [string]$PodLabel  = 'app=unity-grantmanager-web',
    [int]$LocalPort    = 8081,
    [int]$RemotePort   = 8080
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command oc -ErrorAction SilentlyContinue)) {
    throw "oc CLI not found in PATH."
}

oc project $Namespace | Out-Null

$pod = oc get pod -n $Namespace -l $PodLabel -o jsonpath='{.items[0].metadata.name}' 2>$null
if ([string]::IsNullOrWhiteSpace($pod)) {
    throw "No pod found in $Namespace matching '$PodLabel'. Override with -PodLabel."
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "Port-forwarding pod/$pod ${LocalPort}:${RemotePort} (${Namespace})..." -ForegroundColor Cyan
$pf = Start-Process -PassThru -FilePath oc `
    -WindowStyle Hidden `
    -ArgumentList @('port-forward','-n',$Namespace,"pod/$pod","${LocalPort}:${RemotePort}")

try {
    Start-Sleep -Seconds 3
    $base = "http://localhost:$LocalPort/api/app/attachment/chefs"
    $rows = Import-Csv -Path $CsvPath

    $ok = 0; $skip = 0; $fail = 0
    foreach ($row in $rows) {
        if (-not $row.chefs_submission_id -or -not $row.chefs_file_id -or -not $row.file_name) {
            $skip++; continue
        }

        $safe    = ($row.file_name -replace '[\\/:*?"<>|]', '_')
        $outFile = Join-Path $OutDir "$($row.attachment_id)_$safe"
        if (Test-Path $outFile) { $skip++; continue }

        $encoded = [uri]::EscapeDataString($row.file_name)
        $url     = "$base/$($row.chefs_submission_id)/download/$($row.chefs_file_id)/$encoded"
        # Multi-tenant: ApplicationForm + CHEFS API key live in the tenant DB.
        # ABP's default resolver accepts tenant name via the __tenant header.
        $headers = @{ '__tenant' = $row.tenant }

        try {
            Invoke-WebRequest -Uri $url -Headers $headers -OutFile $outFile -UseBasicParsing -TimeoutSec 60 | Out-Null
            $ok++
            Write-Host "  ok   $($row.attachment_id)  $($row.file_name)"
        } catch {
            $fail++
            $body = ''
            try {
                $resp = $_.Exception.Response
                if ($resp) {
                    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
                    $body = $reader.ReadToEnd()
                }
            } catch {}
            Write-Warning "fail  $($row.attachment_id)  $($row.file_name) : $($_.Exception.Message) :: $body"
            if (Test-Path $outFile) { Remove-Item $outFile -Force }
        }
    }

    Write-Host "Done. ok=$ok skipped=$skip failed=$fail -> $OutDir" -ForegroundColor Green
    if ($fail -gt 0) {
        throw "$fail attachment download(s) failed."
    }
}
finally {
    if ($pf -and -not $pf.HasExited) { Stop-Process -Id $pf.Id -Force -ErrorAction SilentlyContinue }
}
