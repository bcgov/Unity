/**
 * Date utility functions for Grant Manager application
 *
 * BC TIMEZONE NOTES (2026):
 *   - BC Pacific zones (Vancouver, Victoria, etc.) do NOT observe DST in 2026.
 *     They remain on PST (UTC-8) year-round. Use formatUtcToBcPacificDateTime / bcPstInputToUtcIso.
 *   - BC Mountain zones (Peace River / NE BC) DO observe DST: MST (UTC-7) in winter,
 *     MDT (UTC-6) in summer. Use formatUtcToBcMountainDateTime for those.
 */
const DateUtils = (function () {
    'use strict';

    // BC PST is a fixed UTC-8 offset -- no DST in 2026.
    const BC_PST_OFFSET_MS = -8 * 60 * 60 * 1000;

    /**
     * Formats a UTC date string to the browser's local date format.
     * NOTE: Uses the browser's system timezone. For BC PST (no DST) use formatUtcToBcPacificDateTime.
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
     * Formats a UTC date/time string as a BC Pacific date (date only).
     * BC PST is fixed at UTC-8 -- no DST adjustment in 2026.
     * @param {string|Date} dateUtc
     * @param {string} type - DataTables type ('sort'|'type' returns numeric timestamp)
     * @param {object} options - Additional Intl.DateTimeFormat options
     */
    function formatUtcToBcPacificDate(dateUtc, type, options) {
        if (type === 'sort' || type === 'type') {
            return dateUtc ? String(new Date(dateUtc).getTime()) : '0';
        }
        if (!dateUtc) return '';
        return new Date(dateUtc).toLocaleDateString(abp.localization.currentCulture.name, {
            timeZone: 'Etc/GMT+8',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            ...options
        });
    }

    /**
     * Formats a UTC date/time string as a BC Pacific date+time string with "PST" label.
     * BC PST is fixed at UTC-8 -- no DST adjustment in 2026.
     * @param {string|Date} dateUtc
     * @param {string} type - DataTables type ('sort'|'type' returns numeric timestamp)
     */
    function formatUtcToBcPacificDateTime(dateUtc, type) {
        if (type === 'sort' || type === 'type') {
            return dateUtc ? String(new Date(dateUtc).getTime()) : '0';
        }
        if (!dateUtc) return '';
        const formatted = new Date(dateUtc).toLocaleString(abp.localization.currentCulture.name, {
            timeZone: 'Etc/GMT+8',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: 'numeric',
            minute: '2-digit'
        });
        return formatted + ' PST';
    }

    /**
     * Formats a UTC date/time string for the BC Mountain timezone (Peace River / NE BC).
     * Mountain Time observes DST in 2026: MST (UTC-7) in winter, MDT (UTC-6) in summer.
     * @param {string|Date} dateUtc
     * @param {string} type - DataTables type ('sort'|'type' returns numeric timestamp)
     */
    function formatUtcToBcMountainDateTime(dateUtc, type) {
        if (type === 'sort' || type === 'type') {
            return dateUtc ? String(new Date(dateUtc).getTime()) : '0';
        }
        if (!dateUtc) return '';
        // America/Edmonton follows MST/MDT -- DST applies in NE BC.
        return new Date(dateUtc).toLocaleString(abp.localization.currentCulture.name, {
            timeZone: 'America/Edmonton',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: 'numeric',
            minute: '2-digit',
            timeZoneName: 'short'
        });
    }

    /**
     * Converts a datetime-local input string (YYYY-MM-DDTHH:mm) to a UTC ISO string,
     * treating the input as BC Pacific Standard Time (fixed UTC-8, no DST in 2026).
     * Use this instead of new Date(localString).toISOString() which relies on the
     * browser's potentially incorrect DST-adjusted timezone offset.
     * @param {string} localDatetimeString - Value from a datetime-local input
     * @returns {string|null} UTC ISO 8601 string, or null if input is empty/invalid
     */
    function bcPstInputToUtcIso(localDatetimeString) {
        if (!localDatetimeString) return null;
        // Append the BC PST fixed offset so Date parses it as UTC-8, not browser-local.
        const withOffset = localDatetimeString + '-08:00';
        const date = new Date(withOffset);
        return Number.isNaN(date.getTime()) ? null : date.toISOString();
    }

    /**
     * Converts a UTC timestamp (ms since epoch) to a datetime-local string (YYYY-MM-DDTHH:mm)
     * in BC Pacific Standard Time (fixed UTC-8, no DST in 2026).
     * Use this to populate datetime-local inputs with the correct BC PST time.
     * @param {number} utcMs - Milliseconds since Unix epoch
     * @returns {string} datetime-local string in BC PST
     */
    function utcMsToBcPstDatetimeLocal(utcMs) {
        // Shift UTC ms by -8h to get BC PST, then read UTC getters (which now represent PST).
        const shifted = new Date(utcMs + BC_PST_OFFSET_MS);
        const pad = n => String(n).padStart(2, '0');
        return `${shifted.getUTCFullYear()}-${pad(shifted.getUTCMonth() + 1)}-${pad(shifted.getUTCDate())}` +
               `T${pad(shifted.getUTCHours())}:${pad(shifted.getUTCMinutes())}`;
    }

    // Public API
    return {
        formatUtcDateToLocal: formatUtcDateToLocal,
        formatUtcToBcPacificDate: formatUtcToBcPacificDate,
        formatUtcToBcPacificDateTime: formatUtcToBcPacificDateTime,
        formatUtcToBcMountainDateTime: formatUtcToBcMountainDateTime,
        bcPstInputToUtcIso: bcPstInputToUtcIso,
        utcMsToBcPstDatetimeLocal: utcMsToBcPstDatetimeLocal
    };
})();
