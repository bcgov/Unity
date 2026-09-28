/// <reference types="cypress" />

import { loginIfNeeded } from "../support/auth";

type NotificationPlan = {
  id: string;
  formId?: string;
  templateId: string;
  triggerType: string;
  module?: string;
  eventStatus?: string;
  applicationStatusId?: string;
  recipientCategory?: string;
  recipientIdentifier?: string;
};

type StatusOption = {
  id: string;
  internalStatus: string;
};

const configuredFormId = () => String(Cypress.env("notificationFormId") || "");
const configuredRecipientGroup = () =>
  String(Cypress.env("notificationRecipientGroup") || "James");
const configuredTemplateName = () =>
  String(Cypress.env("notificationTemplateName") || "");

function requireNotificationConfiguration(): void {
  if (!configuredFormId()) {
    throw new Error(
      "Set notificationFormId in cypress/config/<environment>.json before running eventNotifications.cy.ts.",
    );
  }
}

function getApplicationTemplate(): Cypress.Chainable<{ id: string; name: string }> {
  return cy
    .request("GET", "/api/form-notifications/templates?templateType=Application")
    .then((response) => {
      expect(response.status).to.eq(200);
      const templates = response.body as Array<{ id: string; name: string }>;
      const configuredName = configuredTemplateName().toLowerCase();
      const template = configuredName
        ? templates.find((item) => item.name.toLowerCase() === configuredName)
        : templates[0];

      expect(template, "an application email template").to.exist;
      return template!;
    });
}

function getInternalRecipientGroup(): Cypress.Chainable<string> {
  return cy
    .request("GET", "/api/form-notifications/recipients?category=Internal")
    .then((response) => {
      expect(response.status).to.eq(200);
      const recipients = response.body as Array<{
        id: string;
        displayName: string;
      }>;
      const configuredName = configuredRecipientGroup().toLowerCase();
      const recipient = recipients.find(
        (item) =>
          item.id.toLowerCase() === configuredName ||
          item.displayName.toLowerCase() === configuredName,
      );

      expect(
        recipient,
        `the configured internal recipient group '${configuredRecipientGroup()}'`,
      ).to.exist;
      return recipient!.id;
    });
}

function createNotification(
  payload: Record<string, unknown>,
): Cypress.Chainable<NotificationPlan> {
  return cy
    .request({
      method: "POST",
      url: `/api/form-notifications/${encodeURIComponent(configuredFormId())}`,
      body: payload,
      failOnStatusCode: false,
    })
    .then((response) => {
      expect(response.status, JSON.stringify(response.body)).to.eq(201);
      return response.body as NotificationPlan;
    });
}

function verifyPersistedNotification(
  plan: NotificationPlan,
  expected: Partial<NotificationPlan>,
){
  Object.entries(expected).forEach(([key, value]) => {
    expect(plan[key as keyof NotificationPlan], key).to.deep.eq(value);
  });

  return cy
    .request("GET", `/api/form-notifications/${encodeURIComponent(configuredFormId())}`)
    .then((response) => {
      expect(response.status).to.eq(200);
      const persisted = (response.body as NotificationPlan[]).find(
        (item) => item.id === plan.id,
      );
      expect(persisted, "the generated notification plan").to.exist;
      Object.entries(expected).forEach(([key, value]) => {
        expect(persisted![key as keyof NotificationPlan], `persisted ${key}`).to.deep.eq(
          value,
        );
      });
      return undefined;
    });
}

function deleteNotification(plan: NotificationPlan) {
  return cy
    .request({
      method: "DELETE",
      url: `/api/form-notifications/${encodeURIComponent(configuredFormId())}/${plan.id}`,
      failOnStatusCode: false,
    })
    .then((response) => {
      expect([200, 204], JSON.stringify(response.body)).to.include(response.status);
      return undefined;
    });
}

describe("Event-based scheduled notifications", () => {
  before(() => {
    requireNotificationConfiguration();
    loginIfNeeded({ timeout: 60000 });
  });

  it("creates and persists an Application status trigger for every available application status", () => {
    let templateId: string;
    let recipientIdentifier: string;
    const createdPlans: NotificationPlan[] = [];

    getApplicationTemplate()
      .then((template) => {
        templateId = template.id;
        return getInternalRecipientGroup();
      })
      .then((recipient) => {
        recipientIdentifier = recipient;
        return cy.request<StatusOption[]>("GET", "/api/form-notifications/statuses");
      })
      .then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.length, "available application statuses").to.be.greaterThan(0);

        cy.wrap(response.body).each((status: StatusOption) => {
          return createNotification({
            templateId,
            triggerType: "Event",
            module: "Application",
            applicationStatusId: status.id,
            recipientCategory: "Internal",
            recipientIdentifier,
          }).then((plan) => {
            createdPlans.push(plan);
            return verifyPersistedNotification(plan, {
              templateId,
              triggerType: "Event",
              module: "Application",
              applicationStatusId: status.id,
              recipientCategory: "Internal",
              recipientIdentifier,
            });
          });
        });
      })
      .then(() => {
        createdPlans.forEach((plan) => deleteNotification(plan));
      });
  });

  it("creates and persists a Payment status trigger for every available payment status", () => {
    let templateId: string;
    let recipientIdentifier: string;
    const createdPlans: NotificationPlan[] = [];

    getApplicationTemplate()
      .then((template) => {
        templateId = template.id;
        return getInternalRecipientGroup();
      })
      .then((recipient) => {
        recipientIdentifier = recipient;
        return cy.request<StatusOption[]>("GET", "/api/form-notifications/payment-statuses");
      })
      .then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.length, "available payment statuses").to.be.greaterThan(0);

        cy.wrap(response.body).each((status: StatusOption) => {
          return createNotification({
            templateId,
            triggerType: "Event",
            module: "Payment",
            eventStatus: status.id,
            recipientCategory: "Internal",
            recipientIdentifier,
          }).then((plan) => {
            createdPlans.push(plan);
            return verifyPersistedNotification(plan, {
              templateId,
              triggerType: "Event",
              module: "Payment",
              eventStatus: status.id,
              recipientCategory: "Internal",
              recipientIdentifier,
            });
          });
        });
      })
      .then(() => {
        createdPlans.forEach((plan) => deleteNotification(plan));
      });
  });
});
