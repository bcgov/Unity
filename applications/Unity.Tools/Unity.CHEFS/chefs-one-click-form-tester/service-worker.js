'use strict';

importScripts('export-path.js', 'dashboard-model.js');

const EXTENSION_VERSION = '0.4.0';
const BUILD_NUMBER = '2026.07.23.14';
const RUN_KEY_PREFIX = 'chefsTesterRun:';
const TAB_KEY_PREFIX = 'chefsTesterTab:';
const LAST_RUN_KEY = 'chefsTesterLastRunId';
const SETTINGS_KEY = 'chefsTesterSettings';
const WATCHDOG_ALARM = 'chefsTesterWatchdog';
const BATCH_QUEUE_KEY = 'chefsTesterBatchQueue';
const BATCH_QUEUE_ALARM = 'chefsTesterBatchQueueAlarm';
const BATCH_MARKER_PARAM = 'chefs-one-click-batch';
const DASHBOARD_STATE_KEY = 'chefsTesterDashboardState';
const DASHBOARD_HISTORY_KEY = 'chefsTesterDashboardHistory';
const DASHBOARD_PAGE = 'dashboard.html';
const WATCHDOG_STALE_MS = 75000;
const BATCH_ENTRY_TIMEOUT_MS = 90000;
const BATCH_COLLECTION_DELAY_MS = 1500;
const BATCH_FORM_READY_POLL_MS = 750;
const BATCH_FORM_READY_TIMEOUT_MS = 45000;
const BATCH_COMPLETED_LIMIT = 200;
const ACTIVE_RUN_STATUSES = new Set(['initializing', 'scanning', 'filling', 'settling', 'validating', 'submitting']);
const FINAL_RUN_STATUSES = new Set(['submitted', 'completed', 'failed', 'stalled', 'blocked', 'safety_stop', 'stopped']);
const DEFAULT_SETTINGS = {
  additionalHosts: [],
  allowProduction: false,
  rowsPerGrid: 2,
  captureScreenshot: true,
  customFormatRules: [],
  exportFolder: '',
  autoExportAfterRun: false,
  batchLauncherEnabled: false,
  batchLauncherToken: '',
  batchOrigins: [],
  openDashboardAfterCompletion: false,
  retainDashboardHistory: false,
  dashboardDefaultView: 'simple'
};
const runLocks = new Map();
const automaticExportsInProgress = new Set();
let batchProcessorRunning = false;
let batchScheduleTimer = null;
let batchStateLock = Promise.resolve();

async function ensureWatchdogAlarm() {
  const existing = await chrome.alarms.get(WATCHDOG_ALARM);
  if (!existing) {
    await chrome.alarms.create(WATCHDOG_ALARM, {
      delayInMinutes: 0.5,
      periodInMinutes: 0.5
    });
  }
  const batchAlarm = await chrome.alarms.get(BATCH_QUEUE_ALARM);
  if (!batchAlarm) {
    await chrome.alarms.create(BATCH_QUEUE_ALARM, {
      delayInMinutes: 0.5,
      periodInMinutes: 0.5
    });
  }
}

function normalizeBatchLauncherToken(value) {
  const token = String(value || '').trim();
  return /^[A-Za-z0-9_-]{16,128}$/.test(token) ? token : '';
}

function normalizeBatchOrigins(values) {
  const normalized = [];
  for (const rawValue of Array.isArray(values) ? values : []) {
    try {
      const url = new URL(String(rawValue || '').trim());
      if (
        ['http:', 'https:'].includes(url.protocol) &&
        !url.username &&
        !url.password &&
        url.pathname === '/' &&
        !url.search &&
        !url.hash
      ) {
        normalized.push(url.origin);
      }
    } catch (error) {
      // Invalid stored origins are discarded during migration.
    }
  }
  return Array.from(new Set(normalized));
}

function normalizeSettings(rawSettings) {
  const existing = rawSettings || {};
  const settings = Object.assign({}, DEFAULT_SETTINGS, existing);
  settings.rowsPerGrid = Math.max(2, Math.min(5, Number(settings.rowsPerGrid) || 2));
  settings.customFormatRules = Array.isArray(settings.customFormatRules) ? settings.customFormatRules : [];
  try {
    settings.exportFolder = ChefsExportPath.normalizeExportFolder(settings.exportFolder);
  } catch (error) {
    settings.exportFolder = '';
  }
  if (Object.prototype.hasOwnProperty.call(existing, 'autoExportAfterRun')) {
    settings.autoExportAfterRun = Boolean(existing.autoExportAfterRun);
  } else {
    settings.autoExportAfterRun = Boolean(existing.autoExportAfterSubmit);
  }
  delete settings.exportSubfolder;
  delete settings.autoExportAfterSubmit;
  settings.batchLauncherEnabled = Boolean(settings.batchLauncherEnabled);
  settings.batchLauncherToken = normalizeBatchLauncherToken(settings.batchLauncherToken);
  settings.batchOrigins = normalizeBatchOrigins(settings.batchOrigins);
  settings.openDashboardAfterCompletion = Boolean(settings.openDashboardAfterCompletion);
  settings.retainDashboardHistory = Boolean(settings.retainDashboardHistory);
  settings.dashboardDefaultView = ['simple', 'analyst', 'statistical', 'experimental']
    .includes(settings.dashboardDefaultView)
    ? settings.dashboardDefaultView
    : 'simple';
  return settings;
}

chrome.runtime.onInstalled.addListener(async () => {
  const existing = await chrome.storage.local.get(SETTINGS_KEY);
  const migrated = normalizeSettings(existing[SETTINGS_KEY]);
  await chrome.storage.local.set({ [SETTINGS_KEY]: migrated });
  await ensureWatchdogAlarm();
  scheduleBatchProcessing();
});

chrome.runtime.onStartup.addListener(() => {
  ensureWatchdogAlarm().catch(() => undefined);
  scheduleBatchProcessing();
});

ensureWatchdogAlarm().catch(() => undefined);
scheduleBatchProcessing();

function runKey(runId) {
  return `${RUN_KEY_PREFIX}${runId}`;
}

function tabKey(tabId) {
  return `${TAB_KEY_PREFIX}${tabId}`;
}

async function getSettings() {
  const stored = await chrome.storage.local.get(SETTINGS_KEY);
  return normalizeSettings(stored[SETTINGS_KEY]);
}

function isDefaultAllowedHost(hostname) {
  const host = String(hostname || '').toLowerCase();
  return host === 'localhost' ||
    host === '127.0.0.1' ||
    /^chefs-(test|uat|dev|development|qa)\./.test(host) ||
    /\.(test|uat|dev)\.[a-z0-9.-]+$/.test(host);
}

function isProductionLikeHost(hostname) {
  const host = String(hostname || '').toLowerCase();
  return host.includes('chefs') && !isDefaultAllowedHost(host);
}

async function evaluateEnvironment(urlText) {
  let url;
  try {
    url = new URL(urlText);
  } catch (error) {
    return { allowed: false, reason: 'The active tab does not have a valid web address.' };
  }
  if (!['http:', 'https:'].includes(url.protocol)) {
    return { allowed: false, reason: 'Open a CHEFS form in a normal web tab first.' };
  }
  const settings = await getSettings();
  const host = url.hostname.toLowerCase();
  const additional = (settings.additionalHosts || []).map((value) => String(value).toLowerCase());
  if (isDefaultAllowedHost(host) || additional.includes(host)) {
    return { allowed: true, environment: 'non-production', settings };
  }
  if (settings.allowProduction && isProductionLikeHost(host)) {
    return { allowed: true, environment: 'production-override', settings };
  }
  return {
    allowed: false,
    reason: `Automatic submission is blocked on ${host}. Add the hostname in extension settings only when that environment is approved.`,
    settings
  };
}

async function readRun(runId) {
  if (!runId) {
    return null;
  }
  const stored = await chrome.storage.local.get(runKey(runId));
  return stored[runKey(runId)] || null;
}

function withRunLock(runId, operation) {
  const previous = runLocks.get(runId) || Promise.resolve();
  const next = previous
    .catch(() => undefined)
    .then(operation);
  const tracked = next.finally(() => {
    if (runLocks.get(runId) === tracked) {
      runLocks.delete(runId);
    }
  });
  runLocks.set(runId, tracked);
  return tracked;
}

async function mutateRun(runId, mutator) {
  return withRunLock(runId, async () => {
    const run = await readRun(runId);
    if (!run) {
      throw new Error(`Run ${runId} was not found.`);
    }
    const result = await mutator(run);
    run.updatedAt = new Date().toISOString();
    await chrome.storage.local.set({ [runKey(runId)]: run });
    return result === undefined ? run : result;
  });
}

function buildSummaryText(run) {
  const progress = run.progress || {};
  const failure = run.failure || {};
  const lines = [
    'CHEFS One-Click Form Tester',
    '',
    `Version: ${run.extensionVersion || EXTENSION_VERSION}`,
    `Build: ${run.buildNumber || BUILD_NUMBER}`,
    `Run ID: ${run.runId || 'Unknown'}`,
    '',
    `Form: ${run.formTitle || 'Unknown'}`,
    `URL: ${run.formUrl || 'Unknown'}`,
    `Result: ${String(run.status || 'UNKNOWN').toUpperCase()}`,
    '',
    `Passes completed: ${progress.pass || 0}`,
    `Components discovered: ${progress.discovered || 0}`,
    `Fields filled: ${progress.filled || 0}`,
    `Visible empty fields remaining: ${progress.remaining || 0}`,
    `Fields failed: ${progress.failed || 0}`,
    `Fields unsupported: ${progress.unsupported || 0}`,
    `Rows added: ${progress.rowsAdded || 0}`,
    `Attachments completed: ${progress.attachmentsCompleted || 0}`,
    `Attachments pending: ${progress.attachmentsPending || 0}`,
    `Submission attempts: ${progress.submitAttempts || 0}`,
    '',
    `Custom format rules loaded: ${progress.customRulesLoaded || (run.customRuleSet && run.customRuleSet.enabledRuleCount) || 0}`,
    `Custom rules matched: ${progress.customRuleMatches || 0}`,
    `Custom rule values accepted: ${progress.customRuleAccepted || 0}`,
    `Custom rule values rejected: ${progress.customRuleRejected || 0}`,
    `Detected masks used: ${progress.detectedMasksUsed || 0}`,
    `Mask values rejected: ${progress.maskValuesRejected || 0}`,
    `Rule-set hash: ${(run.customRuleSet && run.customRuleSet.ruleSetHash) || 'None'}`,
    '',
    'Last successful action:',
    run.lastSuccessfulAction || 'None recorded',
    '',
    'Current action:',
    run.currentAction || 'None recorded'
  ];
  if (run.confirmationId) {
    lines.push('', `Confirmation ID: ${run.confirmationId}`);
  }
  if (failure.message || failure.reason) {
    lines.push('', 'Failure or stall:', failure.message || failure.reason);
  }
  if (run.message) {
    lines.push('', 'Run message:', run.message);
  }
  return lines.join('\n');
}

function publicRun(run) {
  if (!run) {
    return null;
  }
  return {
    runId: run.runId,
    extensionVersion: run.extensionVersion,
    buildNumber: run.buildNumber,
    status: run.status,
    statusLabel: run.statusLabel,
    formTitle: run.formTitle,
    formUrl: run.formUrl,
    progress: run.progress || {},
    currentAction: run.currentAction,
    lastSuccessfulAction: run.lastSuccessfulAction,
    confirmationId: run.confirmationId,
    message: run.message,
    finalizedAt: run.finalizedAt || null,
    exportState: run.exportState || null,
    failure: run.failure ? {
      message: run.failure.message,
      reason: run.failure.reason,
      fieldKey: run.failure.fieldKey
    } : null,
    updatedAt: run.updatedAt,
    summaryText: buildSummaryText(run)
  };
}

async function createRun(message, sender) {
  const tabId = message.tabId || (sender.tab && sender.tab.id);
  const run = {
    runId: message.runId,
    tabId,
    extensionVersion: EXTENSION_VERSION,
    buildNumber: BUILD_NUMBER,
    engineVersion: EXTENSION_VERSION,
    chromeVersion: message.chromeVersion || '',
    formTitle: message.formTitle || '',
    formUrl: message.formUrl || '',
    formId: message.formId || '',
    startedAt: message.startedAt || new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    endedAt: null,
    status: 'initializing',
    statusLabel: 'Initializing',
    message: '',
    currentAction: 'Starting run',
    lastSuccessfulAction: '',
    confirmationId: null,
    customRuleSet: message.customRuleSet || { schemaVersion: 1, enabledRuleCount: 0, ruleSetHash: '', lastModifiedAt: '' },
    customFormatRules: Array.isArray(message.customFormatRules) ? message.customFormatRules : [],
    progress: {
      pass: 0,
      discovered: 0,
      filled: 0,
      remaining: 0,
      failed: 0,
      unsupported: 0,
      rowsAdded: 0,
      attachmentsCompleted: 0,
      attachmentsPending: 0,
      submitAttempts: 0,
      customRulesLoaded: message.customRuleSet && message.customRuleSet.enabledRuleCount || 0,
      customRuleMatches: 0,
      customRuleAccepted: 0,
      customRuleRejected: 0,
      detectedMasksUsed: 0,
      maskValuesRejected: 0
    },
    events: [],
    checkpoints: [],
    snapshots: {},
    validationErrors: [],
    attachments: [],
    failure: null,
    failureScreenshotDataUrl: null,
    finalizedAt: null,
    exportState: {
      automatic: {
        status: 'not_requested',
        requestedAt: null,
        completedAt: null,
        failedAt: null,
        filename: '',
        downloadPath: '',
        downloadId: null,
        error: ''
      }
    }
  };
  await chrome.storage.local.set({
    [runKey(run.runId)]: run,
    [tabKey(tabId)]: run.runId,
    [LAST_RUN_KEY]: run.runId
  });
  return publicRun(run);
}

async function appendEvent(message) {
  return mutateRun(message.runId, (run) => {
    const events = Array.isArray(message.events) ? message.events : [message.event];
    for (const event of events.filter(Boolean)) {
      run.events.push(event);
    }
    if (run.events.length > 12000) {
      run.events = run.events.slice(-12000);
      run.message = 'The oldest events were removed because the run exceeded the event retention limit.';
    }
  });
}

async function addCheckpoint(message) {
  return mutateRun(message.runId, (run) => {
    run.checkpoints.push(message.checkpoint);
    if (run.checkpoints.length > 3000) {
      run.checkpoints = run.checkpoints.slice(-3000);
    }
  });
}

async function updateRun(message) {
  return mutateRun(message.runId, (run) => {
    const patch = message.patch || {};
    if (patch.progress) {
      run.progress = Object.assign({}, run.progress || {}, patch.progress);
      delete patch.progress;
    }
    Object.assign(run, patch);
    if (['submitted', 'failed', 'stalled', 'blocked', 'stopped', 'safety_stop', 'completed'].includes(run.status)) {
      run.endedAt = run.endedAt || new Date().toISOString();
    }
  });
}

async function setSnapshot(message) {
  return mutateRun(message.runId, (run) => {
    run.snapshots[message.name] = message.snapshot;
  });
}

async function addValidationErrors(message) {
  return mutateRun(message.runId, (run) => {
    const errors = Array.isArray(message.errors) ? message.errors : [];
    run.validationErrors.push(...errors);
  });
}

async function addAttachmentRecord(message) {
  return mutateRun(message.runId, (run) => {
    run.attachments.push(message.attachment);
  });
}

async function setFailure(message, sender) {
  const run = await mutateRun(message.runId, (storedRun) => {
    storedRun.failure = message.failure || { message: 'Unknown failure' };
    storedRun.status = message.status || storedRun.status || 'failed';
    storedRun.statusLabel = message.statusLabel || String(storedRun.status).replaceAll('_', ' ');
    storedRun.endedAt = storedRun.endedAt || new Date().toISOString();
  });
  const settings = await getSettings();
  const tabId = (sender.tab && sender.tab.id) || run.tabId;
  if (settings.captureScreenshot && tabId) {
    try {
      const tab = await chrome.tabs.get(tabId);
      const dataUrl = await chrome.tabs.captureVisibleTab(tab.windowId, { format: 'png' });
      await mutateRun(message.runId, (storedRun) => {
        storedRun.failureScreenshotDataUrl = dataUrl;
      });
    } catch (error) {
      await appendEvent({
        runId: message.runId,
        event: {
          time: new Date().toISOString(),
          event: 'SCREENSHOT_CAPTURE_FAILED',
          message: error && error.message ? error.message : String(error)
        }
      });
    }
  }
  return true;
}

async function detectStaleRuns() {
  const stored = await chrome.storage.local.get(null);
  const now = Date.now();
  const candidates = Object.entries(stored)
    .filter(([key, value]) => key.startsWith(RUN_KEY_PREFIX) && value && ACTIVE_RUN_STATUSES.has(value.status));

  for (const [, candidate] of candidates) {
    const updated = Date.parse(candidate.updatedAt || candidate.startedAt || '');
    if (!Number.isFinite(updated) || now - updated < WATCHDOG_STALE_MS) {
      continue;
    }
    if (candidate.tabId) {
      try {
        const status = await chrome.tabs.sendMessage(candidate.tabId, {
          type: 'CHEFS_TESTER_STATUS'
        });
        if (
          status &&
          status.running &&
          status.runId === candidate.runId
        ) {
          await mutateRun(candidate.runId, (storedRun) => {
            storedRun.currentAction = status.currentAction || storedRun.currentAction;
            storedRun.progress = Object.assign({}, storedRun.progress || {}, status.progress || {});
            storedRun.watchdogState = Object.assign({}, storedRun.watchdogState || {}, {
              lastResponsiveProbeAt: new Date().toISOString()
            });
          });
          continue;
        }
      } catch (error) {
        // An absent or unresponsive content controller falls through to bounded stall finalization.
      }
    }
    const staleForMs = now - updated;
    await appendEvent({
      runId: candidate.runId,
      event: {
        time: new Date().toISOString(),
        event: 'WATCHDOG_STALL_DETECTED',
        pass: candidate.progress && candidate.progress.pass || 0,
        staleForMs,
        currentAction: candidate.currentAction || '',
        lastSuccessfulAction: candidate.lastSuccessfulAction || '',
        message: 'No diagnostic heartbeat was persisted within the watchdog window.'
      }
    });
    await setFailure({
      runId: candidate.runId,
      status: 'stalled',
      statusLabel: 'Stalled',
      failure: {
        time: new Date().toISOString(),
        reason: 'Background watchdog detected no diagnostic heartbeat.',
        message: `The run stopped persisting progress for ${Math.round(staleForMs / 1000)} seconds.`,
        currentAction: candidate.currentAction || '',
        lastSuccessfulAction: candidate.lastSuccessfulAction || '',
        watchdog: true,
        staleForMs
      }
    }, { tab: { id: candidate.tabId } });
    await setSnapshot({
      runId: candidate.runId,
      name: 'final',
      snapshot: candidate.snapshots && (candidate.snapshots.lastKnown || candidate.snapshots.initial) || []
    });
    await addCheckpoint({
      runId: candidate.runId,
      checkpoint: {
        time: new Date().toISOString(),
        reason: 'Background watchdog finalized stalled run',
        status: 'stalled',
        pass: candidate.progress && candidate.progress.pass || 0,
        currentAction: candidate.currentAction || '',
        lastSuccessfulAction: candidate.lastSuccessfulAction || '',
        progress: candidate.progress || {}
      }
    });
    await finalizeRun(candidate.runId, new Date().toISOString());
  }
}

chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm && alarm.name === WATCHDOG_ALARM) {
    detectStaleRuns().catch(() => undefined);
  } else if (alarm && alarm.name === BATCH_QUEUE_ALARM) {
    processBatchQueue().catch(() => undefined);
  }
});

async function getTabRun(tabId) {
  const stored = await chrome.storage.local.get([tabKey(tabId), LAST_RUN_KEY]);
  const runId = stored[tabKey(tabId)] || stored[LAST_RUN_KEY];
  return publicRun(await readRun(runId));
}

async function startRunInTab(tabId) {
  const tab = await chrome.tabs.get(tabId);
  const environment = await evaluateEnvironment(tab.url || '');
  if (!environment.allowed) {
    throw new Error(environment.reason);
  }
  await chrome.scripting.executeScript({
    target: { tabId },
    world: 'MAIN',
    files: ['page-bridge.js']
  });
  await chrome.scripting.executeScript({
    target: { tabId },
    files: ['content-script.js']
  });
  const response = await chrome.tabs.sendMessage(tabId, {
    type: 'CHEFS_TESTER_START',
    settings: environment.settings,
    environment: environment.environment,
    extensionVersion: EXTENSION_VERSION,
    buildNumber: BUILD_NUMBER
  });
  return response || { ok: true };
}

async function isChefsFormReady(tabId) {
  try {
    const results = await chrome.scripting.executeScript({
      target: { tabId },
      func: async () => {
        const sample = () => {
          const root = document.querySelector('.formio-form, [ref="webform"]');
          if (!root) {
            return { ready: false, componentCount: 0, interactiveCount: 0 };
          }
          const componentCount = root.querySelectorAll(
            '.formio-component[ref="component"], .formio-component'
          ).length;
          const interactiveCount = root.querySelectorAll(
            'input, select, textarea, button, [role="tab"]'
          ).length;
          return {
            ready: componentCount > 0 && interactiveCount > 0,
            componentCount,
            interactiveCount
          };
        };
        const first = sample();
        if (!first.ready) {
          return first;
        }
        await new Promise((resolve) => setTimeout(resolve, 300));
        const second = sample();
        return {
          ready: second.ready,
          componentCount: second.componentCount,
          interactiveCount: second.interactiveCount,
          stable: second.ready &&
            second.componentCount >= first.componentCount &&
            second.interactiveCount >= first.interactiveCount
        };
      }
    });
    const result = results && results[0] && results[0].result;
    return {
      ready: Boolean(result && result.ready && result.stable !== false),
      componentCount: boundedReadinessCount(result && result.componentCount),
      interactiveCount: boundedReadinessCount(result && result.interactiveCount),
      error: ''
    };
  } catch (error) {
    return {
      ready: false,
      error: error && error.message ? error.message : String(error)
    };
  }
}

function boundedReadinessCount(value) {
  const number = Number(value);
  return Number.isFinite(number) ? Math.max(0, Math.min(100000, Math.round(number))) : 0;
}

function emptyBatchState() {
  return {
    queue: [],
    active: null,
    completed: [],
    cancelledSuites: [],
    updatedAt: new Date().toISOString()
  };
}

function normalizeBatchState(rawState) {
  const state = Object.assign(emptyBatchState(), rawState || {});
  state.queue = Array.isArray(state.queue) ? state.queue : [];
  state.active = state.active || null;
  state.completed = Array.isArray(state.completed)
    ? state.completed.slice(-BATCH_COMPLETED_LIMIT)
    : [];
  state.cancelledSuites = Array.isArray(state.cancelledSuites)
    ? state.cancelledSuites.slice(-20)
    : [];
  return state;
}

async function getBatchState() {
  const stored = await chrome.storage.local.get(BATCH_QUEUE_KEY);
  return normalizeBatchState(stored[BATCH_QUEUE_KEY]);
}

async function saveBatchState(state) {
  state.updatedAt = new Date().toISOString();
  state.completed = (state.completed || []).slice(-BATCH_COMPLETED_LIMIT);
  state.cancelledSuites = (state.cancelledSuites || []).slice(-20);
  await chrome.storage.local.set({ [BATCH_QUEUE_KEY]: state });
  return state;
}

function withBatchStateLock(operation) {
  const next = batchStateLock
    .catch(() => undefined)
    .then(operation);
  batchStateLock = next.catch(() => undefined);
  return next;
}

function batchEntryComparator(left, right) {
  if (left.suiteId !== right.suiteId) {
    return (left.queuedAt || 0) - (right.queuedAt || 0);
  }
  const leftNumber = Number(left.index);
  const rightNumber = Number(right.index);
  if (Number.isFinite(leftNumber) && Number.isFinite(rightNumber)) {
    return leftNumber - rightNumber;
  }
  return String(left.index || '').localeCompare(String(right.index || ''));
}

function parseBatchMarker(urlText) {
  try {
    const url = new URL(urlText);
    const parameters = new URLSearchParams(url.hash.replace(/^#/, ''));
    const token = parameters.get(BATCH_MARKER_PARAM) || '';
    if (!token) {
      return null;
    }
    const rawSuiteId = parameters.get('suite') || 'regression';
    const rawIndex = parameters.get('index') || '0';
    parameters.delete(BATCH_MARKER_PARAM);
    parameters.delete('suite');
    parameters.delete('index');
    url.hash = parameters.toString() ? `#${parameters.toString()}` : '';
    return {
      token,
      suiteId: rawSuiteId.replace(/[^A-Za-z0-9_-]/g, '').slice(0, 64) || 'regression',
      index: rawIndex.replace(/[^A-Za-z0-9_-]/g, '').slice(0, 32) || '0',
      url: url.href,
      origin: url.origin
    };
  } catch (error) {
    return null;
  }
}

async function scrubBatchMarker(tabId) {
  await chrome.scripting.executeScript({
    target: { tabId },
    func: (markerName) => {
      const url = new URL(window.location.href);
      const parameters = new URLSearchParams(url.hash.replace(/^#/, ''));
      parameters.delete(markerName);
      parameters.delete('suite');
      parameters.delete('index');
      url.hash = parameters.toString() ? `#${parameters.toString()}` : '';
      window.history.replaceState(window.history.state, document.title, url.href);
    },
    args: [BATCH_MARKER_PARAM]
  });
}

function permissionPatternForUrl(urlText) {
  const url = new URL(urlText);
  return `${url.protocol}//${url.hostname}/*`;
}

function batchCompletedRecord(entry, status, details) {
  return Object.assign({
    tabId: entry.tabId,
    suiteId: entry.suiteId,
    index: entry.index,
    url: entry.url,
    status,
    completedAt: new Date().toISOString()
  }, details || {});
}

async function recordBatchRejection(tabId, marker, reason) {
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (
      state.completed.some((item) => item.tabId === tabId) ||
      (state.active && state.active.tabId === tabId)
    ) {
      return;
    }
    state.queue = state.queue.filter((item) => item.tabId !== tabId);
    state.completed.push(batchCompletedRecord({
      tabId,
      suiteId: marker.suiteId,
      index: marker.index,
      url: marker.url
    }, 'launcher_rejected', { error: reason }));
    await saveBatchState(state);
  });
}

async function upsertBatchTab(tab, ready) {
  const tabId = tab && tab.id;
  const marker = parseBatchMarker(tab && (tab.pendingUrl || tab.url) || '');
  if (!tabId || !marker) {
    return;
  }
  const settings = await getSettings();
  if (
    !settings.batchLauncherEnabled ||
    !settings.batchLauncherToken ||
    marker.token !== settings.batchLauncherToken
  ) {
    return;
  }
  if (!settings.batchOrigins.includes(marker.origin)) {
    await recordBatchRejection(tabId, marker, `Origin ${marker.origin} is not configured for batch launching.`);
    return;
  }
  const environment = await evaluateEnvironment(marker.url);
  if (!environment.allowed) {
    await recordBatchRejection(tabId, marker, environment.reason);
    return;
  }
  const hasPermission = await chrome.permissions.contains({
    origins: [permissionPatternForUrl(marker.url)]
  });
  if (!hasPermission) {
    await recordBatchRejection(tabId, marker, `Chrome host access has not been granted for ${marker.origin}.`);
    return;
  }
  if (ready) {
    try {
      await scrubBatchMarker(tabId);
    } catch (error) {
      await recordBatchRejection(
        tabId,
        marker,
        `The launcher marker could not be removed before automation: ${
          error && error.message ? error.message : String(error)
        }`
      );
      return;
    }
  }

  await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (state.cancelledSuites.includes(marker.suiteId)) {
      return;
    }
    if (
      state.completed.some((item) => item.tabId === tabId) ||
      (state.active && state.active.tabId === tabId)
    ) {
      return;
    }
    const existing = state.queue.find((item) => item.tabId === tabId);
    if (existing) {
      existing.ready = existing.ready || Boolean(ready);
      existing.url = marker.url;
    } else {
      state.queue.push({
        tabId,
        suiteId: marker.suiteId,
        index: marker.index,
        url: marker.url,
        origin: marker.origin,
        ready: Boolean(ready),
        queuedAt: Date.now()
      });
    }
    state.queue.sort(batchEntryComparator);
    await saveBatchState(state);
  });
  scheduleBatchProcessing();
}

function scheduleBatchProcessing(delayMs) {
  if (batchScheduleTimer) {
    clearTimeout(batchScheduleTimer);
  }
  batchScheduleTimer = setTimeout(() => {
    batchScheduleTimer = null;
    processBatchQueue().catch(() => undefined);
  }, Number.isFinite(delayMs) ? delayMs : BATCH_COLLECTION_DELAY_MS);
}

async function completeBatchRun(run) {
  const result = {
    completed: false,
    batchFinished: false,
    entry: null
  };
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (!state.active) {
      return;
    }
    if (
      state.active.tabId !== run.tabId ||
      (state.active.runId && state.active.runId !== run.runId)
    ) {
      return;
    }
    const entry = Object.assign({}, state.active);
    state.completed.push(batchCompletedRecord(entry, run.status, {
      runId: run.runId,
      exportStatus: run.exportState && run.exportState.automatic &&
        run.exportState.automatic.status || 'not_requested'
    }));
    state.active = null;
    result.completed = true;
    result.batchFinished = state.queue.length === 0;
    result.entry = entry;
    await saveBatchState(state);
  });
  return result;
}

async function markActiveBatchFailure(entry, status, error) {
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (!state.active || state.active.tabId !== entry.tabId) {
      return;
    }
    state.completed.push(batchCompletedRecord(state.active, status, {
      error: error && error.message ? error.message : String(error || status)
    }));
    state.active = null;
    await saveBatchState(state);
  });
}

async function claimNextBatchEntry() {
  return await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (state.active || !state.queue.length) {
      return null;
    }
    const first = state.queue[0];
    if (!first.ready) {
      if (Date.now() - first.queuedAt > BATCH_ENTRY_TIMEOUT_MS) {
        state.queue.shift();
        state.completed.push(batchCompletedRecord(first, 'load_timeout', {
          error: 'The marked tab did not finish loading within the batch timeout.'
        }));
        await saveBatchState(state);
        return { retry: true };
      }
      return null;
    }
    state.queue.shift();
    state.active = Object.assign({}, first, {
      status: 'starting',
      startedAt: Date.now(),
      runId: null
    });
    await saveBatchState(state);
    return Object.assign({}, state.active);
  });
}

async function recoverActiveBatchRun(active) {
  if (active.runId) {
    return await readRun(active.runId);
  }
  const stored = await chrome.storage.local.get(tabKey(active.tabId));
  const runId = stored[tabKey(active.tabId)];
  if (!runId) {
    return null;
  }
  const run = await readRun(runId);
  if (run) {
    await withBatchStateLock(async () => {
      const state = await getBatchState();
      if (state.active && state.active.tabId === active.tabId && !state.active.runId) {
        state.active.runId = runId;
        state.active.status = 'running';
        await saveBatchState(state);
      }
    });
  }
  return run;
}

async function resolveInterruptedAutomaticExport(run) {
  const automatic = run && run.exportState && run.exportState.automatic;
  if (
    !automatic ||
    automatic.status !== 'pending' ||
    automaticExportsInProgress.has(run.runId)
  ) {
    return run;
  }
  return await mutateRun(run.runId, (storedRun) => {
    const storedAutomatic = ensureAutomaticExportState(storedRun);
    if (storedAutomatic.status === 'pending') {
      storedAutomatic.status = 'failed';
      storedAutomatic.failedAt = new Date().toISOString();
      storedAutomatic.completedAt = null;
      storedAutomatic.error =
        'The automatic export was interrupted when the extension background worker restarted. Use Export Last Run to retry.';
    }
  });
}

async function updateActiveBatchReadiness(entry, readiness) {
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    if (!state.active || state.active.tabId !== entry.tabId || state.active.runId) {
      return;
    }
    state.active.status = 'waiting_for_form';
    state.active.readinessStartedAt =
      state.active.readinessStartedAt || state.active.startedAt || Date.now();
    state.active.lastReadinessCheckAt = Date.now();
    state.active.readinessError = readiness.error || '';
    state.active.readinessComponentCount = readiness.componentCount || 0;
    state.active.readinessInteractiveCount = readiness.interactiveCount || 0;
    await saveBatchState(state);
  });
}

async function startActiveBatchEntry(entry) {
  await chrome.tabs.get(entry.tabId);
  await chrome.tabs.update(entry.tabId, { active: true });
  const readiness = await isChefsFormReady(entry.tabId);
  if (!readiness.ready) {
    await updateActiveBatchReadiness(entry, readiness);
    scheduleBatchProcessing(BATCH_FORM_READY_POLL_MS);
    return false;
  }
  const response = await startRunInTab(entry.tabId);
  if (!response || !response.ok) {
    throw new Error(response && response.error ? response.error : 'The batch run could not be started.');
  }
  await withBatchStateLock(async () => {
    const latest = await getBatchState();
    if (latest.active && latest.active.tabId === entry.tabId) {
      latest.active.runId = response.runId || null;
      latest.active.status = 'running';
      latest.active.readinessError = '';
      latest.active.readinessComponentCount = readiness.componentCount || 0;
      latest.active.readinessInteractiveCount = readiness.interactiveCount || 0;
      await saveBatchState(latest);
    }
  });
  return true;
}

async function processBatchQueue() {
  if (batchProcessorRunning) {
    return;
  }
  batchProcessorRunning = true;
  try {
    while (true) {
      const state = await getBatchState();
      if (state.active) {
        let run = await recoverActiveBatchRun(state.active);
        if (run && FINAL_RUN_STATUSES.has(run.status)) {
          const automatic = run.exportState && run.exportState.automatic;
          if (
            automatic &&
            automatic.status === 'pending' &&
            automaticExportsInProgress.has(run.runId)
          ) {
            return;
          }
          run = await resolveInterruptedAutomaticExport(run);
          await finalizeRun(run.runId, run.finalizedAt);
          continue;
        }
        if (run) {
          return;
        }
        const readinessStartedAt =
          state.active.readinessStartedAt || state.active.startedAt || Date.now();
        if (Date.now() - readinessStartedAt > BATCH_FORM_READY_TIMEOUT_MS) {
          await markActiveBatchFailure(
            state.active,
            'form_not_ready',
            new Error('The CHEFS Form.io form did not become ready within the launcher timeout.')
          );
          continue;
        }
        try {
          await startActiveBatchEntry(state.active);
        } catch (error) {
          await markActiveBatchFailure(state.active, 'launcher_failed', error);
          continue;
        }
        return;
      }

      const entry = await claimNextBatchEntry();
      if (!entry) {
        return;
      }
      if (entry.retry) {
        continue;
      }
      try {
        await startActiveBatchEntry(entry);
        return;
      } catch (error) {
        await markActiveBatchFailure(entry, 'launcher_failed', error);
      }
    }
  } finally {
    batchProcessorRunning = false;
  }
}

async function stopBatch() {
  let active = null;
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    active = state.active ? Object.assign({}, state.active) : null;
    const suiteIds = new Set(state.cancelledSuites || []);
    for (const entry of state.queue) {
      suiteIds.add(entry.suiteId);
      state.completed.push(batchCompletedRecord(entry, 'stopped_before_start'));
    }
    if (active) {
      suiteIds.add(active.suiteId);
      state.active.stopRequestedAt = new Date().toISOString();
    }
    state.cancelledSuites = Array.from(suiteIds).slice(-20);
    state.queue = [];
    await saveBatchState(state);
  });

  if (active) {
    try {
      await chrome.tabs.sendMessage(active.tabId, { type: 'CHEFS_TESTER_STOP' });
    } catch (error) {
      await markActiveBatchFailure(active, 'launcher_stopped', error);
      scheduleBatchProcessing();
    }
  }
  return await getBatchState();
}

async function handleBatchTabRemoved(tabId) {
  let changed = false;
  await withBatchStateLock(async () => {
    const state = await getBatchState();
    const queued = state.queue.filter((entry) => entry.tabId === tabId);
    if (queued.length) {
      state.queue = state.queue.filter((entry) => entry.tabId !== tabId);
      for (const entry of queued) {
        state.completed.push(batchCompletedRecord(entry, 'tab_closed'));
      }
      changed = true;
    }
    if (state.active && state.active.tabId === tabId) {
      state.completed.push(batchCompletedRecord(state.active, 'tab_closed'));
      state.active = null;
      changed = true;
    }
    if (changed) {
      await saveBatchState(state);
    }
  });
  if (changed) {
    scheduleBatchProcessing();
  }
}

chrome.tabs.onCreated.addListener((tab) => {
  upsertBatchTab(tab, tab.status === 'complete').catch(() => undefined);
});

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  const candidate = Object.assign({}, tab, {
    id: tabId,
    url: changeInfo.url || tab.url
  });
  upsertBatchTab(candidate, changeInfo.status === 'complete' || tab.status === 'complete')
    .catch(() => undefined);
});

chrome.tabs.onRemoved.addListener((tabId) => {
  handleBatchTabRemoved(tabId).catch(() => undefined);
});

function encodeUtf8(text) {
  return new TextEncoder().encode(String(text));
}

function uint16(value) {
  return new Uint8Array([value & 0xff, (value >>> 8) & 0xff]);
}

function uint32(value) {
  return new Uint8Array([
    value & 0xff,
    (value >>> 8) & 0xff,
    (value >>> 16) & 0xff,
    (value >>> 24) & 0xff
  ]);
}

function concatArrays(arrays) {
  const length = arrays.reduce((sum, array) => sum + array.length, 0);
  const output = new Uint8Array(length);
  let offset = 0;
  for (const array of arrays) {
    output.set(array, offset);
    offset += array.length;
  }
  return output;
}

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n += 1) {
    let c = n;
    for (let k = 0; k < 8; k += 1) {
      c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
    }
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc = CRC_TABLE[(crc ^ byte) & 0xff] ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function dosDateTime(date) {
  const year = Math.max(1980, date.getFullYear());
  const dosTime = (date.getHours() << 11) | (date.getMinutes() << 5) | Math.floor(date.getSeconds() / 2);
  const dosDate = ((year - 1980) << 9) | ((date.getMonth() + 1) << 5) | date.getDate();
  return { dosTime, dosDate };
}

function createZip(files) {
  const localParts = [];
  const centralParts = [];
  let offset = 0;
  const now = dosDateTime(new Date());

  for (const file of files) {
    const nameBytes = encodeUtf8(file.name);
    const data = file.data instanceof Uint8Array ? file.data : encodeUtf8(file.data);
    const crc = crc32(data);
    const localHeader = concatArrays([
      uint32(0x04034b50),
      uint16(20),
      uint16(0x0800),
      uint16(0),
      uint16(now.dosTime),
      uint16(now.dosDate),
      uint32(crc),
      uint32(data.length),
      uint32(data.length),
      uint16(nameBytes.length),
      uint16(0),
      nameBytes
    ]);
    localParts.push(localHeader, data);

    const centralHeader = concatArrays([
      uint32(0x02014b50),
      uint16(20),
      uint16(20),
      uint16(0x0800),
      uint16(0),
      uint16(now.dosTime),
      uint16(now.dosDate),
      uint32(crc),
      uint32(data.length),
      uint32(data.length),
      uint16(nameBytes.length),
      uint16(0),
      uint16(0),
      uint16(0),
      uint16(0),
      uint32(0),
      uint32(offset),
      nameBytes
    ]);
    centralParts.push(centralHeader);
    offset += localHeader.length + data.length;
  }

  const local = concatArrays(localParts);
  const central = concatArrays(centralParts);
  const end = concatArrays([
    uint32(0x06054b50),
    uint16(0),
    uint16(0),
    uint16(files.length),
    uint16(files.length),
    uint32(central.length),
    uint32(local.length),
    uint16(0)
  ]);
  return concatArrays([local, central, end]);
}

function dataUrlToBytes(dataUrl) {
  const comma = dataUrl.indexOf(',');
  const base64 = comma >= 0 ? dataUrl.slice(comma + 1) : dataUrl;
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

function bytesToBase64(bytes) {
  let binary = '';
  const chunkSize = 0x8000;
  for (let i = 0; i < bytes.length; i += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
  }
  return btoa(binary);
}

function prettyJson(value) {
  return JSON.stringify(value, null, 2);
}

function createRunBundle(run) {
  const files = [
    {
      name: 'manifest.json',
      data: prettyJson({
        extensionVersion: run.extensionVersion,
        buildNumber: run.buildNumber,
        engineVersion: run.engineVersion,
        chromeVersion: run.chromeVersion,
        runId: run.runId,
        tabId: run.tabId,
        formTitle: run.formTitle,
        formUrl: run.formUrl,
        formId: run.formId,
        startedAt: run.startedAt,
        endedAt: run.endedAt,
        finalizedAt: run.finalizedAt || null,
        result: run.status,
        customRuleSet: run.customRuleSet || null,
        exportState: run.exportState || null
      })
    },
    { name: 'run-summary.txt', data: buildSummaryText(run) },
    { name: 'events.jsonl', data: (run.events || []).map((event) => JSON.stringify(event)).join('\n') },
    { name: 'checkpoints.jsonl', data: (run.checkpoints || []).map((checkpoint) => JSON.stringify(checkpoint)).join('\n') },
    { name: 'initial-components.json', data: prettyJson(run.snapshots && run.snapshots.initial || []) },
    { name: 'last-known-components.json', data: prettyJson(run.snapshots && run.snapshots.lastKnown || []) },
    { name: 'final-components.json', data: prettyJson(run.snapshots && run.snapshots.final || []) },
    { name: 'validation-errors.json', data: prettyJson(run.validationErrors || []) },
    { name: 'attachments.json', data: prettyJson(run.attachments || []) },
    {
      name: 'custom-format-rules.json',
      data: prettyJson({
        schemaVersion: run.customRuleSet && run.customRuleSet.schemaVersion || 1,
        ruleSetHash: run.customRuleSet && run.customRuleSet.ruleSetHash || '',
        enabledRuleCount: run.customRuleSet && run.customRuleSet.enabledRuleCount || 0,
        rules: run.customFormatRules || []
      })
    }
  ];
  if (run.failure) {
    files.push({ name: 'failure.json', data: prettyJson(run.failure) });
  }
  if (run.failureScreenshotDataUrl) {
    files.push({ name: 'failure-screenshot.png', data: dataUrlToBytes(run.failureScreenshotDataUrl) });
  }
  const zip = createZip(files);
  const safeRun = String(run.runId).replace(/[^A-Za-z0-9_-]/g, '');
  const stamp = new Date(run.startedAt || Date.now()).toISOString().replace(/[-:]/g, '').replace(/\..+/, '').replace('T', '-');
  const filename = `chefs-one-click-tester-v${run.extensionVersion}-build-${run.buildNumber}-run-${safeRun}-${stamp}.zip`;
  const url = `data:application/zip;base64,${bytesToBase64(zip)}`;
  return { filename, url };
}

async function downloadRun(runId, options) {
  const run = await readRun(runId);
  if (!run) {
    throw new Error(`Run ${runId} was not found.`);
  }
  const settings = await getSettings();
  const bundle = createRunBundle(run);
  const downloadPath = ChefsExportPath.joinExportPath(settings.exportFolder, bundle.filename);
  const downloadId = await chrome.downloads.download({
    url: bundle.url,
    filename: downloadPath,
    conflictAction: 'uniquify',
    saveAs: Boolean(options && options.saveAs)
  });
  return {
    filename: bundle.filename,
    downloadPath,
    downloadId: downloadId === undefined ? null : downloadId
  };
}

function ensureAutomaticExportState(run) {
  run.exportState = run.exportState || {};
  run.exportState.automatic = Object.assign({
    status: 'not_requested',
    requestedAt: null,
    completedAt: null,
    failedAt: null,
    filename: '',
    downloadPath: '',
    downloadId: null,
    error: ''
  }, run.exportState.automatic || {});
  return run.exportState.automatic;
}

function emptyDashboardState(defaultView) {
  return {
    schemaVersion: ChefsDashboardModel.SCHEMA_VERSION,
    mode: 'empty',
    defaultView: defaultView || 'simple',
    updatedAt: new Date().toISOString(),
    selectedRunRef: '',
    runs: [],
    batch: null,
    history: []
  };
}

function normalizeDashboardState(rawState, defaultView) {
  const state = Object.assign(emptyDashboardState(defaultView), rawState || {});
  state.schemaVersion = ChefsDashboardModel.SCHEMA_VERSION;
  state.mode = ['empty', 'run', 'batch'].includes(state.mode) ? state.mode : 'empty';
  state.defaultView = ['simple', 'analyst', 'statistical', 'experimental']
    .includes(state.defaultView) ? state.defaultView : 'simple';
  state.runs = Array.isArray(state.runs)
    ? state.runs.filter(ChefsDashboardModel.isDashboardSummary).slice(0, 200)
    : [];
  state.history = ChefsDashboardModel.trimHistory(state.history || []);
  state.batch = state.batch && typeof state.batch === 'object' ? {
    suiteRef: /^run-[0-9a-f]{8}$/.test(String(state.batch.suiteRef || ''))
      ? state.batch.suiteRef
      : 'run-unknown',
    completed: Boolean(state.batch.completed),
    completedAt: Number.isFinite(Date.parse(String(state.batch.completedAt || '')))
      ? new Date(state.batch.completedAt).toISOString()
      : null
  } : null;
  return state;
}

async function getDashboardState() {
  const settings = await getSettings();
  const stored = await chrome.storage.local.get([DASHBOARD_STATE_KEY, DASHBOARD_HISTORY_KEY]);
  const state = normalizeDashboardState(stored[DASHBOARD_STATE_KEY], settings.dashboardDefaultView);
  state.defaultView = settings.dashboardDefaultView;
  state.history = settings.retainDashboardHistory
    ? ChefsDashboardModel.trimHistory(stored[DASHBOARD_HISTORY_KEY] || [])
    : [];
  return state;
}

async function saveDashboardState(state, history) {
  state.updatedAt = new Date().toISOString();
  const values = { [DASHBOARD_STATE_KEY]: state };
  if (history) {
    values[DASHBOARD_HISTORY_KEY] = history;
  }
  await chrome.storage.local.set(values);
  return state;
}

function compareDashboardRuns(left, right) {
  const leftIndex = Number(left && left.batch && left.batch.index);
  const rightIndex = Number(right && right.batch && right.batch.index);
  if (Number.isFinite(leftIndex) && Number.isFinite(rightIndex)) {
    return leftIndex - rightIndex;
  }
  return Date.parse(left.startedAt || '') - Date.parse(right.startedAt || '');
}

async function recordDashboardRun(run, batchCompletion, settings) {
  const entry = batchCompletion && batchCompletion.entry;
  const context = entry ? { suiteId: entry.suiteId, index: entry.index } : null;
  const summary = ChefsDashboardModel.buildRunSummary(run, context);
  const stored = await chrome.storage.local.get([DASHBOARD_STATE_KEY, DASHBOARD_HISTORY_KEY]);
  const state = normalizeDashboardState(stored[DASHBOARD_STATE_KEY], settings.dashboardDefaultView);
  let history = ChefsDashboardModel.trimHistory(stored[DASHBOARD_HISTORY_KEY] || []);
  if (settings.retainDashboardHistory) {
    history = ChefsDashboardModel.trimHistory(history.concat(summary));
  }
  state.defaultView = settings.dashboardDefaultView;
  state.selectedRunRef = summary.runRef;
  if (entry) {
    const suiteRef = summary.batch.suiteRef;
    if (!state.batch || state.batch.suiteRef !== suiteRef) {
      state.batch = {
        suiteRef,
        completed: false,
        completedAt: null
      };
      state.runs = [];
    }
    const existingIndex = state.runs.findIndex((item) => item.runRef === summary.runRef);
    if (existingIndex >= 0) {
      state.runs[existingIndex] = summary;
    } else {
      state.runs.push(summary);
    }
    state.runs.sort(compareDashboardRuns);
    state.mode = 'batch';
    if (batchCompletion.batchFinished) {
      state.batch.completed = true;
      state.batch.completedAt = new Date().toISOString();
    }
  } else {
    state.mode = 'run';
    state.batch = null;
    state.runs = [summary];
  }
  state.history = settings.retainDashboardHistory ? history : [];
  await saveDashboardState(state, state.history);
  return state;
}

async function openDashboardTab() {
  const dashboardUrl = chrome.runtime.getURL(DASHBOARD_PAGE);
  const existing = await chrome.tabs.query({ url: `${dashboardUrl}*` });
  if (existing && existing.length) {
    const tab = existing[0];
    if (chrome.tabs.reload) {
      await chrome.tabs.reload(tab.id);
    }
    await chrome.tabs.update(tab.id, { active: true });
    return { reused: true, tabId: tab.id };
  }
  const tab = await chrome.tabs.create({ url: dashboardUrl, active: true });
  return { reused: false, tabId: tab.id };
}

async function clearDashboardHistory() {
  const state = await getDashboardState();
  state.history = [];
  await saveDashboardState(state, []);
  return state;
}

async function handleDashboardCompletion(run, batchCompletion) {
  const settings = await getSettings();
  let shouldProcess = false;
  const processedRun = await mutateRun(run.runId, (storedRun) => {
    storedRun.dashboardState = storedRun.dashboardState || {};
    if (!storedRun.dashboardState.processedAt) {
      storedRun.dashboardState.processedAt = new Date().toISOString();
      storedRun.dashboardState.context = batchCompletion && batchCompletion.completed
        ? 'batch'
        : 'singleton';
      shouldProcess = true;
    }
  });
  if (!shouldProcess) {
    return;
  }
  await recordDashboardRun(processedRun, batchCompletion, settings);
  const shouldOpen = settings.openDashboardAfterCompletion && (
    !batchCompletion ||
    !batchCompletion.completed ||
    batchCompletion.batchFinished
  );
  if (shouldOpen) {
    await openDashboardTab();
  }
}

async function completeRunPostProcessing(run) {
  const batchCompletion = await completeBatchRun(run);
  try {
    await handleDashboardCompletion(run, batchCompletion);
  } catch (error) {
    await appendEvent({
      runId: run.runId,
      event: {
        time: new Date().toISOString(),
        event: 'DASHBOARD_UPDATE_FAILED'
      }
    }).catch(() => undefined);
  } finally {
    if (batchCompletion.completed) {
      scheduleBatchProcessing(0);
    }
  }
}

async function finalizeRun(runId, finalizedAt) {
  const settings = await getSettings();
  let shouldExport = false;
  let run = await mutateRun(runId, (storedRun) => {
    storedRun.finalizedAt = storedRun.finalizedAt || finalizedAt || new Date().toISOString();
    const automatic = ensureAutomaticExportState(storedRun);
    if (
      FINAL_RUN_STATUSES.has(storedRun.status) &&
      settings.autoExportAfterRun &&
      automatic.status === 'not_requested'
    ) {
      automatic.status = 'pending';
      automatic.requestedAt = new Date().toISOString();
      automatic.completedAt = null;
      automatic.failedAt = null;
      automatic.error = '';
      shouldExport = true;
    }
  });

  if (!shouldExport) {
    const automatic = run && run.exportState && run.exportState.automatic;
    if (automatic && automatic.status === 'pending') {
      if (automaticExportsInProgress.has(runId)) {
        return publicRun(run);
      }
      run = await resolveInterruptedAutomaticExport(run);
    }
    await completeRunPostProcessing(run);
    return publicRun(run);
  }

  automaticExportsInProgress.add(runId);
  try {
    const result = await downloadRun(runId, { saveAs: false });
    run = await mutateRun(runId, (storedRun) => {
      const automatic = ensureAutomaticExportState(storedRun);
      automatic.status = 'succeeded';
      automatic.completedAt = new Date().toISOString();
      automatic.failedAt = null;
      automatic.filename = result.filename;
      automatic.downloadPath = result.downloadPath;
      automatic.downloadId = result.downloadId;
      automatic.error = '';
    });
  } catch (error) {
    run = await mutateRun(runId, (storedRun) => {
      const automatic = ensureAutomaticExportState(storedRun);
      automatic.status = 'failed';
      automatic.failedAt = new Date().toISOString();
      automatic.completedAt = null;
      automatic.error = error && error.message ? error.message : String(error);
    });
  } finally {
    automaticExportsInProgress.delete(runId);
  }
  await completeRunPostProcessing(run);
  return publicRun(run);
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  (async () => {
    switch (message.type) {
      case 'START_RUN_IN_TAB':
        return await startRunInTab(message.tabId);
      case 'CREATE_RUN':
        return { ok: true, run: await createRun(message, sender) };
      case 'APPEND_EVENTS':
        await appendEvent(message);
        return { ok: true };
      case 'ADD_CHECKPOINT':
        await addCheckpoint(message);
        return { ok: true };
      case 'UPDATE_RUN':
        await updateRun(message);
        return { ok: true };
      case 'SET_SNAPSHOT':
        await setSnapshot(message);
        return { ok: true };
      case 'ADD_VALIDATION_ERRORS':
        await addValidationErrors(message);
        return { ok: true };
      case 'ADD_ATTACHMENT_RECORD':
        await addAttachmentRecord(message);
        return { ok: true };
      case 'SET_FAILURE':
        await setFailure(message, sender);
        return { ok: true };
      case 'GET_TAB_RUN':
        return { ok: true, run: await getTabRun(message.tabId) };
      case 'GET_BATCH_STATE':
        return { ok: true, state: await getBatchState() };
      case 'STOP_BATCH':
        return { ok: true, state: await stopBatch() };
      case 'GET_DASHBOARD_STATE':
        return { ok: true, state: await getDashboardState() };
      case 'OPEN_DASHBOARD':
        return { ok: true, ...(await openDashboardTab()) };
      case 'CLEAR_DASHBOARD_HISTORY':
        return { ok: true, state: await clearDashboardHistory() };
      case 'EXPORT_RUN': {
        const result = await downloadRun(message.runId, { saveAs: true });
        return { ok: true, ...result };
      }
      case 'RUN_FINALIZED':
        return {
          ok: true,
          run: await finalizeRun(message.runId, message.finalizedAt)
        };
      case 'GET_SETTINGS':
        return { ok: true, settings: await getSettings() };
      default:
        return { ok: false, error: `Unknown message type: ${message.type}` };
    }
  })()
    .then(sendResponse)
    .catch((error) => sendResponse({
      ok: false,
      error: error && error.message ? error.message : String(error)
    }));
  return true;
});
