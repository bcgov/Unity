@echo off
REM Ensure a valid working directory - change to the directory where this batch file is located
cd /d "%~dp0"

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -Command ^
    $ErrorActionPreference = 'Stop'; ^
    $ProgressPreference = 'SilentlyContinue'; ^
    Add-Type -AssemblyName System.Windows.Forms; ^
    Add-Type -AssemblyName System.Drawing; ^
    $projectPath = (Get-Location).Path; ^
    $padding = 20; ^
    $controlWidth = 300; ^
    $controlHeight = 25; ^
    $buttonHeight = 35; ^
    $verticalSpacing = 15; ^
    $formWidth = ($padding * 2) + $controlWidth + 50; ^
    $formHeight = ($padding * 2) + ($controlHeight * 4) + $buttonHeight + ($verticalSpacing * 3) + 50; ^
    $form = New-Object System.Windows.Forms.Form; ^
    $form.Text = 'Cypress Test Launcher'; ^
    $form.Size = New-Object System.Drawing.Size($formWidth, $formHeight); ^
    $form.StartPosition = 'CenterScreen'; ^
    $leftPosition = ($formWidth - $controlWidth) / 2; ^
    $label = New-Object System.Windows.Forms.Label; ^
    $label.Location = New-Object System.Drawing.Point($leftPosition, $padding); ^
    $label.Size = New-Object System.Drawing.Size($controlWidth, $controlHeight); ^
    $label.Text = 'Select Environment:'; ^
    $form.Controls.Add($label); ^
    $environmentDropdown = New-Object System.Windows.Forms.ComboBox; ^
    $environmentDropdown.Location = New-Object System.Drawing.Point($leftPosition, ($padding + $controlHeight)); ^
    $environmentDropdown.Size = New-Object System.Drawing.Size($controlWidth, $controlHeight); ^
    $environmentDropdown.Items.Add('Please select an environment'); ^
    $environmentDropdown.Items.Add('DEV'); ^
    $environmentDropdown.Items.Add('DEV2'); ^
    $environmentDropdown.Items.Add('TEST'); ^
    $environmentDropdown.Items.Add('UAT'); ^
    $environmentDropdown.Items.Add('PROD'); ^
    $environmentDropdown.SelectedIndex = 0; ^
    $form.Controls.Add($environmentDropdown); ^
    $modeLabel = New-Object System.Windows.Forms.Label; ^
    $modeLabel.Location = New-Object System.Drawing.Point($leftPosition, ($padding + ($controlHeight * 2) + $verticalSpacing)); ^
    $modeLabel.Size = New-Object System.Drawing.Size($controlWidth, $controlHeight); ^
    $modeLabel.Text = 'Select Mode:'; ^
    $form.Controls.Add($modeLabel); ^
    $modeDropdown = New-Object System.Windows.Forms.ComboBox; ^
    $modeDropdown.Location = New-Object System.Drawing.Point($leftPosition, ($padding + ($controlHeight * 3) + $verticalSpacing)); ^
    $modeDropdown.Size = New-Object System.Drawing.Size($controlWidth, $controlHeight); ^
    $modeDropdown.Items.Add('Please select a mode'); ^
    $modeDropdown.Items.Add('GUI'); ^
    $modeDropdown.Items.Add('Headless'); ^
    $modeDropdown.SelectedIndex = 0; ^
    $form.Controls.Add($modeDropdown); ^
    $button = New-Object System.Windows.Forms.Button; ^
    $button.Location = New-Object System.Drawing.Point($leftPosition, ($padding + ($controlHeight * 4) + ($verticalSpacing * 2))); ^
    $button.Size = New-Object System.Drawing.Size($controlWidth, $buttonHeight); ^
    $button.Text = 'Launch Cypress'; ^
    $button.Add_Click({ ^
        if ($environmentDropdown.SelectedIndex -eq 0) { ^
            [System.Windows.Forms.MessageBox]::Show('Please select an environment.', 'Error', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error); ^
            return; ^
        } ^
        if ($modeDropdown.SelectedIndex -eq 0) { ^
            [System.Windows.Forms.MessageBox]::Show('Please select a mode.', 'Error', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error); ^
            return; ^
        } ^
        $env = $environmentDropdown.SelectedItem.ToLower(); ^
        $mode = $modeDropdown.SelectedItem; ^
        if ($mode -eq 'Headless') { ^
            $cypressCommand = """Set-Location -Path '$projectPath'; Copy-Item -Path .\cypress.$env.env.json -Destination .\cypress.env.json -Force; npx cypress run"""; ^
            Start-Process powershell -ArgumentList '-NoExit', '-Command', $cypressCommand; ^
        } else { ^
            Set-Location -Path $projectPath; ^
            Copy-Item -Path .\cypress.$env.env.json -Destination .\cypress.env.json -Force; ^
            Start-Process powershell -ArgumentList '-ExecutionPolicy Bypass', '-NoExit', '-Command', ^
            "cd '$projectPath'; npx cypress open" -NoNewWindow; ^

        } ^
    }); ^
    $form.Controls.Add($button); ^
    $form.ShowDialog()
