# Chrome Extension Permissions

This extension is a developer/testing tool for exercising CHEFS forms in approved non-production environments. It is not intended for general browsing use.

## Required permissions

### `activeTab`

Allows the extension to interact with the currently selected CHEFS form tab after the user invokes the extension. This keeps normal operation scoped to the active tab instead of granting broad default site access.

### `scripting`

Allows the extension to inject its Form.io test controller into an approved CHEFS form tab. The injected scripts inspect rendered form controls, populate test values, attach packaged synthetic files and trigger submission.

### `storage`

Persists extension settings and run diagnostics, including environment safeguards, batch launcher configuration, run progress, troubleshooting checkpoints and dashboard state.

### `downloads`

Exports troubleshooting bundles and run evidence from the browser. This includes summaries, event logs, checkpoints, component snapshots and optional failure screenshots.

### `unlimitedStorage`

Prevents Chrome from evicting longer diagnostic runs or larger troubleshooting bundles while a test is in progress. The extension still stores bounded batch/dashboard history and exposes clearing controls.

### `alarms`

Runs background watchdog and batch-queue checks while the Manifest V3 service worker is idle or restarted. This lets the extension detect stalled runs and advance queued regression tabs reliably.

### `tabs`

Supports batch regression orchestration across marked CHEFS form tabs. The extension needs tab metadata to detect launcher markers, activate the next queued tab, scrub launcher markers from tab history and handle closed tabs.

## Optional host permissions

### `http://*/*` and `https://*/*`

Host access is optional and must be granted by the user for approved CHEFS origins before automation is injected. The extension does not declare default `host_permissions`.

Batch launching requires all of the following before a marked tab is processed:

- Batch launcher enabled in settings.
- Matching launcher token.
- Exact approved origin configured in settings.
- Chrome host access granted for that origin.
- Existing environment protection passes.

Production-like CHEFS hosts remain blocked by default. Adding an origin to the batch list does not bypass the separate production override requirement.

## Web-accessible resources

### `page-bridge.js`

Exposed so the extension can bridge into the page context when Form.io state must be read from the rendered CHEFS page.

### `attachments/*`

Exposed so the test runner can upload packaged synthetic attachment files through normal form file inputs.

## Network access

The extension does not load remote extension assets. Its dashboard uses a PID-free projection of run results and does not fetch external resources.
