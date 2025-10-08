$(function () {
    let hasUnsavedChanges = false;    
    let dataTable = null;
    let currentProvider = 'formversion'; // Default provider

    // Provider configuration object
    const PROVIDERS = {
        formversion: {
            name: 'Submissions',
            correlationProvider: 'formversion',
            columns: {
                label: 'CHEFS Label',
                key: 'CHEFS Property Name',
                type: 'CHEFS Type',
                path: 'Path',
                columnName: 'Report Column'
            }
        },
        worksheet: {
            name: 'Worksheets',
            correlationProvider: 'worksheet',
            columns: {
                label: 'Worksheet Label',
                key: 'Worksheet Property Name',
                type: 'Worksheet Type',
                path: 'Path',
                columnName: 'Report Column'
            }
        },
        scoresheet: {
            name: 'Scoresheets',
            correlationProvider: 'scoresheet',
            columns: {
                label: 'Scoresheet Label',
                key: 'Scoresheet Property Name',
                type: 'Scoresheet Type',
                path: 'Path',
                columnName: 'Report Column'
            }
        }
    };

    // PostgreSQL/Database column name validation rules
    const COLUMN_VALIDATION = {
        MAX_LENGTH: 60,
        MIN_LENGTH: 1,
        RESERVED_WORDS: [
            'all', 'analyse', 'analyze', 'and', 'any', 'array', 'as', 'asc', 'asymmetric',
            'authorization', 'binary', 'both', 'case', 'cast', 'check', 'collate', 'collation',
            'column', 'concurrently', 'constraint', 'create', 'cross', 'current_catalog',
            'current_date', 'current_role', 'current_schema', 'current_time', 'current_timestamp',
            'current_user', 'default', 'deferrable', 'desc', 'distinct', 'do', 'else', 'end',
            'except', 'false', 'fetch', 'for', 'foreign', 'freeze', 'from', 'full', 'grant',
            'group', 'having', 'ilike', 'in', 'initially', 'inner', 'intersect', 'into', 'is',
            'isnull', 'join', 'lateral', 'leading', 'left', 'like', 'limit', 'localtime',
            'localtimestamp', 'natural', 'not', 'notnull', 'null', 'offset', 'on', 'only',
            'or', 'order', 'outer', 'overlaps', 'placing', 'primary', 'references', 'returning',
            'right', 'select', 'session_user', 'similar', 'some', 'symmetric', 'table', 'tablesample',
            'then', 'to', 'trailing', 'true', 'union', 'unique', 'user', 'using', 'variadic',
            'verbose', 'when', 'where', 'window', 'with'
        ]
    };

    // PostgreSQL view name validation rules
    const VIEW_NAME_VALIDATION = {
        MAX_LENGTH: 63,
        MIN_LENGTH: 1,
        RESERVED_WORDS: [
            'all', 'analyse', 'analyze', 'and', 'any', 'array', 'as', 'asc', 'asymmetric',
            'authorization', 'binary', 'both', 'case', 'cast', 'check', 'collate', 'collation',
            'column', 'concurrently', 'constraint', 'create', 'cross', 'current_catalog',
            'current_date', 'current_role', 'current_schema', 'current_time', 'current_timestamp',
            'current_user', 'default', 'deferrable', 'desc', 'distinct', 'do', 'else', 'end',
            'except', 'false', 'fetch', 'for', 'foreign', 'freeze', 'from', 'full', 'grant',
            'group', 'having', 'ilike', 'in', 'initially', 'inner', 'intersect', 'into', 'is',
            'isnull', 'join', 'lateral', 'leading', 'left', 'like', 'limit', 'localtime',
            'localtimestamp', 'natural', 'not', 'notnull', 'null', 'offset', 'on', 'only',
            'or', 'order', 'outer', 'overlaps', 'placing', 'primary', 'references', 'returning',
            'right', 'select', 'session_user', 'similar', 'some', 'symmetric', 'table', 'tablesample',
            'then', 'to', 'trailing', 'true', 'union', 'unique', 'user', 'using', 'variadic',
            'verbose', 'when', 'where', 'window', 'with'
        ]
    };

    // Function to get current provider configuration
    function getCurrentProviderConfig() {
        return PROVIDERS[currentProvider] || PROVIDERS.formversion;
    }

    // Function to get current correlation provider
    function getCorrelationProvider() {
        return getCurrentProviderConfig().correlationProvider;
    }

    // Function to handle provider change
    function handleProviderChange(newProvider) {
        if (newProvider === currentProvider) {
            return; // No change needed
        }

        // Check for unsaved changes
        if (hasUnsavedChanges) {
            if (!confirm('You have unsaved changes. Switching providers will discard these changes. Do you want to continue?')) {
                // Reset the toggle to current provider
                $(`input[name="provider-toggle"][value="${currentProvider}"]`).prop('checked', true);
                return;
            }
        }

        // Update current provider
        currentProvider = newProvider;
        
        // Reset changes state since we're switching providers
        resetChangesState();
        
        // Update column headers in the DataTable
        updateDataTableColumnHeaders();
        
        // Check if configuration exists for the new provider and update button visibility
        const versionId = $('#versionSelector').val();
        if (versionId) {
            checkConfigurationExists(versionId, function(exists) {
                updateGenerateViewButtonVisibility(exists);
                updateDeleteButtonVisibility(exists);
            });
            
            // Refresh the view status widget for the new provider
            refreshViewStatusWidget(versionId, getCorrelationProvider());
        } else {
            updateGenerateViewButtonVisibility(false);
            updateDeleteButtonVisibility(false);
        }
        
        // Reload the DataTable with new provider
        if (dataTable) {
            dataTable.ajax.reload();
        }
    }

    // Function to update DataTable column headers
    function updateDataTableColumnHeaders() {
        if (!dataTable) return;
        
        const providerConfig = getCurrentProviderConfig();
        const columns = providerConfig.columns;
        
        // Update column headers
        const columnIndices = [
            { index: 0, title: columns.label },
            { index: 1, title: columns.key },
            { index: 2, title: columns.type },
            { index: 3, title: columns.path },
            { index: 4, title: columns.columnName }
        ];
        
        columnIndices.forEach(col => {
            const header = $(dataTable.column(col.index).header());
            header.text(col.title);
        });
        
        // Force redraw to update headers
        dataTable.draw(false);
    }

    // Handle provider toggle change event
    $(document).on('change', 'input[name="provider-toggle"]', function() {
        const newProvider = $(this).val();
        handleProviderChange(newProvider);
    });

    // Function to check if a path has duplicate key prefix (DKx)
    function hasDuplicateKeyPrefix(path) {
        if (!path) return false;
        return /^\(DK\d+\)/.test(path);
    }

    // Function to check for duplicate keys in current table data
    function checkForDuplicateKeysInTable() {
        let hasDuplicates = false;
        
        if (dataTable) {
            dataTable.rows().every(function () {
                const data = this.data();
                if (hasDuplicateKeyPrefix(data.path) || hasDuplicateKeyPrefix(data.dataPath)) {
                    hasDuplicates = true;
                    return false; // Break the loop
                }
            });
        }
        
        return hasDuplicates;
    }

    // Function to update duplicate keys warning display
    function updateDuplicateKeysWarning(hasDuplicates) {
        const $warning = $('#div-duplicate-keys-warning');
        
        if (hasDuplicates) {
            $warning.removeClass('duplicate-keys-div-hidden');            
        } else {
            $warning.addClass('duplicate-keys-div-hidden');            
        }
    }

    // Function to show or hide the Generate View button based on configuration existence
    function updateGenerateViewButtonVisibility(hasConfiguration) {
        const $generateBtn = $('#btn-generate-view-report-configuration');
        
        if (hasConfiguration) {
            $generateBtn.removeClass('generate-view-btn-hidden');
        } else {
            $generateBtn.addClass('generate-view-btn-hidden');
        }
    }

    // Function to show or hide the Delete button based on configuration existence
    function updateDeleteButtonVisibility(hasConfiguration) {
        const $deleteBtn = $('#btn-delete-report-configuration');
        
        if (hasConfiguration) {
            $deleteBtn.removeClass('delete-config-btn-hidden');
        } else {
            $deleteBtn.addClass('delete-config-btn-hidden');
        }
    }

    // Function to refresh the view status widget
    function refreshViewStatusWidget(versionId, provider) {
        if (!versionId) return;
        
        const currentCorrelationProvider = provider || getCorrelationProvider();
        
        let $viewStatusWidget = $('[data-widget-name="ReportingConfigurationViewStatus"]');
        if ($viewStatusWidget.length > 0) {
            // Update the data attributes on the widget wrapper
            $viewStatusWidget.attr('data-version-id', versionId);
            $viewStatusWidget.attr('data-provider', currentCorrelationProvider);
            
            let widgetManager = new abp.WidgetManager({
                wrapper: $viewStatusWidget,
                filterCallback: function () {
                    return {
                        versionId: versionId,
                        provider: currentCorrelationProvider
                    };
                }
            });
            widgetManager.refresh();
        }
    }

    // Function to display detected changes alert
    function displayDetectedChangesAlert(detectedChanges) {
        if (!detectedChanges || detectedChanges.trim() === '') {
            return;
        }

        // Create a more informative alert message
        const alertMessage = `Schema changes have been detected:\n\n${detectedChanges}\n\nThese changes may affect your report configuration. Please review your column mappings and save the configuration if needed.`;
        
        // Use ABP's message service for a better user experience
        abp.message.warn(alertMessage, 'Schema Changes Detected');
    }

    // Handle version selector change
    $('#versionSelector').on('change', function () {
        const newVersionId = $(this).val();        
        console.log('Selected version ID:', newVersionId);
        
        // Update hidden input
        $('#reportingVersionId').val(newVersionId);
        
        // Check if configuration exists for the new version and current provider
        checkConfigurationExists(newVersionId, function(exists) {
            updateGenerateViewButtonVisibility(exists);
            updateDeleteButtonVisibility(exists);
        });
        
        // Refresh the view status widget for the new version and current provider
        refreshViewStatusWidget(newVersionId, getCorrelationProvider());
        
        // Reload the DataTable
        if (dataTable) {
            dataTable.ajax.reload();
        }
        
        // Reset changes state since we're loading new data
        resetChangesState();
    });

    // Function to check if configuration exists
    function checkConfigurationExists(versionId, callback) {
        if (!versionId) {
            callback(false);
            return;
        }
        
        abp.ajax({
            url: abp.appPath + 'ReportingConfiguration/Exists',
            type: 'GET',
            data: { 
                correlationId: versionId, 
                correlationProvider: getCorrelationProvider()
            }
        }).done(function(exists) {
            callback(exists);
        }).fail(function(error) {
            console.error('Error checking configuration existence:', error);
            callback(false);
        });
    }

    // Initialize DataTable
    function initializeReportConfigTable() {
        const dt = $('#ReportConfigurationTable');
        const providerConfig = getCurrentProviderConfig();

        const listColumns = [
            {
                title: providerConfig.columns.label,
                data: 'label',
                name: 'label',
                className: 'data-table-header',
                index: 0,
                orderable: true
            },
            {
                title: providerConfig.columns.key,
                data: 'key',
                name: 'key',
                className: 'data-table-header',
                index: 1,
                orderable: true
            },
            {
                title: providerConfig.columns.type,
                data: 'type',
                name: 'type',
                className: 'data-table-header',
                index: 2,
                orderable: true
            },
            {
                title: providerConfig.columns.path,
                data: 'dataPath', // We use the dataPath explicitly here
                name: 'path',
                className: 'data-table-header',
                index: 3,
                orderable: true,
                render: function (data, type, row) {
                    if (type === 'display') {
                        return data || '';
                    }
                    return data;
                }
            },
            {
                title: providerConfig.columns.columnName,
                data: 'columnName',
                name: 'columnName',
                className: 'data-table-header',
                index: 4,
                orderable: false,
                render: function (data, type, row) {
                    if (type === 'display') {
                        const sanitizedValue = sanitizeColumnName(data || '');
                        return `<input type="text" class="form-control column-name-input" value="${sanitizedValue}" data-key="${row.key}" data-path="${row.path}" data-original="${data || ''}" />`;
                    }
                    return data;
                }
            }
       ];

        const actionButtons = [
            ...commonTableActionButtons('Report Configuration')
        ];

        const responseCallback = function (result) {
            return {
                recordsTotal: result.totalCount || result.length || 0,
                recordsFiltered: result.totalCount || result.length || 0,
                data: formatItems(result.items || result)
            };
        };

        const formatItems = function (items) {
            const newData = items.map((item, index) => {
                return {
                    ...item,
                    rowCount: index
                };
            });
            return newData;
        };

        // Create data endpoint function that handles the exists check and fallback logic
        const dataEndpoint = function(requestData) {
            const versionId = $('#versionSelector').val();
            if (!versionId) {
                // Return empty result for invalid version
                return Promise.resolve({ items: [], totalCount: 0 });
            }
            
            const correlationProvider = getCorrelationProvider();
            
            // First check if configuration exists
            return abp.ajax({
                url: abp.appPath + 'ReportingConfiguration/Exists',
                type: 'GET',
                data: { 
                    correlationId: versionId, 
                    correlationProvider: correlationProvider 
                }
            }).then(function (exists) {
                // Update button visibility based on configuration existence
                updateGenerateViewButtonVisibility(exists);
                updateDeleteButtonVisibility(exists);
                
                if (exists) {
                    // Configuration exists - load from mapping
                    return abp.ajax({
                        url: abp.appPath + 'ReportingConfiguration/GetConfiguration',
                        type: 'GET',
                        data: { 
                            correlationId: versionId, 
                            correlationProvider: correlationProvider 
                        }
                    }).then(function (result) {
                        // Check for detected changes and display alert if present
                        if (result.detectedChanges && result.detectedChanges.trim() !== '') {
                            // Use setTimeout to ensure the alert appears after the table loads
                            setTimeout(function() {
                                displayDetectedChangesAlert(result.detectedChanges);
                            }, 100);
                        }
                        
                        // Transform configuration result
                        const items = result.mapping.rows.map(row => ({
                            label: row.label,
                            key: row.propertyName,
                            type: row.type,
                            path: row.path,
                            dataPath: row.dataPath,
                            columnName: row.columnName || ''
                        }));
                        
                        return {
                            items: items,
                            totalCount: items.length
                        };
                    });
                } else {
                    // No configuration exists - load from fields metadata
                    return abp.ajax({
                        url: abp.appPath + 'ReportingConfiguration/GetFieldsMetadata',
                        type: 'GET',
                        data: { 
                            correlationId: versionId, 
                            correlationProvider: correlationProvider 
                        }
                    }).then(function (fieldsMetadata) {
                        // Transform fields metadata result
                        const items = fieldsMetadata.fields.map(field => ({
                            label: field.label || field.key,
                            key: field.key,
                            type: field.type,
                            path: field.path,
                            dataPath: field.dataPath,
                            columnName: field.label || field.key
                        }));
                        
                        return {
                            items: items,
                            totalCount: items.length
                        };
                    });
                }
            }).catch(function (error) {
                console.error('Failed to load report configuration:', error);
                abp.message.error('Failed to load report configuration');
                updateGenerateViewButtonVisibility(false);
                updateDeleteButtonVisibility(false);
                return { items: [], totalCount: 0 };
            });
        };

        dataTable = initializeDataTable({
            dt,
            defaultVisibleColumns: ['label', 'key', 'type', 'path', 'columnName'],
            listColumns,            
            defaultSortColumn: 0,
            dataEndpoint: dataEndpoint,
            data: {},
            responseCallback: responseCallback,
            actionButtons,
            serverSideEnabled: false,
            pagingEnabled: false,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'ReportConfigurationTable',
            dynamicButtonContainerId: 'reportConfigDynamicButtons',
            useNullPlaceholder: true,
            externalSearchId: 'search-report-config',
            // Enable scrolling - let CSS handle the height
            scrollY: true,
            scrollCollapse: true,
            responsive: true
        });

        // Add event handler for when DataTable completes drawing
        dataTable.on('draw.dt', function() {
            // Ensure inputs are always enabled (edit mode is always on)
            $('#ReportConfigurationTable .column-name-input').prop('disabled', false);
            
            // Check for duplicate keys and update warning
            const hasDuplicates = checkForDuplicateKeysInTable();
            updateDuplicateKeysWarning(hasDuplicates);
            
            // Force column adjustment after draw if tab is visible
            setTimeout(function() {
                if (isReportingTabVisible()) {
                    forceTableColumnAdjustment();
                }
            }, 50);
        });

        // Add event handler for when DataTable data is loaded
        dataTable.on('xhr.dt', function() {
            // Force layout adjustment after data is loaded
            setTimeout(function() {
                if (isReportingTabVisible()) {
                    adjustTableLayout();
                    forceTableColumnAdjustment();
                }
            }, 100);
        });

        // Initial adjustment with longer delay to ensure DOM is ready
        setTimeout(function() {
            if (isReportingTabVisible()) {
                adjustTableLayout();
                forceTableColumnAdjustment();
            }
        }, 500);

        // Track changes on column name inputs with validation
        $('#ReportConfigurationTable').on('input', '.column-name-input', function () {
            const $input = $(this);
            const value = $input.val();
            const path = $input.data('path');
            
            // Validate and provide feedback
            validateColumnNameInput($input, value, path);
            markAsChanged();
        });

        // Handle blur event to sanitize input
        $('#ReportConfigurationTable').on('blur', '.column-name-input', function () {
            const $input = $(this);
            const value = $input.val().trim();
            
            if (value) {
                const sanitized = sanitizeColumnName(value);
                if (sanitized !== value) {
                    $input.val(sanitized);
                    validateColumnNameInput($input, sanitized, $input.data('path'));
                }
            }
        });

        // Initial check for button visibility on page load
        const initialVersionId = $('#versionSelector').val();
        if (initialVersionId) {
            checkConfigurationExists(initialVersionId, function(exists) {
                updateGenerateViewButtonVisibility(exists);
                updateDeleteButtonVisibility(exists);
            });
        } else {
            updateGenerateViewButtonVisibility(false);
            updateDeleteButtonVisibility(false);
        }

        // Initial check for duplicate keys from the server-side value
        const initialHasDuplicateKeys = $('#hasDuplicateKeys').val() === 'true';
        updateDuplicateKeysWarning(initialHasDuplicateKeys);

        // Refresh view status widget after initial load
        setTimeout(function() {
            if (initialVersionId) {
                refreshViewStatusWidget(initialVersionId, getCorrelationProvider());
            }
        }, 100);
    }

    // Generate Unique Column Names button functionality
    $('#btn-generate-unique-column-names').on('click', function () {
        const $button = $(this);
        
        // Collect current column data (paths and their column names)
        const pathColumns = {};
        $('#ReportConfigurationTable .column-name-input').each(function() {
            const $input = $(this);
            const path = $input.data('path');
            const columnName = $input.val().trim();
            
            if (path && columnName) {
                pathColumns[path] = columnName;
            }
        });
        
        if (Object.keys(pathColumns).length === 0) {
            abp.message.info('No column names found to process');
            return;
        }
        
        // Show loading state
        const originalHtml = $button.html();
        $button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Generating...');
        
        // Call the GenerateColumnNames endpoint
        abp.ajax({
            url: abp.appPath + 'ReportingConfiguration/GenerateColumnNames',
            type: 'POST',
            data: JSON.stringify({ pathColumns: pathColumns }),
            contentType: 'application/json'
        }).done(function(uniqueColumnNames) {
            // Update the inputs with the unique column names
            let updatedCount = 0;
            $('#ReportConfigurationTable .column-name-input').each(function() {
                const $input = $(this);
                const path = $input.data('path');
                
                if (path && uniqueColumnNames[path]) {
                    const newValue = uniqueColumnNames[path];
                    const oldValue = $input.val().trim();
                    
                    if (newValue !== oldValue) {
                        $input.val(newValue);
                        
                        // Clear any previous validation state
                        $input.removeClass('is-valid is-invalid');
                        $input.siblings('.invalid-feedback, .valid-feedback').remove();
                        
                        // Validate the new value
                        validateColumnNameInput($input, newValue, path);
                        updatedCount++;
                    }
                }
            });
            
            if (updatedCount > 0) {
                markAsChanged();
                abp.message.success(`Successfully generated unique column names. ${updatedCount} column name(s) were updated.`);
            } else {
                abp.message.info('All column names were already unique. No changes were needed.');
            }
            
        }).fail(function(error) {
            let errorMessage = 'Failed to generate unique column names';
            if (error?.responseJSON?.error?.message) {
                errorMessage = error.responseJSON.error.message;
            } else if (error?.responseText) {
                const parsedError = JSON.parse(error.responseText);
                if (parsedError?.message) {
                    errorMessage = parsedError.message;
                }
            }
            abp.message.error(errorMessage);
        }).always(function() {
            // Reset button state
            $button.prop('disabled', false).html(originalHtml);
        });
    });

    // Function to check if the reporting configuration tab is currently visible
    function isReportingTabVisible() {
        const reportingPanel = document.querySelector('#nav-reporting-configuration');
        return reportingPanel?.classList.contains('show', 'active');
    }

    // Enhanced function to force table column adjustment
    function forceTableColumnAdjustment() {
        if (!dataTable) return;
        
        try {
            // Multiple approaches to ensure columns are properly sized
            // 1. Force recalc of column widths
            dataTable.columns.adjust();
            
            // 2. Trigger responsive recalc if responsive is enabled
            if (dataTable.responsive) {
                dataTable.responsive.recalc();
            }
            
            // 3. Force a layout recalculation by temporarily changing display
            const wrapper = $('#ReportConfigurationTable_wrapper');
            if (wrapper.length) {
                // Force browser reflow                
                wrapper.hide();                
                wrapper.show();
                
                // Final column adjustment after showing
                setTimeout(function() {
                    dataTable.columns.adjust();
                }, 10);
            }
            
        } catch (error) {
            console.warn('Error during force column adjustment:', error);
        }
    }

    // Column name sanitization function
    function sanitizeColumnName(name) {
        if (!name || typeof name !== 'string') return '';
        
        // Convert to lowercase and trim
        let sanitized = name.toLowerCase().trim();
        
        // Replace multiple spaces/hyphens with single underscore
        sanitized = sanitized.replace(/[\s\-]+/g, '_');
        
        // Remove all non-alphanumeric characters except underscores
        sanitized = sanitized.replace(/[^a-z0-9_]/g, '');
        
        // Remove leading/trailing underscores
        sanitized = sanitized.replace(/^_+|_+$/g, '');
        
        // If starts with number, prefix with 'col_'
        if (sanitized && /^[0-9]/.test(sanitized)) {
            sanitized = 'col_' + sanitized;
        }
        
        // If empty after sanitization, return empty string
        if (!sanitized) {
            return '';
        }
        
        // Truncate to max length
        if (sanitized.length > COLUMN_VALIDATION.MAX_LENGTH) {
            sanitized = sanitized.substring(0, COLUMN_VALIDATION.MAX_LENGTH);
            // Remove trailing underscore if truncation created one
            sanitized = sanitized.replace(/_+$/, '');
        }
        
        return sanitized;
    }

    // Column name validation function
    function validateColumnName(columnName, excludePath = null) {
        const errors = [];
        
        if (!columnName || columnName.trim() === '') {
            return { isValid: true, errors: [] }; // Empty is allowed
        }
        
        const name = columnName.trim();
        
        // Check length
        if (name.length > COLUMN_VALIDATION.MAX_LENGTH) {
            errors.push(`Column name exceeds maximum length of ${COLUMN_VALIDATION.MAX_LENGTH} characters`);
        }
        
        // Check format (should be alphanumeric + underscores, not starting with number)
        if (!/^[a-z_][a-z0-9_]*$/i.test(name)) {
            errors.push('Column name must start with a letter or underscore and contain only letters, numbers, and underscores');
        }
        
        // Check reserved words
        if (COLUMN_VALIDATION.RESERVED_WORDS.includes(name.toLowerCase())) {
            errors.push('Column name is a PostgreSQL reserved word');
        }
        
        // Check uniqueness
        const allColumnNames = getCurrentColumnNames(excludePath);
        const duplicateCount = allColumnNames.filter(n => n.toLowerCase() === name.toLowerCase()).length;
        if (duplicateCount > 0) {
            errors.push('Column name must be unique');
        }
        
        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    // Get all current column names (excluding specified path)
    function getCurrentColumnNames(excludePath = null) {
        const columnNames = [];
        $('#ReportConfigurationTable .column-name-input').each(function() {
            const $input = $(this);
            const path = $input.data('path');
            const value = $input.val().trim();
            
            if (value && path !== excludePath) {
                columnNames.push(value);
            }
        });
        return columnNames;
    }

    // Validate input and show feedback
    function validateColumnNameInput($input, value, path) {
        const validation = validateColumnName(value, path);
        
        // Remove previous validation classes
        $input.removeClass('is-valid is-invalid');
        
        // Remove any existing feedback
        $input.siblings('.invalid-feedback, .valid-feedback').remove();
        
        if (value.trim() === '') {
            // Empty is allowed - no validation styling
            return;
        }
        
        if (validation.isValid) {
            $input.addClass('is-valid');
        } else {
            $input.addClass('is-invalid');
            
            // Add error feedback
            const errorHtml = validation.errors.map(error => `<div>${error}</div>`).join('');
            $input.after(`<div class="invalid-feedback">${errorHtml}</div>`);
        }
    }

    // Validate all column names before save
    function validateAllColumnNames() {
        const errors = [];
        const columnNames = [];
        
        $('#ReportConfigurationTable .column-name-input').each(function() {
            const $input = $(this);
            const value = $input.val().trim();
            const path = $input.data('path');
            const label = $input.closest('tr').find('td:first').text();
            
            if (value) {
                const validation = validateColumnName(value, path);
                if (!validation.isValid) {
                    errors.push(`Field "${label}": ${validation.errors.join(', ')}`);
                }
                columnNames.push(value.toLowerCase());
            }
        });
        
        // Check for duplicates across all fields
        const duplicates = columnNames.filter((name, index) => columnNames.indexOf(name) !== index);
        if (duplicates.length > 0) {
            const uniqueDuplicates = [...new Set(duplicates)];
            errors.push(`Duplicate column names found: ${uniqueDuplicates.join(', ')}`);
        }
        
        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    // Function to adjust table layout dynamically
    function adjustTableLayout() {
        if (dataTable && dataTable.settings().length > 0) {
            try {
                const container = $('.report-config-table-container');
                const containerHeight = container.height();
                
                if (containerHeight > 100) { // Only adjust if container has reasonable height
                    // Calculate available height for the table body
                    const wrapper = $('#ReportConfigurationTable_wrapper');
                    const header = wrapper.find('.dataTables_scrollHead');
                    const info = wrapper.find('.dataTables_info');
                    const paginate = wrapper.find('.dataTables_paginate');
                    
                    const headerHeight = header.length ? header.outerHeight(true) : 0;
                    const infoHeight = info.length ? info.outerHeight(true) : 0;
                    const paginateHeight = paginate.length ? paginate.outerHeight(true) : 0;
                    const padding = 20; // Extra padding
                    
                    const availableHeight = containerHeight - headerHeight - infoHeight - paginateHeight - padding;
                    const minHeight = 200; // Minimum table body height
                    
                    const scrollBodyHeight = Math.max(minHeight, availableHeight);
                    
                    // Apply the calculated height
                    wrapper.find('.dataTables_scrollBody').css({
                        'max-height': scrollBodyHeight + 'px',
                        'height': scrollBodyHeight + 'px'
                    });
                }
            } catch (error) {
                console.warn('Error adjusting table layout:', error);
            }
        }
    }

    // Function to handle tab visibility changes with improved column adjustment
    function handleTabVisibilityChange() {
        // Check if the reporting configuration tab is now visible
        if (isReportingTabVisible()) {
            // Progressive approach with multiple attempts
            const adjustmentSequence = [
                { delay: 50, action: 'initial' },
                { delay: 150, action: 'layout' },
                { delay: 300, action: 'columns' },
                { delay: 500, action: 'final' }
            ];
            
            adjustmentSequence.forEach(step => {
                setTimeout(() => {
                    if (dataTable && isReportingTabVisible()) {
                        switch (step.action) {
                            case 'initial':
                                dataTable.columns.adjust();
                                break;
                            case 'layout':
                                adjustTableLayout();
                                break;
                            case 'columns':
                                forceTableColumnAdjustment();
                                break;
                            case 'final':
                                dataTable.columns.adjust();
                                dataTable.draw(false);
                                break;
                        }
                    }
                }, step.delay);
            });
        }
    }

    // Initialize on page load
    initializeReportConfigTable();
    
    // Set initial undo button state
    updateUndoButtonVisibility();

    // Initialize tooltips
    $(document).ready(function() {
        // Initialize Bootstrap tooltips
        $('[data-bs-toggle="tooltip"]').tooltip();
        
        // Listen for Bootstrap tab shown event
        $('button[data-bs-target="#nav-reporting-configuration"]').on('shown.bs.tab', function (e) {
            handleTabVisibilityChange();
        });
        
        // Alternative: Listen for tab clicks
        $('button[data-bs-target="#nav-reporting-configuration"]').on('click', function (e) {
            setTimeout(() => {
                handleTabVisibilityChange();
            }, 150);
        });
        
        // Also handle window resize events
        $(window).on('resize', function() {
            if (isReportingTabVisible()) {
                adjustTableLayout();
                // Add column adjustment on resize
                setTimeout(function() {
                    forceTableColumnAdjustment();
                }, 100);
            }
        });
        
        // Add intersection observer for visibility detection (fallback)
        if (window.IntersectionObserver) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && entry.target.id === 'nav-reporting-configuration') {
                        setTimeout(() => {
                            handleTabVisibilityChange();
                        }, 50);
                    }
                });
            });
            
            const reportingPanel = document.querySelector('#nav-reporting-configuration');
            if (reportingPanel) {
                observer.observe(reportingPanel);
            }
        }
        
        // Additional fallback: MutationObserver to detect class changes on the tab panel
        if (window.MutationObserver) {
            const tabObserver = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                        const target = mutation.target;
                        if (target.id === 'nav-reporting-configuration' && 
                            target.classList.contains('show', 'active')) {
                            setTimeout(() => {
                                handleTabVisibilityChange();
                            }, 100);
                        }
                    }
                });
            });
            
            const reportingPanel = document.querySelector('#nav-reporting-configuration');
            if (reportingPanel) {
                tabObserver.observe(reportingPanel, {
                    attributes: true,
                    attributeFilter: ['class']
                });
            }
        }
    });

    // Track changes in the data grid
    function markAsChanged() {
        hasUnsavedChanges = true;
        updateUndoButtonVisibility();
    }

    // Function to update undo button visibility and text based on changes
    function updateUndoButtonVisibility() {
        const $undoBtn = $('#btn-undo-report-configuration');
        
        if (hasUnsavedChanges) {
            // Show button with Cancel text but keep the undo icon
            $undoBtn.show();
            
            // Update text while preserving the icon
            const $icon = $undoBtn.find('i');
            if ($icon.length > 0) {
                // If icon exists, replace text but keep icon
                $undoBtn.html($icon.prop('outerHTML') + ' Cancel');
            } else {
                // If no icon, add one with the text
                $undoBtn.html('<i class="fl fl-undo"></i> Cancel');
            }
        } else {
            $undoBtn.hide();
        }
    }

    // Reset changes state and update UI
    function resetChangesState() {
        hasUnsavedChanges = false;
        updateUndoButtonVisibility();
    }

    // Get current table data including edits
    function getCurrentTableData() {
        const rows = [];
        dataTable.rows().every(function () {
            const data = this.data();
            const node = this.node();
            const input = $(node).find('.column-name-input');
            const columnName = input.length > 0 ? input.val().trim() : (data.columnName || '').trim();           

            rows.push({
                propertyName: data.key,
                columnName: columnName,
                path: data.path
            });
        });
        return rows;
    }

    // Save report configuration with validation
    $('#btn-save-report-configuration').on('click', function () {
        const versionId = $('#versionSelector').val();        

        // Validate all column names before saving
        const validation = validateAllColumnNames();
        if (!validation.isValid) {
            const errorMessage = 'Please fix the following validation errors before saving:\n\n' + 
                               validation.errors.join('\n');
            abp.message.error(errorMessage);
            return;
        }
        
        // Disable all control buttons during save
        setControlButtonsLoadingState(true);

        const configData = {
            correlationId: versionId,
            correlationProvider: getCorrelationProvider(),
            mapping: {
                rows: getCurrentTableData()
            }
        };

        // Check if configuration exists to determine whether to create or update
        abp.ajax({
            url: abp.appPath + 'ReportingConfiguration/Exists',
            type: 'GET',
            data: { 
                correlationId: versionId, 
                correlationProvider: getCorrelationProvider()
            }
        }).then(function (exists) {
            if (exists) {
                // Update existing configuration
                return abp.ajax({
                    url: abp.appPath + 'ReportingConfiguration/Update',
                    type: 'PUT',
                    data: JSON.stringify(configData),
                    contentType: 'application/json'
                });
            } else {
                // Create new configuration
                return abp.ajax({
                    url: abp.appPath + 'ReportingConfiguration/Create',
                    type: 'POST',
                    data: JSON.stringify(configData),
                    contentType: 'application/json'
                });
            }
        }).then(function (result) {
            abp.message.success('Configuration saved successfully');
            resetChangesState();
            
            // Show the Generate View button since we now have a saved configuration
            updateGenerateViewButtonVisibility(true);
            
            // Reload the DataTable to get the saved state
            if (dataTable) {
                dataTable.ajax.reload();
            }
            
            // Refresh view status widget to get updated status
            refreshViewStatusWidget(versionId, getCorrelationProvider());
            
        }).catch(function (error) {            
            let errorMessage = 'Failed to save configuration';
            if (error?.responseJSON?.error?.message) {
                errorMessage = error.responseJSON.error.message;
            }
            abp.message.error(errorMessage);
        }).always(function() {
            // Re-enable all control buttons after save completes (success or error)
            setControlButtonsLoadingState(false);
        });
    });

    // Function to set loading state for all control buttons
    function setControlButtonsLoadingState(isLoading) {
        const $controlButtons = $('#btn-save-report-configuration, #btn-generate-view-report-configuration, #btn-undo-report-configuration, #btn-back-report-configuration, #versionSelector');
        
        if (isLoading) {
            // Disable buttons and show loading state
            $controlButtons.prop('disabled', true);
            
            // Update save button text and icon to show saving state
            const $saveBtn = $('#btn-save-report-configuration');
            $saveBtn.data('original-html', $saveBtn.html()); // Store original HTML instead of just text
            
            // Set loading state with spinner icon and text
            $saveBtn.html('<i class="fa fa-spinner fa-spin"></i> Saving...');
        } else {
            // Re-enable buttons and restore normal state
            $controlButtons.prop('disabled', false);
            
            // Restore save button original content
            const $saveBtn = $('#btn-save-report-configuration');
            const originalHtml = $saveBtn.data('original-html');
            if (originalHtml) {
                $saveBtn.html(originalHtml);
            }
        }
    }

    // Cancel/Undo changes
    $('#btn-undo-report-configuration').on('click', function () {
        if (hasUnsavedChanges) {
            // Cancel changes without confirmation
            resetChangesState();
            // Reload the DataTable to get the original state
            if (dataTable) {
                dataTable.ajax.reload();
            }
        }
    });

    // Back button
    $('#btn-back-report-configuration').on('click', function () {
        if (hasUnsavedChanges) {
            if (!confirm('You have unsaved changes. Do you want to leave without saving?')) {
                return;
            }
        }

        // Navigate back
        window.history.back();
    });

    // Generate view button
    $('#btn-generate-view-report-configuration').on('click', function () {
        const versionId = $('#versionSelector').val();

        if (!versionId) {
            abp.message.error('Please select a version first');
            return;
        }

        // Reset modal state
        const modal = new bootstrap.Modal(document.getElementById('generateViewModal'));
        const $viewNameInput = $('#viewNameInput');
        const $confirmButton = $('#confirmGenerateView');
        const $feedback = $('#viewNameFeedback');
        
        // Clear previous state
        $viewNameInput.val('').removeClass('is-valid is-invalid');
        $confirmButton.prop('disabled', true);
        $feedback.text('');
        
        // Check if there's an existing configuration with a view name
        checkForExistingViewName(versionId, function(existingViewName) {
            if (existingViewName) {
                // Populate the existing view name
                $viewNameInput.val(existingViewName);
                
                // Store the original view name for comparison
                $viewNameInput.data('original-view-name', existingViewName);
                
                // Validate the existing name to enable the button
                checkViewNameAvailability(existingViewName, function(isValid, errors) {
                    if (isValid) {
                        $viewNameInput.addClass('is-valid');
                        $confirmButton.prop('disabled', false);
                    } else {
                        $viewNameInput.addClass('is-invalid');
                        $feedback.text(errors.join(', '));
                        $confirmButton.prop('disabled', true);
                    }
                });
            } else {
                // No existing view name, clear the stored original
                $viewNameInput.removeData('original-view-name');
            }
        });
        
        // Show modal
        modal.show();
    });

    // Function to check for existing view name in configuration
    function checkForExistingViewName(versionId, callback) {
        abp.ajax({
            url: abp.appPath + 'ReportingConfiguration/Exists',
            type: 'GET',
            data: { 
                correlationId: versionId, 
                correlationProvider: getCorrelationProvider()
            }
        }).then(function(exists) {
            if (exists) {
                // Configuration exists - get the view name
                return abp.ajax({
                    url: abp.appPath + 'ReportingConfiguration/GetConfiguration',
                    type: 'GET',
                    data: { 
                        correlationId: versionId, 
                        correlationProvider: getCorrelationProvider()
                    }
                }).then(function(result) {
                    callback(result.viewName || '');
                });
            } else {
                callback('');
            }
        }).catch(function(error) {
            console.error('Error checking for existing view name:', error);
            callback('');
        });
    }

    // Handle view name input validation in modal
    $(document).on('input', '#viewNameInput', function() {
        const $input = $(this);
        const rawValue = $input.val();
        const $confirmButton = $('#confirmGenerateView');
        const $feedback = $('#viewNameFeedback');
        const originalViewName = $input.data('original-view-name');
        
        // Sanitize the view name (lowercase, trim, replace invalid chars)
        const viewName = sanitizeViewName(rawValue);
        
        // Update the input with sanitized value if different
        if (viewName !== rawValue) {
            const cursorPosition = $input[0].selectionStart;
            $input.val(viewName);
            $input[0].setSelectionRange(cursorPosition, cursorPosition);
        }
        
        // Reset state
        $input.removeClass('is-valid is-invalid');
        $confirmButton.prop('disabled', true);
        $feedback.text('').removeClass('text-warning text-danger');
        
        if (!viewName.trim()) {
            return;
        }
        
        // Check if user is changing an existing view name
        if (originalViewName && viewName !== originalViewName.toLowerCase() && originalViewName.trim() !== '') {
            // Show warning about deleting existing view
            $feedback.html(`
                <div class="alert alert-warning mt-2 mb-0 p-2">
                    <i class="fa fa-exclamation-triangle me-2"></i>
                    <strong>Warning:</strong> Changing the view name will <strong>delete the existing view "${originalViewName}"</strong> and create a new view "${viewName}". 
                    <br><small>This action cannot be undone. Are you sure you want to proceed?</small>
                </div>
            `);
        }
        
        // Check availability with debouncing
        checkViewNameAvailability(viewName, function(isValid, errors) {
            if (isValid) {
                $input.addClass('is-valid');
                $confirmButton.prop('disabled', false);
            } else {
                $input.addClass('is-invalid');
                // Clear warning and show validation errors
                if (!originalViewName || viewName === originalViewName.toLowerCase() || originalViewName.trim() === '') {
                    $feedback.html('');
                }
                $feedback.append(`<div class="invalid-feedback d-block">${errors.join(', ')}</div>`);
                $confirmButton.prop('disabled', true);
            }
        });
    });

    // Handle view generation confirmation
    $(document).on('click', '#confirmGenerateView', function() {
        const versionId = $('#versionSelector').val();
        const rawViewName = $('#viewNameInput').val().trim();
        const viewName = sanitizeViewName(rawViewName); // Ensure it's sanitized
        const $button = $(this);
        const $cancelButton = $('.modal-footer .btn-secondary');
        
        if (!viewName || !versionId) {
            return;
        }
        
        // Disable modal buttons and show loading state
        $button.prop('disabled', true).text('Generating...');
        $cancelButton.prop('disabled', true);
        
        // Also disable main control buttons during view generation
        setControlButtonsLoadingState(true);
        
        // Call the GenerateView endpoint
        $.ajax({
            url: abp.appPath + 'ReportingConfiguration/GenerateView',
            type: 'POST',
            data: JSON.stringify({
                correlationId: versionId,
                correlationProvider: getCorrelationProvider(),
                viewName: viewName
            }),
            contentType: 'application/json',
            dataType: 'json', // Expect JSON response
            success: function(result, textStatus, xhr) {
                // Handle successful response (both 200 OK and 202 Accepted)
                if (xhr.status === 202 || xhr.status === 200) {
                    let message = 'Database view generation has been initiated successfully';
                    
                    // Use the structured response if available
                    if (result?.message) {
                        message = result.message;
                    }                   
                    
                    abp.message.success(message);
                } else {
                    // Fallback for other success status codes
                    abp.message.success('Database view generation has been initiated successfully');
                }
                
                // Refresh view status widget to show generating status
                refreshViewStatusWidget(versionId, getCorrelationProvider());
                
                // Publish event to start auto-refresh polling
                PubSub.publish('view_generation_started', { versionId: versionId });
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('generateViewModal'));
                modal.hide();
            },
            error: function(xhr, status, error) {
                console.error('Error generating view - Status:', status);
                console.error('Error generating view - Error:', error);
                console.error('Error generating view - XHR:', xhr);
                
                let errorMessage = 'Failed to generate database view';
                
                // Handle different error scenarios
                if (xhr.status === 0) {
                    errorMessage = 'Network error: Could not connect to the server';
                } else if (xhr.status === 400) {
                    // Try to extract detailed error message from response
                    try {
                        const errorResponse = JSON.parse(xhr.responseText);
                        if (errorResponse?.error?.message) {
                            errorMessage = errorResponse.error.message;
                        } else if (typeof errorResponse === 'string') {
                            errorMessage = errorResponse;
                        } else {
                            errorMessage = 'Bad request: Please check your input and try again';
                        }
                    } catch (parseError) {
                        console.error(parseError);
                        errorMessage = xhr.responseText || 'Bad request: Please check your input and try again';
                    }
                } else if (xhr.status === 401) {
                    errorMessage = 'Unauthorized: Please log in and try again';
                } else if (xhr.status === 403) {
                    errorMessage = 'Forbidden: You do not have permission to perform this action';
                } else if (xhr.status === 404) {
                    errorMessage = 'Configuration not found: Please ensure the configuration exists and try again';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error: Please try again later or contact support';
                } else if (xhr.responseText) {
                    try {
                        const parsedError = JSON.parse(xhr.responseText);
                        if (parsedError?.error?.message) {
                            errorMessage = parsedError.error.message;
                        } else if (parsedError.message) {
                            errorMessage = parsedError.message;
                        } else {
                            errorMessage = xhr.responseText;
                        }
                    } catch (parseError) {
                        console.error('Failed to parse error response:', parseError);
                        errorMessage = xhr.responseText;
                    }
                } else if (error && error !== 'parsererror') {
                    errorMessage = error;
                }
                
                abp.message.error(errorMessage);
            },
            complete: function() {
                // Reset modal button states
                $button.prop('disabled', false).text('Generate View');
                $cancelButton.prop('disabled', false);
                
                // Re-enable main control buttons
                setControlButtonsLoadingState(false);
            }
        });
    });

    // Subscribe to PubSub events to refresh the view status widget when needed
    PubSub.subscribe('refresh_view_status', function (msg, data) {
        const versionId = data?.versionId || $('#versionSelector').val();
        if (versionId) {
            refreshViewStatusWidget(versionId, data?.provider || getCorrelationProvider());
        }
    });

    // View name validation function
    function validateViewName(viewName) {
        const errors = [];
        
        if (!viewName || viewName.trim() === '') {
            errors.push('View name is required');
            return { isValid: false, errors: errors };
        }
        
        // Always convert to lowercase and trim
        const name = viewName.trim().toLowerCase();
        
        // Check length
        if (name.length < VIEW_NAME_VALIDATION.MIN_LENGTH) {
            errors.push(`View name must be at least ${VIEW_NAME_VALIDATION.MIN_LENGTH} character long`);
        }
        
        if (name.length > VIEW_NAME_VALIDATION.MAX_LENGTH) {
            errors.push(`View name exceeds maximum length of ${VIEW_NAME_VALIDATION.MAX_LENGTH} characters`);
        }
        
        // Check format (should be alphanumeric + underscores, not starting with number)
        if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/i.test(name)) {
            errors.push('View name must start with a letter or underscore and contain only letters, numbers, and underscores');
        }
        
        // Check reserved words
        if (VIEW_NAME_VALIDATION.RESERVED_WORDS.includes(name.toLowerCase())) {
            errors.push('View name is a PostgreSQL reserved word');
        }
        
        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    // Debounced function for checking view name availability
    let viewNameCheckTimeout;
    function checkViewNameAvailability(viewName, callback) {
        clearTimeout(viewNameCheckTimeout);
        viewNameCheckTimeout = setTimeout(function() {
            if (!viewName || viewName.trim() === '') {
                callback(true, []); // Empty name is valid but not available
                return;
            }
            
            // First validate the format
            const validation = validateViewName(viewName);
            if (!validation.isValid) {
                callback(false, validation.errors);
                return;
            }
            
            const versionId = $('#versionSelector').val();
            
            // Check availability with server, including correlation parameters
            abp.ajax({
                url: abp.appPath + 'ReportingConfiguration/IsViewNameAvailable',
                type: 'GET',
                data: { 
                    viewName: viewName,
                    correlationId: versionId,
                    correlationProvider: getCorrelationProvider()
                }
            }).done(function(isAvailable) {
                if (!isAvailable) {
                    callback(false, ['View name is already in use by another reporting configuration']);
                } else {
                    callback(true, []);
                }
            }).fail(function(error) {
                console.error('Error checking view name availability:', error);
                callback(false, ['Error checking view name availability']);
            });
        }, 500); // 500ms debounce
    }

    // Function to sanitize view name
    function sanitizeViewName(viewName) {
        if (!viewName || typeof viewName !== 'string') return '';
        
        let sanitized = viewName.toLowerCase().trim();
        
        // Replace spaces and hyphens with underscores
        sanitized = sanitized.replace(/[\s\-]+/g, '_');
        
        // Remove all non-alphanumeric characters except underscores
        sanitized = sanitized.replace(/[^a-z0-9_]/g, '');
        
        // Remove leading/trailing underscores
        sanitized = sanitized.replace(/^_+|_+$/g, '');
        
        // If starts with number, prefix with 'v_'
        if (sanitized && /^[0-9]/.test(sanitized)) {
            sanitized = 'v_' + sanitized;
        }
        
        // If empty after sanitization, return empty string
        if (!sanitized) {
            return '';
        }
        
        // Truncate to max length
        if (sanitized.length > VIEW_NAME_VALIDATION.MAX_LENGTH) {
            sanitized = sanitized.substring(0, VIEW_NAME_VALIDATION.MAX_LENGTH);
            // Remove trailing underscore if truncation created one
            sanitized = sanitized.replace(/_+$/, '');
        }
        
        return sanitized;
    }

    // Delete configuration button
    $('#btn-delete-report-configuration').on('click', function () {
        const versionId = $('#versionSelector').val();

        if (!versionId) {
            abp.message.error('Please select a version first');
            return;
        }

        // Show delete confirmation modal
        const modal = new bootstrap.Modal(document.getElementById('deleteConfigurationModal'));
        modal.show();
    });

    // Handle delete confirmation
    $(document).on('click', '#confirmDeleteConfiguration', function() {
        const versionId = $('#versionSelector').val();
        const deleteView = $('#deleteViewCheckbox').is(':checked');
        const correlationProvider = getCorrelationProvider();
        const $button = $(this);
        const $cancelButton = $('.modal-footer .btn-secondary');
        
        if (!versionId) {
            return;
        }
        
        // Disable modal buttons and show loading state
        $button.prop('disabled', true).text('Deleting...');
        $cancelButton.prop('disabled', true);
        
        // Also disable main control buttons during deletion
        setControlButtonsLoadingState(true);
        
        // Call the Delete endpoint
        abp.ajax({
            url: abp.appPath + 'ReportingConfiguration/Delete',
            type: 'DELETE',
            data: JSON.stringify({
                correlationId: versionId,
                correlationProvider: correlationProvider,
                deleteView: deleteView
            }),
        }).done(function(result) {
            let message = result?.message || 'Configuration deleted successfully';
            abp.message.success(message);
            
            // Hide the delete and generate view buttons since configuration no longer exists
            updateGenerateViewButtonVisibility(false);
            updateDeleteButtonVisibility(false);
            
            // Reload the DataTable to get the fields metadata (since no configuration exists)
            if (dataTable) {
                dataTable.ajax.reload();
            }
            
            // Refresh view status widget
            refreshViewStatusWidget(versionId, getCorrelationProvider());
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('deleteConfigurationModal'));
            modal.hide();
            
        }).fail(function(xhr, status, error) {
            console.error('Error deleting configuration - Status:', status);
            console.error('Error deleting configuration - Error:', error);
            console.error('Error deleting configuration - XHR:', xhr);
            
            let errorMessage = 'Failed to delete configuration';
            
            // Handle different error scenarios
            if (xhr.status === 0) {
                errorMessage = 'Network error: Could not connect to the server';
            } else if (xhr.status === 400) {
                // Try to extract detailed error message from response
                try {
                    const errorResponse = JSON.parse(xhr.responseText);
                    if (errorResponse?.error?.message) {
                        errorMessage = errorResponse.error.message;
                    } else if (typeof errorResponse === 'string') {
                        errorMessage = errorResponse;
                    } else {
                        errorMessage = 'Bad request: Please check your input and try again';
                    }
                } catch (parseError) {
                    console.error(parseError);
                    errorMessage = xhr.responseText || 'Bad request: Please check your input and try again';
                }
            } else if (xhr.status === 401) {
                errorMessage = 'Unauthorized: Please log in and try again';
            } else if (xhr.status === 403) {
                errorMessage = 'Forbidden: You do not have permission to perform this action';
            } else if (xhr.status === 404) {
                errorMessage = 'Configuration not found: The configuration may have already been deleted';
            } else if (xhr.status === 500) {
                errorMessage = 'Server error: Please try again later or contact support';
            } else if (xhr.responseText) {
                try {
                    const parsedError = JSON.parse(xhr.responseText);
                    if (parsedError?.error?.message) {
                        errorMessage = parsedError.error.message;
                    } else if (parsedError.message) {
                        errorMessage = parsedError.message;
                    } else {
                        errorMessage = xhr.responseText;
                    }
                } catch (parseError) {
                    console.error('Failed to parse error response:', parseError);
                    errorMessage = xhr.responseText;
                }
            } else if (error && error !== 'parsererror') {
                errorMessage = error;
            }
            
            abp.message.error(errorMessage);
        }).always(function() {
            // Reset modal button states
            $button.prop('disabled', false).text('Delete Configuration');
            $cancelButton.prop('disabled', false);
            
            // Re-enable main control buttons
            setControlButtonsLoadingState(false);
        });
    });

    // Update the function to handle both generate and delete button visibility
    // This function was modified earlier to support both buttons
    function updateButtonsVisibility(hasConfiguration) {
        updateGenerateViewButtonVisibility(hasConfiguration);
        updateDeleteButtonVisibility(hasConfiguration);
    }

    // Update existing function calls to use the new combined function
    function updateCheckConfigurationExistsCallbacks() {
        // Update all places where checkConfigurationExists is called
        const $versionSelector = $('#versionSelector');
        
        // Handle version selector change
        $versionSelector.off('change.deleteButton').on('change.deleteButton', function () {
            const newVersionId = $(this).val();
            
            // Check if configuration exists for the new version and current provider
            checkConfigurationExists(newVersionId, function(exists) {
                updateButtonsVisibility(exists);
            });
        });

        // Handle provider change
        $(document).off('change.deleteButton', 'input[name="provider-toggle"]').on('change.deleteButton', 'input[name="provider-toggle"]', function() {
            const versionId = $versionSelector.val();
            if (versionId) {
                checkConfigurationExists(versionId, function(exists) {
                    updateButtonsVisibility(exists);
                });
            } else {
                updateButtonsVisibility(false);
            }
        });

        // Update initial check
        const initialVersionId = $versionSelector.val();
        if (initialVersionId) {
            checkConfigurationExists(initialVersionId, function(exists) {
                updateButtonsVisibility(exists);
            });
        } else {
            updateButtonsVisibility(false);
        }
    }

    // Initialize the delete button functionality
    updateCheckConfigurationExistsCallbacks();
});