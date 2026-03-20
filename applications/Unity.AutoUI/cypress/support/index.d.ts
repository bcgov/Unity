// ***********************************************************
// This /support/index.d.ts file is processed and
// loaded automatically before test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

/**
 * Grant Application interface matching actual API response
 * from /api/app/grant-application endpoint
 */
interface GrantApplication {
  id: string;
  /** The submission reference number displayed in UI (e.g., "209BD469") */
  referenceNo: string;
  /** Application status (e.g., "Submitted", "Approved", "Under Assessment", "Closed", "Deferred") */
  status: string;
  /** ISO date string of submission */
  submissionDate: string;
  /** Project name */
  projectName: string;
  /** Organization name */
  organizationName: string;
  /** Requested funding amount */
  requestedAmount: number;
  /** Approved funding amount */
  approvedAmount: number;
  /** Category/program name */
  category: string;
  /** City */
  city: string;
  /** Additional fields from API */
  [key: string]: unknown;
}

/**
 * Options for fetching dynamic submissions
 */
interface FetchSubmissionOptions {
  /** Filter by status values (e.g., ['Submitted', 'Under Assessment']) */
  statusFilter?: string[];
  /** Filter by category/program name (e.g., 'Data Seeder') */
  categoryFilter?: string;
  /** Max age in days (default: no limit) */
  maxAge?: number;
  /** Sort by field (default: 'submissionDate') */
  sortBy?: 'submissionDate' | 'requestedAmount' | 'approvedAmount';
  /** Sort order (default: 'desc' for latest first) */
  sortOrder?: 'asc' | 'desc';
  /** Which submission to return after sorting (default: 0 = first/latest) */
  index?: number;
}

declare namespace Cypress {
  interface Chainable {
    /** Custom command to login to Unity */
    login(): void;

    /** Custom command to log out of Unity */
    logout(): void;

    /** Custom command to get submission details by key for the current environment */
    getSubmissionDetail(key: string): Chainable<string>;

    /** Custom command to get metabase details by key for the current environment */
    getMetabaseDetail(key: string): Chainable<string>;

    /** Custom command to login to Metabase */
    metabaseLogin(): Chainable<void>;

    /** Custom command to get chefs details by key for the current environment */
    getChefsDetail(key: string): Chainable<string>;

    /** Custom command to login to Chefs */
    chefsLogin(): Chainable<void>;

    /** Custom command to log out of Chefs */
    chefsLogout(): Chainable<void>;

    /** Custom command to clear session storage */
    clearSessionStorage(): Chainable<void>;

    /** Custom command to clear browser cache */
    clearBrowserCache(): Chainable<void>;

    /**
     * Fetches a dynamic submission ID from the API after login.
     * Uses session cookies automatically from Cypress.
     *
     * @param options - Optional filters for selecting submissions
     * @returns Chainable containing the confirmation ID
     *
     * @example
     * // Get first available submission
     * cy.fetchDynamicSubmission().then((id) => { ... })
     *
     * @example
     * // Get second submission with specific status
     * cy.fetchDynamicSubmission({ statusFilter: ['SUBMITTED'], index: 1 }).then((id) => { ... })
     */
    fetchDynamicSubmission(options?: FetchSubmissionOptions): Chainable<string>;

    /**
     * Fetches all available submissions from the API.
     * Useful for selecting a specific submission based on custom criteria.
     *
     * @returns Chainable containing array of grant applications
     */
    fetchAllSubmissions(): Chainable<GrantApplication[]>;
  }
}
