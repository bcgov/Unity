import { defineConfig } from "cypress";
import FormData from "form-data";
import fs from "fs";
import path from "path";

function loadLocalEnvironmentConfig(requestedEnvironment?: string): {
  environmentName: string;
  environmentSource: string;
  environmentConfig: Record<string, unknown>;
} {
  let environmentName: string;
  let environmentSource: string;

  if (requestedEnvironment) {
    environmentName = requestedEnvironment.toLowerCase();
    environmentSource = "--env environment";
  } else if (process.env.UNITY_CYPRESS_ENV) {
    environmentName = process.env.UNITY_CYPRESS_ENV.toLowerCase();
    environmentSource = "UNITY_CYPRESS_ENV";
  } else {
    environmentName = "dev";
    environmentSource = "default";
  }

  // Per-env file (cypress/config/<env>.json) takes precedence so existing
  // local setups keep working.
  const perEnvConfig = readJsonFile(
    path.resolve("cypress", "config", `${environmentName}.json`),
  );
  if (perEnvConfig) {
    return { environmentName, environmentSource, environmentConfig: perEnvConfig };
  }

  // Fallback: the combined file, same shape as the UNITY_CYPRESS_CONFIG CI
  // secret — one CYPRESS_CONFIG_<ENV> key per environment.
  const combinedConfig = readJsonFile(
    path.resolve("cypress", "config", "unity-cypress-config.json"),
  );
  let entry: unknown =
    combinedConfig?.[`CYPRESS_CONFIG_${environmentName.toUpperCase()}`];
  if (typeof entry === "string") {
    // Matches the workflow's jq `fromjson` handling of string-encoded values.
    entry = JSON.parse(entry);
  }

  return {
    environmentName,
    environmentSource,
    environmentConfig: (entry as Record<string, unknown> | undefined) ?? {},
  };
}

function readJsonFile(filePath: string): Record<string, unknown> | null {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf-8")) as Record<string, unknown>;
  } catch {
    return null;
  }
}

// https://docs.cypress.io/guides/references/configuration
export default defineConfig({
  allowCypressEnv: false,
  e2e: {
    setupNodeEvents(on, config) {
      // Supports selecting the environment either via Cypress's native
      // `--env environment=<name>` CLI flag or the UNITY_CYPRESS_ENV OS
      // environment variable (the flag takes precedence).
      const { environmentName, environmentSource, environmentConfig } =
        loadLocalEnvironmentConfig(
          config.env?.environment as string | undefined,
        );

      // eslint-disable-next-line no-console
      console.log(
        `\n[Cypress] Running against environment: ${environmentName} (source: ${environmentSource})\n`,
      );

      on("after:run", () => {
        // eslint-disable-next-line no-console
        console.log(
          `\n[Cypress] Finished run against environment: ${environmentName} (source: ${environmentSource})\n`,
        );
      });

      on("task", {
        readJsonIfExists(filePath: string): Record<string, unknown> | null {
          try {
            // path.resolve handles both forward and back slashes on Windows
            const content = fs.readFileSync(path.resolve(filePath), "utf-8");
            return JSON.parse(content);
          } catch {
            return null;
          }
        },

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

      return {
        ...config,
        baseUrl:
          (environmentConfig["webapp.url"] as string | undefined) ||
          config.baseUrl,
        expose: {
          ...config.expose,
          "webapp.url":
            (environmentConfig["webapp.url"] as string | undefined) ||
            (config.env["webapp.url"] as string | undefined) ||
            config.baseUrl,
          environment: environmentConfig.environment || config.env.environment || environmentName,
          CHEFS_ENV: environmentConfig.CHEFS_ENV || config.env.CHEFS_ENV,
        },
        env: {
          ...config.env,
          ...environmentConfig,
        },
      };
    },
    specPattern: [
      "cypress/e2e/**/*.cy.{js,jsx,ts,tsx}",
      "cypress/scripts/**/*.cy.{js,jsx,ts,tsx}",
      "cypress/regression/**/*.cy.{js,jsx,ts,tsx}",
    ],
    baseUrl: "https://dev-unity.apps.gold.devops.gov.bc.ca/",
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
