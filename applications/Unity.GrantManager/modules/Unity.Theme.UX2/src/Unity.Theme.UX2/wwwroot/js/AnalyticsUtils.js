/**
 * Analytics utility functions for Grant Manager application
 */
const AnalyticsUtils = (function () {
    'use strict';

    /**
     * Initialises Matomo analytics tracking.
     * Disables tracking cookies (GDPR), sets the authenticated user ID when
     * available, sets custom dimensions, then queues a page view and loads
     * the Matomo script async.
     *
     * @param {string} url - The base URL of the Matomo instance (no trailing slash).
     * @param {string|number} siteId - The Matomo site ID.
     */
    function initMatomo(url, siteId) {
        console.debug('[Analytics] initMatomo called — url:', url, '| siteId:', siteId);

        if (!url || !siteId) {
            console.warn('[Analytics] initMatomo aborted: url or siteId is missing.');
            return;
        }

        var _paq = window._paq = window._paq || [];

        // GDPR: avoid storing tracking cookies
        _paq.push(['disableCookies']);

        // IMPORTANT:
        // Tracker URL + Site ID should be set before tracking calls
        _paq.push(['setTrackerUrl', url + '/matomo.php']);
        _paq.push(['setSiteId', String(siteId)]);

        // Identify authenticated user if available
        if (window.abp && abp.currentUser && abp.currentUser.id) {
            _paq.push(['setUserId', abp.currentUser.id]);
        }

        /**
         * Custom Dimensions
         * Dimension 1 = Tenant Name
         * Dimension 2 = User Name
         *
         * These MUST be set BEFORE trackPageView()
         */

        // Safe tenant lookup
        var tenantName =
            window.abp &&
            abp.currentTenant &&
            abp.currentTenant.name
                ? abp.currentTenant.name
                : null;

        if (tenantName) {
            _paq.push(['setCustomDimension', 1, tenantName]);
        } else {
            console.warn('[Analytics] tenantName is not defined. Custom Dimension 1 not set.');
        }

        // Safe user name lookup
        var firstName =
            window.abp &&
            abp.currentUser &&
            abp.currentUser.name
                ? abp.currentUser.name
                : '';

        var lastName =
            window.abp &&
            abp.currentUser &&
            (
                abp.currentUser.surname ||
                abp.currentUser.surName ||
                ''
            );

        var userName = (firstName + ' ' + lastName).trim();

        if (userName) {
            _paq.push(['setCustomDimension', 2, userName]);
        } else {
            console.warn('[Analytics] userName is not defined. Custom Dimension 2 not set.');
        }

        /**
         * Track initial page view
         * MUST happen after custom dimensions are set
         */
        _paq.push(['trackPageView']);

        // Track outbound links / downloads
        _paq.push(['enableLinkTracking']);

        /**
         * Load Matomo JS tracker
         */
        var g = document.createElement('script');
        g.async = true;
        g.src = url + '/matomo.js';

        g.onerror = function () {
            console.error('[Analytics] Failed to load matomo.js from:', g.src);
        };

        document.head.appendChild(g);
    }

    /**
     * Tracks a virtual page view for AJAX-driven navigation
     * without a full page reload.
     *
     * If your dimensions are Action-scoped in Matomo,
     * set them again here before trackPageView().
     *
     * @param {string} [url] - Virtual URL (defaults to current URL)
     * @param {string} [title] - Virtual page title (defaults to document.title)
     */
    function trackVirtualPageView(url, title) {
        var _paq = window._paq;
        if (!_paq) return;

        // Optional: reset dimensions here if using Action scope
        var tenantName =
            window.abp &&
            abp.currentTenant &&
            abp.currentTenant.name
                ? abp.currentTenant.name
                : null;

        if (tenantName) {
            _paq.push(['setCustomDimension', 1, tenantName]);
        }

        var firstName =
            window.abp &&
            abp.currentUser &&
            abp.currentUser.name
                ? abp.currentUser.name
                : '';

        var lastName =
            window.abp &&
            abp.currentUser &&
            (
                abp.currentUser.surname ||
                abp.currentUser.surName ||
                ''
            );

        var userName = (firstName + ' ' + lastName).trim();

        if (userName) {
            _paq.push(['setCustomDimension', 2, userName]);
        }

        _paq.push(['setCustomUrl', url || window.location.href]);
        _paq.push(['setDocumentTitle', title || document.title]);
        _paq.push(['trackPageView']);
    }

    /**
     * Tracks internal site search
     *
     * @param {string} keyword - Search term entered by the user
     * @param {string|false} [category] - Optional category
     * @param {number|false} [resultCount] - Optional result count
     */
    function trackSearch(keyword, category, resultCount) {
        var _paq = window._paq;

        if (!_paq || !keyword) {
            return;
        }

        _paq.push([
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
        initMatomo: initMatomo,
        trackVirtualPageView: trackVirtualPageView,
        trackSearch: trackSearch
    };
})();