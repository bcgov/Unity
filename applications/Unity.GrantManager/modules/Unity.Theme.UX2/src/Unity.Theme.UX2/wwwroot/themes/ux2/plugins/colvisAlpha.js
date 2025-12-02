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
                        .replace(/\n/g, ' ')
                        .replace(/<br\s*\/?>/gi, ' ')
                        .replace(/<select[^>]*>.*?<\/select>/gi, '')
                        .trim();
                    
                    // Strip HTML comments if available
                    if (DataTable.Buttons && DataTable.Buttons.stripHtmlComments) {
                        title = DataTable.Buttons.stripHtmlComments(title);
                    }
                    
                    // Strip HTML if DataTable utility is available
                    if (DataTable.util && DataTable.util.stripHtml) {
                        title = DataTable.util.stripHtml(title).trim();
                    }
                    
                    // Apply custom columnText function if provided
                    if (conf.columnText) {
                        title = conf.columnText(dt, idx, title);
                    }
                    
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
     * This is used internally by colvisAlpha and does not respond to column reorder events.
     */
    columnVisibilityAlpha: {
        columns: undefined,
        text: function (dt, button, conf) {
            return conf.text || dt.column(conf.columns).title();
        },
        className: 'buttons-columnVisibility',
        action: function (e, dt, button, conf) {
            let col = dt.columns(conf.columns);
            let curr = col.visible();

            col.visible(
                conf.visibility !== undefined ? conf.visibility : !(curr.length ? curr[0] : false)
            );
        },
        init: function (dt, button, conf) {
            let that = this;
            let column = dt.column(conf.columns);

            button.attr('data-cv-idx', conf.columns);

            // Listen to visibility changes to update button state
            dt.on('column-visibility.dt' + conf.namespace, function (e, settings, index, state) {
                if (
                    column.index() === index &&
                    !settings.bDestroying &&
                    settings.nTable == dt.settings()[0].nTable
                ) {
                    that.active(state);
                }
            });

            // Do NOT listen to column-reorder events - maintain original column reference
            // This is the key difference from standard columnVisibility

            this.active(column.visible());
        },
        destroy: function (dt, button, conf) {
            dt.off('column-visibility.dt' + conf.namespace);
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