
/**
 * Date utility functions for Grant Manager application
 *
 * BC TIMEZONE NOTES (2026):
 *   - Most of BC, including Vancouver and Victoria, now uses
 *     permanent Pacific Time (PCT, UTC-7).
 *   - Clocks will NOT fall back on November 1, 2026.
 *   - Pacific formatting below uses a fixed UTC-7 offset.
 *   - Southeastern BC (e.g. Cranbrook, Fernie, Golden) continues
 *     to observe Mountain Standard/Daylight Time.
 *   - Northeastern BC (e.g. Fort St. John, Dawson Creek) stays
 *     at UTC-7 year-round.
 *
 * IMPORTANT:
 *   Fixed UTC-7 is appropriate for current/future PCT dates.
 *   Do not use it to format historical Pacific dates that occurred
 *   under the previous seasonal PST/PDT rules.
 */
const DateUtils = (function () {
    'use strict';

    // Etc/GMT signs are reversed: GMT+7 means UTC-7.
    const BC_PACIFIC_TIME_ZONE = 'Etc/GMT+7';
    const BC_PACIFIC_OFFSET_MS = -7 * 60 * 60 * 1000;
    const BC_PACIFIC_OFFSET = '-07:00';
    const BC_PACIFIC_LABEL = 'PCT';

    // Used for southeastern BC locations that still follow Alberta's
    // seasonal Mountain Time rules.
    const BC_SOUTHEAST_MOUNTAIN_TIME_ZONE = 'America/Edmonton';

    function isInvalidDate(date) {
        return Number.isNaN(date.getTime());
    }

    function getTimestamp(dateUtc) {
        if (!dateUtc) return 0;

        const date = new Date(dateUtc);
        return isInvalidDate(date) ? 0 : date.getTime();
    }

    /**
     * Formats a UTC date using the browser's local timezone.
     *
     * NOTE:
     * This intentionally uses the browser's timezone.
     * Use the BC-specific functions for BC business dates.
     */
    function formatUtcDateToLocal(dateUtc, type, options) {
        if (type === 'sort' || type === 'type') {
            return String(getTimestamp(dateUtc));
        }

        if (!dateUtc) return '';

        const date = new Date(dateUtc);
        if (isInvalidDate(date)) return '';

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
     * Formats a UTC timestamp as a BC Pacific date (date only).
     *
     * Uses fixed UTC-7 (PCT).
     * Intended for current/future dates under BC's new time standard.
     */
    function formatUtcToBcPacificDate(dateUtc, type, options) {
        if (type === 'sort' || type === 'type') {
            return String(getTimestamp(dateUtc));
        }

        if (!dateUtc) return '';

        const date = new Date(dateUtc);
        if (isInvalidDate(date)) return '';

        return date.toLocaleDateString(
            abp.localization.currentCulture.name,
            {
                timeZone: BC_PACIFIC_TIME_ZONE,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                ...options
            }
        );
    }

    /**
     * Formats a UTC timestamp as BC Pacific date and time.
     *
     * Displays PCT (UTC-7), not PST/PDT.
     * Intended for current/future dates under the new BC time standard.
     */
    function formatUtcToBcPacificDateTime(dateUtc, type) {
        if (type === 'sort' || type === 'type') {
            return String(getTimestamp(dateUtc));
        }

        if (!dateUtc) return '';

        const date = new Date(dateUtc);
        if (isInvalidDate(date)) return '';

        const formatted = date.toLocaleString(
            abp.localization.currentCulture.name,
            {
                timeZone: BC_PACIFIC_TIME_ZONE,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: 'numeric',
                minute: '2-digit'
            }
        );

        return `${formatted} ${BC_PACIFIC_LABEL}`;
    }

    /**
     * Formats a UTC timestamp for southeastern BC Mountain Time.
     *
     * Locations include Cranbrook, Fernie, Golden and Invermere.
     * These areas continue to follow seasonal Mountain Time rules.
     *
     * Do not use this function for northeastern BC locations such
     * as Fort St. John or Dawson Creek, which remain UTC-7 year-round.
     */
    function formatUtcToBcMountainDateTime(dateUtc, type) {
        if (type === 'sort' || type === 'type') {
            return String(getTimestamp(dateUtc));
        }

        if (!dateUtc) return '';

        const date = new Date(dateUtc);
        if (isInvalidDate(date)) return '';

        return date.toLocaleString(
            abp.localization.currentCulture.name,
            {
                timeZone: BC_SOUTHEAST_MOUNTAIN_TIME_ZONE,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: 'numeric',
                minute: '2-digit',
                timeZoneName: 'short'
            }
        );
    }

    /**
     * Converts a datetime-local input (YYYY-MM-DDTHH:mm) to UTC ISO.
     *
     * Treats the entered time as BC Pacific Time at UTC-7.
     * This avoids interpreting the value using the browser's timezone.
     *
     * IMPORTANT:
     * Use for dates under the new permanent PCT rule.
     * This fixed-offset conversion does not handle historical dates
     * or validate whether the entered date is otherwise meaningful.
     */
    function bcPstInputToUtcIso(localDatetimeString) {
        if (!localDatetimeString) return '';

        const match = localDatetimeString.match(
            /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/
        );

        if (!match) return '';

        const [, year, month, day, hour, minute, second = '0'] = match;

        const components = [
            Number(year),
            Number(month),
            Number(day),
            Number(hour),
            Number(minute),
            Number(second)
        ];

        const [y, mo, d, h, mi, s] = components;

        // Validate the calendar components rather than allowing Date
        // to silently normalize invalid values such as February 30.
        const check = new Date(Date.UTC(y, mo - 1, d, h, mi, s));

        if (
            check.getUTCFullYear() !== y ||
            check.getUTCMonth() !== mo - 1 ||
            check.getUTCDate() !== d ||
            check.getUTCHours() !== h ||
            check.getUTCMinutes() !== mi ||
            check.getUTCSeconds() !== s
        ) {
            return '';
        }

        // Explicit UTC-7 offset: never use the browser-local offset.
        const date = new Date(
            `${localDatetimeString}${BC_PACIFIC_OFFSET}`
        );

        return isInvalidDate(date) ? '' : date.toISOString();
    }

    /**
     * Converts UTC milliseconds to a datetime-local string
     * (YYYY-MM-DDTHH:mm) in BC Pacific Time (UTC-7).
     *
     * Intended for current/future dates under permanent PCT.
     */
    function utcMsToBcPstDatetimeLocal(utcMs) {
        if (utcMs == null || !Number.isFinite(Number(utcMs))) {
            return '';
        }

        const date = new Date(Number(utcMs));

        if (isInvalidDate(date)) return '';

        // Shift the instant by -7 hours, then use UTC getters
        // to produce the corresponding PCT wall-clock time.
        const shifted = new Date(
            date.getTime() + BC_PACIFIC_OFFSET_MS
        );

        const pad = n => String(n).padStart(2, '0');

        return (
            `${shifted.getUTCFullYear()}-` +
            `${pad(shifted.getUTCMonth() + 1)}-` +
            `${pad(shifted.getUTCDate())}T` +
            `${pad(shifted.getUTCHours())}:` +
            `${pad(shifted.getUTCMinutes())}`
        );
    }

    /**
     * Formats a date-only field without browser-local timezone
     * conversion.
     *
     * Use for fields such as Due Date, Decision Date and Project
     * Start/End Date when the stored calendar date must be preserved.
     */
    function formatDate(dateUtc, type) {
        if (!dateUtc) return '';

        if (type === 'sort' || type === 'type') {
            return getTimestamp(dateUtc);
        }

        try {
            const parsed = luxon.DateTime.fromISO(dateUtc, {
                locale: abp.localization.currentCulture.name
            });

            if (!parsed.isValid) return dateUtc;

            return parsed.toUTC().toLocaleString();
        } catch (e) {
            console.warn('Date parse error:', e);
            return dateUtc;
        }
    }

    // Public API.
    //
    // The original names are retained to avoid breaking existing callers.
    // Consider renaming the two PST-named functions to PCT in a future
    // cleanup to reflect the new permanent BC time standard.
    return {
        formatUtcDateToLocal: formatUtcDateToLocal,
        formatUtcToBcPacificDate: formatUtcToBcPacificDate,
        formatUtcToBcPacificDateTime: formatUtcToBcPacificDateTime,
        formatUtcToBcMountainDateTime: formatUtcToBcMountainDateTime,
        bcPstInputToUtcIso: bcPstInputToUtcIso,
        utcMsToBcPstDatetimeLocal: utcMsToBcPstDatetimeLocal,
        formatDate: formatDate
    };
})();
