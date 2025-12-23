READ ME for the Cypress Test Launcher (Windows 11)

Batch file that opens a small Windows dialog to launch Cypress in either GUI or Headless mode, after copying the right cypress.<env>.env.json into cypress.env.json.

What this is (and what it is not)

This was created for a single workstation and contains hard-coded local paths.

It is NOT portable as-is.

If you pull it from source control, you WILL NEED to edit at least one line (the local project path), and maybe more depending on your setup.

This launcher assumes these files exist in the AutoUI root directory and mirror how OpenShift does token replacement.

Obviously no passwords are in these templates below, you'll have to add them yourself. The creds are securely stored in KeePass. 

When the smoke test is run on the server the creds are securely provided in OpenShift from the secrets vault and inserted via token replacement. 

But on your local system you still need to replace the tokens. These json files serve that purpose.

cypress.dev.env.json

{
  "webapp.url": "https://dev-unity.apps.silver.devops.gov.bc.ca/",
  "reporting.url": "{{reporting.url}}",
  "test1username": REQUIRED
  "test1password": REQUIRED
  "test2username": "{{test2username}}",
  "test2password": "{{test2password}}",
  "test3username": "{{test3username}}",
  "test3password": "{{test3password}}",
  "environment": "DEV"
}

cypress.dev2.env.json

{
  "webapp.url": "https://dev2-unity.apps.silver.devops.gov.bc.ca/",,
  "reporting.url": "{{reporting.url}}",
  "test1username": REQUIRED
  "test1password": REQUIRED
  "test2username": "{{test2username}}",
  "test2password": "{{test2password}}",
  "test3username": "{{test3username}}",
  "test3password": "{{test3password}}",
  "environment": "DEV2"
}

cypress.test.env.json

{
  "webapp.url": "https://test-unity.apps.silver.devops.gov.bc.ca/",
  "reporting.url": "{{reporting.url}}",
  "test1username": REQUIRED
  "test1password": REQUIRED
  "test2username": "{{test2username}}",
  "test2password": "{{test2password}}",
  "test3username": "{{test3username}}",
  "test3password": "{{test3password}}",
  "environment": "TEST"
}

cypress.uat.env.json

{
  "webapp.url": "https://uat-unity.apps.silver.devops.gov.bc.ca/",
  "reporting.url": "{{reporting.url}}",
  "test1username": REQUIRED
  "test1password": REQUIRED
  "test2username": "{{test2username}}",
  "test2password": "{{test2password}}",
  "test3username": "{{test3username}}",
  "test3password": "{{test3password}}",
  "environment": "UAT"
}

cypress.prod.env.json

{
  "webapp.url": "https://prod-unity.apps.silver.devops.gov.bc.ca/",
  "reporting.url": "{{reporting.url}}",
  "test1username": REQUIRED
  "test1password": REQUIRED
  "test2username": "{{test2username}}",
  "test2password": "{{test2password}}",
  "test3username": "{{test3username}}",
  "test3password": "{{test3password}}",
  "environment": "PROD"
}

The batch script will overwrite (create/update) this file each run:

cypress.env.json

The contents of this will be identical to one of the above DEV/DEV2/TEST/UAT/PROD depending on whatever was launched last.

Prerequisites on your machine

Windows 11

PowerShell available (built-in)

Node.js installed and available on PATH (node, npx)

Cypress dependencies installed in the project (npm ci or npm install in the repo)

How to run

Double-click the .bat file.

Pick an Environment (DEV, DEV2, TEST, UAT, PROD) from the select list.

Pick a Mode from the select list:

GUI: launches npx cypress open

Headless: launches npx cypress run

Click Launch Cypress.

What it does

When you click Launch Cypress:

Converts the chosen environment to lowercase (DEV → dev, etc.).

Changes to the configured project directory.

Copies the selected env file into cypress.env.json:

Copy-Item .\cypress.<env>.env.json .\cypress.env.json -Force

Launches Cypress:

Headless: npx cypress run in a new PowerShell window

GUI: npx cypress open

HOW THE BAT FILE WORKS: (using relative paths)

The launcher now uses relative paths and automatically detects the project directory:

The batch file changes to its own directory using:

%~dp0 (changes to the directory where the batch file is located)

The $projectPath variable gets the current location:

$projectPath = (Get-Location).Path;

The GUI mode Start-Process command uses the dynamic path:

cd '$($projectPath)'; npx cypress open

This makes the script portable - no manual path changes required.

Notes / gotchas

The script uses -ExecutionPolicy Bypass.

It uses Start-Process powershell ... -NoExit so the window stays open for logs.

It always overwrites cypress.env.json. If you keep custom local settings in that file, they will be replaced.

The bat file is located in the AutoUI folder and can be run directly from there. No need to move it to your desktop since it now uses relative paths.