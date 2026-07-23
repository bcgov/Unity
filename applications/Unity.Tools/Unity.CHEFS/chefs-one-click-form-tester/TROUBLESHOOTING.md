# Troubleshooting

## Batch tabs open but no run starts

The project batch file must launch the Chrome profile that contains this unpacked extension. Confirm the profile folder in `chrome://version`, then set `CHROME_PROFILE` in `run-regression-suite.cmd`.

Open extension **Settings** and confirm the batch launcher is enabled, the saved launcher token exactly matches `LAUNCHER_TOKEN`, the tab's exact origin is listed and **Grant Host Access** was approved. Existing non-production checks still apply. An invalid token is deliberately ignored; an origin or safety rejection appears in batch completed history.

The popup briefly reports **Preparing marked tabs** while the batch file finishes opening its list. It then reports **waiting for CHEFS form** if the selected tab has only an outer shell or is still mounting Form.io components and controls. A form that does not become ready within 45 seconds is recorded as `form_not_ready`, and the queue advances.

Once a run begins, it owns the queue slot for its full execution; a long form is not released by the launcher-start timeout. **Stop Batch** requests a stop for the active run and removes queued work without closing the tabs. See the project-root `BATCH-REGRESSION.md` for full setup.

## The results dashboard did not open

Automatic opening is disabled by default. Open **Settings**, enable **Open results dashboard after completion**, and save. A singleton opens after terminal processing. A batch opens only after the final queued item, so no dashboard should appear between batch runs.

Use **Results Dashboard** in the popup or **Open Dashboard** in Settings to open it manually. An already-open dashboard tab is refreshed and focused rather than duplicated.

## An advanced dashboard chart is unavailable

The dashboard intentionally disables a chart when the current PID-free aggregates cannot support it. Use the reason shown beside the unavailable choice. Bell curves require 20 retained comparable runs; control charts require eight; candlesticks require at least eight comparable runs across two days.

Aggregate history is disabled by default. Enable **Retain PID-free aggregate history for advanced charts** in Settings to accumulate eligible summaries. It retains at most 200 records and 90 days. Clear it from Settings or the dashboard at any time.

The dashboard never receives troubleshooting-bundle detail such as field labels or values. Use **Export Last Run** when detailed diagnosis is needed.

## Select rejects an Export Folder

Select accepts a folder only when the browser can prove that it is directly inside Downloads. It creates a uniquely named temporary download, verifies that the file appears through the selected folder and removes the file and download-history entry.

Select a direct child of Downloads. Clear **Export Folder** to use Downloads itself. For a validated nested relative path, type the path in the field instead. Browser settings or workplace policy that interrupt the validation download can prevent Select from completing; the prior field value remains unchanged.

## An export does not arrive in the preferred folder

Open **Settings** and confirm **Export Folder** is blank when exports should go directly to the browser's Downloads folder. For an optional subfolder, enter only a relative name such as `CHEFS Exports`. Do not include `Downloads\`, a drive letter or an absolute path.

When an operating-system link routes the relative folder elsewhere, inspect that link separately and confirm its target still exists. A missing or broken link must be repaired outside the extension. Clearing **Export Folder** sends later exports to Downloads directly.

## Automatic export did not start

Automatic export runs after a success, failure, stall, block, safety stop or user stop once the extension has persisted the available terminal evidence. Confirm **Automatically export after each run** is enabled in **Settings**. Browser preferences or managed workplace policy can still display a confirmation window or reject the destination.

Reopen the extension popup on the form tab. A failed automatic export is reported without changing the underlying run result. Use **Export Last Run** to retry manually.

## A bureaucratic identifier needs a special format

Current builds inspect the live Form.io schema and runtime Inputmask configuration before generating ordinary text. Standard masks such as `aaa-999999`, `(999) 999-9999` and `*****************` are handled automatically.

When the form does not expose the necessary format, open **Settings**, add a Custom field format rule, enter a distinctive phrase from the visible field label and enter the CHEFS input mask. Label matching is case-insensitive, removes label markup and required markers, normalizes punctuation and uses phrase containment by default.

The exported run includes `custom-format-rules.json`. Component snapshots record `maskSource`, `resolvedMask`, `customRule` and `maskGenerationStrategy`. Use the mask and custom-rule events in `events.jsonl` to determine whether the value came from a user rule, Form.io metadata or runtime Inputmask settings.

If a custom rule conflicts with a detected form mask, attempt one uses the custom rule and a later attempt can use the form-defined mask. `CUSTOM_RULE_VALUE_REJECTED` records the detected fallback mask.

Optional mask groups, alternation, quantifiers and custom mask definitions are not guessed. The run records `MASK_SYNTAX_UNSUPPORTED` and continues through the normal retry and validation-repair process.

## Importing or sharing custom field format rules

Use **Export Rules** to create schema-versioned JSON. Use **Import Rules** with Merge to retain local rules or Replace to replace the current table. The importer rejects malformed JSON, unsupported schema versions, missing phrases, masks without standard tokens, duplicate IDs and duplicate phrase-and-mask rules. Imported changes are not persistent until **Save Settings** is selected.

## Submit exists on an earlier tab

v0.1.9 records every Form.io submit control as a submit landmark, including the containing tab. If the fill loop finishes on a later information-only or resources-only tab, the extension activates the remembered submit tab and reacquires the live button before submission. Relevant events are `SUBMIT_LANDMARK_DISCOVERED`, `SUBMIT_LANDMARK_BECAME_VISIBLE`, `SUBMIT_TAB_ACTIVATION_ATTEMPT`, `SUBMIT_TAB_ACTIVATED`, and `SUBMIT_LANDMARK_REACQUIRED`.

## Checkbox group says select only one

When Form.io renders a checkbox-based choice group with an error such as `Please select only one`, v0.1.9 resolves the maximum selection count as one, unchecks the excess selections, and retries validation before searching for Submit.

## A checkbox group says too many items were selected

v0.1.8 reads Form.io `minSelectedCount` and `maxSelectedCount` validation metadata. When that metadata is unavailable, it reads rendered instructions such as `Maximum 2 partners` and `You can only select up to 2 items`. The extension unchecks excess options and records `CHECKBOX_SELECTION_LIMIT_DETECTED` and `CHECKBOX_SELECTION_REPAIRED`.

## A text field contains a value but CHEFS says it is required or over length

Current builds resolve minimum and maximum character and word limits from Form.io metadata, HTML attributes, rendered guidance and live character counters. Each `FILL_ATTEMPT` event records `resolvedConstraints`. Submission repair records the component key, reopens its hidden tab when necessary, and retries it with a value inside the detected limit.

## File appears in CHEFS but the run reports an upload failure

v0.1.7 added exact wrapper tracking for Form.io file controls whose default property key is also `simplefile`. It recognizes list and table file rows and remembers in-flight uploads so a slow upload is monitored instead of repeated. Relevant events are `UPLOAD_PENDING_RECHECK`, `UPLOAD_STILL_PENDING`, `UPLOAD_PENDING_TIMEOUT`, `UPLOAD_WRAPPER_REPLACED`, and `UPLOAD_COMPLETED`.

## A run stops or stalls

Open the extension popup on the affected tab and select **Export Last Run**. Do not refresh the form first when the partial form state may be useful.

Provide the exported ZIP together with a brief visible observation. The bundle records the build, last successful action, current action, unresolved fields, validation messages, component snapshots and recent event history.

## A horizontal tabbed form stops before the final tabs are complete

v0.1.6 clicks the interactive anchor or button inside a Form.io tab wrapper and verifies that the tab became active. Logs record `TAB_SET_DISCOVERED`, `TAB_ACTIVATION_ATTEMPT`, `TAB_ACTIVATED`, `TAB_ACTIVATION_FAILED` and `TAB_SKIPPED_AFTER_FAILURE`.

The pass budget scales with the number of tabs and can extend when a boundary pass reveals another conditional field. `FILL_PASS_BUDGET_SET` and `FILL_PASS_BUDGET_EXTENDED` show the applied limits.

## A data grid receives only one row

Current builds target two total rows by default. The run log records `GRID_INSPECTED`, `GRID_ROW_ADD_ATTEMPT`, `GRID_ROW_ADDED` and `GRID_TARGET_REACHED` events. Component snapshots also record the current and target row counts.

When no enabled add-row control exists, the run records `GRID_TARGET_UNAVAILABLE` rather than repeatedly clicking the grid.

## A Day component has only a month

Current builds use a dedicated simple-day adapter. It fills month, day and year and does not count the component as filled unless every enabled part has a value.

## CHEFS submitted, but the extension reports Blocked

This was corrected in v0.1.4. Current builds detect the success route and success-page wording, capture the confirmation ID and stop the run as submitted.

## A phone field appears partly masked

Export the run bundle. The phone adapter records the input mask and requires ten rendered digits before the field is counted as filled.

## A file appears in CHEFS but the extension still says Uploading

Export the run bundle. Current builds reacquire file wrappers after Form.io redraws and inspect the replacement wrapper for the uploaded row.

## The extension says the environment is blocked

Open **Settings** and add the exact approved hostname. Production-like CHEFS hosts remain blocked unless the explicit override is enabled.
