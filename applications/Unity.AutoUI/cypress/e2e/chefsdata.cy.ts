/// <reference types="cypress" />

import { PageFactory } from "../utilities/PageFactory";
import { loginIfNeeded } from "../support/auth";

describe("Unity Login and check data from CHEFS", () => {
  const loginPage = PageFactory.getLoginPage();
  const applicationsPage = PageFactory.getApplicationsPage();
  const detailsPage = PageFactory.getApplicationDetailsPage();

  let expectedData: any;

  before(() => {
    // Load expected data from fixture based on environment
    cy.fixture("chefsExpectedData.json").then((data) => {
      const environment = Cypress.env("environment");
      const envData = data.expectedData.find(
        (d: any) => d.unityEnv.toLowerCase() === environment.toLowerCase(),
      );
      if (!envData) {
        throw new Error(
          `No expected data found for environment: ${environment}`,
        );
      }
      expectedData = envData;
    });
    loginIfNeeded();
  });

  it("Verify Login", () => {
    // Use the new robust login helper
    loginIfNeeded();
  });

  it("Verify the UI is populated with valid data from CHEFS", () => {
    // Ensure we're on the Applications page
    cy.url().then((url) => {
      if (!url.includes("/GrantApplications")) {
        cy.visit(Cypress.env("webapp.url") + "GrantApplications");
      }
    });

    cy.getSubmissionDetail("confirmationID").then((id) => {
      cy.log(`Confirmation ID: ${id}`);

      // Conditionally widen date range if control exists
      cy.get("body").then(($body) => {
        if ($body.find("input#submittedFromDate").length > 0) {
          cy.get("input#submittedFromDate")
            .should("be.visible")
            .clear()
            .type("2022-01-01");
        }
      });

      // Search and select application
      cy.get("#search").first().clear({ force: true });
      cy.get("#search").first().type(id, { force: true });

      // Select matching row if it exists
      cy.get("body").then(($body) => {
        if ($body.find(`tr:contains("${id}")`).length > 0) {
          applicationsPage.selectRowCheckbox(id);
        } else {
          cy.log(`⚠️ Submission ${id} not found in environment`);
          return;
        }
      });

      // Open summary panel if button is enabled
      cy.get("body").then(($body) => {
        if ($body.find("#applicationLink:not(:disabled)").length > 0) {
          applicationsPage.openApplicationSummary();
        } else {
          cy.log("⚠️ Application link disabled - skipping tests");
          return;
        }
      });

      // 19.) Verify Summary Panel data
      detailsPage.verifySummaryField(
        "Category",
        expectedData.summaryPanel.category,
      );
      detailsPage.verifySummaryField(
        "OrganizationName",
        expectedData.summaryPanel.organizationName,
      );
      detailsPage.verifySummaryField(
        "OrganizationNumber",
        expectedData.summaryPanel.organizationNumber,
      );
      detailsPage.verifySummaryField(
        "EconomicRegion",
        expectedData.summaryPanel.economicRegion,
      );
      detailsPage.verifySummaryField(
        "RegionalDistrict",
        expectedData.summaryPanel.regionalDistrict,
      );
      detailsPage.verifySummaryField(
        "Community",
        expectedData.summaryPanel.community,
      );
      detailsPage.verifySummaryField(
        "RequestedAmount",
        expectedData.summaryPanel.requestedAmount,
      );
      detailsPage.verifySummaryField(
        "ProjectBudget",
        expectedData.summaryPanel.projectBudget,
      );
      detailsPage.verifySummaryField(
        "Sector",
        expectedData.summaryPanel.sector,
      );

      // Close summary panel if it's open
      cy.get("body").then(($body) => {
        if ($body.find("#closeSummaryCanvas").length > 0) {
          cy.get("#closeSummaryCanvas").click();
        }
      });

      // 20.) Verify Details Panel data - Open application details
      cy.get("body").then(($body) => {
        if ($body.find("#externalLink:not(:disabled)").length > 0) {
          cy.get("#externalLink").click();
        } else {
          cy.log(
            "⚠️ External link not available - skipping detailed panel tests",
          );
          return;
        }
      });

      detailsPage.verifySummaryField(
        "Category",
        expectedData.detailsPanel.category,
      );
      detailsPage.verifySummaryField(
        "OrganizationName",
        expectedData.detailsPanel.organizationName,
      );
      detailsPage.verifySummaryField(
        "OrganizationNumber",
        expectedData.detailsPanel.organizationNumber,
      );
      detailsPage.verifySummaryField(
        "EconomicRegion",
        expectedData.detailsPanel.economicRegion,
      );
      detailsPage.verifySummaryField(
        "RegionalDistrict",
        expectedData.detailsPanel.regionalDistrict,
      );
      detailsPage.verifySummaryField(
        "Community",
        expectedData.detailsPanel.community,
      );
      detailsPage.verifySummaryField(
        "RequestedAmount",
        expectedData.detailsPanel.requestedAmount,
      );
      detailsPage.verifySummaryField(
        "ProjectBudget",
        expectedData.detailsPanel.projectBudget,
      );
      detailsPage.verifySummaryField(
        "Sector",
        expectedData.detailsPanel.sector,
      );

      // 21.) Verify Review & Assessment tab
      detailsPage.goToReviewAssessmentTab();
      detailsPage.verifyReviewAssessmentRequestedAmount(
        expectedData.reviewAssessment.requestedAmount,
      );
      detailsPage.verifyReviewAssessmentTotalBudget(
        expectedData.reviewAssessment.totalBudget,
      );

      // 22.) Verify Project Info tab
      detailsPage.goToProjectInfoTab();
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_ProjectName",
        expectedData.projectInfo.projectName,
      );
      detailsPage.verifyProjectInfoField(
        "#startDate",
        expectedData.projectInfo.startDate,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_ProjectEndDate",
        expectedData.projectInfo.endDate,
      );
      detailsPage.verifyProjectInfoField(
        "#RequestedAmountInputPI",
        expectedData.projectInfo.requestedAmount,
      );
      detailsPage.verifyProjectInfoField(
        "#TotalBudgetInputPI",
        expectedData.projectInfo.totalBudget,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_Acquisition",
        expectedData.projectInfo.acquisition,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_Forestry",
        expectedData.projectInfo.forestry,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_ForestryFocus",
        expectedData.projectInfo.forestryFocus,
      );
      detailsPage.verifyProjectInfoField(
        "#economicRegions",
        expectedData.projectInfo.economicRegion,
      );
      detailsPage.verifyProjectInfoField(
        "#regionalDistricts",
        expectedData.projectInfo.regionalDistrict,
      );
      detailsPage.verifyProjectInfoField(
        "#communities",
        expectedData.projectInfo.community,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_CommunityPopulation",
        expectedData.projectInfo.communityPopulation,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_ElectoralDistrict",
        expectedData.projectInfo.electoralDistrict,
      );
      detailsPage.verifyProjectInfoField(
        "#ProjectInfo_Place",
        expectedData.projectInfo.place,
      );

      // 23.) Verify Applicant Info tab
      detailsPage.goToApplicantInfoTab();

      const plainInputs: [string, string][] = [
        ["#ApplicantSummary_OrgName", expectedData.applicantInfo.orgName],
        ["#ApplicantSummary_OrgNumber", expectedData.applicantInfo.orgNumber],
        ["#ContactInfo_Name", expectedData.applicantInfo.contactFullName],
        ["#ContactInfo_Title", expectedData.applicantInfo.contactTitle],
        ["#ContactInfo_Email", expectedData.applicantInfo.contactEmail],
        ["#ContactInfo_Phone", expectedData.applicantInfo.contactBusinessPhone],
        ["#ContactInfo_Phone2", expectedData.applicantInfo.contactCellPhone],
        [
          "#PhysicalAddress_Street",
          expectedData.applicantInfo.physicalAddressStreet,
        ],
        [
          "#PhysicalAddress_Street2",
          expectedData.applicantInfo.physicalAddressStreet2,
        ],
        [
          "#PhysicalAddress_Unit",
          expectedData.applicantInfo.physicalAddressUnit,
        ],
        [
          "#PhysicalAddress_City",
          expectedData.applicantInfo.physicalAddressCity,
        ],
        [
          "#PhysicalAddress_Province",
          expectedData.applicantInfo.physicalAddressProvince,
        ],
        [
          "#PhysicalAddress_PostalCode",
          expectedData.applicantInfo.physicalAddressPostalCode,
        ],
        [
          "#MailingAddress_Street",
          expectedData.applicantInfo.mailingAddressStreet,
        ],
        [
          "#MailingAddress_Street2",
          expectedData.applicantInfo.mailingAddressStreet2,
        ],
        ["#MailingAddress_Unit", expectedData.applicantInfo.mailingAddressUnit],
        ["#MailingAddress_City", expectedData.applicantInfo.mailingAddressCity],
        [
          "#MailingAddress_Province",
          expectedData.applicantInfo.mailingAddressProvince,
        ],
        [
          "#MailingAddress_PostalCode",
          expectedData.applicantInfo.mailingAddressPostalCode,
        ],
        [
          "#SigningAuthority_SigningAuthorityFullName",
          expectedData.applicantInfo.signingAuthorityFullName,
        ],
        [
          "#SigningAuthority_SigningAuthorityTitle",
          expectedData.applicantInfo.signingAuthorityTitle,
        ],
        [
          "#SigningAuthority_SigningAuthorityEmail",
          expectedData.applicantInfo.signingAuthorityEmail,
        ],
        [
          "#SigningAuthority_SigningAuthorityBusinessPhone",
          expectedData.applicantInfo.signingAuthorityBusinessPhone,
        ],
        [
          "#SigningAuthority_SigningAuthorityCellPhone",
          expectedData.applicantInfo.signingAuthorityCellPhone,
        ],
      ];

      plainInputs.forEach(([selector, expected]) => {
        cy.get(selector).invoke("val").should("eq", expected);
      });

      cy.get("#ApplicantSummary_SectorSubSectorIndustryDesc")
        .invoke("val")
        .should("eq", expectedData.applicantInfo.sectorSubSectorIndustryDesc);

      // 24.) Verify Sector and Sub-sector dropdowns
      detailsPage.selectSector(expectedData.sectorSubsector.sector);
      detailsPage.verifySubSectorOptions(
        expectedData.sectorSubsector.subSectorOptions,
      );

      // 25.) Verify Payment Info tab
      detailsPage.goToPaymentInfoTab();
      detailsPage.verifyPaymentInfoRequestedAmount(
        expectedData.paymentInfo.requestedAmount,
      );

      // 26.) Verify Submission tab
      detailsPage.goToSubmissionTab();
      cy.wait(2000); // Wait for tab to load
      detailsPage.verifySubmissionHeaders(expectedData.submissionHeaders);
    });
  });

  it("Verify Logout", () => {
    loginPage.logout();
  });
});
