/**
 * Analytics utility functions for Grant Manager application
 */
const AnalyticsUtils = (function () {
    'use strict';

    const DIMENSION_TENANT_NAME = 1;
    const DIMENSION_USER_NAME = 2;

    /**
     * Safely gets the current tenant name from ABP.
     *
     * @returns {string|null} Tenant name or null if unavailable.
     */
    function getTenantName() {
        return globalThis.abp &&
            abp.currentTenant &&
            abp.currentTenant.name
            ? abp.currentTenant.name
            : null;
    }

    /**
     * Safely gets the current authenticated user ID from ABP.
     *
     * @returns {string|null} User ID or null if unavailable.
     */
    function getUserId() {
        return globalThis.abp &&
            abp.currentUser &&
            abp.currentUser.id
            ? String(abp.currentUser.id)
            : null;
    }

    /**
     * Safely builds the current user's full name from ABP.
     *
     * @returns {string|null} Full user name or null if unavailable.
     */
    function getUserName() {
        const firstName = globalThis.abp &&
            abp.currentUser &&
            abp.currentUser.name
            ? abp.currentUser.name
            : '';

        const lastName = globalThis.abp &&
            abp.currentUser &&
            (
                abp.currentUser.surname ||
                abp.currentUser.surName ||
                ''
            );

        const fullName = `${firstName} ${lastName}`.trim();

        return fullName || null;
    }

    /**
     * Applies common custom dimensions for tenant and user.
     *
     * @param {Array} trackerQueue - Matomo tracker queue.
     */
    function applyCustomDimensions(trackerQueue) {
        const tenantName = getTenantName();
        const userName = getUserName();

        if (tenantName) {
            trackerQueue.push([
                'setCustomDimension',
                DIMENSION_TENANT_NAME,
                tenantName
            ]);
        } else {
            console.warn(
                '[Analytics] Tenant name unavailable. Custom Dimension 1 not set.'
            );
        }

        if (userName) {
            trackerQueue.push([
                'setCustomDimension',
                DIMENSION_USER_NAME,
                userName
            ]);
        } else {
            console.warn(
                '[Analytics] User name unavailable. Custom Dimension 2 not set.'
            );
        }
    }

    /**
     * Loads the Matomo tracking script asynchronously.
     *
     * @param {string} url - Base Matomo URL.
     */
    function loadMatomoScript(url) {
        const script = document.createElement('script');

        script.async = true;
        script.src = `${url}/matomo.js`;

        script.onerror = function () {
            console.error(
                '[Analytics] Failed to load matomo.js from:',
                script.src
            );
        };

        document.head.appendChild(script);
    }

    /**
     * Initialises Matomo analytics tracking.
     * Disables cookies (GDPR), sets user identity,
     * applies custom dimensions, tracks initial page view,
     * and enables link tracking.
     *
     * @param {string} url - Base Matomo URL (no trailing slash).
     * @param {string|number} siteId - Matomo site ID.
     */
    function initMatomo(url, siteId) {
        console.debug(
            '[Analytics] initMatomo called — url:',
            url,
            '| siteId:',
            siteId
        );

        if (!url || !siteId) {
            console.warn(
                '[Analytics] initMatomo aborted: url or siteId is missing.'
            );
            return;
        }

        const trackerQueue = globalThis._paq = globalThis._paq || [];

        trackerQueue.push(
            ['disableCookies'],
            ['setTrackerUrl', `${url}/matomo.php`],
            ['setSiteId', String(siteId)]
        );

        const userId = getUserId();

        if (userId) {
            trackerQueue.push(['setUserId', userId]);
        }

        applyCustomDimensions(trackerQueue);

        /**
         * Track initial page view after dimensions are set.
         */
        trackerQueue.push(['trackPageView']);

        /**
         * Track outbound links and downloads.
         */
        trackerQueue.push(['enableLinkTracking']);

        loadMatomoScript(url);
    }

    /**
     * Tracks a virtual page view for AJAX-driven navigation
     * without a full page reload.
     *
     * Re-applies dimensions for Action-scoped dimensions.
     *
     * @param {string} [url] - Virtual URL.
     * @param {string} [title] - Virtual page title.
     */
    function trackVirtualPageView(url, title) {
        const trackerQueue = globalThis._paq;

        if (!trackerQueue) {
            return;
        }

        applyCustomDimensions(trackerQueue);

        trackerQueue.push(
            ['setCustomUrl', url || globalThis.location.href],
            ['setDocumentTitle', title || document.title],
            ['trackPageView']
        );
    }

    /**
     * Tracks internal site search.
     *
     * @param {string} keyword - Search term entered by the user.
     * @param {string|false} [category] - Optional search category.
     * @param {number|false} [resultCount] - Optional result count.
     */
    function trackSearch(keyword, category, resultCount) {
        const trackerQueue = globalThis._paq;

        if (!trackerQueue || !keyword) {
            return;
        }

        trackerQueue.push([
            'trackSiteSearch',
            keyword,
            typeof category === 'string' ? category : false,
            typeof resultCount === 'number' ? resultCount : false
        ]);
    }

    /**
     * Public API
     */
    return {
        initMatomo,
        trackVirtualPageView,
        trackSearch
    };
})();