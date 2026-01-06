/**
 * Date utility functions for Grant Manager application
 */
var DateUtils = (function () {
    'use strict';

    /**
     * Formats a UTC date string to local date format
     * @param {string|Date} dateUtc - The UTC date to format
     * @param {string} type - The type of formatting (for DataTables compatibility)
     * @param {object} options - Additional formatting options
     * @returns {string|number} Formatted date string or timestamp for sorting
     */
    function formatUtcDateToLocal(dateUtc, type, options) {
        if (!dateUtc) {
            return '';
        }

        const date = new Date(dateUtc);

        // Required for DataTables sorting & filtering
        if (type === 'sort' || type === 'type') {
            return date.getTime();
        }

        return date.toLocaleDateString(
            abp.localization.currentCulture.name,
            {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                ...options
            }
        );
    }

    // Public API
    return {
        formatUtcDateToLocal: formatUtcDateToLocal
    };
})();