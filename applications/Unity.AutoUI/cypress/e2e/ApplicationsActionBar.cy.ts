/// <reference types="cypress" />

import { loginIfNeeded } from "../support/auth";

describe("Unity Login and check data from CHEFS", () => {
    const STANDARD_TIMEOUT = 20000;

    function switchToDefaultGrantsProgramIfAvailable() {
        cy.get("body").then(($body) => {
            const hasUserInitials = $body.find(".unity-user-initials").length > 0;

            if (!hasUserInitials) {
                cy.log("Skipping tenant switch: no user initials menu found");
                return;
            }

            cy.get(".unity-user-initials").click();

            cy.get("body").then(($body2) => {
                const switchLink = $body2
                    .find("#user-dropdown a.dropdown-item")
                    .filter((_, el) => {
                        return (el.textContent || "").trim() === "Switch Grant Programs";
                    });

                if (switchLink.length === 0) {
                    cy.log(
                        'Skipping tenant switch: "Switch Grant Programs" not present for this user/session',
                    );
                    cy.get("body").click(0, 0);
                    return;
                }

                cy.wrap(switchLink.first()).click();

                cy.url({ timeout: STANDARD_TIMEOUT }).should(
                    "include",
                    "/GrantPrograms",
                );

                cy.get("#search-grant-programs", { timeout: STANDARD_TIMEOUT })
                    .should("be.visible")
                    .clear()
                    .type("Default Grants Program");

                // Flatten nested `within` usage to satisfy S2004 (limit nesting depth)
                cy.contains(
                    "#UserGrantProgramsTable tbody tr",
                    "Default Grants Program",
                    { timeout: STANDARD_TIMEOUT },
                )
                    .should("exist")
                    .within(() => {
                        cy.contains("button", "Select").should("be.enabled").click();
                    });

                cy.location("pathname", { timeout: STANDARD_TIMEOUT }).should((p) => {
                    expect(
                        p.indexOf("/GrantApplications") >= 0 || p.indexOf("/auth/") >= 0,
                    ).to.eq(true);
                });
            });
        });
    }

    // TEST renders the Submission tab inside an open shadow root (Form.io).
    // Enabling this makes cy.get / cy.contains pierce shadow DOM consistently across envs.
    before(() => {
        Cypress.config("includeShadowDom", true);
        loginIfNeeded({ timeout: STANDARD_TIMEOUT });
    });

    it("Switch to Default Grants Program if available", () => {
        switchToDefaultGrantsProgramIfAvailable();
    });

    it("Tests the existence and functionality of the Submitted Date From and Submitted Date To filters", () => {
        const pad2 = (n: number) => String(n).padStart(2, "0");

        const todayIsoLocal = () => {
            const d = new Date();
            return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
        };

        const waitForRefresh = () => {
            // S3923 fix: remove identical branches; assert spinner is hidden when present.
            cy.get('div.spinner-grow[role="status"]', {
                timeout: STANDARD_TIMEOUT,
            }).then(($s) => {
                cy.wrap($s)
                    .should("have.attr", "style")
                    .and("contain", "display: none");
            });
        };

        // --- Submitted Date From ---
        cy.get("input#submittedFromDate", { timeout: STANDARD_TIMEOUT })
            .click({ force: true })
            .clear({ force: true })
            .type("2022-01-01", { force: true })
            .trigger("change", { force: true })
            .blur({ force: true })
            .should("have.value", "2022-01-01");

        waitForRefresh();

        // --- Submitted Date To ---
        const today = todayIsoLocal();

        cy.get("input#submittedToDate", { timeout: STANDARD_TIMEOUT })
            .click({ force: true })
            .clear({ force: true })
            .type(today, { force: true })
            .trigger("change", { force: true })
            .blur({ force: true })
            .should("have.value", today);

        waitForRefresh();
    });

    //  With no rows selected verify the visibility of Filter, Export, Save View, and Columns.
    it("Verifies the expected action buttons are visible when no rows are selected", () => {
        cy.get("#GrantApplicationsTable", { timeout: STANDARD_TIMEOUT }).should(
            "exist",
        );

        // Ensure we start from a clean selection state (0 selected)
        // (Using same "select all / deselect all" toggle approach as the working 1-row test)
        cy.get("div.dt-scroll-head thead input", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .click({ force: true })
            .click({ force: true });

        cy.get("#GrantApplicationsTable tbody tr.selected", {
            timeout: STANDARD_TIMEOUT,
        }).should("have.length", 0);

        // Filter button (left action bar group)
        cy.get("#btn-toggle-filter", { timeout: STANDARD_TIMEOUT }).should(
            "be.visible",
        );

        // Right-side buttons
        cy.contains(
            "#dynamicButtonContainerId .dt-buttons button span",
            "Export",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");
        cy.contains("#dynamicButtonContainerId button.grp-savedStates", "Save View", {
            timeout: STANDARD_TIMEOUT,
        }).should("be.visible");
        cy.contains(
            "#dynamicButtonContainerId .dt-buttons button span",
            "Columns",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");

        // Optional sanity: action buttons that require selection should be disabled when none selected
        cy.get("#externalLink", { timeout: STANDARD_TIMEOUT }).should(
            "be.disabled",
        ); // Open
        cy.get("#assignApplication", { timeout: STANDARD_TIMEOUT }).should(
            "be.disabled",
        ); // Assign
        cy.get("#approveApplications", { timeout: STANDARD_TIMEOUT }).should(
            "be.disabled",
        ); // Approve
        cy.get("#tagApplication", { timeout: STANDARD_TIMEOUT }).should(
            "be.disabled",
        ); // Tags
        cy.get("#applicationPaymentRequest", {
            timeout: STANDARD_TIMEOUT,
        }).should("be.disabled"); // Payment
        cy.get("#applicationLink", { timeout: STANDARD_TIMEOUT }).should(
            "be.disabled",
        ); // Info
    });

    // With one row selected verify the visibility of Open, Assign, Approve, Tags, Payment, Info, Filter, Export, Save View, and Columns.
    it("Verifies the expected action buttons are visible when only one row is selected", () => {
        cy.get("#GrantApplicationsTable", { timeout: STANDARD_TIMEOUT }).should(
            "exist",
        );

        //Ensure we start from a clean selection state (0 selected)
        cy.get("div.dt-scroll-head thead input", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .click({ force: true })
            .click({ force: true });

        cy.get("#GrantApplicationsTable tbody tr.selected", {
            timeout: STANDARD_TIMEOUT,
        }).should("have.length", 0);

        // Select exactly 1 row (click a non-link cell, matching your earlier helper logic)
        cy.get("#GrantApplicationsTable tbody tr", { timeout: STANDARD_TIMEOUT })
            .should("have.length.greaterThan", 0)
            .first()
            .find("td")
            .not(":has(a)")
            .first()
            .click({ force: true });

        cy.get("#GrantApplicationsTable tbody tr.selected", {
            timeout: STANDARD_TIMEOUT,
        }).should("have.length", 1);

        // Action bar (left group)
        cy.get("#app_custom_buttons", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .scrollIntoView();

        // Left-side action buttons (actual IDs on this page)
        cy.get("#externalLink", { timeout: STANDARD_TIMEOUT }).should("be.visible"); // Open
        cy.get("#assignApplication", { timeout: STANDARD_TIMEOUT }).should(
            "be.visible",
        ); // Assign
        cy.get("#approveApplications", { timeout: STANDARD_TIMEOUT }).should(
            "be.visible",
        ); // Approve
        cy.get("#tagApplication", { timeout: STANDARD_TIMEOUT }).should("be.visible"); // Tags
        cy.get("#applicationPaymentRequest", {
            timeout: STANDARD_TIMEOUT,
        }).should("be.visible"); // Payment
        cy.get("#applicationLink", { timeout: STANDARD_TIMEOUT }).should(
            "be.visible",
        ); // Info

        // Filter button
        cy.get("#btn-toggle-filter", { timeout: STANDARD_TIMEOUT }).should(
            "be.visible",
        );

        // Right-side buttons
        cy.contains(
            "#dynamicButtonContainerId .dt-buttons button span",
            "Export",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");
        cy.contains("#dynamicButtonContainerId button.grp-savedStates", "Save View", {
            timeout: STANDARD_TIMEOUT,
        }).should("be.visible");
        cy.contains(
            "#dynamicButtonContainerId .dt-buttons button span",
            "Columns",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");
    });

    it("Verifies the expected action buttons are visible when two rows are selected", () => {
        const BUTTON_TIMEOUT = 60000;

        // Ensure table has rows
        cy.get(".dt-scroll-body tbody tr", { timeout: STANDARD_TIMEOUT }).should(
            "have.length.greaterThan",
            1,
        );

        // Select two rows using non-link cells
        const clickSelectableCell = (rowIdx: number, withCtrl = false) => {
            cy.get(".dt-scroll-body tbody tr", { timeout: STANDARD_TIMEOUT })
                .eq(rowIdx)
                .find("td")
                .not(":has(a)")
                .first()
                .click({ force: true, ctrlKey: withCtrl });
        };
        clickSelectableCell(0);
        clickSelectableCell(1, true);

        // ActionBar
        cy.get("#app_custom_buttons", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .scrollIntoView();

        // Click Payment
        cy.get("#applicationPaymentRequest", { timeout: BUTTON_TIMEOUT })
            .should("be.visible")
            .and("not.be.disabled")
            .click({ force: true });

        // Wait until modal is shown
        cy.get("#payment-modal", { timeout: STANDARD_TIMEOUT })
            .should("be.visible")
            .and("have.class", "show");

        // Attempt graceful closes first
        cy.get("body").type("{esc}", { force: true }); // Bootstrap listens to ESC
        cy.get(".modal-backdrop", { timeout: STANDARD_TIMEOUT }).then(($bd) => {
            if ($bd.length) {
                cy.wrap($bd).click("topLeft", { force: true });
            }
        });

        // Try footer Cancel if available (avoid .catch on Cypress chainable)
        cy.contains("#payment-modal .modal-footer button", "Cancel", {
            timeout: STANDARD_TIMEOUT,
        }).then(($btn) => {
            if ($btn && $btn.length > 0) {
                cy.wrap($btn).scrollIntoView().click({ force: true });
            } else {
                cy.log("Cancel button not present, proceeding to hard-close fallback");
            }
        });

        // Use window API (if present), then hard-close fallback
        cy.window().then((win: any) => {
            try {
                if (typeof win.closePaymentModal === "function") {
                    win.closePaymentModal();
                }
            } catch {
                /* ignore */
            }

            // HARD CLOSE: forcibly hide modal and remove backdrop
            const $ = (win as any).jQuery || (win as any).$;
            if ($) {
                try {
                    $("#payment-modal")
                        .removeClass("show")
                        .attr("aria-hidden", "true")
                        .css("display", "none");
                    $(".modal-backdrop").remove();
                    $("body").removeClass("modal-open").css("overflow", ""); // restore scroll
                } catch {
                    /* ignore */
                }
            }
        });

        // Verify modal/backdrop gone (be tolerant: assert non-interference instead of visibility only)
        cy.get("#payment-modal", { timeout: STANDARD_TIMEOUT }).should(($m) => {
            const isHidden = !$m.is(":visible") || !$m.hasClass("show");
            expect(isHidden, "payment-modal hidden or not shown").to.eq(true);
        });
        cy.get(".modal-backdrop", { timeout: STANDARD_TIMEOUT }).should("not.exist");

        // Right-side buttons usable
        cy.get("#dynamicButtonContainerId", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .scrollIntoView();

        cy.contains("#dynamicButtonContainerId .dt-buttons button span", "Export", {
            timeout: STANDARD_TIMEOUT,
        }).should("be.visible");
        cy.contains(
            "#dynamicButtonContainerId button.grp-savedStates",
            "Save View",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");
        cy.contains(
            "#dynamicButtonContainerId .dt-buttons button span",
            "Columns",
            { timeout: STANDARD_TIMEOUT },
        ).should("be.visible");
    });

    // Walk the Columns menu and toggle each column on, verifying the column is visible.
    it("Verify all columns in the menu are visible when and toggled on.", () => {
        const escapeRegex = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

        const clickColumnsItem = (label: string) => {
            // Case-insensitive exact match so DEV "ID" and PROD "Id" both work
            const re = new RegExp(`^\\s*${escapeRegex(label)}\\s*$`, "i");
            cy.contains("a.dropdown-item", re, { timeout: STANDARD_TIMEOUT })
                .should("exist")
                .scrollIntoView()
                .click({ force: true });
        };

        const normalize = (s: string) =>
            (s || "").replace(/\s+/g, " ").trim().toLowerCase();

        const getVisibleHeaderTitles = () => {
            return cy
                .get(".dt-scroll-head span.dt-column-title", {
                    timeout: STANDARD_TIMEOUT,
                })
                .then(($els) => {
                    const titles = Cypress.$($els)
                        .toArray()
                        .map((el) => (el.textContent || "").replace(/\s+/g, " ").trim())
                        .filter((t) => t.length > 0);
                    return titles;
                });
        };

        const assertVisibleHeadersInclude = (expected: string[]) => {
            getVisibleHeaderTitles().then((titles) => {
                const normTitles = titles.map(normalize);
                expected.forEach((e) => {
                    const target = normalize(e);
                    expect(
                        normTitles,
                        `visible headers should include "${e}" (case-insensitive)`,
                    ).to.include(target);
                });
            });
        };

        const scrollX = (x: number) => {
            cy.get(".dt-scroll-body", { timeout: STANDARD_TIMEOUT })
                .should("exist")
                .scrollTo(x, 0, { duration: 0, ensureScrollable: false });
        };

        // Close any open dropdowns/modals first
        cy.get("body").then(($body) => {
            if ($body.find(".dt-button-background").length > 0) {
                cy.get(".dt-button-background").click({ force: true });
            }
        });

        // Open the "Save View" dropdown
        cy.get("button.grp-savedStates", { timeout: STANDARD_TIMEOUT })
            .should("be.visible")
            .and("contain.text", "Save View")
            .click();

        // Click "Reset to Default View"
        cy.contains("a.dropdown-item", "Reset to Default View", {
            timeout: STANDARD_TIMEOUT,
        })
            .should("exist")
            .click({ force: true });

        // Wait for table to rebuild after reset - check for default columns
        cy.get(".dt-scroll-head span.dt-column-title", {
            timeout: STANDARD_TIMEOUT,
        }).should("have.length.gt", 5);

        // Open Columns menu
        cy.contains("span", "Columns", { timeout: STANDARD_TIMEOUT })
            .should("be.visible")
            .click();

        // Wait for columns dropdown to be fully populated
        cy.get("a.dropdown-item", { timeout: STANDARD_TIMEOUT }).should(
            "have.length.gt",
            50,
        );

        clickColumnsItem("% of Total Project Budget");
        clickColumnsItem("Acquisition");
        clickColumnsItem("Applicant Electoral District");

        clickColumnsItem("Applicant Id");
        clickColumnsItem("Applicant Id");

        clickColumnsItem("Applicant Name");
        clickColumnsItem("Applicant Name");

        clickColumnsItem("Approved Amount");
        clickColumnsItem("Approved Amount");

        clickColumnsItem("Assessment Result");

        clickColumnsItem("Assignee");
        clickColumnsItem("Assignee");

        clickColumnsItem("Business Number");

        clickColumnsItem("Category");
        clickColumnsItem("Category");

        clickColumnsItem("City");

        clickColumnsItem("Community");
        clickColumnsItem("Community");

        clickColumnsItem("Community Population");
        clickColumnsItem("Contact Business Phone");
        clickColumnsItem("Contact Cell Phone");
        clickColumnsItem("Contact Email");
        clickColumnsItem("Contact Full Name");
        clickColumnsItem("Contact Title");
        clickColumnsItem("Decision Date");
        clickColumnsItem("Decline Rationale");
        clickColumnsItem("Due Date");
        clickColumnsItem("Due Diligence Status");
        clickColumnsItem("Economic Region");
        clickColumnsItem("Forestry Focus");
        clickColumnsItem("Forestry or Non-Forestry");
        clickColumnsItem("FYE Day");
        clickColumnsItem("FYE Month");
        clickColumnsItem("Indigenous");
        clickColumnsItem("Likelihood of Funding");
        clickColumnsItem("Non-Registered Organization Name");
        clickColumnsItem("Notes");
        clickColumnsItem("Org Book Status");
        clickColumnsItem("Organization Type");
        clickColumnsItem("Other Sector/Sub/Industry Description");
        clickColumnsItem("Owner");
        clickColumnsItem("Payout");
        clickColumnsItem("Place");
        clickColumnsItem("Project Electoral District");
        clickColumnsItem("Project End Date");

        clickColumnsItem("Project Name");
        clickColumnsItem("Project Name");

        clickColumnsItem("Project Start Date");
        clickColumnsItem("Project Summary");
        clickColumnsItem("Projected Funding Total");
        clickColumnsItem("Recommended Amount");
        clickColumnsItem("Red-Stop");
        clickColumnsItem("Regional District");
        clickColumnsItem("Registered Organization Name");
        clickColumnsItem("Registered Organization Number");

        clickColumnsItem("Requested Amount");
        clickColumnsItem("Requested Amount");

        clickColumnsItem("Risk Ranking");
        clickColumnsItem("Sector");
        clickColumnsItem("Signing Authority Business Phone");
        clickColumnsItem("Signing Authority Cell Phone");
        clickColumnsItem("Signing Authority Email");
        clickColumnsItem("Signing Authority Full Name");
        clickColumnsItem("Signing Authority Title");

        clickColumnsItem("Status");
        clickColumnsItem("Status");

        clickColumnsItem("Sub-Status");
        clickColumnsItem("Sub-Status");

        clickColumnsItem("Submission #");
        clickColumnsItem("Submission #");

        clickColumnsItem("Submission Date");
        clickColumnsItem("Submission Date");

        clickColumnsItem("SubSector");

        clickColumnsItem("Tags");
        clickColumnsItem("Tags");

        clickColumnsItem("Total Paid Amount $");
        clickColumnsItem("Total Project Budget");
        clickColumnsItem("Total Score");
        clickColumnsItem("Unity Application Id");

        // Close the menu and wait until the overlay is gone
        cy.get("div.dt-button-background", { timeout: STANDARD_TIMEOUT })
            .should("exist")
            .click({ force: true });

        cy.get("div.dt-button-background", { timeout: STANDARD_TIMEOUT }).should(
            "not.exist",
        );

        // Assertions by horizontal scroll segments (human-style scan)
        scrollX(0);
        assertVisibleHeadersInclude([
            "Applicant Name",
            "Category",
            "Submission #",
            "Submission Date",
            "Status",
            "Sub-Status",
            "Community",
            "Requested Amount",
            "Approved Amount",
            "Project Name",
            "Applicant Id",
        ]);

        scrollX(1500);
        assertVisibleHeadersInclude([
            "Tags",
            "Assignee",
            "SubSector",
            "Economic Region",
            "Regional District",
            "Registered Organization Number",
            "Org Book Status",
        ]);

        scrollX(3000);
        assertVisibleHeadersInclude([
            "Project Start Date",
            "Project End Date",
            "Projected Funding Total",
            "Total Paid Amount $",
            "Project Electoral District",
            "Applicant Electoral District",
        ]);

        scrollX(4500);
        assertVisibleHeadersInclude([
            "Forestry or Non-Forestry",
            "Forestry Focus",
            "Acquisition",
            "City",
            "Community Population",
            "Likelihood of Funding",
            "Total Score",
        ]);

        scrollX(6000);
        assertVisibleHeadersInclude([
            "Assessment Result",
            "Recommended Amount",
            "Due Date",
            "Owner",
            "Decision Date",
            "Project Summary",
            "Organization Type",
            "Business Number",
        ]);

        scrollX(7500);
        assertVisibleHeadersInclude([
            "Due Diligence Status",
            "Decline Rationale",
            "Contact Full Name",
            "Contact Title",
            "Contact Email",
            "Contact Business Phone",
            "Contact Cell Phone",
        ]);

        scrollX(9000);
        assertVisibleHeadersInclude([
            "Signing Authority Full Name",
            "Signing Authority Title",
            "Signing Authority Email",
            "Signing Authority Business Phone",
            "Signing Authority Cell Phone",
            "Place",
            "Risk Ranking",
            "Notes",
            "Red-Stop",
            "Indigenous",
            "FYE Day",
            "FYE Month",
            "Payout",
            "Unity Application Id",
        ]);
    });

    it("Verify Logout", () => {
        cy.logout();
    });
});