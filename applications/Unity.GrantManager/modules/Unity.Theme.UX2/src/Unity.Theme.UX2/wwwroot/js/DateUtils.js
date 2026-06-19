/**
 * Date utility functions for Grant Manager application
 */
const DateUtils = (function () {
    'use strict';

    /**
     * Formats a UTC date string to local date format
     * @param {string|Date} dateUtc - The UTC date to format
     * @param {string} type - The type of formatting (for DataTables compatibility)
     * @param {object} options - Additional formatting options
     * @returns {string|number|null} Formatted date string or timestamp for sorting, null if input is invalid
     */
    function formatUtcDateToLocal(dateUtc, type, options) {
        if (!dateUtc) {
            return null;
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

    /**
     * Formats a date-only UTC string without local timezone conversion.
     * Use this for date-only fields (Due Date, Decision Date, Project Start/End Date)
     * where the stored date should be displayed as-is regardless of browser timezone.
     * @param {string} dateUtc - The UTC date string to format
     * @param {string} type - The type of formatting (for DataTables compatibility)
     * @returns {string|number|null} Formatted date string or timestamp for sorting, empty string if input is invalid
     */
    function formatDate(dateUtc, type) {
        if (!dateUtc) return '';
        if (type === 'sort' || type === 'type') {
            return new Date(dateUtc).getTime();
        }
        try {
            return luxon.DateTime.fromISO(dateUtc, {
                locale: abp.localization.currentCulture.name,
            }).toUTC().toLocaleString();
        } catch (e) {
            console.warn('Date parse error:', e);
            return dateUtc;
        }
    }

    // Public API
    return {
        formatUtcDateToLocal: formatUtcDateToLocal,
        formatDate: formatDate
    };
})();