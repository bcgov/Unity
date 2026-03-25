// ***********************************************
// This commands.ts file is used to
// create custom commands and overwrite
// existing commands.
//
// For comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************

Cypress.Commands.add("login", () => {
  // 1.) Load Main Page
  cy.visit(Cypress.env("webapp.url"));
  // 2.) Login to the Default Grant Tenant
  cy.contains("LOGIN").should("exist").click();
  cy.wait(1000);
  cy.contains("IDIR").should("exist").click();
  cy.wait(1000);
  cy.get("body").then(($body) => {
    // Check if you're already logged in.
    if ($body.find("#user").length) {
      // If #user exists, perform the login steps
      cy.get("#user").type(Cypress.env("test1username"));
      cy.get("#password").type(Cypress.env("test1password"));
      cy.contains("Continue").should("exist").click();
    } else {
      // If #user does not exist, log a message and proceed
      cy.log("Already logged in");
    }
  });
});

Cypress.Commands.add("logout", () => {
  cy.log("🚪 Logging out");

  // Send logout request FIRST (while cookies are present to invalidate server session)
  cy.request({
    method: "GET",
    url: Cypress.env("webapp.url") + "Account/Logout",
    failOnStatusCode: false,
  });

  // THEN clear client-side storage
  cy.clearCookies();
  cy.clearLocalStorage();

  // Visit and verify logout
  cy.visit(Cypress.env("webapp.url"));
  cy.get('button:contains("LOGIN")', { timeout: 10000 }).should("be.visible");

  cy.log("✓ Logged out");
});

/// <reference types="cypress" />

interface SubmissionDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add("getSubmissionDetail", (key: string) => {
  return cy
    .fixture<{ submissionDetails: SubmissionDetail[] }>("submissions.json")
    .then(({ submissionDetails }) => {
      const environment = Cypress.env("environment")?.toUpperCase();
      const submissionDetail = submissionDetails.find(
        (detail) => detail.unityEnv.toUpperCase() === environment,
      );

      if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
        return submissionDetail[key];
      } else {
        throw new Error(
          `No submission detail found for environment: ${environment} and key: ${key}`,
        );
      }
    });
});

interface MetabaseDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add("getMetabaseDetail", (key: string) => {
  return cy
    .fixture<{ metabaseDetails: MetabaseDetail[] }>("metabase.json")
    .then(({ metabaseDetails }) => {
      const environment = Cypress.env("environment")?.toUpperCase();
      const submissionDetail = metabaseDetails.find(
        (detail) => detail.unityEnv.toUpperCase() === environment,
      );

      if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
        return submissionDetail[key];
      } else {
        throw new Error(
          `No submission detail found for environment: ${environment} and key: ${key}`,
        );
      }
    });
});

Cypress.Commands.add("metabaseLogin", () => {
  cy.getMetabaseDetail("baseURL").then((baseURL) => {
    cy.visit(baseURL);

    // Target the username field using its `name` attribute
    cy.get('input[name="username"]')
      .should("exist")
      .click()
      .type("iDontHave@ValidEmail.com"); // Placeholder email address

    // Target the password field using its `name` attribute
    cy.get('input[name="password"]').should("exist").click().type("pointless"); // Placeholder password
  });
});

interface chefsDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add("getChefsDetail", (key: string) => {
  return cy
    .fixture<{ chefsDetails: chefsDetail[] }>("chefs.json")
    .then(({ chefsDetails }) => {
      const environment = Cypress.env("environment")?.toUpperCase();
      const submissionDetail = chefsDetails.find(
        (detail) => detail.unityEnv.toUpperCase() === environment,
      );

      if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
        return submissionDetail[key];
      } else {
        throw new Error(
          `No submission detail found for environment: ${environment} and key: ${key}`,
        );
      }
    });
});

Cypress.Commands.add("chefsLogin", () => {
  cy.getChefsDetail("chefsBaseURL").then((baseURL) => {
    cy.visit(baseURL); // Visit the URL fetched from chefs.json
    cy.get("#app > div > main > header > header > div > div.d-print-none")
      .should("exist")
      .click(); // click the login button
    cy.wait(1000);
    cy.get(
      "#app > div > main > div.v-container.v-locale--is-ltr.text-center.main > div > div:nth-child(2) > div > button",
    )
      .should("exist")
      .click(); // click the idir buttton
    cy.wait(1000);
    cy.get("body").then(($body) => {
      // Check if you're already logged in.
      if ($body.find("#user").length) {
        // If #user exists, perform the login steps
        cy.get("#user").type(Cypress.env("test1username"));
        cy.get("#password").type(Cypress.env("test1password"));
        cy.contains("Continue").should("exist").click();
      } else {
        // If #user does not exist, log a message and proceed
        cy.log("Already logged in");
      }
    });
    cy.wait(1000);
  });
});

Cypress.Commands.add("chefsLogout", () => {
  cy.getChefsDetail("chefsBaseURL").then((baseURL) => {
    cy.visit(baseURL);
  }); // Load Main Page
  cy.wait(1000);
  cy.contains("Logout").should("exist").click(); // Logout
  cy.wait(1000);
  cy.contains("Login").should("exist");
});

Cypress.Commands.add("clearSessionStorage", () => {
  cy.window().then((window) => {
    window.sessionStorage.clear();
  });
});

Cypress.Commands.add("clearBrowserCache", () => {
  cy.window().then((win) => {
    win.caches.keys().then((keyList) => {
      return Promise.all(
        keyList.map((key) => {
          return win.caches.delete(key);
        }),
      );
    });
  });
});

// ============ Dynamic Submission Fetching ============

// Use interfaces from index.d.ts - only define API response wrapper here
interface GrantApplicationResponse {
  items: GrantApplication[];
  totalCount: number;
}

/**
 * Fetches a dynamic submission ID (referenceNo) from the API after login.
 * Uses session cookies automatically from Cypress.
 * Results are sorted by submissionDate descending (latest first) by default.
 *
 * @param options - Optional filters for selecting submissions
 * @returns Chainable containing the referenceNo (e.g., "209BD469")
 *
 * @example
 * // Get latest submission from "Data Seeder" category
 * cy.fetchDynamicSubmission({ categoryFilter: 'Data Seeder' }).then((id) => { ... })
 *
 * @example
 * // Get latest "Submitted" submission
 * cy.fetchDynamicSubmission({ statusFilter: ['Submitted'] }).then((id) => { ... })
 *
 * @example
 * // Get second-latest submission from specific category
 * cy.fetchDynamicSubmission({ categoryFilter: 'Data Seeder', index: 1 }).then((id) => { ... })
 *
 * Available status values: 'Submitted', 'Under Assessment', 'Approved', 'Closed', 'Deferred'
 */
function fetchGrantApplications(): Cypress.Chainable<GrantApplication[]> {
  const apiUrl = `${Cypress.env("webapp.url")}api/app/grant-application`;
  return cy.getCookie("XSRF-TOKEN").then((xsrfCookie) => {
    return cy
      .request({
        method: "GET",
        url: apiUrl,
        qs: { submittedFromDate: "", submittedToDate: "" },
        headers: {
          Accept: "application/json, text/javascript, */*; q=0.01",
          "Content-Type": "application/json",
          "X-Requested-With": "XMLHttpRequest",
          RequestVerificationToken: xsrfCookie?.value || "",
        },
        failOnStatusCode: false,
      })
      .then((response) => {
        if (response.status !== 200) {
          throw new Error(
            `API request failed with status ${response.status}: ${JSON.stringify(response.body)}`
          );
        }
        const data = response.body as GrantApplicationResponse;
        Cypress.log({ name: "fetch", message: `📋 Fetched ${data.items?.length || 0} applications` });
        return data.items || [];
      });
  });
}

Cypress.Commands.add(
  "fetchDynamicSubmission",
  (options: FetchSubmissionOptions = {}) => {
    return fetchGrantApplications().then((allApplications) => {
      let applications = allApplications;

          Cypress.log({ name: "fetch", message: `📋 Fetched ${applications.length} applications from API` });

          // Filter by category if specified (e.g., 'Data Seeder')
          if (options.categoryFilter) {
            applications = applications.filter((app) =>
              app.category === options.categoryFilter
            );
            Cypress.log({
              name: "filter",
              message: `📋 Filtered to ${applications.length} applications with category: ${options.categoryFilter}`,
            });
          }

          // Filter by status if specified (e.g., 'Submitted', 'Under Assessment', 'Approved')
          if (options.statusFilter && options.statusFilter.length > 0) {
            applications = applications.filter((app) =>
              options.statusFilter!.includes(app.status)
            );
            Cypress.log({
              name: "filter",
              message: `📋 Filtered to ${applications.length} applications with status: ${options.statusFilter.join(", ")}`,
            });
          }

          // Filter by max age if specified
          if (options.maxAge) {
            const cutoffDate = new Date();
            cutoffDate.setDate(cutoffDate.getDate() - options.maxAge);
            applications = applications.filter((app) => {
              const submissionDate = new Date(app.submissionDate);
              return submissionDate >= cutoffDate;
            });
            Cypress.log({
              name: "filter",
              message: `📋 Filtered to ${applications.length} applications within ${options.maxAge} days`,
            });
          }

          if (applications.length === 0) {
            throw new Error(
              "No applications found matching the specified criteria"
            );
          }

          // Sort applications (default: by submissionDate descending for latest first)
          const sortBy = options.sortBy || 'submissionDate';
          const sortOrder = options.sortOrder || 'desc';
          applications.sort((a, b) => {
            let aVal: number | string;
            let bVal: number | string;

            if (sortBy === 'submissionDate') {
              aVal = new Date(a.submissionDate).getTime();
              bVal = new Date(b.submissionDate).getTime();
            } else {
              aVal = a[sortBy] as number;
              bVal = b[sortBy] as number;
            }

            if (sortOrder === 'desc') {
              return bVal > aVal ? 1 : bVal < aVal ? -1 : 0;
            } else {
              return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
            }
          });

          // Get the submission at the specified index (default: 0 = first/latest)
          const index = options.index || 0;
          if (index >= applications.length) {
            throw new Error(
              `Index ${index} out of range. Only ${applications.length} applications available.`
            );
          }

          const selectedApp = applications[index];
          Cypress.log({
            name: "selected",
            message: `✅ Selected submission: ${selectedApp.referenceNo} (Status: ${selectedApp.status}, Category: ${selectedApp.category})`,
          });

          return selectedApp.referenceNo;
        });
  }
);

/**
 * Fetches all available submissions from the API.
 * Useful for selecting a specific submission based on custom criteria.
 *
 * @returns Chainable containing array of grant applications
 */
Cypress.Commands.add("fetchAllSubmissions", () => {
  return fetchGrantApplications();
});
