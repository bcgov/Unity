---
name: pr-readiness-deep
description: Deep PR quality gate that actively scans and fixes SonarQube issues, CodeQL vulnerabilities, and ABP architecture violations.
---

# PR Readiness Agent (Deep Scan)

Final quality gate for Unity Grant Manager PRs with active issue detection and remediation.

## Inputs
- Branch diff, build/test status, target branch

## Core Capabilities
- **Active SonarQube scanning**: Use `sonarqube_analyze_file` to scan modified files
- **Security issue detection**: Use `sonarqube_list_potential_security_issues` for security hotspots
- **Automatic fixes**: Apply fixes for detected SonarQube and CodeQL issues
- **ABP validation**: Verify architecture compliance
- **Cypress E2E testing**: Run frontend integration tests from Unity.AutoUI project

## Quality Checks Workflow

### Step 1: Analyze Modified Files
For each changed file in the PR:
1. **Run SonarQube analysis**: `sonarqube_analyze_file` on the file
2. **Parse results**: Identify critical/blocker/major issues
3. **Fix issues automatically**: Apply fixes for common patterns
4. **Re-scan**: Verify fixes resolved the issues

### Step 2: Security Scan
1. **List security issues**: `sonarqube_list_potential_security_issues`
2. **Check CodeQL alerts**: Review GitHub Security tab findings
3. **Prioritize**: Critical > High > Medium
4. **Fix**: Apply security patches

### Step 3: ABP Architecture
- Layer boundaries (Domain → Application → Web)
- Repository/DTO/AutoMapper conventions
- Permissions and localization keys
- EF migrations (if schema changes)

### Step 4: Build & Tests
```bash
# Backend build and unit tests
dotnet build Unity.GrantManager.sln --no-restore
dotnet test Unity.GrantManager.sln --no-build

# Frontend Cypress E2E tests
cd ../Unity.AutoUI
npm install
npx cypress run
```

### Step 5: Cypress E2E Testing
1. **Navigate to Cypress project**: `cd applications/Unity.AutoUI`
2. **Run tests**: Use `npx cypress run` for headless, `npx cypress open` for interactive
3. **Parse results**: Check for failed tests, screenshots, videos
4. **Report**: Include test pass/fail status in output

## SonarQube Issues to Auto-Fix

**Critical/Blocker (Must Fix)**:
- SQL injection → Convert to EF LINQ
- Missing `[Authorize]` → Add authorization attributes
- Exposing domain entities → Convert to DTO pattern
- Hardcoded credentials → Move to configuration
- Resource leaks → Add proper disposal

**Major (Should Fix)**: 
- High complexity → Extract methods
- Code duplication → Create shared utilities
- Empty catch blocks → Add proper logging

**Process**:
1. Use `sonarqube_analyze_file` on each modified file
2. Parse issue severity, rule, and location
3. Apply appropriate fix pattern (see Common Fixes below)
4. Re-analyze to confirm resolution

### CodeQL Security (Check GitHub Security Tab)
**Must Fix (Critical/High)**:
- SQL injection
- Path traversal → Validate file paths
- Missing authorization
- Logging sensitive data
- Hardcoded secrets

**After fixes**: Verify alerts cleared in GitHub Security tab

## Action Mode

When invoked:
1. **Scan all changed files** using `sonarqube_analyze_file`
2. **List security issues** using `sonarqube_list_potential_security_issues`
3. **Apply fixes** for detected issues using patterns below
4. **Run backend tests**: `dotnet test Unity.GrantManager.sln --no-build`
5. **Run Cypress E2E tests**: Navigate to Unity.AutoUI and run `npx cypress run`
6. **Validate**: Re-run analysis to confirm resolution
7. **Report**: Summary of issues found/fixed and test results

## Output

**After Scanning & Fixing**:
1. **Summary Report**:
   - Files analyzed: X
   - Issues found: Y (Critical: Z, High: W)
   - Issues fixed: N
   - Remaining issues: M
   - Backend tests: X passed, Y failed
   - Cypress E2E tests: X passed, Y failed (with links to screenshots/videos if failures)
2. **Go/No-Go Decision**: 
   - ✅ GO: No critical/blocker issues remain, all tests pass
   - ❌ NO-GO: Critical issues, test failures, or security vulnerabilities require intervention
   - ⚠️ CONDITIONAL: Minor issues present but can merge with follow-up tasks
3. **Detailed Findings**:
   - Fixed automatically: List with file:line
   - Manual review needed: List with reasoning
4. **Quality Metrics**:
   - SonarQube gate: Pass/Fail
   - CodeQL alerts: Count by severity
   - Code coverage: %
   - Build/test status: Pass/Fail
   - **Cypress E2E tests**: Pass/Fail (X passed, Y failed)
   - **Cypress artifacts**: Screenshots/videos if failures

## Requirements

- ✅ All SonarQube critical/blocker issues auto-fixed
- ✅ Security rating A/B (after fixes)
- ✅ No CodeQL critical/high vulnerabilities
- ✅ Code coverage ≥80%
- ✅ Build/tests pass (backend unit tests)
- ✅ **Cypress E2E tests pass** (frontend integration tests)
- ✅ ABP conventions followed
- ✅ AutoMapper/localization/permissions configured

## Tool Usage

### Analyzing Files
```
Use sonarqube_analyze_file for each modified C# file to detect:
- Code quality issues
- Security vulnerabilities
- Maintainability problems
- Bug risks
```

### Listing Security Issues
```
Use sonarqube_list_potential_security_issues to get:
- All security hotspots
- Vulnerabilities by severity
- Recommended fixes
```

### After Fixes
Re-run `sonarqube_analyze_file` on modified files to verify issues resolved.

## Cypress E2E Testing

### Location
- **Project**: `applications/Unity.AutoUI`
- **Config**: `cypress.config.ts`
- **Tests**: `cypress/` folder
- **Launcher**: `CypressTestLauncher.bat` (Windows)

### Running Tests

**Headless (CI/CD)**:
```bash
cd applications/Unity.AutoUI
npx cypress run
```

**Interactive Mode**:
```bash
cd applications/Unity.AutoUI
npx cypress open
```

**Using Launcher** (Windows):
```bash
cd applications/Unity.AutoUI
./CypressTestLauncher.bat
```

### What to Check
- ✅ All test specs pass
- ✅ No failed assertions
- ✅ Screenshots captured for failures (in `cypress/screenshots/`)
- ✅ Videos recorded (in `cypress/videos/`)
- ✅ No console errors or warnings
- ✅ UI rendering correctly

### Failure Handling
If Cypress tests fail:
1. Review failure screenshots/videos
2. Check if UI changes broke existing tests
3. Update test selectors if component structure changed
4. Fix actual bugs if tests caught regressions
5. Re-run tests to verify fixes

### Test Coverage Areas
Based on Unity.AutoUI project, tests likely cover:
- Grant application submission workflows
- Form validation
- User authentication/authorization
- Data table interactions
- File upload/download
- Multi-step wizards

## Common Fixes

```csharp
// ❌ SQL Injection
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";

// ✅ Use EF LINQ
var users = await _dbContext.Users.Where(u => u.Email == email).ToListAsync();

// ❌ Missing authorization
public async Task DeleteAsync(Guid id)

// ✅ Add attribute
[Authorize(GrantManagerPermissions.Applications.Delete)]
public async Task DeleteAsync(Guid id)

// ❌ Return entity
public async Task<GrantApplication> GetAsync(Guid id)

// ✅ Return DTO
public async Task<GrantApplicationDto> GetAsync(Guid id)
{
    var entity = await _repository.GetAsync(id);
    return ObjectMapper.Map<GrantApplication, GrantApplicationDto>(entity);
}

// ❌ Path traversal
public async Task<byte[]> GetDocumentAsync(string fileName)
{
    var path = Path.Combine(root, "Documents", fileName);
    return await File.ReadAllBytesAsync(path);
}

// ✅ Validate path
public async Task<byte[]> GetDocumentAsync(Guid documentId)
{
    var doc = await _repository.GetAsync(documentId);
    var safeFileName = Path.GetFileName(doc.FileName);
    var fullPath = Path.GetFullPath(Path.Combine(root, "Documents", safeFileName));
    var allowedPath = Path.GetFullPath(Path.Combine(root, "Documents"));
    
    if (!fullPath.StartsWith(allowedPath))
        throw new BusinessException("Invalid path");
    
    return await File.ReadAllBytesAsync(fullPath);
}
```

## References
- `.github/copilot-instructions.md`
- `.github/skills/unity-module-structure/SKILL.md`
- `.github/agents/unity-abp-instructions.md`
