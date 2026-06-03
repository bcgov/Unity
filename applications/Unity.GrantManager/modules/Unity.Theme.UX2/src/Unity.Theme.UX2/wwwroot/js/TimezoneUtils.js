/**
 * TimezoneUtils.js
 *
 * Utility functions for handling timezone-related operations, such as setting a cookie with the user's timezone offset.
 */
const TimezoneUtils = (function () {
    'use strict';

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/;SameSite=Lax;secure;";
    }

    function setTimezoneCookie() {
        const cookieName = "timezoneoffset";
        const currentOffset = new Date().getTimezoneOffset();

        const existing = document.cookie
            .split('; ')
            .find(row => row.startsWith(cookieName + '='))
            ?.split('=')[1];

        if (existing === undefined || Number.parseInt(existing, 10) !== currentOffset) {
            setCookie(cookieName, currentOffset, 1);
            return true;
        }
        return false;
    }
    
    return {
        setTimezoneCookie: setTimezoneCookie
    };
})();