/// <reference types="cypress" />

import { loginIfNeeded } from "../support/auth";

type Application = {
  id: string;
  projectName?: string;
  projectStartDate?: string | null;
  projectEndDate?: string | null;
  contractExecutionDate?: string | null;
  requestedAmount?: number;
  totalProjectBudget?: number;
};

type NotificationPlan = {
  id: string;
  templateId: string;
  triggerType: string;
  module?: string;
  dateType?: string;
  applicationStatusIds?: string[];
  recipientCategory?: string;
  recipientIdentifier?: string;
};

type DateScenario = {
  dateType: "ProjectStartDate" | "ProjectEndDate" | "ContractExecutionDate";
  date: string;
};

const env = (name: string): string => String(Cypress.env(name) || "");
const formId = (): string => env("notificationFormId");
const applicationId = (): string => env("notificationApplicationId");
const recipientName = (): string => env("notificationRecipientGroup") || "James";
const templateName = (): string => env("notificationTemplateName").toLowerCase();

const requireConfiguration = (): void => {
  const missing = [
    ["notificationFormId", formId()],
    ["notificationApplicationId", applicationId()],
  ]
    .filter(([, value]) => !value)
    .map(([name]) => name);

  if (missing.length > 0) {
    throw new Error(
      `Set ${missing.join(" and ")} in cypress/config/<environment>.json before running dateNotifications.cy.ts.`,
    );
  }
};

const getApplication = (): Cypress.Chainable<Application> =>
  cy.request<Application>("GET", `/api/app/grant-application/${applicationId()}`).then(
    (response) => {
      expect(response.status).to.eq(200);
      return response.body;
    },
  );

const getTemplateId = (): Cypress.Chainable<string> =>
  cy
    .request("GET", "/api/form-notifications/templates?templateType=Application")
    .then((response) => {
      expect(response.status).to.eq(200);
      const templates = response.body as Array<{ id: string; name: string }>;
      const template = templateName()
        ? templates.find((item) => item.name.toLowerCase() === templateName())
        : templates[0];
      expect(template, "an application notification template").to.exist;
      return template!.id;
    });

const getRecipientId = (): Cypress.Chainable<string> =>
  cy.request("GET", "/api/form-notifications/recipients?category=Internal").then(
    (response) => {
      expect(response.status).to.eq(200);
      const recipients = response.body as Array<{ id: string; displayName: string }>;
      const configured = recipientName().toLowerCase();
      const recipient = recipients.find(
        (item) =>
          item.id.toLowerCase() === configured || item.displayName.toLowerCase() === configured,
      );
      expect(recipient, `internal recipient group '${recipientName()}'`).to.exist;
      return recipient!.id;
    },
  );

const updateApplicationDates = (
  original: Application,
  scenario: DateScenario[],
): Cypress.Chainable<Application> => {
  const start = scenario.find((item) => item.dateType === "ProjectStartDate")!.date;
  const end = scenario.find((item) => item.dateType === "ProjectEndDate")!.date;
  const execution = scenario.find((item) => item.dateType === "ContractExecutionDate")!.date;

  return cy
    .request({
      method: "PUT",
      url: `/api/app/grant-application/${applicationId()}/partial-project-info`,
      body: {
        data: {
          projectStartDate: start,
          projectEndDate: end,
        },
      },
    })
    .then(() =>
      cy.request({
        method: "PUT",
        url: `/api/app/grant-application/${applicationId()}/funding-agreement-info`,
        body: { contractExecutionDate: execution },
      }),
    )
    .then(() => getApplication());
};

const restoreApplicationDates = (original: Application) =>
  cy
    .request({
      method: "PUT",
      url: `/api/app/grant-application/${applicationId()}/partial-project-info`,
      body: {
        data: {
          projectStartDate: original.projectStartDate,
          projectEndDate: original.projectEndDate,
        },
      },
      failOnStatusCode: false,
    })
    .then(() =>
      cy.request({
        method: "PUT",
        url: `/api/app/grant-application/${applicationId()}/funding-agreement-info`,
        body: { contractExecutionDate: original.contractExecutionDate },
        failOnStatusCode: false,
      }),
    );

const createNotification = (
  templateId: string,
  recipientIdentifier: string,
  dateType: DateScenario["dateType"],
): Cypress.Chainable<NotificationPlan> =>
  cy
    .request({
      method: "POST",
      url: `/api/form-notifications/${formId()}`,
      body: {
        templateId,
        triggerType: "Date",
        module: "Application",
        dateType,
        applicationStatusIds: [],
        recipientCategory: "Internal",
        recipientIdentifier,
      },
    })
    .then((response) => {
      expect(response.status).to.eq(201);
      return response.body as NotificationPlan;
    });

const deleteNotification = (plan: NotificationPlan) =>
  cy.request({
    method: "DELETE",
    url: `/api/form-notifications/${formId()}/${plan.id}`,
    failOnStatusCode: false,
  });

describe("Date-based notifications on one application", () => {
  before(() => {
    requireConfiguration();
    loginIfNeeded({ timeout: 60000 });
  });

  it("creates three date plans for separate weekend dates and restores the application", () => {
    const createdPlans: NotificationPlan[] = [];
    let original: Application;

    const scenario: DateScenario[] = [
      { dateType: "ProjectStartDate", date: env("notificationProjectStartDate") || "2026-09-25" },
      { dateType: "ProjectEndDate", date: env("notificationProjectEndDate") || "2026-09-26" },
      {
        dateType: "ContractExecutionDate",
        date: env("notificationContractExecutionDate") || "2026-09-27",
      },
    ];

    getApplication()
      .then((application) => {
        original = application;
        return updateApplicationDates(application, scenario);
      })
      .then(() => getTemplateId())
      .then((templateId) =>
        getRecipientId().then((recipientIdentifier) => ({ templateId, recipientIdentifier })),
      )
      .then(({ templateId, recipientIdentifier }) =>
        cy.wrap(scenario).each((rawItem) => {
          const item = rawItem as unknown as DateScenario;
          return (
          createNotification(templateId, recipientIdentifier, item.dateType).then((plan) => {
            createdPlans.push(plan);
            expect(plan.triggerType).to.eq("Date");
            expect(plan.dateType).to.eq(item.dateType);
          })
          );
        }),
      )
      .then(() =>
        cy.request<NotificationPlan[]>("GET", `/api/form-notifications/${formId()}`),
      )
      .then((response) => {
        expect(response.status).to.eq(200);
        createdPlans.forEach((plan) => {
          const persisted = response.body.find((item) => item.id === plan.id);
          expect(persisted, `persisted ${plan.dateType} notification`).to.exist;
          expect(persisted!.dateType).to.eq(plan.dateType);
          expect(persisted!.triggerType).to.eq("Date");
        });
      })
      .then(() => {
        createdPlans.forEach((plan) => deleteNotification(plan));
        return restoreApplicationDates(original);
      });
  });
});