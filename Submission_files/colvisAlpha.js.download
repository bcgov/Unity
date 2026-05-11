/*!
 * Column visibility button with alphabetical sorting for Buttons and DataTables.
 * Unlike the standard 'colvis' button, this maintains alphabetical order
 * of column names and does not reorder when columns are reordered in the table.
 * 
 * Usage:
 * buttons: [
 *     {
 *         extend: 'colvisAlpha',
 *         text: 'Columns',
 *         columns: ':not(.notexport)'
 *     }
 * ]
 */

$.extend(DataTable.ext.buttons, {
    /**
     * Collection button that shows column visibility toggles in alphabetical order.
     * Does not rebuild on column reorder events, maintaining consistent alphabetical sorting.
     */
    colvisAlpha: function (dt, conf) {
        let buttonConf = {
            extend: 'collection',
            text: function (dt) {
                return conf.text || dt.i18n('buttons.colvis', 'Column visibility');
            },
            className: conf.className || 'buttons-colvisAlpha',
            closeButton: false,
            buttons: function () {
                // Get all column indices matching the selector
                let columns = dt.columns(conf.columns).indexes().toArray();
                
                // Build array of {index, title} objects for sorting
                let columnData = columns.map(function (idx) {
                    let column = dt.column(idx);
                    let title = column.header().textContent;
                    
                    // Clean up title text
                    title = title
                        .replaceAll('\n', ' ')
                        .replaceAll(/<br\s*\/?>/gi, ' ')
                        .replaceAll(/<select[^>]*>.*?<\/select>/gi, '')
                        .trim();
                    
                    // Strip HTML comments if available
                    title = DataTable.Buttons?.stripHtmlComments?.(title) || title;
                    
                    // Strip HTML if DataTable utility is available
                    title = DataTable.util?.stripHtml?.(title)?.trim() || title;
                    
                    // Apply custom columnText function if provided
                    title = conf.columnText?.(dt, idx, title) || title;
                    
                    return {
                        index: idx,
                        title: title
                    };
                });
                
                // Sort alphabetically by title (case-insensitive)
                columnData.sort(function (a, b) {
                    return a.title.toLowerCase().localeCompare(b.title.toLowerCase());
                });
                
                // Build button configurations in alphabetical order
                return columnData.map(function (col) {
                    return {
                        extend: 'columnVisibilityAlpha',
                        columns: col.index,
                        text: col.title,
                        _originalIndex: col.index
                    };
                });
            }
        };

        return buttonConf;
    },

    /**
     * Single button to toggle column visibility (alpha variant).
     * This is used internally by colvisAlpha. It tracks columns by their original index
     * to maintain correct functionality after ColReorder changes column positions.
     */
    columnVisibilityAlpha: {
        columns: undefined,
        text: function (dt, button, conf) {
            return conf.text || dt.column(conf.columns).title();
        },
        className: 'buttons-columnVisibility',
        action: function (e, dt, button, conf) {
            // Get the current column index - may have changed due to ColReorder
            let currentIdx = conf._currentColumnIndex === undefined ? conf.columns : conf._currentColumnIndex;
            let col = dt.columns(currentIdx);
            let curr = col.visible();

            let currentVisibility = curr.length ? curr[0] : false;
            let newVisibility = conf.visibility === undefined ? !currentVisibility : conf.visibility;

            col.visible(newVisibility);
        },
        init: function (dt, button, conf) {
            // Store the original column index for tracking across reorders
            conf._originalColumnIndex = conf.columns;
            conf._currentColumnIndex = conf.columns;
            
            let column = dt.column(conf.columns);

            button.attr('data-cv-idx', conf.columns);

            // Listen to visibility changes to update button state
            dt.on('column-visibility.dt' + conf.namespace, (e, settings, index, state) => {
                if (
                    conf._currentColumnIndex === index &&
                    !settings.bDestroying &&
                    settings.nTable == dt.settings()[0].nTable
                ) {
                    this.active(state);
                }
            });

            // Listen to column-reorder events to update the current column index
            // This ensures the button continues to target the correct column after reorder
            dt.on('column-reorder.dt' + conf.namespace, () => {
                // Button has been removed from the DOM
                if (conf.destroying) {
                    return;
                }

                // Use ColReorder's transpose to find where our original column now lives
                if (dt.colReorder?.transpose) {
                    conf._currentColumnIndex = dt.colReorder.transpose(conf._originalColumnIndex, 'toCurrent');
                }
                
                // Update the column reference and check visibility
                column = dt.column(conf._currentColumnIndex);

                // Update button active state based on current visibility
                this.active(column.visible());
            });

            this.active(column.visible());
        },
        destroy: function (dt, button, conf) {
            dt.off('column-visibility.dt' + conf.namespace).off(
                'column-reorder.dt' + conf.namespace
            );
        }
    },

    /**
     * Restore original column visibility (alpha variant).
     * Works with colvisAlpha to restore default visibility state.
     */
    colvisRestoreAlpha: {
        className: 'buttons-colvisRestore',
        text: function (dt) {
            return dt.i18n('buttons.colvisRestore', 'Restore visibility');
        },
        init: function (dt, button, conf) {
            // Store original visibility state on each column
            dt.columns().every(function () {
                let init = this.init();
                if (init.__visOriginal === undefined) {
                    init.__visOriginal = this.visible();
                }
            });
        },
        action: function (e, dt, button, conf) {
            dt.columns().every(function (i) {
                let init = this.init();
                this.visible(init.__visOriginal);
            });
        }
    }
});