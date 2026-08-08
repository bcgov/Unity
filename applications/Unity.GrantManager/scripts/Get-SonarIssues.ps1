<#
.SYNOPSIS
    Pulls open SonarCloud issues for a branch via the public API and writes them to a Markdown report.

.DESCRIPTION
    Calls the SonarCloud /api/issues/search endpoint (paginating past its 500-per-page limit),
    then groups the results by severity into a Markdown file - handy for pasting into Copilot/Claude
    or attaching to a PR instead of screen-scraping the SonarCloud UI.

.PARAMETER ProjectKey
    SonarCloud project (component) key. Default: bcgov_Unity.

.PARAMETER Branch
    Branch name to query. If omitted, you'll be prompted to pick the current git branch, one of
    dev/test/main, or type a custom name.

.PARAMETER Token
    SonarCloud token (Account > Security > Generate Token) with browse access to the project.
    Falls back to the SONAR_TOKEN environment variable. Not required for public projects, but
    without it you'll only see issues visible to anonymous users.

.PARAMETER OutputPath
    Path to the Markdown file to write. Default: sonar-issues-<branch>.md in the current directory.

.PARAMETER Severities
    Optional filter, e.g. -Severities BLOCKER,CRITICAL. Valid values: BLOCKER, CRITICAL, MAJOR, MINOR, INFO.

.PARAMETER Types
    Optional filter, e.g. -Types BUG,VULNERABILITY. Valid values: BUG, VULNERABILITY, CODE_SMELL.

.PARAMETER IncludeResolved
    Include resolved/closed issues too. By default only unresolved (open) issues are fetched.

.PARAMETER FixLevel
    Which fix-complexity tier(s) Claude Code should fix after the report is written, without being
    prompted: None, Quick (quick wins only), QuickModerate (quick + moderate), or All. Omit to be
    prompted interactively instead.

.PARAMETER NoFixPrompt
    Skip the "what should Claude Code fix?" prompt entirely (equivalent to -FixLevel None) - use
    this for unattended/CI runs where nothing should launch afterwards.

.EXAMPLE
    .\Get-SonarIssues.ps1 -Branch main -Token $env:SONAR_TOKEN

.EXAMPLE
    .\Get-SonarIssues.ps1 -ProjectKey bcgov_Unity -Branch feature/AB-12345 -Severities BLOCKER,CRITICAL -OutputPath .\sonar-report.md

.EXAMPLE
    # No -Branch given -> prompts to choose current/dev/test/main/custom
    .\Get-SonarIssues.ps1

.EXAMPLE
    # Skips both prompts: known branch, and never launches Claude Code afterwards
    .\Get-SonarIssues.ps1 -Branch main -NoFixPrompt

.EXAMPLE
    # Skips the fix-level prompt only, going straight to fixing quick wins
    .\Get-SonarIssues.ps1 -Branch main -FixLevel Quick
#>
param(
    [string]$ProjectKey = "bcgov_Unity",

    [string]$Branch = "",

    [string]$Token = $env:SONAR_TOKEN,

    [string]$OutputPath = "",

    [ValidateSet("BLOCKER", "CRITICAL", "MAJOR", "MINOR", "INFO")]
    [string[]]$Severities = @(),

    [ValidateSet("BUG", "VULNERABILITY", "CODE_SMELL")]
    [string[]]$Types = @(),

    [switch]$IncludeResolved,

    [ValidateSet("None", "Quick", "QuickModerate", "All")]
    [string]$FixLevel = "",

    [switch]$NoFixPrompt
)

$ErrorActionPreference = "Stop"

$ApiBase = "https://sonarcloud.io/api/issues/search"
$PageSize = 500
$SeverityOrder = @("BLOCKER", "CRITICAL", "MAJOR", "MINOR", "INFO")
$WellKnownBranches = @("dev", "test", "main")

function Get-CurrentGitBranch {
    try {
        $branch = git rev-parse --abbrev-ref HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $branch -and $branch -ne "HEAD") {
            return $branch.Trim()
        }
    }
    catch {
        # Not in a git repo, or git isn't on PATH - fall through to $null.
    }
    return $null
}

function Read-BranchSelection {
    param([string]$CurrentBranch)

    # Build an ordered menu: current branch first (if known), then the well-known branches
    # (skipping one that duplicates the current branch), then a free-text option.
    $menu = [ordered]@{}
    $index = 1

    if ($CurrentBranch) {
        $menu["$index"] = @{ Label = "Current branch ($CurrentBranch)"; Value = $CurrentBranch }
        $index++
    }
    foreach ($wellKnown in $WellKnownBranches) {
        if ($wellKnown -ne $CurrentBranch) {
            $menu["$index"] = @{ Label = $wellKnown; Value = $wellKnown }
            $index++
        }
    }
    $menu["$index"] = @{ Label = "Other (type a branch name)"; Value = $null }

    Write-Host ""
    Write-Host "Which branch's SonarCloud issues do you want?" -ForegroundColor Cyan
    foreach ($key in $menu.Keys) {
        Write-Host "  [$key] $($menu[$key].Label)"
    }

    $defaultKey = ($menu.Keys | Select-Object -First 1)

    while ($true) {
        $choice = Read-Host "Enter choice (default: $defaultKey)"
        if ([string]::IsNullOrWhiteSpace($choice)) { $choice = $defaultKey }

        if ($menu.Contains($choice)) {
            $selected = $menu[$choice]
            if ($null -ne $selected.Value) {
                return $selected.Value
            }

            $custom = Read-Host "Branch name"
            if (-not [string]::IsNullOrWhiteSpace($custom)) {
                return $custom.Trim()
            }
            Write-Host "Branch name cannot be empty." -ForegroundColor Yellow
            continue
        }

        Write-Host "Invalid choice '$choice' - try again." -ForegroundColor Yellow
    }
}

# Maps a -FixLevel value (or the equivalent interactive menu choice) to the FixComplexity tiers
# it covers. "None" intentionally maps to an empty array - nothing gets fixed.
$FixLevelTierMap = [ordered]@{
    "None"          = @()
    "Quick"         = @("Quick")
    "QuickModerate" = @("Quick", "Moderate")
    "All"           = @("Quick", "Moderate", "Complex")
}

function Read-FixLevelSelection {
    $menu = [ordered]@{
        "1" = @{ Label = "None - just write the report"; Level = "None" }
        "2" = @{ Label = "Quick wins only"; Level = "Quick" }
        "3" = @{ Label = "Quick wins + Moderate"; Level = "QuickModerate" }
        "4" = @{ Label = "Everything, including Complex (review its plan before it edits!)"; Level = "All" }
    }

    Write-Host ""
    Write-Host "Should Claude Code fix any of these issues now?" -ForegroundColor Cyan
    foreach ($key in $menu.Keys) {
        Write-Host "  [$key] $($menu[$key].Label)"
    }

    $defaultKey = "1"
    while ($true) {
        $choice = Read-Host "Enter choice (default: $defaultKey - None)"
        if ([string]::IsNullOrWhiteSpace($choice)) { $choice = $defaultKey }

        if ($menu.Contains($choice)) {
            return $menu[$choice].Level
        }
        Write-Host "Invalid choice '$choice' - try again." -ForegroundColor Yellow
    }
}

if (-not $Branch) {
    $Branch = Read-BranchSelection -CurrentBranch (Get-CurrentGitBranch)
}

if (-not $OutputPath) {
    $branchSlug = if ($Branch) { ($Branch -replace '[\\/:*?"<>|]', '-') } else { "default-branch" }
    $OutputPath = "sonar-issues-$branchSlug.md"
}

# Windows PowerShell 5.1's Invoke-RestMethod doesn't reliably honor UTF-8 for JSON responses
# (SonarCloud's Content-Type header doesn't include a charset), which silently mangles any
# non-ASCII characters in issue messages (e.g. "…" becomes "â¦"). Reading the response as raw
# bytes and decoding as UTF-8 ourselves sidesteps that, on both Windows PowerShell and PS7+.
Add-Type -AssemblyName System.Net.Http

function Get-SonarIssuePage {
    param(
        [int]$Page
    )

    $queryParams = @{
        componentKeys = $ProjectKey
        p             = $Page
        ps            = $PageSize
    }
    # Omitting "resolved" returns issues in every status; passing "false" restricts to open ones.
    if (-not $IncludeResolved) { $queryParams["resolved"] = "false" }
    if ($Branch) { $queryParams["branch"] = $Branch }
    if ($Severities.Count -gt 0) { $queryParams["severities"] = ($Severities -join ",") }
    if ($Types.Count -gt 0) { $queryParams["types"] = ($Types -join ",") }

    $queryString = ($queryParams.GetEnumerator() | ForEach-Object {
        "$($_.Key)=$([uri]::EscapeDataString([string]$_.Value))"
    }) -join "&"
    $uri = "$ApiBase`?$queryString"

    $client = New-Object System.Net.Http.HttpClient
    try {
        if ($Token) {
            $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token)
        }

        $response = $client.GetAsync($uri).GetAwaiter().GetResult()
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)

        if (-not $response.IsSuccessStatusCode) {
            $status = [int]$response.StatusCode
            if ($status -eq 401 -or $status -eq 403) {
                throw "SonarCloud returned $status - pass -Token (or set `$env:SONAR_TOKEN) with access to '$ProjectKey'."
            }
            throw "SonarCloud returned $status`: $text"
        }

        return $text | ConvertFrom-Json
    }
    finally {
        $client.Dispose()
    }
}

Write-Host "Fetching SonarCloud issues for '$ProjectKey'$(if ($Branch) { " (branch: $Branch)" })..." -ForegroundColor Cyan

$allIssues = New-Object System.Collections.Generic.List[object]
$page = 1
$total = $null

do {
    $response = Get-SonarIssuePage -Page $page
    if ($null -eq $total) {
        $total = $response.total
        Write-Host "Total matching issues: $total"
        if ($total -gt 10000) {
            Write-Warning "SonarCloud caps searchable results at 10000. Narrow with -Severities/-Types to see everything."
        }
    }

    foreach ($issue in $response.issues) { $allIssues.Add($issue) }
    Write-Host "  Page $page - fetched $($response.issues.Count) issues (running total: $($allIssues.Count))"
    $page++
}
while ($allIssues.Count -lt $total -and $response.issues.Count -gt 0 -and $allIssues.Count -lt 10000)

if ($allIssues.Count -eq 0) {
    Write-Host "No issues found." -ForegroundColor Green
}

# --- Build component (file path) lookup so we can print clean relative paths ---
$componentPaths = @{}
foreach ($comp in $response.components) {
    $componentPaths[$comp.key] = if ($comp.path) { $comp.path } else { $comp.name }
}

function Get-IssueFilePath {
    param($Issue)
    if ($componentPaths.ContainsKey($Issue.component)) {
        return $componentPaths[$Issue.component]
    }
    return ($Issue.component -replace "^$([regex]::Escape($ProjectKey)):", "")
}

# --- Fix-complexity classification ---
# Goal: separate mechanical, low-risk fixes (rename, swap one API for another, drop an unused var)
# from ones that ripple across every call site or need an actual design change, so the report can
# surface the safe/quick wins separately from the ones that need a careful look first.
#
# Rule suffixes (the part after "repo:", e.g. "S107" in "javascript:S107") that are almost always
# a multi-call-site or structural change, regardless of Sonar's own per-occurrence effort estimate -
# that estimate only covers touching the one flagged location, not everywhere it ripples to.
$RippleRuleSuffixes = @{
    "S107"  = "too many parameters - fixing this well means updating every call site"
    "S1200" = "too many dependencies/coupling - needs a broader design change"
    "S110"  = "inheritance depth too deep - needs restructuring the class hierarchy"
    "S1448" = "too many methods (God Class) - needs splitting the class"
}

function Get-EffortMinutes {
    param([string]$Effort)
    if (-not $Effort) { return 0 }
    $minutes = 0
    if ($Effort -match '(\d+)d') { $minutes += [int]$matches[1] * 8 * 60 }
    if ($Effort -match '(\d+)h') { $minutes += [int]$matches[1] * 60 }
    if ($Effort -match '(\d+)min') { $minutes += [int]$matches[1] }
    return $minutes
}

function Get-FixComplexity {
    param($Issue)

    $ruleSuffix = ($Issue.rule -split ':')[-1]
    if ($RippleRuleSuffixes.Contains($ruleSuffix)) {
        return [PSCustomObject]@{
            Tier   = "Complex"
            Order  = 2
            Badge  = "Complex"
            Reason = $RippleRuleSuffixes[$ruleSuffix]
        }
    }

    $minutes = Get-EffortMinutes $Issue.effort
    if ($minutes -le 5) {
        return [PSCustomObject]@{ Tier = "Quick"; Order = 0; Badge = "Quick win"; Reason = "Small, localized fix" }
    }
    elseif ($minutes -le 30) {
        return [PSCustomObject]@{ Tier = "Moderate"; Order = 1; Badge = "Moderate"; Reason = "Localized but takes some care" }
    }
    else {
        return [PSCustomObject]@{ Tier = "Complex"; Order = 2; Badge = "Complex"; Reason = "High estimated effort" }
    }
}

# Precompute complexity once per issue and hang it off the object so sorting/grouping/rendering
# below don't each recompute it.
foreach ($issue in $allIssues) {
    $complexity = Get-FixComplexity -Issue $issue
    Add-Member -InputObject $issue -MemberType NoteProperty -Name "FixComplexity" -Value $complexity -Force
}

# --- Group and sort ---
$bySeverity = $allIssues | Group-Object severity
$summaryRows = $SeverityOrder | ForEach-Object {
    $sev = $_
    $count = ($bySeverity | Where-Object { $_.Name -eq $sev } | Select-Object -ExpandProperty Count)
    [PSCustomObject]@{ Severity = $sev; Count = if ($count) { $count } else { 0 } }
}

$byComplexity = $allIssues | Group-Object { $_.FixComplexity.Tier }
$complexityOrder = @("Quick", "Moderate", "Complex")
$complexitySummaryRows = $complexityOrder | ForEach-Object {
    $tier = $_
    $count = ($byComplexity | Where-Object { $_.Name -eq $tier } | Select-Object -ExpandProperty Count)
    [PSCustomObject]@{ Tier = $tier; Count = if ($count) { $count } else { 0 } }
}

# --- Write Markdown ---
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# SonarCloud Issues Report")
[void]$md.AppendLine("")
[void]$md.AppendLine("- **Project:** $ProjectKey")
[void]$md.AppendLine("- **Branch:** $(if ($Branch) { $Branch } else { '(default)' })")
[void]$md.AppendLine("- **Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$md.AppendLine("- **Total open issues:** $($allIssues.Count)")
[void]$md.AppendLine("")
[void]$md.AppendLine("## Summary")
[void]$md.AppendLine("")
[void]$md.AppendLine("| Severity | Count |")
[void]$md.AppendLine("|---|---|")
foreach ($row in $summaryRows) {
    [void]$md.AppendLine("| $($row.Severity) | $($row.Count) |")
}
[void]$md.AppendLine("")
[void]$md.AppendLine("### Fix complexity")
[void]$md.AppendLine("")
[void]$md.AppendLine("How much a fix is likely to touch: **Quick win** is a small, localized change ")
[void]$md.AppendLine("(rename, swap one call for another, drop something unused). **Moderate** is localized ")
[void]$md.AppendLine("but takes some care. **Complex** either has a high estimated effort or is a rule ")
[void]$md.AppendLine("(like too-many-parameters) that typically means updating every call site, not just ")
[void]$md.AppendLine("the flagged line - review those before diving in.")
[void]$md.AppendLine("")
[void]$md.AppendLine("| Fix Complexity | Count |")
[void]$md.AppendLine("|---|---|")
foreach ($row in $complexitySummaryRows) {
    [void]$md.AppendLine("| $($row.Tier) | $($row.Count) |")
}
[void]$md.AppendLine("")

$quickWins = $allIssues | Where-Object { $_.FixComplexity.Tier -eq "Quick" } | Sort-Object { Get-IssueFilePath $_ }, { $_.textRange.startLine }
if ($quickWins.Count -gt 0) {
    [void]$md.AppendLine("## Quick Wins ($($quickWins.Count))")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("Safe, low-risk fixes across every severity - start here.")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("| Severity | File | Line | Rule | Message |")
    [void]$md.AppendLine("|---|---|---|---|---|")
    foreach ($issue in $quickWins) {
        $file = Get-IssueFilePath $issue
        $line = if ($issue.textRange -and $issue.textRange.startLine) { $issue.textRange.startLine } else { "-" }
        $message = ($issue.message -replace '\|', '\|')
        $link = "https://sonarcloud.io/project/issues?id=$ProjectKey&issues=$($issue.key)&open=$($issue.key)$(if ($Branch) { "&branch=$Branch" })"
        [void]$md.AppendLine("| $($issue.severity) | ``$file`` | $line | [$($issue.rule)]($link) | $message |")
    }
    [void]$md.AppendLine("")
}

foreach ($sev in $SeverityOrder) {
    $group = $bySeverity | Where-Object { $_.Name -eq $sev }
    if (-not $group -or $group.Count -eq 0) { continue }

    [void]$md.AppendLine("## $sev ($($group.Count))")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("| File | Line | Type | Complexity | Effort | Rule | Message |")
    [void]$md.AppendLine("|---|---|---|---|---|---|---|")

    $sorted = $group.Group | Sort-Object { $_.FixComplexity.Order }, { Get-IssueFilePath $_ }, { $_.textRange.startLine }
    foreach ($issue in $sorted) {
        $file = Get-IssueFilePath $issue
        $line = if ($issue.textRange -and $issue.textRange.startLine) { $issue.textRange.startLine } else { "-" }
        $message = ($issue.message -replace '\|', '\|')
        $effort = if ($issue.effort) { $issue.effort } else { "-" }
        $link = "https://sonarcloud.io/project/issues?id=$ProjectKey&issues=$($issue.key)&open=$($issue.key)$(if ($Branch) { "&branch=$Branch" })"
        [void]$md.AppendLine("| ``$file`` | $line | $($issue.type) | $($issue.FixComplexity.Badge) | $effort | [$($issue.rule)]($link) | $message |")
    }
    [void]$md.AppendLine("")
}

Set-Content -Path $OutputPath -Value $md.ToString() -Encoding utf8
Write-Host "Report written to $OutputPath" -ForegroundColor Green

# --- Optionally hand some/all of the issues to Claude Code to fix ---
if ($allIssues.Count -gt 0) {
    if ($NoFixPrompt) {
        $resolvedFixLevel = "None"
    }
    elseif ($FixLevel) {
        $resolvedFixLevel = $FixLevel
    }
    else {
        $resolvedFixLevel = Read-FixLevelSelection
    }

    $fixTiers = $FixLevelTierMap[$resolvedFixLevel]

    if ($fixTiers.Count -gt 0) {
        $issuesToFix = $allIssues | Where-Object { $fixTiers -contains $_.FixComplexity.Tier }

        if ($issuesToFix.Count -eq 0) {
            Write-Host "No issues match fix level '$resolvedFixLevel'." -ForegroundColor Yellow
        }
        else {
            $claudeCmd = Get-Command claude -ErrorAction SilentlyContinue
            if (-not $claudeCmd) {
                Write-Warning "Claude Code CLI ('claude') was not found on PATH. Open $OutputPath yourself and share the relevant rows with your AI assistant of choice."
            }
            else {
                $tierList = $fixTiers -join ", "
                $extraGuidance = ""
                if ($fixTiers -contains "Complex") {
                    $extraGuidance = " Issues marked Complex often ripple across multiple call sites (e.g. too-many-parameters) or need a design change - for those, work out and share your plan before editing, rather than doing a blind mechanical fix."
                }
                elseif ($fixTiers -contains "Moderate") {
                    $extraGuidance = " Issues marked Moderate are localized to one function/file but need a bit more care than the Quick wins."
                }

                $fixPrompt = "I generated a SonarCloud issues report at '$OutputPath'. In it, fix every issue " +
                    "whose Complexity column is one of: $tierList (there are $($issuesToFix.Count) such issues, " +
                    "spread across the per-severity tables and/or the Quick Wins section).$extraGuidance " +
                    "Skip anything not in that complexity set. Summarize what you changed when done."

                Write-Host ""
                Write-Host "Launching Claude Code to fix $($issuesToFix.Count) issue(s) [$tierList]..." -ForegroundColor Cyan
                & claude $fixPrompt
            }
        }
    }
}
