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
var UnityJsonEditor = (function ($) {
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
         * Callback invoked when the user cancels or closes the modal.
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
        var json = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
        var blob = new Blob([json], { type: 'application/json' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
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
        var accept = opts.accept || '.json';
        var validators = opts.validators || [];
        var onWarning = opts.onWarning || null;

        return new Promise(function (resolve, reject) {
            var input = document.createElement('input');
            input.type = 'file';
            input.accept = accept;
            input.style.display = 'none';

            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) {
                    reject(new Error('No file selected.'));
                    return;
                }
                var reader = new FileReader();
                reader.onload = function (e) {
                    try {
                        var data = JSON.parse(e.target.result);

                        // Run validators if provided
                        var result = _runValidators(data, validators, null);
                        if (result.errors.length > 0) {
                            reject(new Error(result.errors.map(function (err) { return err.message; }).join('\n')));
                            return;
                        }

                        // Report warnings but allow import
                        if (result.warnings.length > 0 && typeof onWarning === 'function') {
                            onWarning(result.warnings);
                        }

                        resolve(data);
                    } catch (ex) {
                        reject(new Error('Invalid JSON file: ' + ex.message));
                    }
                };
                reader.onerror = function () {
                    reject(new Error('Failed to read file.'));
                };
                reader.readAsText(file);
            });

            document.body.appendChild(input);
            input.click();
            document.body.removeChild(input);
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

            var json = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
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
            } catch (_) {
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
            var self = this;
            var opts = this._opts;
            var id = this._modalId;

            var html =
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
            this._textarea.on('input', function () {
                self._validate();
            });

            // Event: format button
            this._modal.find('.uje-format-btn').on('click', function () {
                self._format();
            });

            // Event: save button
            if (!opts.readOnly) {
                this._saveBtn.on('click', function () {
                    self._onSave();
                });
            }

            // Event: modal hidden (cancel)
            this._modal.on('hidden.bs.modal', function () {
                if (typeof opts.onCancel === 'function') {
                    opts.onCancel();
                }
            });

            // Tab key inserts spaces instead of changing focus
            this._textarea.on('keydown', function (e) {
                if (e.key === 'Tab') {
                    e.preventDefault();
                    var start = this.selectionStart;
                    var end = this.selectionEnd;
                    var value = $(this).val();
                    $(this).val(value.substring(0, start) + '  ' + value.substring(end));
                    this.selectionStart = this.selectionEnd = start + 2;
                    $(self._textarea).trigger('input');
                }
            });

            this._built = true;
        },

        /**
         * Updates UI elements to reflect current options.
         * @private
         */
        _updateUI: function () {
            var opts = this._opts;
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
                var raw = this._textarea.val();
                var parsed = JSON.parse(raw);
                this._textarea.val(JSON.stringify(parsed, null, 2));
                this._validate();
            } catch (_) {
                // Validation will show the error
                this._validate();
            }
        },

        /**
         * Runs all validation checks and updates the UI.
         * @private
         * @returns {boolean} True if no errors (warnings are acceptable).
         */
        _validate: function () {
            var raw = this._textarea.val().trim();
            this._lastWarnings = [];

            // Empty check
            if (!raw) {
                this._showStatus('warning', 'Editor is empty');
                this._setSaveEnabled(false);
                return false;
            }

            // JSON syntax check
            var data;
            try {
                data = JSON.parse(raw);
            } catch (e) {
                this._showStatus('error', 'Invalid JSON: ' + e.message);
                this._setSaveEnabled(false);
                return false;
            }

            // Required fields check (only for arrays of objects)
            var requiredFields = this._opts.requiredFields;
            if (requiredFields && requiredFields.length > 0 && Array.isArray(data)) {
                for (var i = 0; i < data.length; i++) {
                    if (typeof data[i] !== 'object' || data[i] === null) {
                        this._showStatus('error', 'Item at index ' + i + ' is not an object.');
                        this._setSaveEnabled(false);
                        return false;
                    }
                    for (var f = 0; f < requiredFields.length; f++) {
                        var field = requiredFields[f];
                        if (!(field in data[i]) || data[i][field] === null || data[i][field] === undefined) {
                            this._showStatus('error', 'Item at index ' + i + ' is missing required field "' + field + '".');
                            this._setSaveEnabled(false);
                            return false;
                        }
                    }
                }
            }

            // Custom validators (errors + warnings)
            var result = _runValidators(data, this._opts.validators, this._opts.requiredFields);

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
            var itemCount = Array.isArray(data) ? data.length + ' items' : 'Object';
            if (result.warnings.length > 0) {
                this._lastWarnings = result.warnings;
                var warningText = result.warnings[0].message;
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

            var data = JSON.parse(this._textarea.val());
            var warnings = this._lastWarnings || [];
            if (typeof this._opts.onSave === 'function') {
                this._opts.onSave(data, warnings);
            }
            this.close();
        },

        /**
         * Displays a status message.
         * @private
         * @param {'success'|'error'|'warning'} type
         * @param {string} message
         */
        _showStatus: function (type, message, secondLine) {
            var icon = type === 'success' ? 'fa-check-circle' :
                       type === 'error'   ? 'fa-times-circle' :
                                            'fa-exclamation-circle';
            var colorClass = type === 'success' ? 'text-success' :
                             type === 'error'   ? 'text-danger' :
                                                  'text-warning';

            var html = '<i class="fa ' + icon + ' me-1"></i>' + _escapeHtml(message);
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
        var errors = [];
        var warnings = [];
        if (!validators || validators.length === 0) return { errors: errors, warnings: warnings };

        for (var i = 0; i < validators.length; i++) {
            var v = validators[i];
            var isWarning = v.severity === 'warning';
            var target = isWarning ? warnings : errors;
            try {
                var result = v.validate(data, requiredFields);
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
     * Escapes HTML special characters.
     * @param {string} str
     * @returns {string}
     */
    function _escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    return UnityJsonEditor;

})(jQuery);
