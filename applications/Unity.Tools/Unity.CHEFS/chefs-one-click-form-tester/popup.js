'use strict';

const VERSION = '0.4.0';
const BUILD = '2026.07.23.14';
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
  const isRunning = ['initializing', 'scanning', 'filling', 'settling', 'validating', 'submitting', 'waiting'].includes(status);
  const isSuccess = status === 'submitted' || status === 'completed';
  const isFailure = ['failed', 'stalled', 'blocked', 'safety_stop'].includes(status);

  elements.statusPill.textContent = run.statusLabel || status.replaceAll('_', ' ');
  elements.statusPill.className = `pill ${isRunning ? 'running' : isSuccess ? 'success' : isFailure ? 'failure' : 'warning'}`;
  elements.runId.textContent = run.runId || 'Unknown';
  elements.passCount.textContent = String(run.progress && run.progress.pass || 0);
  elements.filledCount.textContent = String(run.progress && run.progress.filled || 0);
  elements.remainingCount.textContent = String(run.progress && run.progress.remaining || 0);
  elements.currentAction.textContent = run.currentAction || 'Idle';
  elements.confirmationId.textContent = run.confirmationId || '-';
  elements.startButton.classList.toggle('hidden', isRunning);
  elements.stopButton.classList.toggle('hidden', !isRunning);
  elements.exportButton.disabled = !run.runId;
  elements.copyButton.disabled = !run.runId;

  if (run.exportState && run.exportState.automatic) {
    const automatic = run.exportState.automatic;
    const resultLabel = run.statusLabel || status.replaceAll('_', ' ');
    if (automatic.status === 'pending') {
      setMessage(`${resultLabel}. Preparing automatic export...`);
    } else if (automatic.status === 'succeeded') {
      setMessage(`${resultLabel}. Automatic export saved to ${automatic.downloadPath}.`);
    } else if (automatic.status === 'failed') {
      setMessage(`${resultLabel}. Automatic export failed: ${automatic.error} Use Export Last Run.`);
    } else if (run.failure && run.failure.message) {
      setMessage(run.failure.message);
    } else if (run.message) {
      setMessage(run.message);
    } else {
      setMessage('');
    }
  } else if (run.failure && run.failure.message) {
    setMessage(run.failure.message);
  } else if (run.message) {
    setMessage(run.message);
  } else {
    setMessage('');
  }
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
    renderRun(response && response.run ? response.run : null);
    renderBatchState(batchResponse && batchResponse.state);
  } catch (error) {
    setMessage(error && error.message ? error.message : 'Unable to read run status.');
  }
}

function renderBatchState(state) {
  const batch = state || { queue: [], active: null, completed: [] };
  const queuedCount = (batch.queue || []).length;
  if (batch.active) {
    const statusLabel = batch.active.status === 'waiting_for_form'
      ? ' — waiting for CHEFS form'
      : batch.active.status === 'starting'
        ? ' — starting'
        : '';
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
    !batch.active && !(batch.queue && batch.queue.length)
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
    if (!response || !response.ok) {
      throw new Error(response && response.error ? response.error : 'The run could not be started.');
    }
    await refreshStatus();
  } catch (error) {
    setMessage(error && error.message ? error.message : String(error));
  } finally {
    elements.startButton.disabled = false;
  }
}

async function stopRun() {
  try {
    await chrome.tabs.sendMessage(activeTabId, { type: 'CHEFS_TESTER_STOP' });
    setMessage('Stop requested.');
  } catch (error) {
    setMessage(error && error.message ? error.message : 'Unable to stop the run.');
  }
}

async function exportRun() {
  if (!latestRun || !latestRun.runId) {
    return;
  }
  elements.exportButton.disabled = true;
  setMessage('Preparing run bundle...');
  try {
    const response = await chrome.runtime.sendMessage({
      type: 'EXPORT_RUN',
      runId: latestRun.runId
    });
    if (!response || !response.ok) {
      throw new Error(response && response.error ? response.error : 'Export failed.');
    }
    setMessage(`Save As requested for ${response.downloadPath || response.filename}.`);
  } catch (error) {
    setMessage(error && error.message ? error.message : String(error));
  } finally {
    elements.exportButton.disabled = false;
  }
}

async function copySummary() {
  if (!latestRun || !latestRun.summaryText) {
    return;
  }
  try {
    await navigator.clipboard.writeText(latestRun.summaryText);
    setMessage('Summary copied.');
  } catch (error) {
    setMessage('Unable to copy the summary. Export the run bundle instead.');
  }
}

async function stopBatch() {
  elements.stopBatchButton.disabled = true;
  try {
    const response = await chrome.runtime.sendMessage({ type: 'STOP_BATCH' });
    if (!response || !response.ok) {
      throw new Error(response && response.error ? response.error : 'Unable to stop the batch.');
    }
    setMessage('Batch stop requested.');
    await refreshStatus();
  } catch (error) {
    setMessage(error && error.message ? error.message : String(error));
  } finally {
    elements.stopBatchButton.disabled = false;
  }
}

async function openDashboard() {
  try {
    const response = await chrome.runtime.sendMessage({ type: 'OPEN_DASHBOARD' });
    if (!response || !response.ok) {
      throw new Error(response && response.error ? response.error : 'Unable to open the dashboard.');
    }
  } catch (error) {
    setMessage(error && error.message ? error.message : String(error));
  }
}

elements.startButton.addEventListener('click', startRun);
elements.stopButton.addEventListener('click', stopRun);
elements.exportButton.addEventListener('click', exportRun);
elements.copyButton.addEventListener('click', copySummary);
elements.dashboardButton.addEventListener('click', openDashboard);
elements.settingsButton.addEventListener('click', () => chrome.runtime.openOptionsPage());
elements.stopBatchButton.addEventListener('click', stopBatch);

(async function initialize() {
  const tab = await getActiveTab();
  activeTabId = tab ? tab.id : null;
  if (!activeTabId) {
    setMessage('No active tab is available.');
    elements.startButton.disabled = true;
    return;
  }
  await refreshStatus();
  pollTimer = setInterval(refreshStatus, 700);
})();

window.addEventListener('unload', () => {
  if (pollTimer) {
    clearInterval(pollTimer);
  }
});
