@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem CHEFS One-Click Form Tester regression launcher
rem 1. Copy the launcher token from the extension Settings page below.
rem 2. Set CHROME_PROFILE to the profile folder that has the extension loaded.
rem 3. Add, remove, or reorder the OPEN_FORM lines in the embedded test list.

set "LAUNCHER_TOKEN=PASTE_TOKEN_FROM_EXTENSION_SETTINGS_HERE"
set "CHROME_PROFILE=Default"
set "SUITE_ID=regression-%RANDOM%-%RANDOM%"

if "%LAUNCHER_TOKEN%"=="PASTE_TOKEN_FROM_EXTENSION_SETTINGS_HERE" (
  echo ERROR: Set LAUNCHER_TOKEN in this file before running it.
  pause
  exit /b 2
)

set "CHROME_EXE=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if not exist "%CHROME_EXE%" set "CHROME_EXE=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if not exist "%CHROME_EXE%" set "CHROME_EXE=%LocalAppData%\Google\Chrome\Application\chrome.exe"
if not exist "%CHROME_EXE%" (
  echo ERROR: Google Chrome was not found in a standard installation location.
  pause
  exit /b 3
)

echo Opening CHEFS regression suite %SUITE_ID% in Chrome profile "%CHROME_PROFILE%"...

rem Evidence-derived regression suite from feedback round 004. Keep indexes unique and sortable.
rem 001 - CGG - Human and Social Services (TEST)
call :OPEN_FORM "001" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=8e1678c7-5f1e-4f9b-b9e4-87a81d0ecd7f"
rem 002 - CGG - DPAC - UAT
call :OPEN_FORM "002" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=13d98806-cf0a-4e96-a396-f98322220ca2"
rem 003 - Template - Simple Functional Chefs Form (TEST)
call :OPEN_FORM "003" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=6f3fe864-8942-4849-9396-0c343e24a72d"
rem 004 - Template - Custom Fields
call :OPEN_FORM "004" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=90d32c33-4932-4de3-adaa-ea3d5998059e"
rem 005 - Template - Core Fields
call :OPEN_FORM "005" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=8a1aae54-f534-4207-b3f7-e8c1f61c337e"
rem 006 - REDIP - Economic Capacity (UAT)
call :OPEN_FORM "006" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=34a28edb-251d-4f94-80ac-89c42e68e17c"
rem 007 - 2026 Community Event Support Fund
call :OPEN_FORM "007" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=55d5a529-3687-4726-8c09-3f8aa6ae2431"
rem 008 - A and C Rebate calculations
call :OPEN_FORM "008" "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form/submit?f=b73a4e19-d607-4c4e-be7c-6cac281b099f"

echo Opened all marked tabs. The extension will process them sequentially when its batch launcher is enabled.
exit /b 0

:OPEN_FORM
set "FORM_INDEX=%~1"
set "FORM_URL=%~2"
start "" "%CHROME_EXE%" --profile-directory="%CHROME_PROFILE%" --new-tab "%FORM_URL%#chefs-one-click-batch=%LAUNCHER_TOKEN%&suite=%SUITE_ID%&index=%FORM_INDEX%"
exit /b 0
