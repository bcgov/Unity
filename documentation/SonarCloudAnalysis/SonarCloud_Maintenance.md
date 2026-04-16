# Unity SonarCloud Maintenance Guide

This document provides operational procedures for maintaining the SonarCloud integration for the Unity Grant Manager project, including recurring housekeeping tasks, troubleshooting procedures, and optimization recommendations.

---

## Overview

**Maintenance Level:** Low (SaaS platform, minimal operational overhead)  
**Primary Maintenance:** Token renewal (90-day cycles)  
**Secondary Maintenance:** Configuration updates, workflow optimization  
**Support:** SonarSource enterprise support + BC Gov DevOps team  

---

## Recurring Maintenance Tasks

### 🔄 **Token Renewal (Every 90 Days)**

**Critical Task - Required for Continued Operation**

#### Schedule
- **Personal tokens expire every 90 days**
- **Recommended renewal:** 85 days (5-day buffer)
- **Calendar reminder suggested** for token expiration tracking

#### Renewal Process
1. **Generate new token:**
   - Go to https://sonarcloud.io/account/security
   - Click "Generate Token"
   - Name: `Unity GitHub Actions - YYYY-MM-DD`
   - Expiration: 90 days
   - **Copy token immediately** (only shown once)

2. **Update GitHub Secret:**
   - Go to https://github.com/bcgov/Unity/settings/secrets/actions
   - Find `SONAR_TOKEN` secret
   - Click "Update"
   - Paste new token value
   - Click "Update secret"

3. **Verify operation:**
   - Trigger workflow manually or create test PR
   - Monitor GitHub Actions for successful execution
   - Check SonarCloud for analysis results

4. **Clean up old token:**
   - Return to https://sonarcloud.io/account/security
   - Revoke previous token (optional)

#### Automation Opportunities
- **Calendar integration:** Outlook/Google Calendar reminders
- **GitHub Issues:** Create recurring issues with 85-day intervals
- **Monitoring alerts:** Set up alerts for workflow failures (token expiration symptom)

---

### 🔍 **Monthly Health Checks**

#### Workflow Performance Review
- **Check average execution time:** Target <5 minutes
- **Review failure rate:** Target <1% failure rate
- **Monitor resource usage:** Cache hit rates, download times

```bash
# GitHub CLI commands for monitoring
gh run list --workflow="SonarCloud Analysis" --limit 50
gh run view --log <run-id>  # For failed runs
```

#### Quality Gate Monitoring
- **Review quality gate status** trends in SonarCloud
- **Check coverage metrics** for any significant drops
- **Monitor security hotspots** and vulnerabilities

#### Configuration Drift Detection
- **Verify `sonar-project.properties`** hasn't been inadvertently modified
- **Check branch pattern configuration** in SonarCloud
- **Validate workflow file** against documented standard

---

### 📊 **Quarterly Analysis Reviews**

#### Performance Metrics
- **Workflow execution trends**
- **Analysis coverage statistics**
- **Quality gate compliance rates**
- **Developer adoption/usage patterns**

#### Cost Optimization
- **Review GitHub Actions usage** (included in BC Gov plan)
- **Assess SonarCloud organization limits** (project count, analysis frequency)
- **Evaluate upgrade opportunities** (Automatic vs CI-based analysis)

#### Security Assessment
- **Review token usage patterns** in audit logs
- **Validate permissions** are still minimal required set
- **Check for security warnings** in GitHub dependency scanning
- **Assess workflow security** against latest best practices

---

## Common Issues and Resolutions

### 🚨 **Critical Issues**

#### Analysis Failures (Authentication)
**Symptoms:**
- HTTP 403 Forbidden errors
- "Project not found" messages
- "Authentication with the server has failed"

**Immediate Actions:**
1. **Check token expiration** (most common cause)
2. **Verify GitHub secret** contains valid token
3. **Test token manually** in SonarCloud UI
4. **Generate new token** if expired/invalid
5. **Update GitHub secret** with new token

**Prevention:**
- Set token renewal reminders 5 days before expiration
- Monitor workflow failures for authentication patterns

#### Analysis Method Conflicts
**Symptoms:**
- "CI analysis while Automatic Analysis is enabled" error
- Duplicate analysis attempts

**Resolution:**
1. **Go to SonarCloud project** → Administration → Analysis Method
2. **Choose one method:**
   - **CI-based:** Disable Automatic Analysis
   - **Automatic:** Remove/disable GitHub Actions workflow
3. **Cannot run both simultaneously**

#### Workflow Permission Errors
**Symptoms:**
- GitHub Actions permission denied
- Cannot write PR comments
- Check run creation failures

**Resolution:**
1. **Verify workflow permissions** block exists:
```yaml
permissions:
  contents: read
  pull-requests: write
  checks: write
  security-events: write
```
2. **Check repository settings** for Actions permissions
3. **Verify GitHub App** is properly installed

### ⚠️ **Warning-Level Issues**

#### Slow Analysis Performance
**Symptoms:**
- Workflow execution >10 minutes
- Frequent timeouts
- Cache miss patterns

**Optimization Steps:**
1. **Review cache configuration** in workflow
2. **Check test execution time** (largest component)
3. **Consider excluding large files** if appropriate
4. **Monitor SonarCloud performance** status page

#### Coverage Drops
**Symptoms:**
- Significant test coverage decreases
- Missing coverage reports

**Investigation Steps:**
1. **Check test execution logs** for failures
2. **Verify coverage report paths** in configuration
3. **Review recent code changes** for uncovered areas
4. **Validate test suite completeness**

#### Node.js Deprecation Warnings
**Symptoms:**
- GitHub Actions deprecation notices
- Action version warnings

**Resolution:**
1. **Update action versions** in workflow file:
```yaml
- uses: actions/setup-java@v5      # was v4
- uses: actions/setup-dotnet@v5    # was v4  
- uses: actions/cache@v5           # was v4
```
2. **Test updated workflow** thoroughly
3. **Monitor for new deprecation notices**

---

## Optimization Opportunities

### 🔄 **Analysis Method Evaluation**

#### Consider Automatic Analysis
**When to evaluate:**
- High maintenance overhead from token management
- Simple build requirements (no custom coverage collection)
- Multiple repositories needing similar setup

**Benefits:**
- Zero token management
- Automatic execution on all pushes
- Reduced GitHub Actions usage

**Trade-offs:**
- Less control over build process
- No custom test coverage collection
- Cannot customize analysis execution

#### Evaluation Process:
1. **Test Automatic Analysis** on feature branch
2. **Compare results** with CI-based analysis
3. **Assess coverage** and quality metrics
4. **Make decision** based on requirements vs. maintenance overhead

### ⚡ **Performance Optimization**

#### Workflow Caching Improvements
```yaml
# Enhanced caching strategy
- name: Cache .NET packages
  uses: actions/cache@v5
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

- name: Cache SonarCloud data
  uses: actions/cache@v5
  with:
    path: ~/.sonar/cache
    key: ${{ runner.os }}-sonar-${{ github.sha }}
    restore-keys: ${{ runner.os }}-sonar-
```

#### Selective Analysis
```yaml
# Run analysis only on specific branches for faster feedback
on:
  push:
    branches: [main, test, dev]  # Exclude feature branches if desired
  pull_request:
    branches: [main, test, dev]   # Analyze PRs to main branches
```

### 🔐 **Security Enhancements**

#### Token Security Improvements
- **Use organization-level tokens** when available
- **Implement token rotation** automation
- **Monitor token usage** in audit logs
- **Restrict token scope** to minimum required

#### Workflow Security Hardening
```yaml
# Enhanced permissions (principle of least privilege)
permissions:
  contents: read          # Read repository code
  pull-requests: write   # Comment on PRs (can be read for view-only)
  checks: write          # Create status checks (can be read for view-only)  
  security-events: write # Report security findings (if needed)
```

---

## Troubleshooting Procedures

### 📋 **Diagnostic Checklist**

#### Pre-troubleshooting Information Gathering
1. **Error message** and full context
2. **Workflow run ID** and logs
3. **Recent changes** to configuration
4. **SonarCloud project** current state
5. **Token expiration** status

#### Step-by-step Diagnosis
1. **Check GitHub Actions logs:**
   ```
   Repository → Actions → Failed workflow → View logs
   ```

2. **Verify SonarCloud project:**
   ```
   https://sonarcloud.io/project/overview?id=bcgov_Unity
   ```

3. **Test authentication manually:**
   ```
   Go to SonarCloud → Try to trigger manual analysis
   ```

4. **Check configuration files:**
   ```
   applications/Unity.GrantManager/sonar-project.properties
   .github/workflows/sonarsource-scan.yml
   ```

### 🛠️ **Recovery Procedures**

#### Complete Workflow Reset
If issues persist after standard troubleshooting:

1. **Generate fresh token** in SonarCloud
2. **Update GitHub secret** with new token
3. **Verify configuration files** match documented standards
4. **Test on clean feature branch** with minimal changes
5. **Escalate to BC Gov DevOps** if issues continue

#### Configuration Rollback
```bash
# Restore known-good configuration
git checkout HEAD~1 -- .github/workflows/sonarsource-scan.yml
git checkout HEAD~1 -- applications/Unity.GrantManager/sonar-project.properties
```

#### Emergency Disable
```yaml
# Temporarily disable workflow by commenting out triggers
# on:
#   push:
#   pull_request:
```

---

## Monitoring and Alerting

### 📈 **Key Metrics to Monitor**

#### Success Metrics
- **Workflow success rate:** >99%
- **Analysis completion time:** <5 minutes average
- **Quality gate pass rate:** Monitor trends
- **PR decoration success:** 100%

#### Failure Indicators
- **Authentication failures:** Token expiration
- **Timeout failures:** Performance issues
- **Configuration errors:** File/path issues
- **Quality gate failures:** Code quality degradation

### 🔔 **Alerting Setup**

#### GitHub Actions Notifications
- **Enable workflow notifications** in personal GitHub settings
- **Set up team notifications** for critical repositories
- **Use GitHub Apps** for Slack/Teams integration

#### SonarCloud Notifications
- **Quality gate failures** on main branches
- **New security vulnerabilities** detection
- **Coverage threshold** breaches

### 📊 **Reporting**

#### Weekly Reports
- Workflow execution summary
- Quality gate status trends
- Token expiration warnings

#### Monthly Reports  
- Performance metrics analysis
- Security findings summary
- Configuration drift assessment

#### Quarterly Reviews
- Cost/benefit analysis
- Optimization opportunities
- Strategic planning recommendations

---

## Support and Escalation

### 🆘 **Support Contacts**

#### Primary Support
- **BC Gov DevOps Team:** [devops-requests](https://github.com/bcgov/devops-requests/issues)
- **SonarCloud Documentation:** https://docs.sonarcloud.io/
- **GitHub Actions Documentation:** https://docs.github.com/actions

#### Secondary Support
- **SonarSource Enterprise Support:** (via BC Gov contract)
- **GitHub Enterprise Support:** (via BC Gov contract)
- **Unity Development Team:** Internal escalation

### 📞 **Escalation Procedures**

#### Level 1: Self-Service (Target: <1 hour)
- Check this maintenance guide
- Review GitHub Actions logs
- Verify token expiration status
- Test with known-good configuration

#### Level 2: Team Support (Target: <4 hours)
- Post in team Slack/Teams channel
- Create GitHub issue with diagnostics
- Contact BC Gov DevOps via standard channels

#### Level 3: Enterprise Support (Target: <24 hours)
- Submit BC Gov DevOps request ticket
- Engage SonarSource enterprise support
- Contact GitHub Enterprise support if needed

### 🆘 **Emergency Contacts**
For production-impacting issues:
- **Critical workflow failures** preventing releases
- **Security vulnerability** discoveries
- **Compliance** or audit issues

---

## Knowledge Management

### 📚 **Documentation Updates**

#### When to Update Documentation
- **Configuration changes** made to workflow or properties
- **New issues discovered** and resolved
- **Process improvements** identified
- **Support procedures** modified

#### Documentation Review Schedule
- **Monthly:** Review accuracy of troubleshooting procedures
- **Quarterly:** Update performance benchmarks and metrics
- **Annually:** Comprehensive review and refresh

### 🎓 **Training and Knowledge Transfer**

#### Onboarding New Team Members
1. **Review this maintenance guide** thoroughly
2. **Walk through token renewal process** (hands-on)
3. **Practice troubleshooting** common issues
4. **Understand escalation procedures** and contacts

#### Cross-Training Activities
- **Document tribal knowledge** in maintenance procedures
- **Create runbooks** for complex scenarios
- **Share lessons learned** from incidents
- **Regular knowledge sharing** sessions

---

## Maintenance Calendar

### 📅 **Scheduled Maintenance Windows**

#### Monthly (First Tuesday)
- [ ] Health check review
- [ ] Performance metrics analysis
- [ ] Token expiration verification
- [ ] Documentation updates

#### Quarterly (End of quarter)
- [ ] Comprehensive performance review
- [ ] Security assessment
- [ ] Cost optimization analysis
- [ ] Strategic planning update

#### Annually (End of fiscal year)
- [ ] Complete documentation refresh
- [ ] Support contact verification
- [ ] Process improvement assessment
- [ ] Technology roadmap review

---

## Change Management

### 🔄 **Configuration Change Process**

#### Standard Changes
1. **Review change** against documented standards
2. **Test in feature branch** before merging
3. **Update documentation** if configuration modified
4. **Communicate changes** to team

#### Emergency Changes
1. **Document rationale** for emergency change
2. **Implement minimum viable fix** quickly
3. **Follow up with proper testing** and documentation
4. **Review incident** for process improvements

#### Change Approval
- **Minor configuration updates:** Team lead approval
- **Workflow modifications:** Peer review required
- **Major architectural changes:** BC Gov DevOps consultation

---

## Conclusion

The Unity SonarCloud integration requires minimal ongoing maintenance due to its SaaS nature and stable configuration. The primary maintenance task is **token renewal every 90 days**, with monthly health checks ensuring continued optimal performance.

**Key Success Factors:**
- ✅ **Proactive token management** prevents service disruptions
- ✅ **Regular monitoring** identifies issues before they impact development
- ✅ **Clear escalation procedures** ensure rapid resolution of complex issues
- ✅ **Comprehensive documentation** enables self-service troubleshooting

This maintenance approach ensures reliable, high-quality code analysis with minimal operational overhead, supporting the Unity team's development productivity and code quality goals.