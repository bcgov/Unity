'use strict';

const VERSION = '0.4.9';
const BUILD = '2026.08.28.23';
const RUNNING_STATUSES = new Set(['initializing', 'scanning', 'filling', 'settling', 'validating', 'submitting', 'waiting']);
const SUCCESS_STATUSES = new Set(['submitted', 'completed']);
const FAILURE_STATUSES = new Set(['failed', 'stalled', 'blocked', 'safety_stop']);
let activeTabId = null;
let pollTimer = null;
let latestRun = null;

const elements = {
  statusPill: document.getElementById('statusPill'),
  startButton: document.getElementById('startButton'),
  stopButton: document.getElementById('stopButton'),
  exportButton: document.getElementById('exportButton'),
  copyButton: document.getElementById('copyButton'),
  dashboardButton: document.getElementById('dashboardButton'),
  settingsButton: document.getElementById('settingsButton'),
  stopBatchButton: document.getElementById('stopBatchButton'),
  runId: document.getElementById('runId'),
  passCount: document.getElementById('passCount'),
  filledCount: document.getElementById('filledCount'),
  remainingCount: document.getElementById('remainingCount'),
  currentAction: document.getElementById('currentAction'),
  confirmationId: document.getElementById('confirmationId'),
  batchActive: document.getElementById('batchActive'),
  batchQueued: document.getElementById('batchQueued'),
  batchCompleted: document.getElementById('batchCompleted'),
  message: document.getElementById('message')
};

document.getElementById('versionText').textContent = `v${VERSION} build ${BUILD}`;

function setMessage(text) {
  elements.message.textContent = text || '';
}

function errorMessage(error, fallback) {
  return error?.message || fallback || String(error);
}

function statusClass(status) {
  if (RUNNING_STATUSES.has(status)) {
    return 'running';
  }
  if (SUCCESS_STATUSES.has(status)) {
    return 'success';
  }
  return FAILURE_STATUSES.has(status) ? 'failure' : 'warning';
}

function runMessage(run, resultLabel) {
  const automatic = run.exportState?.automatic;
  if (automatic?.status === 'pending') {
    return `${resultLabel}. Preparing automatic export...`;
  }
  if (automatic?.status === 'succeeded') {
    return `${resultLabel}. Automatic export saved to ${automatic.downloadPath}.`;
  }
  if (automatic?.status === 'failed') {
    return `${resultLabel}. Automatic export failed: ${automatic.error} Use Export Last Run.`;
  }
  return run.failure?.message || run.message || '';
}

function renderRun(run) {
  latestRun = run || null;
  if (!run) {
    elements.statusPill.textContent = 'Ready';
    elements.statusPill.className = 'pill idle';
    elements.runId.textContent = 'None';
    elements.passCount.textContent = '0';
    elements.filledCount.textContent = '0';
    elements.remainingCount.textContent = '0';
    elements.currentAction.textContent = 'Idle';
    elements.confirmationId.textContent = '-';
    elements.startButton.classList.remove('hidden');
    elements.stopButton.classList.add('hidden');
    elements.exportButton.disabled = true;
    elements.copyButton.disabled = true;
    return;
  }

  const status = String(run.status || 'unknown').toLowerCase();
  const isRunning = RUNNING_STATUSES.has(status);

  elements.statusPill.textContent = run.statusLabel || status.replaceAll('_', ' ');
  elements.statusPill.className = `pill ${statusClass(status)}`;
  elements.runId.textContent = run.runId || 'Unknown';
  elements.passCount.textContent = String(run.progress?.pass || 0);
  elements.filledCount.textContent = String(run.progress?.filled || 0);
  elements.remainingCount.textContent = String(run.progress?.remaining || 0);
  elements.currentAction.textContent = run.currentAction || 'Idle';
  elements.confirmationId.textContent = run.confirmationId || '-';
  elements.startButton.classList.toggle('hidden', isRunning);
  elements.stopButton.classList.toggle('hidden', !isRunning);
  elements.exportButton.disabled = !run.runId;
  elements.copyButton.disabled = !run.runId;

  const resultLabel = run.statusLabel || status.replaceAll('_', ' ');
  setMessage(runMessage(run, resultLabel));
}

async function getActiveTab() {
  const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
  return tabs[0] || null;
}

async function refreshStatus() {
  if (!activeTabId) {
    return;
  }
  try {
    const [response, batchResponse] = await Promise.all([
      chrome.runtime.sendMessage({
        type: 'GET_TAB_RUN',
        tabId: activeTabId
      }),
      chrome.runtime.sendMessage({ type: 'GET_BATCH_STATE' })
    ]);
    renderRun(response?.run || null);
    renderBatchState(batchResponse?.state);
  } catch (error) {
    setMessage(errorMessage(error, 'Unable to read run status.'));
  }
}

function batchStatusLabel(status) {
  if (status === 'waiting_for_form') {
    return ' — waiting for CHEFS form';
  }
  return status === 'starting' ? ' — starting' : '';
}

function renderBatchState(state) {
  const batch = state || { queue: [], active: null, completed: [] };
  const queuedCount = (batch.queue || []).length;
  if (batch.active) {
    const statusLabel = batchStatusLabel(batch.active.status);
    elements.batchActive.textContent =
      `${batch.active.suiteId || 'suite'} #${batch.active.index || '?'}${statusLabel}`;
    if (batch.active.status === 'waiting_for_form') {
      setMessage(`Batch item #${batch.active.index || '?'} is waiting for the CHEFS form to finish loading.`);
    }
  } else if (queuedCount) {
    elements.batchActive.textContent = 'Preparing marked tabs…';
    setMessage(`Preparing ${queuedCount} marked regression tab${queuedCount === 1 ? '' : 's'}…`);
  } else {
    elements.batchActive.textContent = 'No';
  }
  elements.batchQueued.textContent = String(queuedCount);
  elements.batchCompleted.textContent = String((batch.completed || []).length);
  elements.stopBatchButton.classList.toggle(
    'hidden',
    !batch.active && !batch.queue?.length
  );
}

async function startRun() {
  setMessage('Starting run...');
  elements.startButton.disabled = true;
  try {
    const response = await chrome.runtime.sendMessage({
      type: 'START_RUN_IN_TAB',
      tabId: activeTabId
    });
    if (!response?.ok) {
      throw new Error(response?.error || 'The run could not be started.');
    }
    await refreshStatus();
  } catch (error) {
    setMessage(errorMessage(error));
  } finally {
    elements.startButton.disabled = false;
  }
}

async function stopRun() {
  elements.stopButton.disabled = true;
  try {
    const response = await chrome.runtime.sendMessage({
      type: 'STOP_RUN_IN_TAB',
      tabId: activeTabId
    });
    if (!response?.ok) {
      throw new Error(response?.error || 'The run did not acknowledge the stop request.');
    }
    setMessage('Stopping run…');
    for (let attempt = 0; attempt < 10; attempt += 1) {
      await new Promise((resolve) => setTimeout(resolve, 100));
      await refreshStatus();
      const status = String(latestRun?.status || '').toLowerCase();
      if (!RUNNING_STATUSES.has(status)) {
        break;
      }
    }
  } catch (error) {
    setMessage(errorMessage(error, 'Unable to stop the run.'));
  } finally {
    elements.stopButton.disabled = false;
  }
}

async function exportRun() {
  if (!latestRun?.runId) {
    return;
  }
  elements.exportButton.disabled = true;
  setMessage('Preparing run bundle...');
  try {
    const response = await chrome.runtime.sendMessage({
      type: 'EXPORT_RUN',
      runId: latestRun.runId
    });
    if (!response?.ok) {
      throw new Error(response?.error || 'Export failed.');
    }
    setMessage(`Save As requested for ${response.downloadPath || response.filename}.`);
  } catch (error) {
    setMessage(errorMessage(error));
  } finally {
    elements.exportButton.disabled = false;
  }
}

async function copySummary() {
  if (!latestRun?.summaryText) {
    return;
  }
  try {
    await navigator.clipboard.writeText(latestRun.summaryText);
    setMessage('Summary copied.');
  } catch {
    setMessage('Unable to copy the summary. Export the run bundle instead.');
  }
}

async function stopBatch() {
  elements.stopBatchButton.disabled = true;
  try {
    const response = await chrome.runtime.sendMessage({ type: 'STOP_BATCH' });
    if (!response?.ok) {
      throw new Error(response?.error || 'Unable to stop the batch.');
    }
    setMessage('Batch stop requested.');
    await refreshStatus();
  } catch (error) {
    setMessage(errorMessage(error));
  } finally {
    elements.stopBatchButton.disabled = false;
  }
}

async function openDashboard() {
  try {
    const response = await chrome.runtime.sendMessage({ type: 'OPEN_DASHBOARD' });
    if (!response?.ok) {
      throw new Error(response?.error || 'Unable to open the dashboard.');
    }
  } catch (error) {
    setMessage(errorMessage(error));
  }
}

elements.startButton.addEventListener('click', startRun);
elements.stopButton.addEventListener('click', stopRun);
elements.exportButton.addEventListener('click', exportRun);
elements.copyButton.addEventListener('click', copySummary);
elements.dashboardButton.addEventListener('click', openDashboard);
elements.settingsButton.addEventListener('click', () => chrome.runtime.openOptionsPage());
elements.stopBatchButton.addEventListener('click', stopBatch);

async function initialize() {
  const tab = await getActiveTab();
  activeTabId = tab?.id || null;
  if (!activeTabId) {
    setMessage('No active tab is available.');
    elements.startButton.disabled = true;
    return;
  }
  await refreshStatus();
  pollTimer = setInterval(refreshStatus, 700);
}

await initialize();

window.addEventListener('unload', () => {
  if (pollTimer) {
    clearInterval(pollTimer);
  }
});
