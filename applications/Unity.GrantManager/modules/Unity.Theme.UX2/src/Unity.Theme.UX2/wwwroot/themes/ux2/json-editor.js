/**
 * UnityJsonEditor - A reusable, extensible JSON editor component.
 *
 * Provides a Bootstrap modal with a monospace textarea for editing JSON data,
 * real-time validation, format/beautify, and file export/import capabilities.
 * Supports custom validators for domain-specific rules.
 *
 * @requires jQuery, Bootstrap 5
 *
 * @example
 *   // Basic usage
 *   const editor = new UnityJsonEditor({
 *       title: 'Edit Mapping',
 *       onSave: function(data) {
 *           console.log('Saved:', data);
 *       }
 *   });
 *   editor.open([{ key: 'value' }]);
 *
 * @example
 *   // With custom validators
 *   const editor = new UnityJsonEditor({
 *       title: 'Edit Column Mapping',
 *       requiredFields: ['propertyName', 'columnName'],
 *       validators: [
 *           {
 *               name: 'uniqueColumns',
 *               message: 'Column names must be unique',
 *               validate: function(data) {
 *                   const cols = data.map(r => r.columnName);
 *                   return new Set(cols).size === cols.length;
 *               }
 *           }
 *       ],
 *       onSave: function(data) { applyChanges(data); }
 *   });
 *
 * @example
 *   // File operations (standalone, no modal needed)
 *   UnityJsonEditor.exportToFile(myData, 'config.json');
 *   UnityJsonEditor.importFromFile({ accept: '.json' }).then(function(data) {
 *       console.log('Imported:', data);
 *   });
 */
const UnityJsonEditor = (function ($) {
    'use strict';

    let _instanceCount = 0;

    /**
     * Default configuration options.
     * @type {object}
     */
    const DEFAULTS = {
        /** Modal dialog title */
        title: 'Edit JSON',

        /** Bootstrap modal size class: 'modal-sm', 'modal-lg', 'modal-xl' */
        size: 'modal-lg',

        /** Number of visible rows in the textarea */
        rows: 18,

        /** Text for the save/apply button */
        saveButtonText: 'Apply Changes',

        /** Text for the cancel button */
        cancelButtonText: 'Cancel',

        /** Text for the format button */
        formatButtonText: 'Format JSON',

        /** Whether the modal should be read-only (disables editing and save) */
        readOnly: false,

        /**
         * Array of field names required on each item when the data is an array of objects.
         * Set to null or [] to skip required-field validation.
         * @type {string[]|null}
         */
        requiredFields: null,

        /**
         * Custom validators array. Each validator is an object with:
         *   - name {string}:       Identifier for the validator
         *   - message {string}:    Message shown on failure
         *   - severity {string}:   'error' (default) blocks save; 'warning' allows save
         *   - validate {function(data): boolean}: Returns true if valid
         *
         * Validators run after JSON syntax and required-field checks pass.
         * @type {Array<{name: string, message: string, severity?: string, validate: function}>}
         */
        validators: [],

        /**
         * Callback invoked when the user clicks Save/Apply and all validation passes.
         * Receives the parsed JSON data and an array of active warnings.
         * @type {function(data, warnings): void|null}
         */
        onSave: null,

        /**
         * Callback invoked when the user cancels or closes the modal without saving.
         * Not called when the modal is closed after a successful save.
         * @type {function(): void|null}
         */
        onCancel: null,

        /**
         * Callback invoked when validation fails with errors.
         * Receives an array of error objects: [{ validator: string, message: string }].
         * @type {function(errors): void|null}
         */
        onValidationError: null
    };

    // ========================================================================
    // Constructor
    // ========================================================================

    /**
     * Creates a new UnityJsonEditor instance.
     * @param {object} opts - Configuration options (merged with DEFAULTS).
     */
    function UnityJsonEditor(opts) {
        this._id = ++_instanceCount;
        this._opts = $.extend({}, DEFAULTS, opts);
        this._modalId = 'unityJsonEditorModal_' + this._id;
        this._modal = null;
        this._bsModal = null;
        this._textarea = null;
        this._statusBar = null;
        this._saveBtn = null;
        this._lastWarnings = [];
        this._savedFlag = false;
        this._built = false;
    }

    // ========================================================================
    // Static helpers (usable without an instance)
    // ========================================================================

    /**
     * Exports data as a downloadable JSON file.
     * @param {*} data - Data to serialize.
     * @param {string} [filename='export.json'] - Download filename.
     */
    UnityJsonEditor.exportToFile = function (data, filename) {
        filename = filename || 'export.json';
        const json = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 100);
    };

    /**
     * Opens a file picker and reads a JSON file.
     * @param {object} [opts] - Options.
     * @param {string} [opts.accept='.json'] - File input accept attribute.
     * @param {Array<{name:string,message:string,severity?:string,validate:function}>} [opts.validators] - Optional validators to run on imported data.
     * @param {function} [opts.onWarning] - Callback receiving an array of warning objects when warnings are present but no errors.
     * @returns {Promise<*>} Resolves with parsed JSON data, rejects on error.
     */
    UnityJsonEditor.importFromFile = function (opts) {
        opts = opts || {};
        const accept = opts.accept || '.json';
        const validators = opts.validators || [];
        const onWarning = opts.onWarning || null;

        return new Promise(function (resolve, reject) {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = accept;
            input.style.display = 'none';

            input.addEventListener('change', function () {
                // Remove from DOM now that the browser has fired the change event
                input.remove();

                const file = input.files?.[0];
                if (!file) {
                    reject(new Error('No file selected.'));
                    return;
                }
                _processImportedFile(file, validators, onWarning)
                    .then(resolve)
                    .catch(reject);
            });

            document.body.appendChild(input);
            input.click();
        });
    };

    // ========================================================================
    // Prototype (instance methods)
    // ========================================================================

    UnityJsonEditor.prototype = {
        constructor: UnityJsonEditor,

        /**
         * Opens the editor modal with the given data.
         * @param {*} data - Data to edit (will be serialized to JSON).
         */
        open: function (data) {
            if (!this._built) {
                this._build();
            }

            this._savedFlag = false;
            const json = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
            this._textarea.val(json);
            this._clearStatus();
            this._validate();
            this._bsModal.show();
        },

        /**
         * Closes the editor modal.
         */
        close: function () {
            if (this._bsModal) {
                this._bsModal.hide();
            }
        },

        /**
         * Returns the current parsed data from the editor, or null if invalid.
         * @returns {*|null}
         */
        getData: function () {
            try {
                return JSON.parse(this._textarea.val());
            } catch (e) {
                console.debug('getData: invalid JSON in editor', e.message);
                return null;
            }
        },

        /**
         * Updates configuration options on the fly.
         * @param {object} opts - Options to merge.
         */
        setOptions: function (opts) {
            $.extend(this._opts, opts);
            if (this._built) {
                this._updateUI();
            }
        },

        /**
         * Destroys the editor instance and removes the modal from the DOM.
         */
        destroy: function () {
            if (this._bsModal) {
                this._bsModal.dispose();
            }
            if (this._modal) {
                this._modal.remove();
            }
            this._built = false;
        },

        // ====================================================================
        // Private methods
        // ====================================================================

        /**
         * Builds the Bootstrap modal and appends it to the document body.
         * @private
         */
        _build: function () {
            const opts = this._opts;
            const id = this._modalId;

            const html =
                '<div class="modal fade" id="' + id + '" tabindex="-1" aria-labelledby="' + id + 'Label" aria-hidden="true">' +
                '  <div class="modal-dialog ' + opts.size + '">' +
                '    <div class="modal-content">' +
                '      <div class="modal-header">' +
                '        <h5 class="modal-title" id="' + id + 'Label">' + _escapeHtml(opts.title) + '</h5>' +
                '        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                '      </div>' +
                '      <div class="modal-body">' +
                '        <div class="uje-toolbar">' +
                '          <button type="button" class="btn btn-sm btn-outline-secondary uje-format-btn">' +
                '            <i class="fa fa-align-left me-1"></i>' + _escapeHtml(opts.formatButtonText) +
                '          </button>' +
                '          <span class="uje-status-text"></span>' +
                '        </div>' +
                '        <textarea class="form-control uje-textarea" rows="' + opts.rows + '" spellcheck="false"' +
                (opts.readOnly ? ' readonly' : '') + '></textarea>' +
                '        <div class="uje-status-bar">' +
                '          <span class="uje-validation-msg"></span>' +
                '        </div>' +
                '      </div>' +
                '      <div class="modal-footer">' +
                '        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">' + _escapeHtml(opts.cancelButtonText) + '</button>' +
                (opts.readOnly ? '' :
                '        <button type="button" class="btn btn-primary uje-save-btn" disabled>' + _escapeHtml(opts.saveButtonText) + '</button>') +
                '      </div>' +
                '    </div>' +
                '  </div>' +
                '</div>';

            this._modal = $(html);
            $('body').append(this._modal);

            this._textarea = this._modal.find('.uje-textarea');
            this._statusBar = this._modal.find('.uje-validation-msg');
            this._statusText = this._modal.find('.uje-status-text');
            this._saveBtn = this._modal.find('.uje-save-btn');
            this._bsModal = new bootstrap.Modal(document.getElementById(id));

            // Event: real-time validation on input
            this._textarea.on('input', () => {
                this._validate();
            });

            // Event: format button
            this._modal.find('.uje-format-btn').on('click', () => {
                this._format();
            });

            // Event: save button
            if (!opts.readOnly) {
                this._saveBtn.on('click', () => {
                    this._onSave();
                });
            }

            // Event: modal hidden — only fire onCancel when not closed via save
            this._modal.on('hidden.bs.modal', () => {
                if (!this._savedFlag && typeof opts.onCancel === 'function') {
                    opts.onCancel();
                }
                this._savedFlag = false;
            });

            // Tab key inserts spaces instead of changing focus
            this._textarea.on('keydown', (e) => {
                if (e.key === 'Tab') {
                    e.preventDefault();
                    const target = e.target;
                    const start = target.selectionStart;
                    const end = target.selectionEnd;
                    const value = $(target).val();
                    $(target).val(value.substring(0, start) + '  ' + value.substring(end));
                    target.selectionStart = target.selectionEnd = start + 2;
                    $(this._textarea).trigger('input');
                }
            });

            this._built = true;
        },

        /**
         * Updates UI elements to reflect current options.
         * @private
         */
        _updateUI: function () {
            const opts = this._opts;
            this._modal.find('.modal-title').text(opts.title);
            this._modal.find('.uje-format-btn').html('<i class="fa fa-align-left me-1"></i>' + _escapeHtml(opts.formatButtonText));
            if (this._saveBtn.length) {
                this._saveBtn.text(opts.saveButtonText);
            }
            this._textarea.prop('readonly', opts.readOnly);
        },

        /**
         * Formats/beautifies the JSON in the textarea.
         * @private
         */
        _format: function () {
            try {
                const raw = this._textarea.val();
                const parsed = JSON.parse(raw);
                this._textarea.val(JSON.stringify(parsed, null, 2));
                this._validate();
            } catch (e) {
                console.debug('_format: could not parse JSON', e.message);
                this._validate();
            }
        },

        /**
         * Checks required fields on each item in an array of objects.
         * @private
         * @param {*} data - Parsed JSON data.
         * @returns {string|null} Error message if validation fails, null if valid.
         */
        _checkRequiredFields: function (data) {
            const requiredFields = this._opts.requiredFields;
            if (!requiredFields || requiredFields.length === 0 || !Array.isArray(data)) {
                return null;
            }

            for (const [i, item] of data.entries()) {
                if (typeof item !== 'object' || item === null) {
                    return 'Item at index ' + i + ' is not an object.';
                }
                for (const field of requiredFields) {
                    if (!(field in item) || item[field] === null || item[field] === undefined) {
                        return 'Item at index ' + i + ' is missing required field "' + field + '".';
                    }
                }
            }

            return null;
        },

        /**
         * Runs all validation checks and updates the UI.
         * @private
         * @returns {boolean} True if no errors (warnings are acceptable).
         */
        _validate: function () {
            const raw = this._textarea.val().trim();
            this._lastWarnings = [];

            // Empty check
            if (!raw) {
                this._showStatus('warning', 'Editor is empty');
                this._setSaveEnabled(false);
                return false;
            }

            // JSON syntax check
            let data;
            try {
                data = JSON.parse(raw);
            } catch (e) {
                this._showStatus('error', 'Invalid JSON: ' + e.message);
                this._setSaveEnabled(false);
                return false;
            }

            // Required fields check (only for arrays of objects)
            const requiredFieldsError = this._checkRequiredFields(data);
            if (requiredFieldsError) {
                this._showStatus('error', requiredFieldsError);
                this._setSaveEnabled(false);
                return false;
            }

            // Custom validators (errors + warnings)
            const result = _runValidators(data, this._opts.validators, this._opts.requiredFields);

            // Errors block save
            if (result.errors.length > 0) {
                this._showStatus('error', result.errors[0].message);
                this._setSaveEnabled(false);
                if (typeof this._opts.onValidationError === 'function') {
                    this._opts.onValidationError(result.errors);
                }
                return false;
            }

            // Warnings allow save but show amber status
            const itemCount = Array.isArray(data) ? data.length + ' items' : 'Object';
            if (result.warnings.length > 0) {
                this._lastWarnings = result.warnings;
                const warningText = result.warnings[0].message;
                this._showStatus('warning', 'Valid JSON \u00B7 ' + itemCount, '\u26A0 ' + warningText);
                this._textarea.removeClass('is-invalid');
                this._setSaveEnabled(true);
                return true;
            }

            // All passed — no errors, no warnings
            this._showStatus('success', 'Valid JSON \u00B7 ' + itemCount);
            this._setSaveEnabled(true);
            return true;
        },

        /**
         * Handles the save action.
         * @private
         */
        _onSave: function () {
            if (!this._validate()) return;

            const data = JSON.parse(this._textarea.val());
            const warnings = this._lastWarnings || [];
            if (typeof this._opts.onSave === 'function') {
                this._opts.onSave(data, warnings);
            }
            this._savedFlag = true;
            this.close();
        },

        /**
         * Displays a status message.
         * @private
         * @param {'success'|'error'|'warning'} type
         * @param {string} message
         */
        _showStatus: function (type, message, secondLine) {
            const iconMap = { success: 'fa-check-circle', error: 'fa-times-circle', warning: 'fa-exclamation-circle' };
            const colorMap = { success: 'text-success', error: 'text-danger', warning: 'text-warning' };
            const icon = iconMap[type] || iconMap.warning;
            const colorClass = colorMap[type] || colorMap.warning;

            let html = '<i class="fa ' + icon + ' me-1"></i>' + _escapeHtml(message);
            if (secondLine) {
                html += '<br>' + _escapeHtml(secondLine);
            }

            this._statusBar
                .html(html)
                .removeClass('text-success text-danger text-warning')
                .addClass(colorClass);

            this._textarea
                .toggleClass('is-invalid', type === 'error')
                .toggleClass('is-valid', type === 'success');
        },

        /**
         * Clears the status bar.
         * @private
         */
        _clearStatus: function () {
            this._statusBar.html('').removeClass('text-success text-danger text-warning');
            this._textarea.removeClass('is-invalid is-valid');
        },

        /**
         * Enables or disables the save button.
         * @private
         * @param {boolean} enabled
         */
        _setSaveEnabled: function (enabled) {
            if (this._saveBtn.length) {
                this._saveBtn.prop('disabled', !enabled);
            }
        }
    };

    // ========================================================================
    // Private utility functions
    // ========================================================================

    /**
     * Runs an array of custom validators against parsed data.
     * Validators with severity:'warning' produce warnings (non-blocking);
     * all others produce errors (blocking).
     * @param {*} data - Parsed JSON data.
     * @param {Array} validators - Validator definitions.
     * @param {string[]|null} requiredFields - Required field names (for context).
     * @returns {{errors: Array<{validator: string, message: string}>, warnings: Array<{validator: string, message: string}>}}
     */
    function _runValidators(data, validators, requiredFields) {
        const errors = [];
        const warnings = [];
        if (!validators || validators.length === 0) return { errors: errors, warnings: warnings };

        for (const v of validators) {
            const isWarning = v.severity === 'warning';
            const target = isWarning ? warnings : errors;
            try {
                const result = v.validate(data, requiredFields);
                if (result === false) {
                    target.push({ validator: v.name, message: v.message || 'Validation failed: ' + v.name });
                } else if (typeof result === 'object' && result !== null && result.valid === false) {
                    target.push({ validator: v.name, message: result.message || v.message || 'Validation failed: ' + v.name });
                }
            } catch (ex) {
                errors.push({ validator: v.name, message: 'Validator "' + v.name + '" error: ' + ex.message });
            }
        }
        return { errors: errors, warnings: warnings };
    }

    /**
     * Reads a file, parses JSON, runs validators, and resolves with the data.
     * Extracted from importFromFile to keep function nesting shallow.
     * @param {File} file - The file to read.
     * @param {Array} validators - Validators to run on the parsed data.
     * @param {function|null} onWarning - Callback for non-blocking warnings.
     * @returns {Promise<*>} Resolves with parsed JSON data, rejects on error.
     */
    function _processImportedFile(file, validators, onWarning) {
        return file.text().then(function (text) {
            const data = JSON.parse(text);

            // Run validators if provided
            const result = _runValidators(data, validators, null);
            if (result.errors.length > 0) {
                throw new Error(result.errors.map(function (err) { return err.message; }).join('\n'));
            }

            // Report warnings but allow import
            if (result.warnings.length > 0 && typeof onWarning === 'function') {
                onWarning(result.warnings);
            }

            return data;
        }).catch(function (ex) {
            throw new Error('Invalid JSON file: ' + ex.message);
        });
    }

    /**
     * Escapes HTML special characters.
     * @param {string} str
     * @returns {string}
     */
    function _escapeHtml(str) {
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    return UnityJsonEditor;

})(jQuery);
