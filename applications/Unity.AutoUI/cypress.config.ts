import { defineConfig } from "cypress";
import FormData from "form-data";
import fs from "fs";
import path from "path";

// https://docs.cypress.io/guides/references/configuration
export default defineConfig({
  e2e: {
    setupNodeEvents(on) {
      on("task", {
        async uploadChefsFile({
          baseURL,
          authToken,
          filePath,
        }: {
          baseURL: string;
          authToken: string;
          filePath: string;
        }) {
          const fileBuffer = fs.readFileSync(filePath);
          const fileName = path.basename(filePath);

          const form = new FormData();
          form.append("files", fileBuffer, {
            filename: fileName,
            contentType: "text/plain",
          });

          // Use getBuffer() so fetch receives a complete binary buffer
          // rather than a piped stream (which causes "Unexpected end of form")
          const formBuffer = form.getBuffer();
          const formHeaders = form.getHeaders();

          const response = await fetch(`${baseURL}/app/api/v1/files`, {
            method: "POST",
            headers: {
              Authorization: `Bearer ${authToken}`,
              ...formHeaders,
            },
            body: formBuffer as unknown as BodyInit,
          });

          if (!response.ok) {
            throw new Error(
              `File upload failed: ${response.status} ${await response.text()}`,
            );
          }

          return response.json();
        },
      });
    },
    specPattern: [
      "cypress/e2e/**/*.cy.{js,jsx,ts,tsx}",
      "cypress/scripts/**/*.cy.{js,jsx,ts,tsx}",
      "cypress/regression/**/*.cy.{js,jsx,ts,tsx}",
    ],
    baseUrl: "https://dev-unity.apps.silver.devops.gov.bc.ca/",
    defaultCommandTimeout: 20000, // Time, in milliseconds, to wait until most DOM based commands are considered timed out.
    viewportWidth: 1440, // Default width in pixels.
    viewportHeight: 900, // Default height in pixels.
    chromeWebSecurity: false, // Chromium-based browser's Web Security for same-origin policy and insecure mixed content.
    testIsolation: false, // Set true to ensure a clean browser context between test cases.
    // The number of times to retry a failing test.
    retries: {
      runMode: 3,
      openMode: 0,
    },
    experimentalMemoryManagement: true,
    numTestsKeptInMemory: 3,
  },
});
