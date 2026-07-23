# CHEFS One-Click Form Tester

Version: 0.4.0  
Build: 2026.07.23.14  
Browser: Google Chrome, Manifest V3

## Install or update

1. Extract the release ZIP to a permanent folder.
2. Open `chrome://extensions` in Chrome.
3. Turn on **Developer mode**.
4. Remove the previous unpacked build, or select **Load unpacked** for the new folder.
5. Select the folder containing `manifest.json`.
6. Pin **CHEFS One-Click Form Tester** to the Chrome toolbar.

## Run

1. Open a fresh CHEFS form in an approved TEST, UAT or DEV environment.
2. Select the extension icon.
3. Select **Fill and Submit**.
4. Leave the form tab open while the run is active.
5. After the run ends, select **Export Last Run**, unless automatic export is enabled.

The engine repeatedly scans the rendered form, fills every reachable user-facing field, waits for conditional changes, creates repeating rows, uploads packaged synthetic attachments and submits after the current path stabilizes.

## v0.4.0 results dashboard and readiness correction

Open **Settings** to enable **Open results dashboard after completion**. The setting is disabled by default. A singleton opens the dashboard after its terminal processing; a batch opens it once after the final queued run. If a dashboard tab already exists, the extension refreshes and focuses it instead of opening a duplicate. The popup and Settings also provide a manual **Results Dashboard** / **Open Dashboard** action.

The dashboard is a fixed widescreen extension page with four summary cards, a primary chart and a plain-language findings panel. Its menu ranges from:

- **Simple:** outcome, field progress, batch duration and phase timing.
- **Analyst:** pass trends, strategy latency, component outcomes and strategy heatmaps.
- **Statistical:** duration bell curve, control chart, complexity scatter and percentiles.
- **Experimental:** duration candlesticks, pass activity density and build distributions.

Advanced choices never invent missing evidence. A chart that lacks enough comparable aggregate runs is marked unavailable and states its minimum-history requirement.

The page receives only a strict PID-free projection: enumerated result categories, opaque references, timestamps and bounded numeric aggregates. It receives no field values, labels, names, emails, uploaded filenames or content, screenshots, raw URLs, confirmation IDs, arbitrary failure text, raw events, stack traces or browser identity. It loads no network resources.

**Retain PID-free aggregate history for advanced charts** is a separate setting and is also disabled by default. When enabled, it retains at most 200 projected records and 90 days. History can be cleared from Settings or the dashboard. Turning retention off prevents retained history from being supplied to the page and clears it during the next completed-run update.

Batch readiness now rejects an empty `.formio-form` shell. A marked tab must expose mounted Form.io components and interactive controls across two samples before tester injection. The existing 45-second observable timeout remains.

## v0.3.1 batch startup and sequencing correction

Round 008 exposed two separate orchestration defects. The first marked tab started before its Form.io form mounted and remained at **Finding CHEFS form** for 92 seconds. Later, REDIP legitimately exceeded the launcher's 90-second startup window, so the queue incorrectly released its slot and started later forms concurrently.

v0.3.1 waits 1.5 seconds after the latest marked tab arrives before claiming index `001`, preventing the remaining batch-file tab launches from stealing focus. It then activates each selected tab and waits for the actual Form.io form root before creating a run. The popup reports **Preparing marked tabs** and **waiting for CHEFS form** instead of presenting the wait as an unexplained run initialization.

The launcher timeout now applies only before a run exists. An established run retains the sole active queue slot until terminal finalization and automatic-export completion. The watchdog asks a stale-looking content controller for live status before declaring a stall, allowing long advanced-select operations to remain active while keeping the unresponsive-controller fallback bounded.

The popup **Settings** and **Stop Batch** actions now span the full secondary-action width.

## v0.3.0 batch regression launcher

The project root contains `run-regression-suite.cmd`, an editable launcher preloaded with the eight-form regression suite evidenced by feedback round 004. It opens marked forms in a selected Chrome profile. When the batch launcher is configured, this extension activates and runs one marked tab at a time. It waits for the terminal run evidence and any automatic export attempt before advancing.

Batch launching is disabled by default. Open **Settings** and configure **Batch regression launcher**:

1. Generate a launcher token and copy it to `LAUNCHER_TOKEN` in the batch file.
2. Enter each exact approved regression origin.
3. Select **Grant Host Access** and approve Chrome's prompt.
4. Enable the launcher and save Settings.

The token, exact configured origin, Chrome host permission and existing environment protection must all pass. A production-like host is not made safe merely by adding it to the batch list. Before injecting the tester, the extension removes the launcher marker from browser history and retains only the cleaned form URL in queue records. The extension popup reports active, queued and completed items and provides **Stop Batch**.

The launcher contains no extension ID. If the extension is absent, disabled or not loaded in the chosen Chrome profile, the form tabs simply open.

## v0.2.6 Select Export Folder

**Select** is a convenience for populating the existing **Export Folder** setting. It starts in Downloads and supports folders directly inside Downloads. The extension derives the folder name, applies the existing relative-path validation and sends a uniquely named temporary file through the normal downloads API. The field is populated only when that probe appears in the folder that was selected.

The validation file and its download-history entry are removed automatically. Cancellation, an unsafe name, an arbitrary location, a nested location that cannot be represented, a browser-policy failure or a validation mismatch leaves the previous field value unchanged.

Select stores no folder handle and does not change export behaviour. Blank continues to mean Downloads directly, a selected folder remains a Downloads-relative name, and validated nested relative paths can still be typed manually.

## v0.2.5 export destination and automatic export

Open **Settings** and use **Export Folder** to select an optional Downloads-relative destination. The field is blank by default, which saves exports directly to the browser's normal Downloads folder. To organize exports, enter a relative folder such as `CHEFS Exports`. Do not enter `Downloads\CHEFS Exports`, a drive letter, or an absolute path.

**Export Last Run** retains the browser Save As confirmation and suggests the configured relative destination. Enable **Automatically export after each run** to request one download without an extension-requested confirmation window after the terminal evidence is stored. The browser or workplace policy can still require confirmation.

Automatic export applies to submitted, completed, failed, blocked, stalled, stopped and safety-stop runs. Failure details, attempted failure screenshot capture, the final component snapshot and the terminal checkpoint are persisted before the export begins. An automatic-export failure is reported separately and does not change the run result.

The extension rejects absolute paths, drive letters, parent traversal, repeated or trailing separators, Windows-reserved names and Windows-invalid filename characters. An operating-system link can route a configured Downloads subfolder elsewhere, but creating and maintaining such links remains outside the extension.

## v0.2.3 synthetic email identity

Generated email addresses use the fictional `cedarridgecommunity.ca` domain, matching the generated Cedar Ridge Community Association identity. They retain role and run identifiers for traceability without using product branding or blunt fakery markers.

## v0.2.0 custom field formats

v0.2.0 reads CHEFS/Form.io input masks before using label-based value guesses. Standard CHEFS mask tokens are generated automatically: `9` for numeric, `a` for alphabetic and `*` for alphanumeric. Literal punctuation and spaces are retained.

Open **Settings** to manage **Custom field formats** when a program-area identifier needs a format that is missing from the form or needs a tester-controlled override. Each rule contains a normalized visible-label phrase and a CHEFS input mask. User rules take precedence on the first attempt; when a conflicting form-defined mask exists, the detected form mask is available as the retry fallback.

Rules can be imported and exported as schema-versioned JSON. Import supports Merge and Replace modes. The active rule set, its SHA-256 identity, every rule match and every accepted or rejected masked value are recorded in the troubleshooting bundle.

A reference file is included at `examples/custom-format-rules.example.json`.

## v0.1.9 correction

v0.1.9 treats the submit control as a persistent form landmark. When a form places Submit on an attestation tab and then adds a later resources-only tab, the extension remembers where Submit was found, returns to that tab after the fill loop, reacquires the live control, and submits. Pre-submit Form.io validation errors are repaired before submit-button discovery, including checkbox groups that render guidance such as `Please select only one`.

## v0.1.8 correction

REDIP run `6FC991` reached the final tab with 108 fields filled and five attachments completed, but submission exposed two validation defects. A checkbox group configured for a maximum of two partners had every option selected, and a 100-character conditional text field received a 282-character generated response.

v0.1.8 resolves field constraints from the live Form.io component schema before using rendered guidance. Checkbox groups obey minimum and maximum selection counts, text values obey character and word limits, and submission validation can reopen hidden tabs and retry the rejected components.

## v0.1.6 correction

Run `3C0FD3` against **REDIP - Economic Capacity (UAT)** reached 92 filled fields, four completed attachments and no validation errors, but exhausted the fixed 40-pass limit immediately after a final conditional textarea became visible. The run also exposed that the tab detector was clicking the non-interactive `<li role="tab">` wrapper rather than its child tab link, so the first 14 tab activations were logged without changing the active pane.

v0.1.6 normalizes Form.io tab wrappers to their interactive anchor or button, verifies that the requested tab became active before recording success, and only falls back to Next-button navigation after direct tab traversal has been exhausted.

The fill-pass budget is now adaptive. Forms with many tabs receive a larger starting budget, and the budget extends in small increments when the final allowed pass made progress or revealed another field. A separate hard ceiling and the existing no-progress watchdog remain in place.

## Grid settings

Open **Settings** to choose a target of two to five rows per data grid. A form-defined maximum takes precedence when it can be detected.

## Troubleshooting bundle

Every run is persisted while it executes. A partial or completed run remains exportable and can include:

- `manifest.json`
- `run-summary.txt`
- `events.jsonl`
- `checkpoints.jsonl`
- component snapshots
- validation errors
- attachment records
- the active `custom-format-rules.json` rule set and SHA-256 identity
- failure details and screenshot when applicable

## Environment protection

Automatic submission is allowed by default only on CHEFS hosts that look like TEST, UAT or DEV environments, plus localhost. Other hosts are blocked. Additional exact hostnames and an explicit production override are available under **Settings**.
