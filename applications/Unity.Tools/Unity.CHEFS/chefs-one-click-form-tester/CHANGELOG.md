# Changelog

## 0.4.9 - build 2026.08.28.23

Corrective release based on submitted TEST Import run `45DE20`.

- Fixes page-bridge lookup when CHEFS exposes more than one live Form.io root and the first cached root does not own the rendered form.
- Starts discovery at the exact rendered OrgBook wrapper, walks its Vue/Form.io ownership graph, and searches every bounded live root.
- Ranks an exact wrapper-owning component ahead of same-key components from unrelated roots.
- Executes the existing CHEFS/Form.io `triggerUpdate` → `itemsLoaded` → `selectOptions` → `setValue` lifecycle on the resolved OrgBook instance.
- Adds PID-free root count, inspected component count, and wrapper-match diagnostics to OrgBook selection evidence.
- Adds an executable multiple-root fixture that first caches the wrong form and proves the live OrgBook wrapper still resolves.

## 0.4.8 - build 2026.08.28.22

- Replaces OrgBook's primary synthetic Choices interaction with CHEFS's actual Form.io Select runtime lifecycle.
- Calls `triggerUpdate('wonderful', true)` on the matching OrgBook instance and awaits `itemsLoaded`.
- Requires `WONDERFUL FLOORING` to exist in Form.io `selectOptions` before selection.
- Calls `setValue('WONDERFUL FLOORING')`, triggers change processing and retains the sustained rendered-state check.
- Keeps synthetic Choices interaction only as a fallback.
- Validates the component-configured HTTPS OrgBook endpoint before invoking the lifecycle.
- Strengthens `FIELD-03` from CHEFS commit `f3f8731` and pinned Form.io 4.17.4 source evidence.

## 0.4.7 - build 2026.08.28.21

- Replaces the generic OrgBook query with the exact evidenced returned value `WONDERFUL FLOORING`.
- Presses Enter directly after exact-value entry rather than requiring an enumerable result row or ArrowDown navigation.
- Requires the restricted Form.io fallback to match and apply that same exact returned value.
- Retains endpoint restriction, placeholder exclusion, PID-free diagnostics and sustained persistence verification.
- Strengthens `FIELD-03` for the live v0.4.6 **No choices to choose from** state.

## 0.4.6 - build 2026.08.28.20

Corrective release based on submitted TEST Import run `010397` and the supplied OrgBook network response.

- Confirms character-by-character input reaches the configured OrgBook endpoint and returns valid results.
- Attempts ArrowDown/Enter selection even when the content-script DOM cannot enumerate the custom Choices result nodes.
- Adds a page-context fallback restricted to the component-configured HTTPS `orgbook.gov.bc.ca/api/v3/search/autocomplete` endpoint.
- Applies the first returned string value through the matching Form.io OrgBook instance when DOM and keyboard selection cannot persist.
- Records only result count, selection method and persistence duration—not the selected business name.
- Retains the placeholder exclusions and sustained selected-state requirement.
- Strengthens `FIELD-03` coverage for the exact opaque-result-list failure in `010397`.

## 0.4.5 - build 2026.08.28.19

Corrective release based on submitted TEST Import run `C30A88` and the supplied live OrgBook placeholder DOM.

- Opens the OrgBook Choices control with a normal click.
- Clears the cloned search input and types the demonstrated `wonderful` query character by character with keyboard and input events.
- Waits for non-placeholder remote results before choosing a result.
- Uses the same human-equivalent input path for a selection retry.
- Stops a remote no-result path before it can fall through to the hidden native select.
- Excludes placeholder-like native options even when they have a non-empty value.
- Retains the continuous 1.2-second selected-state persistence requirement.
- Strengthens `FIELD-03` coverage for the exact 56-character placeholder false positive from `C30A88`.

## 0.4.4 - build 2026.08.28.18

Corrective release based on submitted TEST Import run `F55F2F` and the demonstrated OrgBook result list.

- Replaces the unproven OrgBook query with the demonstrated `wonderful` query.
- Selects the returned OrgBook item with a normal click instead of a synthetic multi-event pointer sequence.
- Retains an ArrowDown/Enter retry against freshly returned results.
- Requires selected Choices state to remain continuously present for 1.2 seconds before reporting success.
- Allows later scans to retry when a transient OrgBook selection disappears.
- Records explicit remote-selection success/failure and the persistence window without recording the selected business name.
- Strengthens `FIELD-03` regression coverage for the exact false-positive observed in `F55F2F`.

## 0.4.3 - build 2026.08.28.17

Corrective release based on the stalled upload and unresponsive Stop Run shown in active run `01FEF0`.

- Treats a filename row as pending while **Starting upload**, another pending message, or an incomplete visible progress indicator remains.
- Uses the rendered file-drop path before the potentially blocking Form.io component upload API.
- Applies one cumulative 20-second pending-upload deadline across passes.
- Does not redispatch a timed-out pending upload through a second API path.
- Records the timeout and releases the fill loop so later map and Edit Grid components can still be processed.
- Rejects every pending page-bridge request when Stop Run is selected.
- Makes upload polling and field handling propagate cancellation immediately.
- Starts idempotent stopped-run finalization without waiting for the current field timeout.
- Routes popup cancellation through a background fallback that finalizes an orphaned run when its page controller is unavailable after refresh or extension reload.
- Requires popup stop acknowledgement and refreshes until the run leaves active state.
- Adds `UPLOAD-01` and `STOP-01` regression coverage.

## 0.4.2 - build 2026.08.28.16

Corrective release based on antagonistic TEST run `BE27D7` and its attached form schema.

- Treats OrgBook placeholder text as empty and performs a remote search before selecting a returned item.
- Searches the BC Geocoder-backed Simple BC Address component and selects a returned address instead of leaving query text behind.
- Requires map components to contain a selected feature or marker; search text alone is not accepted as completion.
- Retains the existing CHEFS file-upload strategy for optional `simplefile` components.
- Commits open Edit Grid rows only after their reachable nested fields have been processed.
- Keeps an Edit Grid eligible for later row additions while its current row editor is open.
- Excludes row Save, Cancel, Remove, Delete, Edit and close controls from real form-submit discovery.
- Removes generic HTML `type="submit"` as sufficient proof of a CHEFS submit landmark; the Form.io submit wrapper or `data[submit]` identity is required.
- Adds `FIELD-03` and `GRID-02` regression coverage for the exact failure modes in `BE27D7`.

## 0.4.1 - build 2026.08.28.15

Corrective release based on blocked UAT run `AC1EC1`.

- Extracts unambiguous numeric minimums and maximums from rendered ranges such as `$0 to $5,000` when custom components omit usable Form.io metadata.
- Recovers numeric bounds from rendered validation messages such as `cannot be greater than 5000`.
- Keeps generated currency values inside the detected range instead of repeating an invalid default after validation repair.
- Extracts calendar limits from rendered month-name ranges and `on or after` / `no later than` guidance.
- Generates the evidenced project start and end dates inside April 1, 2027 through March 31, 2028.
- Adds Flatpickr and rendered readonly-control date paths before retaining the Form.io bridge fallback.
- Adds PID-free `DATE_VALUE_APPLIED` diagnostics with method and date bounds.
- Adds `FIELD-02` regression coverage for the exact currency and date constraints from run `AC1EC1`.

## 0.4.0 - build 2026.07.23.14

Dashboard release and round-009 readiness correction.

- Adds an internal results dashboard that fits its default layout into a widescreen viewport without document scrolling.
- Adds disabled-by-default automatic dashboard opening after singleton completion or once after the final batch item.
- Reuses, refreshes and focuses an existing dashboard tab instead of creating duplicates.
- Adds manual dashboard actions to the popup and Settings.
- Projects raw run data through a strict PID-free schema containing only opaque references, enumerated categories, timestamps and bounded numeric aggregates.
- Excludes field values, labels, names, emails, files, screenshots, raw URLs, confirmation IDs, arbitrary failure text, raw events, stack traces and browser identity.
- Adds Simple, Analyst, Statistical and Experimental chart groups.
- Gives advanced charts explicit comparable-history thresholds and unavailable reasons.
- Adds separately controlled aggregate history, disabled by default and bounded to 200 records / 90 days, with clear actions in Settings and the dashboard.
- Clears/withholds retained history when retention is disabled during completed-run processing.
- Rejects unexpected fields in stored dashboard summaries before they reach the page.
- Hashes run and suite references so user-provided identifiers are not echoed.
- Strengthens batch readiness to require mounted Form.io components and interactive controls across stable samples, rather than accepting an empty outer shell.
- Adds `DASH-01` through `DASH-05` automated regression coverage and a round-009 empty-shell readiness fixture.

## 0.3.1 - build 2026.07.23.13

Corrective release based on round-008 managed-Chrome batch evidence.

- Waits for a 1.5-second quiet period after the latest marked tab arrives before starting the suite.
- Activates the selected tab and verifies that its Form.io form is mounted before creating a run.
- Reports **Preparing marked tabs** and **waiting for CHEFS form** in the popup.
- Bounds a never-ready form at 45 seconds and advances without creating a misleading empty run.
- Stops applying the 90-second launcher timeout after a run record exists.
- Preserves strict sequential ownership for long-running forms such as REDIP.
- Probes a stale-looking content controller for live status before watchdog finalization.
- Keeps the unresponsive-controller stall fallback intact.
- Makes **Settings** and **Stop Batch** span the full popup width.
- Adds `BATCH-05`, `BATCH-06`, and `UX-02` regression coverage.

## 0.3.0 - build 2026.07.23.12

- Adds a project-root `run-regression-suite.cmd` with the editable eight-form regression list evidenced by feedback round 004.
- Opens marked tabs without a hard-coded extension ID; tabs still open safely when the extension is absent.
- Adds a disabled-by-default **Batch regression launcher** settings section.
- Requires a per-install token, an exact configured origin, explicit Chrome host access and the existing environment check before queueing a marked tab.
- Removes the launcher token and suite marker from browser history before injecting the tester and stores only the cleaned form URL in queue records.
- Keeps production-like hosts subject to the existing explicit production policy.
- Persists a sequential queue in extension storage and activates the test tab before starting each run.
- Waits for terminal evidence and the automatic export attempt before advancing to the next form.
- Recovers queued work after a Manifest V3 service-worker restart.
- Resolves an automatic export interrupted by worker restart as a reported export failure before advancing, avoiding a wedged or silently skipped pending state.
- Adds popup active, queued and completed counts and a **Stop Batch** action.
- Bounds completed history and records rejected, closed, failed-to-start and timed-out items without wedging the queue.
- Adds `BATCH-01` through `BATCH-04` regressions for the launcher, security gates, persistence, sequencing, export ordering and stop/recovery behaviour.

## 0.2.6 - build 2026.07.23.11

- Adds **Select** beside **Export Folder** as a convenience for feeding the existing Downloads-relative setting.
- Starts the folder picker in Downloads and derives a candidate from the selected folder name.
- Applies the existing relative-path validator before attempting location validation.
- Downloads a uniquely named temporary probe through the unchanged Chrome downloads path.
- Confirms the probe is visible through the selected directory handle before populating Export Folder.
- Removes the probe file and download-history entry after successful or rejected location validation.
- Accepts direct child folders of Downloads, including operating-system links when the browser exposes them as the selected destination.
- Rejects arbitrary or nested selections that cannot be proven to map to the derived Downloads-relative name.
- Leaves the previous field value unchanged after cancellation, unsafe selection, arbitrary location, policy failure or validation failure.
- Stores no directory handle and does not change manual export, automatic export, blank-folder or typed relative-path behaviour.
- Adds `EXPORT-04` regression coverage for valid selection, picker start location, probe routing, cleanup, arbitrary-location rejection, unsafe-name rejection, policy failure and cancellation.

## 0.2.5 - build 2026.07.23.10

Corrective release based on round-005 Settings review.

- Renames the setting to **Export Folder** and makes its default blank for portable multi-user installation.
- Removes machine-specific folder names and development-environment guidance from product defaults and operating instructions.
- Treats blank as the normal configuration and routes filenames directly to the browser's Downloads folder.
- Retains optional validated Downloads-relative folders for users who want subfolder organization.
- Migrates the v0.2.4 automatic-export preference while discarding its non-portable legacy folder value.
- Renames the checkbox to **Automatically export after each run**.
- Automatically exports finalized `submitted`, `completed`, `failed`, `stalled`, `blocked`, `safety_stop`, and `stopped` runs when enabled.
- Persists final snapshots and terminal checkpoints before failure and stopped-run exports.
- Waits for attempted failure screenshot capture before signalling automatic export.
- Finalizes background-watchdog stalls with a final snapshot and checkpoint before export.
- Extends regressions across blank defaults, blank-path routing, neutral UI, every terminal outcome, representative success and failure ZIP contents, screenshot ordering, idempotency, and export-failure isolation.

## 0.2.4 - build 2026.07.23.9

- Adds an Export settings section with a validated Downloads-relative folder preference.
- Introduced a machine-specific initial folder that was rejected in round-005 and removed by v0.2.5.
- Routes `Export Last Run` through the preferred folder and retains the browser Save As confirmation.
- Adds `Automatically export after submitting`, disabled by default.
- Creates one prompt-free download request after a confirmed successful submission when automatic export is enabled.
- Waits for the final component snapshot and success checkpoint before initiating automatic export.
- Persists pending, successful, or failed automatic-export state without changing the submission result.
- Prevents repeated run-finalization messages from creating duplicate automatic downloads.
- Rejects absolute, drive-qualified, traversal, malformed, and Windows-invalid folder values.
- Added the initial `EXPORT-01`, `EXPORT-02`, and `EXPORT-03` regressions, later broadened by v0.2.5.

## 0.2.3 - build 2026.07.22.8

- Replaces the rejected `chefs.invalid` email domain with `cedarridgecommunity.ca`.
- Aligns generated email addresses with the fictional Cedar Ridge Community Association identity.
- Keeps CHEFS product identity separate from synthetic applicant identity.
- Extends the generated-email regression to reject product branding and blunt fakery markers.

## 0.2.2 - build 2026.07.22.7

- Replaces the generated `example.ca` email domain with the reserved, non-routable `chefs.invalid` domain.
- Preserves role-based local parts and per-run traceability.
- Adds a project-owned regression that executes the shipped `emailValue` method across representative field contexts.

## 0.2.1 - build 2026.07.22.6

Corrective release based on regression run `A9D2A6`.

- Prevents Form.io data-grid and edit-grid add-row controls from being indexed as submit landmarks, even when the rendered button uses `type="submit"`.
- Excludes generic add-row labels such as `Add Another`, `Add Row`, `Add Item`, and `New Row` from submit-button detection.
- Excludes lookup/search action controls from submit-button detection.
- Keeps the configured grid target at two total rows and prevents the submission stage from opening an unintended third row.
- Retains v0.2.0 custom mask rules, JSON import/export, automatic mask detection, and rule provenance.

## 0.2.0 - build 2026.07.22.5

- Detects input masks from live Form.io component metadata and runtime Inputmask configuration.
- Generates minimally conforming values for standard CHEFS `9`, `a` and `*` mask tokens while preserving literal punctuation and spaces.
- Adds a Custom field formats table to Settings with enabled state, normalized label phrase, CHEFS mask and row removal.
- Gives matching user rules precedence over detected form masks on the first attempt.
- Falls back to a conflicting form-defined mask on a later attempt when the custom value is rejected.
- Adds schema-versioned JSON Import Rules and Export Rules actions with Merge and Replace import modes.
- Validates imported JSON, rule IDs, label phrases, duplicate rules and mask syntax before changing the settings table.
- Adds the active custom rule set and rule-set SHA-256 identity to each run record and troubleshooting bundle.
- Adds `custom-format-rules.json` to run exports.
- Adds mask and custom-rule provenance to component snapshots and `FILL_ATTEMPT` events.
- Adds `CUSTOM_RULE_SET_LOADED`, `CUSTOM_RULE_MATCHED`, `CUSTOM_RULE_VALUE_ACCEPTED`, `CUSTOM_RULE_VALUE_REJECTED`, `MASK_METADATA_DETECTED`, `MASK_RUNTIME_DETECTED`, `MASK_VALUE_GENERATED`, `MASK_VALUE_PERSISTED`, `MASK_VALUE_REJECTED` and `MASK_SYNTAX_UNSUPPORTED` diagnostics.
- Retains v0.1.9 submit-landmark and hidden-tab validation behaviour.

## 0.1.9 - build 2026.07.22.4

- Records Form.io submit controls as persistent landmarks while scanning each tab.
- Remembers the tab that contains each submit control, even after that tab becomes hidden.
- Returns to the remembered submit tab when the final visited tab contains no submit control.
- Reacquires the live submit button after tab activation or Form.io redraw.
- Runs repairable Form.io validation errors before requiring a visible submit button.
- Recognizes rendered single-selection guidance such as `Please select only one` for checkbox-based choice groups.
- Prevents layout containers that inherit a child key from being treated as validation-repair fields.
- Adds submit-landmark and submit-tab diagnostic events.

## 0.1.8 - build 2026.07.22.3

Corrective release based on REDIP run `6FC991`.

- Reads checkbox-group selection limits from live Form.io validation metadata when available.
- Falls back to rendered guidance such as `Maximum 2 partners` or `You can only select up to 2 items` when schema metadata is unavailable.
- Selects only the permitted number of checkbox options and unchecks excess selections during repair.
- Preserves the fill-all behaviour for checkbox groups that do not define a maximum, while avoiding contradictory `None` or `Not applicable` choices when normal options exist.
- Corrects text-length resolution so the browser's default input `maxLength` value no longer overrides a CHEFS/Form.io validation limit.
- Reads minimum and maximum character and word limits from Form.io metadata, rendered guidance and live character counters.
- Trims generated text before dispatching it to the control.
- Collects component-level validation errors from hidden tabs and associates them with the actual CHEFS property key.
- Resets rejected fields for another fill attempt and reopens the tab containing each invalid component.
- Adds `CHECKBOX_SELECTION_LIMIT_DETECTED`, `CHECKBOX_SELECTION_REPAIRED`, `VALIDATION_REPAIR_PREPARED`, and validation-repair tab diagnostics.
- Adds resolved constraints to each `FILL_ATTEMPT` event and to component snapshots.

## 0.1.6 - build 2026.07.22.1

Corrective build based on run `3C0FD3` against **REDIP - Economic Capacity (UAT)**.

- Normalizes Form.io tab wrappers to their interactive anchor or button before clicking.
- Verifies that the requested tab became active before recording `TAB_ACTIVATED`.
- Records failed tab activation attempts and skips a tab only after two verified failures.
- Prevents false-positive tab visits caused by clicking a non-interactive `<li role="tab">` wrapper.
- Defers Next-button wizard navigation until direct traversal of the visible tab set is complete.
- Adds tab-set and activation diagnostics.
- Replaces the fixed 40-pass ceiling with a tab-aware starting budget.
- Extends the pass budget when the boundary pass made progress or revealed another conditional field.
- Retains a 200-pass hard ceiling and the existing no-progress watchdog.

## 0.1.5 - build 2026.07.21.6

Corrective build based on run `5712D1` against **Template - Custom Fields**.

- Treats the configured grid value as a target total row count rather than a number of rows to add only when the grid is empty.
- Uses a default target of two rows for every reachable data grid or edit grid.
- Counts existing rows, respects detected row limits, and adds only the missing rows.
- Finds add-row controls by Form.io class or `ref` suffix before using text as a fallback.
- Reacquires the live grid wrapper after each row addition and waits for the rendered row count to increase.
- Rescans newly created rows so every field in every row is filled.
- Adds grid diagnostics for inspection, add attempts, success, unavailable targets and target completion.
- Adds grid row count and target row count to component snapshots.
- Adds a dedicated simple-day adapter that fills month, day and year together.
- A simple-day component is no longer counted as filled when only the month has a value.
- Changes the grid setting to a target of two to five rows and migrates earlier stored values below two.

## 0.1.4 - build 2026.07.21.5

- Detects the standard CHEFS success route and success-page wording.
- Captures the confirmation ID and stops all further work after successful submission.
- Prevents broad page-level alert containers from being treated as validation errors.

## 0.1.3 - build 2026.07.21.4

- Treats count questions as counts before considering nearby funding language.
- Yields between input, change and blur events.
- Adds control-event diagnostics and an independent stale-run watchdog.

## 0.1.2 - build 2026.07.21.3

- Reacquires live Form.io file wrappers after CHEFS redraws file components.
- Detects rendered upload rows without polling detached wrappers.
- Adds stronger Choices.js interaction fallbacks.

## 0.1.1 - build 2026.07.21.2

- Corrects phone, address, distinct-email, confirmation-checkbox and file-upload handling.
- Improves wrapper-key detection and component metadata logging.

## 0.1.0 - build 2026.07.21.1

Initial complete one-button fill-and-submit implementation.
