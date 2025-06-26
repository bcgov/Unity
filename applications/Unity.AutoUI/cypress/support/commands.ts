// ***********************************************
// This commands.ts file is used to 
// create custom commands and overwrite
// existing commands.
//
// For comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************

Cypress.Commands.add('login', () => {
  // 1.) Load Main Page
  cy.visit(Cypress.env('webapp.url'))
  // 2.) Login to the Default Grant Tenant
  cy.contains('LOGIN').should('exist').click()
  cy.wait(1000)
  cy.contains('IDIR').should('exist').click()
  cy.wait(1000)
  cy.get('body').then($body => { // Check if you're already logged in.
    if ($body.find('#user').length) { // If #user exists, perform the login steps
      cy.get("#user").type(Cypress.env('test1username'));
      cy.get("#password").type(Cypress.env('test1password'));
      cy.contains("Continue").should('exist').click();
    } else {// If #user does not exist, log a message and proceed
        cy.log('Already logged in');
      }
  });
 });

Cypress.Commands.add('logout', () => {
  // 1.) Load Main Page
  cy.visit(Cypress.env('webapp.url'))
  // 2.) Logout
  cy.request('GET', (Cypress.env('webapp.url') + 'Account/Logout'))
  cy.wait(1000)
  cy.visit(Cypress.env('webapp.url'))
})

/// <reference types="cypress" />

interface SubmissionDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add('getSubmissionDetail', (key: string) => {
  return cy.fixture<{submissionDetails: SubmissionDetail[]}>('submissions.json').then(({submissionDetails}) => {
    const environment = Cypress.env('environment');
    const submissionDetail = submissionDetails.find(detail => detail.unityEnv === environment);
    
    if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
      return submissionDetail[key];
    } else {
      throw new Error(`No submission detail found for environment: ${environment} and key: ${key}`);
    }
  });
});

interface MetabaseDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add('getMetabaseDetail', (key: string) => {
  return cy.fixture<{metabaseDetails: MetabaseDetail[]}>('metabase.json').then(({metabaseDetails}) => {
    const environment = Cypress.env('environment');
    const submissionDetail = metabaseDetails.find(detail => detail.unityEnv === environment);
    
    if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
      return submissionDetail[key];
    } else {
      throw new Error(`No submission detail found for environment: ${environment} and key: ${key}`);
    }
  });
});

Cypress.Commands.add('metabaseLogin', () => {
  cy.getMetabaseDetail('baseURL').then((baseURL) => {
    cy.visit(baseURL);

    // Target the username field using its `name` attribute
    cy.get('input[name="username"]')
      .should('exist')
      .click()
      .type('iDontHave@ValidEmail.com'); // Placeholder email address

    // Target the password field using its `name` attribute
    cy.get('input[name="password"]')
      .should('exist')
      .click()
      .type('pointless'); // Placeholder password
  });
});

interface chefsDetail {
  unityEnv: string;
  [key: string]: string; // Allow additional properties with string keys
}

Cypress.Commands.add('getChefsDetail', (key: string) => {
  return cy.fixture<{chefsDetails: chefsDetail[]}>('chefs.json').then(({chefsDetails}) => {
    const environment = Cypress.env('environment');
    const submissionDetail = chefsDetails.find(detail => detail.unityEnv === environment);
    
    if (submissionDetail && submissionDetail.hasOwnProperty(key)) {
      return submissionDetail[key];
    } else {
      throw new Error(`No submission detail found for environment: ${environment} and key: ${key}`);
    }
  });
});

Cypress.Commands.add('chefsLogin', () => {
  cy.getChefsDetail('chefsBaseURL').then(baseURL => {cy.visit(baseURL); // Visit the URL fetched from chefs.json
    cy.get('#app > div > main > header > header > div > div.d-print-none') 
      .should('exist')
      .click(); // click the login button
    cy.wait(1000)
    cy.get('#app > div > main > div.v-container.v-locale--is-ltr.text-center.main > div > div:nth-child(2) > div > button')
      .should('exist')
      .click(); // click the idir buttton
    cy.wait(1000)
	cy.get('body').then($body => { // Check if you're already logged in.
	if ($body.find('#user').length) { // If #user exists, perform the login steps
		cy.get("#user").type(Cypress.env('test1username'));
		cy.get("#password").type(Cypress.env('test1password'));
		cy.contains("Continue").should('exist').click();
	} else {// If #user does not exist, log a message and proceed
		cy.log('Already logged in');
		}
	});
    cy.wait(1000)
  });
});

Cypress.Commands.add('chefsLogout', () => {
  cy.getChefsDetail('chefsBaseURL').then(baseURL => {cy.visit(baseURL);}); // Load Main Page
    cy.wait(1000)
    cy.contains("Logout").should('exist').click()// Logout
    cy.wait(1000)
	cy.contains("Login").should('exist')
});

Cypress.Commands.add('clearSessionStorage', () => {
  cy.window().then((window) => {
    window.sessionStorage.clear();
  });
});

Cypress.Commands.add('clearBrowserCache', () => {
  cy.window().then((win) => {
    win.caches.keys().then((keyList) => {
      return Promise.all(keyList.map((key) => {
        return win.caches.delete(key);
      }));
    });
  });
});