# CHEFS API Testing

This directory contains Cypress tests for the CHEFS (Common Hosted Form Service) API.

## Files

- **chefs-api-submission.cy.ts**: Cypress test spec for CHEFS form submissions
- **chefs-api-config.json**: Environment configuration (baseURL, formId, versionId, headers)
- **chefs-submission-payload.json**: Form submission payload template

## Setup

### 1. Get a Valid CHEFS Authentication Token

JWT tokens expire regularly (typically within hours/days). To get a fresh token:

#### Option A: From Browser DevTools

1. Navigate to CHEFS test environment: https://chefs-test.apps.silver.devops.gov.bc.ca
2. Login with your IDIR credentials
3. Open browser DevTools (F12)
4. Go to the **Network** tab
5. Submit a form or perform any API action
6. Find an API request to `/app/api/v1/forms/`
7. Click on the request and go to **Headers**
8. Copy the `Authorization` header value (starts with `Bearer eyJ...`)

#### Option B: From Curl Command

If you have a working curl command:

```bash
curl 'https://chefs-test.apps.silver.devops.gov.bc.ca/...' \
  -H 'authorization: Bearer eyJhbGc...'
```

Copy the token from the authorization header.

### 2. Update cypress.env.json

Add or update the token in `cypress.env.json`:

```json
{
  "CHEFS_AUTH_TOKEN": "Bearer *..."
}
```

**Note**: The `Bearer` prefix is optional - the test will handle both formats.

### 3. Update Configuration (if needed)

Edit `chefs-api-config.json` to match your environment:

```json
{
  "environments": {
    "test": {
      "baseURL": "https://chefs-test.apps.silver.devops.gov.bc.ca",
      "formId": "your-form-id-here",
      "versionId": "your-version-id-here"
    }
  }
}
```

## Running the Tests

```bash
# Run all CHEFS API tests
npx cypress run --spec "cypress/scripts/chefs-api-submission.cy.ts"

# Run in headed mode (see browser)
npx cypress open --spec "cypress/scripts/chefs-api-submission.cy.ts"
```

## Test Cases

1. **Submit form via CHEFS API**: Submit a complete form with all fields
2. **Submit with custom data**: Override specific fields (applicant name, project title, etc.)
3. **Draft submission**: Submit as draft (not final)
4. **Retrieve submission**: Get submission details by ID
5. **Update submission files**: Add/update file attachments

## Customizing the Payload

Edit `chefs-submission-payload.json` to customize the form data:

```json
{
  "submission": {
    "data": {
      "_ApplicantName": "Your Custom Name",
      "_organizationName": "Your Organization",
      "_projectTitle": "Your Project",
      "_fundingRequest": 50000
      // ... other fields
    }
  }
}
```

## Troubleshooting

### 401 Unauthorized Error

**Cause**: Token is expired or invalid

**Solution**:

1. Get a fresh token (see Setup step 1)
2. Update `cypress.env.json` with the new token
3. Re-run the tests

### 400 Bad Request Error

**Cause**: Payload data doesn't match form schema

**Solution**:

1. Check form version ID in `chefs-api-config.json`
2. Verify payload structure matches the CHEFS form schema
3. Use browser DevTools to capture a valid submission payload

### Test Skipped (No Token)

**Cause**: `CHEFS_AUTH_TOKEN` not set in `cypress.env.json`

**Solution**: Add token to `cypress.env.json` (see Setup step 2)

## Security Note

⚠️ **Never commit tokens to version control**

- Add `cypress.env.json` to `.gitignore`
- Use environment variables in CI/CD pipelines
- Rotate tokens regularly
- Store tokens securely (e.g., password manager, CI secrets)

## CI/CD Integration

For automated testing in pipelines:

```bash
# Set token as environment variable
export CYPRESS_CHEFS_AUTH_TOKEN="Bearer eyJhbGc..."

# Run tests
npx cypress run --spec "cypress/scripts/chefs-api-submission.cy.ts"
```

Or use Cypress environment variable syntax in your CI config:

```yaml
# Example GitHub Actions
env:
  CYPRESS_CHEFS_AUTH_TOKEN: ${{ secrets.CHEFS_AUTH_TOKEN }}
```

```yaml
# Example GitLab CI
variables:
  CYPRESS_CHEFS_AUTH_TOKEN: $CHEFS_AUTH_TOKEN # From CI/CD variables
```
