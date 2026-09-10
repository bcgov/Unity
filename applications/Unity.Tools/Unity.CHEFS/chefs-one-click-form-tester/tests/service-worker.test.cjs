const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const { test } = require('node:test');
const { createContext, runInContext } = require('node:vm');

const source = readFileSync(join(__dirname, '..', 'service-worker.js'), 'utf8');

function listener() {
  return { addListener() {} };
}

function loadWorker() {
  const context = createContext({
    importScripts() {},
    ChefsExportPath: {
      normalizeExportFolder: (value) => String(value || '').trim(),
      joinExportPath: (folder, filename) => folder ? `${folder}/${filename}` : filename
    },
    ChefsDashboardModel: {
      SCHEMA_VERSION: 1,
      isDashboardSummary: () => true,
      trimHistory: (history) => history,
      buildRunSummary: (run) => ({ runRef: run.runId })
    },
    chrome: {
      alarms: { get: async () => ({}), create: async () => {}, onAlarm: listener() },
      runtime: { onInstalled: listener(), onStartup: listener(), onMessage: listener(), getURL: (path) => path },
      storage: { local: { get: async () => ({}), set: async () => {} } },
      tabs: { onCreated: listener(), onUpdated: listener(), onRemoved: listener() },
      scripting: {},
      permissions: {},
      downloads: {}
    },
    URL,
    URLSearchParams,
    TextEncoder,
    Uint8Array,
    Uint32Array,
    Date,
    Map,
    Set,
    Promise,
    Math,
    Object,
    Array,
    String,
    Number,
    Boolean,
    Error,
    JSON,
    atob,
    btoa,
    setTimeout: () => 1,
    clearTimeout() {}
  });
  runInContext(source, context);
  return runInContext(`({
    normalizeBatchLauncherToken,
    normalizeBatchOrigins,
    normalizeSettings,
    parseBatchMarker,
    dataUrlToBytes,
    bytesToBase64,
    createZip,
    handleRuntimeMessage
  })`, context);
}

function plain(value) {
  return JSON.parse(JSON.stringify(value));
}

test('settings normalization preserves supported values and migrates legacy export settings', () => {
  const { normalizeSettings } = loadWorker();
  const settings = normalizeSettings({
    rowsPerGrid: 99,
    autoExportAfterSubmit: true,
    dashboardDefaultView: 'analyst',
    customFormatRules: null,
    exportFolder: ' reports '
  });
  assert.equal(settings.rowsPerGrid, 5);
  assert.equal(settings.autoExportAfterRun, true);
  assert.equal(settings.dashboardDefaultView, 'analyst');
  assert.deepEqual(plain(settings.customFormatRules), []);
  assert.equal(settings.exportFolder, 'reports');
  assert.equal('autoExportAfterSubmit' in settings, false);
});

test('batch settings retain only valid tokens and HTTP origins', () => {
  const { normalizeBatchLauncherToken, normalizeBatchOrigins } = loadWorker();
  assert.equal(normalizeBatchLauncherToken('abcdefghijklmnop'), 'abcdefghijklmnop');
  assert.equal(normalizeBatchLauncherToken('too-short'), '');
  assert.deepEqual(plain(normalizeBatchOrigins([
    'https://example.test',
    'https://example.test/',
    'http://localhost',
    'ftp://example.test',
    'https://user@example.test',
    'not a URL'
  ])), ['https://example.test', 'http://localhost']);
});

test('batch marker parsing removes control parameters and sanitizes identifiers', () => {
  const { parseBatchMarker } = loadWorker();
  const marker = parseBatchMarker(
    'https://chefs.example.test/form#chefs-one-click-batch=token&suite=suite%21&id=keep&index=2%2F3'
  );
  assert.deepEqual(plain(marker), {
    token: 'token',
    suiteId: 'suite',
    index: '23',
    url: 'https://chefs.example.test/form#id=keep',
    origin: 'https://chefs.example.test'
  });
  assert.equal(parseBatchMarker('https://chefs.example.test/form'), null);
  assert.equal(parseBatchMarker('not a URL'), null);
});

test('binary conversion preserves bytes across chunk boundaries', () => {
  const { dataUrlToBytes, bytesToBase64 } = loadWorker();
  for (const length of [0, 256, 0x8000, 0x8001]) {
    const bytes = Uint8Array.from({ length }, (_, index) => index % 256);
    const encoded = bytesToBase64(bytes);
    assert.equal(encoded, Buffer.from(bytes).toString('base64'));
    assert.deepEqual(Array.from(dataUrlToBytes(`data:application/octet-stream;base64,${encoded}`)), Array.from(bytes));
  }
});

test('ZIP generation produces a ZIP archive containing each filename', () => {
  const { createZip } = loadWorker();
  const zip = createZip([
    { name: 'first.txt', data: 'first' },
    { name: 'second.json', data: '{"ok":true}' }
  ]);
  const binary = Buffer.from(zip);
  assert.equal(binary.readUInt32LE(0), 0x04034b50);
  assert.ok(binary.includes(Buffer.from('first.txt')));
  assert.ok(binary.includes(Buffer.from('second.json')));
  assert.equal(binary.readUInt32LE(binary.length - 22), 0x06054b50);
});

test('unknown runtime messages return a structured error', async () => {
  const { handleRuntimeMessage } = loadWorker();
  assert.deepEqual(plain(await handleRuntimeMessage({ type: 'UNKNOWN' }, {})), {
    ok: false,
    error: 'Unknown message type: UNKNOWN'
  });
});
