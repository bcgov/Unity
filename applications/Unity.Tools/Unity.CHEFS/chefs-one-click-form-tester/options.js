'use strict';

const VERSION = '0.4.9';
const RULE_SCHEMA_VERSION = 1;
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

let rules = [];

function newRuleId() {
  if (typeof crypto?.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  const bytes = new Uint8Array(16);
  if (typeof crypto?.getRandomValues === 'function') {
    crypto.getRandomValues(bytes);
  }
  const suffix = Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
  return `rule-${Date.now()}-${suffix}`;
}

function isPhraseWhitespace(character) {
  return character === '\u00a0' ||
    character === ' ' ||
    character === '\t' ||
    character === '\n' ||
    character === '\r' ||
    character === '\f' ||
    character === '\v';
}

function isPhraseEdgeCharacter(character) {
  return character === ' ' || character === '*' || character === ':' || character === '-';
}

function normalizePhraseBody(value) {
  let compact = '';
  let inTag = false;
  let pendingSpace = false;
  const text = String(value || '').replaceAll('\u00a0', ' ');
  for (const character of text) {
    if (character === '<') {
      inTag = true;
      pendingSpace = true;
      continue;
    }
    if (character === '>') {
      inTag = false;
      pendingSpace = true;
      continue;
    }
    if (inTag) {
      continue;
    }
    if (isPhraseWhitespace(character)) {
      pendingSpace = compact.length > 0;
      continue;
    }
    if (pendingSpace && compact.length > 0) {
      compact += ' ';
    }
    compact += character;
    pendingSpace = false;
  }
  return compact;
}

function trimPhraseEdges(text) {
  let start = 0;
  let end = text.length;
  while (start < end && isPhraseEdgeCharacter(text[start])) {
    start += 1;
  }
  while (end > start && isPhraseEdgeCharacter(text[end - 1])) {
    end -= 1;
  }
  return text.slice(start, end);
}

function normalizePhrase(value) {
  return trimPhraseEdges(normalizePhraseBody(value));
}

function normalizeRule(raw) {
  const source = raw || {};
  return {
    id: String(source.id || newRuleId()),
    enabled: source.enabled !== false,
    labelMatch: normalizePhrase(source.labelMatch || source.labelPhrase || source.label || ''),
    matchMode: source.matchMode === 'exact' ? 'exact' : 'contains',
    caseSensitive: Boolean(source.caseSensitive),
    mask: String(source.mask || '').trim(),
    notes: String(source.notes || '').trim()
  };
}

function validateMask(mask) {
  const text = String(mask || '').trim();
  if (!text) {
    return 'A mask is required.';
  }
  if (text.length > 200) {
    return 'Masks cannot exceed 200 characters.';
  }
  if (!text.includes('9') && !text.includes('a') && !text.includes('*')) {
    return 'The mask must contain at least one 9, a, or * token.';
  }
  for (const character of text) {
    const code = character.codePointAt(0);
    if (code < 32 || code > 126) {
      return 'The mask must contain printable characters only.';
    }
  }
  return '';
}

function validateRules(candidateRules) {
  const normalized = candidateRules.map(normalizeRule);
  const errors = [];
  const ids = new Set();
  const identities = new Set();
  normalized.forEach((rule, index) => {
    const row = index + 1;
    if (!rule.labelMatch) {
      errors.push(`Row ${row}: a label keyword or phrase is required.`);
    }
    const maskError = validateMask(rule.mask);
    if (maskError) {
      errors.push(`Row ${row}: ${maskError}`);
    }
    if (ids.has(rule.id)) {
      errors.push(`Row ${row}: duplicate rule ID ${rule.id}.`);
    }
    ids.add(rule.id);
    const identity = `${rule.labelMatch.toLowerCase()}\u0000${rule.mask}\u0000${rule.matchMode}`;
    if (identities.has(identity)) {
      errors.push(`Row ${row}: duplicate label-and-mask rule.`);
    }
    identities.add(identity);
  });
  return { rules: normalized, errors };
}

function setRulesMessage(text, type) {
  const element = document.getElementById('rulesMessage');
  element.textContent = text || '';
  element.className = 'message' + (type ? ' ' + type : '');
}

function setSettingsMessage(text, type) {
  const element = document.getElementById('settingsMessage');
  element.textContent = text || '';
  element.className = 'message' + (type ? ' ' + type : '');
}

function setExportFolderMessage(text, type) {
  const element = document.getElementById('exportFolderMessage');
  element.textContent = text || '';
  element.className = 'message' + (type ? ' ' + type : '');
}

function setBatchSettingsMessage(text, type) {
  const element = document.getElementById('batchSettingsMessage');
  element.textContent = text || '';
  element.className = 'message' + (type ? ' ' + type : '');
}

function normalizeBatchLauncherToken(value) {
  const token = String(value || '').trim();
  if (!token) {
    return '';
  }
  if (!/^[A-Za-z0-9_-]{16,128}$/.test(token)) {
    throw new Error('The launcher token must contain 16 to 128 letters, numbers, underscores or hyphens.');
  }
  return token;
}

function normalizeBatchOrigins(values) {
  const origins = Array.isArray(values)
    ? values
    : String(values || '').split(/\r?\n/);
  const normalized = [];
  for (const rawValue of origins) {
    const value = String(rawValue || '').trim();
    if (!value) {
      continue;
    }
    let url;
    try {
      url = new URL(value);
    } catch (error) {
      throw new Error(`Invalid batch origin: ${value}`);
    }
    if (!['http:', 'https:'].includes(url.protocol) ||
        url.username ||
        url.password ||
        url.pathname !== '/' ||
        url.search ||
        url.hash) {
      throw new Error(`Enter an exact HTTP or HTTPS origin without a path: ${value}`);
    }
    normalized.push(url.origin);
  }
  return Array.from(new Set(normalized));
}

function permissionPatternForOrigin(origin) {
  const url = new URL(origin);
  return `${url.protocol}//${url.hostname}/*`;
}

function renderRules() {
  const body = document.getElementById('rulesBody');
  body.textContent = '';
  if (!rules.length) {
    const row = document.createElement('tr');
    row.className = 'empty-row';
    const cell = document.createElement('td');
    cell.colSpan = 4;
    cell.textContent = 'No custom field format rules are configured.';
    row.appendChild(cell);
    body.appendChild(row);
    return;
  }

  rules.forEach((rule, index) => {
    const row = document.createElement('tr');
    row.dataset.ruleId = rule.id;

    const enabledCell = document.createElement('td');
    enabledCell.className = 'enabled-cell';
    const enabled = document.createElement('input');
    enabled.type = 'checkbox';
    enabled.checked = rule.enabled !== false;
    enabled.setAttribute('aria-label', `Enable custom format rule ${index + 1}`);
    enabled.addEventListener('change', () => { rule.enabled = enabled.checked; });
    enabledCell.appendChild(enabled);

    const phraseCell = document.createElement('td');
    const phrase = document.createElement('input');
    phrase.type = 'text';
    phrase.value = rule.labelMatch;
    phrase.placeholder = 'IRMA Number';
    phrase.setAttribute('aria-label', `Label keyword or phrase for rule ${index + 1}`);
    phrase.addEventListener('input', () => { rule.labelMatch = phrase.value; });
    phraseCell.appendChild(phrase);

    const maskCell = document.createElement('td');
    const mask = document.createElement('input');
    mask.type = 'text';
    mask.value = rule.mask;
    mask.placeholder = 'aaa-999999';
    mask.setAttribute('aria-label', `CHEFS input mask for rule ${index + 1}`);
    mask.addEventListener('input', () => { rule.mask = mask.value; });
    maskCell.appendChild(mask);

    const removeCell = document.createElement('td');
    removeCell.className = 'remove-cell';
    const remove = document.createElement('button');
    remove.type = 'button';
    remove.className = 'danger';
    remove.textContent = 'Remove';
    remove.setAttribute('aria-label', `Remove custom format rule ${index + 1}`);
    remove.addEventListener('click', () => {
      rules = rules.filter((item) => item.id !== rule.id);
      renderRules();
      setRulesMessage('Rule removed. Save settings to keep this change.', 'success');
    });
    removeCell.appendChild(remove);

    row.append(enabledCell, phraseCell, maskCell, removeCell);
    body.appendChild(row);
  });
}

function loadSettings() {
  return chrome.storage.local.get('chefsTesterSettings').then((stored) => {
    const existing = stored.chefsTesterSettings || {};
    const settings = { ...DEFAULT_SETTINGS, ...existing };
    if (!Object.hasOwn(existing, 'autoExportAfterRun')) {
      settings.autoExportAfterRun = Boolean(existing.autoExportAfterSubmit);
    }
    document.getElementById('additionalHosts').value = (settings.additionalHosts || []).join('\n');
    document.getElementById('allowProduction').checked = Boolean(settings.allowProduction);
    document.getElementById('rowsPerGrid').value = String(Math.max(2, settings.rowsPerGrid || 2));
    document.getElementById('captureScreenshot').checked = settings.captureScreenshot !== false;
    document.getElementById('exportFolder').value = settings.exportFolder || '';
    document.getElementById('autoExportAfterRun').checked = Boolean(settings.autoExportAfterRun);
    document.getElementById('batchLauncherEnabled').checked = Boolean(settings.batchLauncherEnabled);
    document.getElementById('batchLauncherToken').value = settings.batchLauncherToken || '';
    document.getElementById('batchOrigins').value = (settings.batchOrigins || []).join('\n');
    document.getElementById('openDashboardAfterCompletion').checked =
      Boolean(settings.openDashboardAfterCompletion);
    document.getElementById('retainDashboardHistory').checked =
      Boolean(settings.retainDashboardHistory);
    document.getElementById('dashboardDefaultView').value =
      ['simple', 'analyst', 'statistical', 'experimental'].includes(settings.dashboardDefaultView)
        ? settings.dashboardDefaultView
        : 'simple';
    rules = Array.isArray(settings.customFormatRules) ? settings.customFormatRules.map(normalizeRule) : [];
    renderRules();
  });
}

function collectSettings() {
  const additionalHosts = document.getElementById('additionalHosts').value
    .split(/\r?\n/)
    .map((value) => value.trim().toLowerCase())
    .filter(Boolean);
  const rowsPerGrid = Math.max(2, Math.min(5, Number(document.getElementById('rowsPerGrid').value) || 2));
  const validation = validateRules(rules);
  if (validation.errors.length) {
    throw new Error(validation.errors.join('\n'));
  }
  const exportFolder = ChefsExportPath.normalizeExportFolder(
    document.getElementById('exportFolder').value
  );
  const batchLauncherEnabled = document.getElementById('batchLauncherEnabled').checked;
  const batchLauncherToken = normalizeBatchLauncherToken(
    document.getElementById('batchLauncherToken').value
  );
  const batchOrigins = normalizeBatchOrigins(document.getElementById('batchOrigins').value);
  if (batchLauncherEnabled && !batchLauncherToken) {
    throw new Error('Generate or enter a launcher token before enabling the batch regression launcher.');
  }
  if (batchLauncherEnabled && !batchOrigins.length) {
    throw new Error('Add at least one permitted regression origin before enabling the batch regression launcher.');
  }
  rules = validation.rules;
  return {
    additionalHosts: Array.from(new Set(additionalHosts)),
    allowProduction: document.getElementById('allowProduction').checked,
    rowsPerGrid,
    captureScreenshot: document.getElementById('captureScreenshot').checked,
    customFormatRules: rules,
    exportFolder,
    autoExportAfterRun: document.getElementById('autoExportAfterRun').checked,
    batchLauncherEnabled,
    batchLauncherToken,
    batchOrigins,
    openDashboardAfterCompletion:
      document.getElementById('openDashboardAfterCompletion').checked,
    retainDashboardHistory:
      document.getElementById('retainDashboardHistory').checked,
    dashboardDefaultView:
      document.getElementById('dashboardDefaultView').value
  };
}

async function saveSettings() {
  try {
    const settings = collectSettings();
    await chrome.storage.local.set({ chefsTesterSettings: settings });
    renderRules();
    setSettingsMessage('', '');
    setRulesMessage(`${rules.length} custom format rule${rules.length === 1 ? '' : 's'} saved.`, 'success');
    const message = document.getElementById('savedMessage');
    message.textContent = 'Saved.';
    setTimeout(() => { message.textContent = ''; }, 1800);
  } catch (error) {
    setSettingsMessage(error?.message || String(error), 'error');
  }
}

async function selectExportFolder() {
  const input = document.getElementById('exportFolder');
  const button = document.getElementById('selectExportFolderButton');
  const previousValue = input.value;
  button.disabled = true;
  setExportFolderMessage('Select a folder directly inside Downloads...', '');
  try {
    const folder = await ChefsExportFolderPicker.selectValidatedFolder();
    input.value = folder;
    setExportFolderMessage(
      `Validated ${folder}. Select Save Settings to keep this Export Folder.`,
      'success'
    );
  } catch (error) {
    input.value = previousValue;
    if (error?.name === 'AbortError') {
      setExportFolderMessage('Folder selection cancelled.', '');
    } else {
      setExportFolderMessage(error?.message || String(error), 'error');
    }
  } finally {
    button.disabled = false;
  }
}

function generateBatchToken() {
  const token = crypto.randomUUID().replaceAll('-', '') + crypto.randomUUID().replaceAll('-', '');
  document.getElementById('batchLauncherToken').value = token;
  setBatchSettingsMessage('Token generated. Copy it into LAUNCHER_TOKEN in the batch file, then save Settings.', 'success');
}

async function grantBatchPermissions() {
  const button = document.getElementById('grantBatchPermissionsButton');
  button.disabled = true;
  try {
    const origins = normalizeBatchOrigins(document.getElementById('batchOrigins').value);
    if (!origins.length) {
      throw new Error('Add at least one permitted regression origin first.');
    }
    const granted = await chrome.permissions.request({
      origins: origins.map(permissionPatternForOrigin)
    });
    if (!granted) {
      throw new Error('Chrome did not grant access to the listed regression origins.');
    }
    setBatchSettingsMessage('Host access granted. Select Save Settings to keep the origin list.', 'success');
  } catch (error) {
    setBatchSettingsMessage(error?.message || String(error), 'error');
  } finally {
    button.disabled = false;
  }
}

function setDashboardSettingsMessage(text, type) {
  const element = document.getElementById('dashboardSettingsMessage');
  element.textContent = text || '';
  element.className = 'message' + (type ? ' ' + type : '');
}

async function openDashboard() {
  try {
    const response = await chrome.runtime.sendMessage({ type: 'OPEN_DASHBOARD' });
    if (!response?.ok) {
      throw new Error(response?.error || 'The dashboard could not be opened.');
    }
    setDashboardSettingsMessage('Dashboard opened.', 'success');
  } catch (error) {
    setDashboardSettingsMessage(error?.message || String(error), 'error');
  }
}

async function clearDashboardHistory() {
  if (!window.confirm('Clear all retained PID-free aggregate dashboard history?')) {
    return;
  }
  try {
    const response = await chrome.runtime.sendMessage({ type: 'CLEAR_DASHBOARD_HISTORY' });
    if (!response?.ok) {
      throw new Error(response?.error || 'Dashboard history could not be cleared.');
    }
    setDashboardSettingsMessage('Dashboard history cleared.', 'success');
  } catch (error) {
    setDashboardSettingsMessage(error?.message || String(error), 'error');
  }
}

function exportRules() {
  try {
    const validation = validateRules(rules);
    if (validation.errors.length) {
      throw new Error(validation.errors.join('\n'));
    }
    const payload = {
      schemaVersion: RULE_SCHEMA_VERSION,
      exportedAt: new Date().toISOString(),
      extensionVersion: VERSION,
      rules: validation.rules
    };
    const data = `data:application/json;charset=utf-8,${encodeURIComponent(JSON.stringify(payload, null, 2))}`;
    const timestamp = new Date().toISOString();
    const stamp = timestamp.slice(0, timestamp.indexOf('.')).replaceAll('-', '').replaceAll(':', '').replace('T', '-');
    chrome.downloads.download({
      url: data,
      filename: `chefs-custom-format-rules-v${RULE_SCHEMA_VERSION}-${stamp}.json`,
      saveAs: true
    });
    setRulesMessage(`Exported ${validation.rules.length} rule${validation.rules.length === 1 ? '' : 's'}.`, 'success');
  } catch (error) {
    setRulesMessage(error?.message || String(error), 'error');
  }
}

function mergeRules(localRules, importedRules) {
  const byId = new Map(localRules.map((rule) => [rule.id, normalizeRule(rule)]));
  const identities = new Map();
  for (const rule of byId.values()) {
    identities.set(`${rule.labelMatch.toLowerCase()}\u0000${rule.mask}\u0000${rule.matchMode}`, rule.id);
  }
  for (const imported of importedRules.map(normalizeRule)) {
    const identity = `${imported.labelMatch.toLowerCase()}\u0000${imported.mask}\u0000${imported.matchMode}`;
    if (byId.has(imported.id) || !identities.has(identity)) {
      byId.set(imported.id, imported);
      identities.set(identity, imported.id);
    }
  }
  return Array.from(byId.values());
}

async function importRulesFromFile(file) {
  try {
    const text = await file.text();
    const payload = JSON.parse(text);
    if (payload?.schemaVersion !== RULE_SCHEMA_VERSION || !Array.isArray(payload?.rules)) {
      throw new Error(`The file must use rule schema version ${RULE_SCHEMA_VERSION} and contain a rules array.`);
    }
    const validation = validateRules(payload.rules);
    if (validation.errors.length) {
      throw new Error(validation.errors.join('\n'));
    }
    const mode = document.getElementById('importMode').value;
    rules = mode === 'replace' ? validation.rules : mergeRules(rules, validation.rules);
    renderRules();
    setRulesMessage(`Imported ${validation.rules.length} rule${validation.rules.length === 1 ? '' : 's'} using ${mode} mode. Save settings to keep the changes.`, 'success');
  } catch (error) {
    setRulesMessage(`Import rejected: ${error?.message || String(error)}`, 'error');
  } finally {
    document.getElementById('rulesFileInput').value = '';
  }
}

document.getElementById('addRuleButton').addEventListener('click', () => {
  rules.push(normalizeRule({ labelMatch: '', mask: '', enabled: true }));
  renderRules();
  const lastRowInput = document.querySelector('#rulesBody tr:last-child input[type="text"]');
  if (lastRowInput) {
    lastRowInput.focus();
  }
});
document.getElementById('importRulesButton').addEventListener('click', () => document.getElementById('rulesFileInput').click());
document.getElementById('exportRulesButton').addEventListener('click', exportRules);
document.getElementById('rulesFileInput').addEventListener('change', (event) => {
  const file = event.target.files?.[0];
  if (file) {
    importRulesFromFile(file);
  }
});
document.getElementById('saveButton').addEventListener('click', saveSettings);
document.getElementById('selectExportFolderButton').addEventListener('click', selectExportFolder);
document.getElementById('exportFolder').addEventListener('input', () => setExportFolderMessage('', ''));
document.getElementById('generateBatchTokenButton').addEventListener('click', generateBatchToken);
document.getElementById('grantBatchPermissionsButton').addEventListener('click', grantBatchPermissions);
document.getElementById('openDashboardButton').addEventListener('click', openDashboard);
document.getElementById('clearDashboardHistoryButton').addEventListener('click', clearDashboardHistory);
void loadSettings();
