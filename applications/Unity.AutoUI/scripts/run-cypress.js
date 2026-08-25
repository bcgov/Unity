const path = require("path");
const { spawnSync } = require("child_process");

delete process.env.ELECTRON_RUN_AS_NODE;

const cypressCli = path.resolve(
  __dirname,
  "..",
  "node_modules",
  "cypress",
  "bin",
  "cypress",
);

const result = spawnSync(process.execPath, [cypressCli, ...process.argv.slice(2)], {
  stdio: "inherit",
  env: process.env,
  shell: false,
});

if (result.error) {
  throw result.error;
}

process.exit(result.status ?? 1);
