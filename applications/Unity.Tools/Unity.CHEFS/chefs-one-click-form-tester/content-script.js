'use strict';

(function installChefsTesterContentScript() {
  if (window.__CHEFS_TESTER_CONTENT_CONTROLLER__) {
    return;
  }

  const VERSION = '0.4.0';
  const BUILD = '2026.07.23.14';
  const CONFIG = {
    maxFillPasses: 40,
    hardMaxFillPasses: 200,
    tabPassesPerTab: 6,
    passExtensionIncrement: 10,
    stablePassesRequired: 2,
    settleDelayMs: 700,
    bridgeTimeoutMs: 12000,
    uploadTimeoutMs: 90000,
    uploadActionTimeoutMs: 180000,
    actionTimeoutMs: 35000,
    submitOutcomeTimeoutMs: 35000,
    maxFieldAttempts: 3,
    maxSubmitAttempts: 3,
    overallStallMs: 60000
  };
  const LAYOUT_TYPES = new Set([
    'form', 'panel', 'simplepanel', 'fieldset', 'columns', 'column', 'tabs', 'tab',
    'well', 'content', 'simplecontent', 'htmlelement', 'html', 'table', 'container'
  ]);
  const PROTECTED_TYPES = new Set(['hidden', 'button', 'resource', 'signature']);
  const ACTION_EXCLUDE = /save\s*as\s*draft|create\s*pdf|print|delete|reset|cancel|previous|back|logout|view\s*my\s*drafts|wide\s*layout/i;
  const LOOKUP_ACTION = /lookup|search|find\s+(?:applicant|organization|address|record)|retrieve/i;
  const GRID_ADD_ACTION = /add\s+(?:another|row|item|program|contact|entry)|new\s+row/i;

  function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  function withTimeout(promise, timeoutMs, label) {
    let timer;
    const timeout = new Promise((resolve, reject) => {
      timer = setTimeout(() => reject(new Error(`${label} timed out after ${timeoutMs} ms.`)), timeoutMs);
    });
    return Promise.race([promise, timeout]).finally(() => clearTimeout(timer));
  }

  function cleanText(value) {
    return String(value || '').replace(/\s+/g, ' ').trim();
  }

  function normalizeRulePhrase(value, caseSensitive) {
    let normalized = cleanText(String(value || '')
      .replace(/<[^>]*>/g, ' ')
      .replace(/^[*:\-\s]+|[*:\-\s]+$/g, ' '))
      .replace(/[^A-Za-z0-9]+/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
    if (!caseSensitive) {
      normalized = normalized.toLowerCase();
    }
    return normalized;
  }

  function normalizeCustomRule(raw, index) {
    const source = raw || {};
    return {
      id: String(source.id || `rule-${index + 1}`),
      enabled: source.enabled !== false,
      labelMatch: cleanText(source.labelMatch || source.labelPhrase || source.label || ''),
      matchMode: source.matchMode === 'exact' ? 'exact' : 'contains',
      caseSensitive: Boolean(source.caseSensitive),
      mask: String(source.mask || '').trim(),
      notes: cleanText(source.notes || '')
    };
  }

  function stableRuleJson(rules) {
    return JSON.stringify((rules || []).map((rule) => ({
      id: rule.id,
      enabled: rule.enabled !== false,
      labelMatch: rule.labelMatch,
      matchMode: rule.matchMode,
      caseSensitive: Boolean(rule.caseSensitive),
      mask: rule.mask,
      notes: rule.notes || ''
    })).sort((a, b) => a.id.localeCompare(b.id)));
  }

  async function sha256Text(value) {
    if (!crypto || !crypto.subtle) {
      return '';
    }
    const bytes = new TextEncoder().encode(String(value));
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    return Array.from(new Uint8Array(digest)).map((item) => item.toString(16).padStart(2, '0')).join('');
  }

  function isVisible(element) {
    if (!element || !element.isConnected) {
      return false;
    }
    if (element.closest('.formio-hidden, [hidden], [aria-hidden="true"]')) {
      return false;
    }
    const style = getComputedStyle(element);
    if (style.display === 'none' || style.visibility === 'hidden' || Number(style.opacity) === 0) {
      return false;
    }
    return element.getClientRects().length > 0;
  }

  function getFieldLabel(wrapper) {
    const label = wrapper.querySelector(':scope > label, :scope > legend, label, legend');
    if (label) {
      return cleanText(label.textContent).slice(0, 1000);
    }
    const heading = wrapper.querySelector('h1, h2, h3, h4, h5, h6');
    if (heading) {
      return cleanText(heading.textContent).slice(0, 1000);
    }
    return '';
  }

  function getDescription(wrapper) {
    const description = wrapper.querySelector('.form-text, [ref="description"], .help-block:not([ref="buttonMessage"])');
    return description ? cleanText(description.textContent).slice(0, 1200) : '';
  }

  function getInputKey(wrapper) {
    const named = wrapper.querySelector('input[name^="data["], select[name^="data["], textarea[name^="data["]');
    if (named) {
      const matches = Array.from(named.name.matchAll(/\[([^\]]+)\]/g));
      if (matches.length) {
        return matches[matches.length - 1][1];
      }
    }
    return '';
  }

  function safeValueInfo(value, generated) {
    const text = value === null || value === undefined ? '' : String(value);
    return {
      valueType: Array.isArray(value) ? 'array' : value === null ? 'null' : typeof value,
      valueLength: text.length,
      generatedByExtension: Boolean(generated)
    };
  }

  class ChefsTesterController {
    constructor() {
      this.running = false;
      this.stopRequested = false;
      this.runId = '';
      this.settings = {};
      this.customFormatRules = [];
      this.customRuleSet = { schemaVersion: 1, enabledRuleCount: 0, ruleSetHash: '', lastModifiedAt: '' };
      this.environment = '';
      this.startedAt = 0;
      this.currentPass = 0;
      this.lastProgressAt = 0;
      this.currentAction = 'Idle';
      this.lastSuccessfulAction = '';
      this.domRevision = 0;
      this.lastScannedRevision = 0;
      this.componentStates = new Map();
      this.gridHandled = new Set();
      this.fileHandled = new Set();
      this.fileUploadsInFlight = new Map();
      this.actionHandled = new Set();
      this.visitedTabs = new Set();
      this.tabActivationFailures = new Map();
      this.submitLandmarks = new Map();
      this.passBudget = CONFIG.maxFillPasses;
      this.lastPassHadProgress = false;
      this.visitedWizardSignatures = new Set();
      this.bridgeReady = false;
      this.bridgeRequests = new Map();
      this.mutationObserver = null;
      this.errorHandlersInstalled = false;
      this.successFinalized = false;
      this.progress = {
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
        customRulesLoaded: 0,
        customRuleMatches: 0,
        customRuleAccepted: 0,
        customRuleRejected: 0,
        detectedMasksUsed: 0,
        maskValuesRejected: 0
      };
      this.boundBridgeMessage = this.handleBridgeMessage.bind(this);
      window.addEventListener('message', this.boundBridgeMessage);
    }

    async runtimeMessage(message) {
      try {
        return await chrome.runtime.sendMessage(message);
      } catch (error) {
        return { ok: false, error: error && error.message ? error.message : String(error) };
      }
    }

    makeRunId() {
      const bytes = new Uint8Array(3);
      crypto.getRandomValues(bytes);
      return Array.from(bytes).map((value) => value.toString(16).padStart(2, '0')).join('').toUpperCase();
    }

    async log(eventName, data) {
      if (!this.runId) {
        return;
      }
      const event = Object.assign({
        time: new Date().toISOString(),
        elapsedMs: this.startedAt ? Date.now() - this.startedAt : 0,
        event: eventName,
        pass: this.currentPass
      }, data || {});
      await this.runtimeMessage({
        type: 'APPEND_EVENTS',
        runId: this.runId,
        events: [event]
      });
    }

    async checkpoint(reason, extra) {
      if (!this.runId) {
        return;
      }
      const checkpoint = Object.assign({
        time: new Date().toISOString(),
        reason,
        pass: this.currentPass,
        currentAction: this.currentAction,
        lastSuccessfulAction: this.lastSuccessfulAction,
        scrollX: window.scrollX,
        scrollY: window.scrollY,
        progress: Object.assign({}, this.progress)
      }, extra || {});
      await this.runtimeMessage({
        type: 'ADD_CHECKPOINT',
        runId: this.runId,
        checkpoint
      });
    }

    async updateRun(patch) {
      if (!this.runId) {
        return;
      }
      await this.runtimeMessage({ type: 'UPDATE_RUN', runId: this.runId, patch });
    }

    async setStatus(status, statusLabel, action, message) {
      this.currentAction = action || this.currentAction;
      await this.updateRun({
        status,
        statusLabel,
        currentAction: this.currentAction,
        message: message || '',
        progress: Object.assign({}, this.progress)
      });
    }

    async markProgress(action) {
      this.lastProgressAt = Date.now();
      this.lastSuccessfulAction = action;
      await this.updateRun({
        lastSuccessfulAction: action,
        currentAction: this.currentAction,
        progress: Object.assign({}, this.progress)
      });
    }

    installErrorHandlers() {
      if (this.errorHandlersInstalled) {
        return;
      }
      this.errorHandlersInstalled = true;
      window.addEventListener('error', (event) => {
        if (!this.running) {
          return;
        }
        this.log('JAVASCRIPT_ERROR', {
          message: event.message || 'Unhandled page error',
          filename: event.filename || '',
          line: event.lineno || 0,
          column: event.colno || 0,
          stack: event.error && event.error.stack ? event.error.stack : ''
        });
      });
      window.addEventListener('unhandledrejection', (event) => {
        if (!this.running) {
          return;
        }
        const reason = event.reason;
        this.log('UNHANDLED_REJECTION', {
          message: reason && reason.message ? reason.message : String(reason),
          stack: reason && reason.stack ? reason.stack : ''
        });
      });
    }

    installMutationObserver() {
      if (this.mutationObserver) {
        this.mutationObserver.disconnect();
      }
      this.mutationObserver = new MutationObserver((mutations) => {
        this.domRevision += mutations.length || 1;
      });
      this.mutationObserver.observe(document.documentElement, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['class', 'style', 'hidden', 'disabled', 'aria-hidden', 'aria-invalid', 'aria-expanded']
      });
    }

    async injectBridge() {
      try {
        const probe = await this.bridgeCommand('PING', {}, 1500, true);
        if (probe && probe.ready) {
          this.bridgeReady = true;
          return;
        }
      } catch (error) {
        // Use the packaged-script fallback below.
      }
      if (!document.getElementById('chefs-tester-page-bridge')) {
        const script = document.createElement('script');
        script.id = 'chefs-tester-page-bridge';
        script.src = chrome.runtime.getURL('page-bridge.js');
        script.async = false;
        (document.head || document.documentElement).appendChild(script);
      }
      await withTimeout(new Promise((resolve) => {
        if (this.bridgeReady) {
          resolve();
          return;
        }
        const check = setInterval(() => {
          if (this.bridgeReady) {
            clearInterval(check);
            resolve();
          }
        }, 50);
      }), 5000, 'Page bridge initialization').catch(async (error) => {
        await this.log('BRIDGE_INITIALIZATION_FAILED', { message: error.message });
      });
    }

    handleBridgeMessage(event) {
      if (event.source !== window || !event.data) {
        return;
      }
      if (event.data.channel === 'CHEFS_TESTER_BRIDGE' && event.data.type === 'BRIDGE_READY') {
        this.bridgeReady = true;
        return;
      }
      if (event.data.channel !== 'CHEFS_TESTER_BRIDGE_RESPONSE') {
        return;
      }
      const pending = this.bridgeRequests.get(event.data.requestId);
      if (!pending) {
        return;
      }
      this.bridgeRequests.delete(event.data.requestId);
      if (event.data.ok) {
        pending.resolve(event.data.result);
      } else {
        const error = new Error(event.data.error && event.data.error.message ? event.data.error.message : 'Page bridge command failed.');
        if (event.data.error && event.data.error.stack) {
          error.stack = event.data.error.stack;
        }
        pending.reject(error);
      }
    }

    bridgeCommand(command, payload, timeoutMs, allowUnready) {
      if (!this.bridgeReady && !allowUnready) {
        return Promise.reject(new Error('Page bridge is not ready.'));
      }
      const requestId = `${this.runId}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
      const promise = new Promise((resolve, reject) => {
        this.bridgeRequests.set(requestId, { resolve, reject });
        window.postMessage({
          channel: 'CHEFS_TESTER_BRIDGE_REQUEST',
          requestId,
          command,
          payload: payload || {}
        }, '*');
      });
      return withTimeout(promise, timeoutMs || CONFIG.bridgeTimeoutMs, `Bridge command ${command}`)
        .finally(() => this.bridgeRequests.delete(requestId));
    }

    async waitForForm() {
      await withTimeout(new Promise((resolve) => {
        const existing = document.querySelector('.formio-form, [ref="webform"]');
        if (existing) {
          resolve(existing);
          return;
        }
        const observer = new MutationObserver(() => {
          const form = document.querySelector('.formio-form, [ref="webform"]');
          if (form) {
            observer.disconnect();
            resolve(form);
          }
        });
        observer.observe(document.documentElement, { subtree: true, childList: true });
      }), 20000, 'CHEFS form detection');
    }

    formTitle() {
      const heading = document.querySelector('main h1, .main-wide h1, h1');
      return heading ? cleanText(heading.textContent) : document.title;
    }

    formId() {
      const url = new URL(location.href);
      const match = url.pathname.match(/[0-9a-f]{8}-[0-9a-f-]{27,}/i);
      return match ? match[0] : '';
    }

    async start(startMessage) {
      if (this.running) {
        return { ok: false, error: 'A run is already active in this tab.' };
      }
      this.running = true;
      this.stopRequested = false;
      this.runId = this.makeRunId();
      this.settings = startMessage.settings || {};
      this.customFormatRules = (Array.isArray(this.settings.customFormatRules) ? this.settings.customFormatRules : [])
        .map((rule, index) => normalizeCustomRule(rule, index))
        .filter((rule) => rule.enabled && rule.labelMatch && rule.mask);
      const ruleSetHash = await sha256Text(stableRuleJson(this.customFormatRules));
      this.customRuleSet = {
        schemaVersion: 1,
        enabledRuleCount: this.customFormatRules.length,
        ruleSetHash: ruleSetHash ? `sha256:${ruleSetHash}` : '',
        lastModifiedAt: new Date().toISOString()
      };
      this.environment = startMessage.environment || '';
      this.startedAt = Date.now();
      this.lastProgressAt = this.startedAt;
      this.currentPass = 0;
      this.componentStates.clear();
      this.gridHandled.clear();
      this.fileHandled.clear();
      this.actionHandled.clear();
      this.visitedTabs.clear();
      this.tabActivationFailures.clear();
      this.passBudget = CONFIG.maxFillPasses;
      this.lastPassHadProgress = false;
      this.visitedWizardSignatures.clear();
      this.successFinalized = false;
      this.progress = {
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
        customRulesLoaded: this.customRuleSet.enabledRuleCount,
        customRuleMatches: 0,
        customRuleAccepted: 0,
        customRuleRejected: 0,
        detectedMasksUsed: 0,
        maskValuesRejected: 0
      };
      this.installErrorHandlers();
      this.installMutationObserver();

      const createResponse = await this.runtimeMessage({
        type: 'CREATE_RUN',
        runId: this.runId,
        tabId: null,
        extensionVersion: VERSION,
        buildNumber: BUILD,
        chromeVersion: navigator.userAgent,
        formTitle: this.formTitle(),
        formUrl: location.href,
        formId: this.formId(),
        startedAt: new Date().toISOString(),
        customRuleSet: this.customRuleSet,
        customFormatRules: this.customFormatRules
      });
      if (!createResponse || !createResponse.ok) {
        this.running = false;
        throw new Error(createResponse && createResponse.error ? createResponse.error : 'The run record could not be created.');
      }

      await this.log('CUSTOM_RULE_SET_LOADED', {
        schemaVersion: this.customRuleSet.schemaVersion,
        enabledRuleCount: this.customRuleSet.enabledRuleCount,
        ruleSetHash: this.customRuleSet.ruleSetHash
      });
      await this.log('RUN_STARTED', {
        environment: this.environment,
        settings: {
          rowsPerGrid: Math.max(2, this.settings.rowsPerGrid || 2),
          captureScreenshot: this.settings.captureScreenshot !== false,
          customFormatRuleCount: this.customRuleSet.enabledRuleCount,
          customRuleSetHash: this.customRuleSet.ruleSetHash
        }
      });

      this.executeRun().catch(async (error) => {
        if (error && error.message === 'RUN_ALREADY_FAILED') {
          return;
        }
        await this.failRun('failed', 'Failed', error, { reason: 'Unhandled run-controller error' });
      });
      return { ok: true, runId: this.runId };
    }

    async executeRun() {
      await this.setStatus('initializing', 'Initializing', 'Finding CHEFS form');
      await this.injectBridge();
      await this.waitForForm();
      if (this.detectPaymentTransaction()) {
        throw new Error('A payment transaction control was detected. Automatic submission was stopped.');
      }

      const initial = await this.scanComponents();
      await this.setSnapshot('initial', initial.snapshot);
      await this.log('INITIAL_SCAN_COMPLETED', {
        discovered: initial.snapshot.length,
        visibleFillable: initial.fillable.length,
        formioInstanceFound: initial.formioInstanceFound
      });
      await this.checkpoint('Initial scan completed');

      await this.fillUntilStable();
      if (!this.running) {
        return;
      }
      if (this.stopRequested) {
        await this.finishStopped();
        return;
      }
      await this.submitWithRecovery();
    }

    detectPaymentTransaction() {
      return Boolean(
        document.querySelector('input[autocomplete="cc-number"], input[name*="cardNumber" i], iframe[src*="stripe" i], iframe[src*="moneris" i]') ||
        Array.from(document.querySelectorAll('button')).some((button) => isVisible(button) && /pay\s+now|complete\s+payment/i.test(cleanText(button.textContent)))
      );
    }

    async expandContainers() {
      const collapsed = Array.from(document.querySelectorAll('[ref="header"][aria-expanded="false"], .card-header[aria-expanded="false"]'));
      for (const header of collapsed) {
        if (isVisible(header)) {
          header.click();
          await delay(40);
        }
      }
    }

    metadataMap(bridgeResult) {
      const map = new Map();
      if (!bridgeResult || !Array.isArray(bridgeResult.components)) {
        return map;
      }
      for (const item of bridgeResult.components) {
        if (item && item.key && !map.has(item.key)) {
          map.set(item.key, item);
        }
        if (item && item.domId && !map.has(`__dom:${item.domId}`)) {
          map.set(`__dom:${item.domId}`, item);
        }
      }
      return map;
    }

    inferKey(wrapper, metadata) {
      // Prefer the wrapper's own Form.io component class. Searching descendant
      // inputs first causes layout containers to inherit the key of their first
      // child, which creates duplicate and incorrectly protected descriptors.
      for (const key of metadata.keys()) {
        if (wrapper.classList.contains(`formio-component-${key}`)) {
          return key;
        }
      }
      const classTokens = Array.from(wrapper.classList)
        .filter((token) => token.startsWith('formio-component-'))
        .map((token) => token.slice('formio-component-'.length))
        .filter((token) => !['form', 'label-hidden', 'multiple', 'file'].includes(token));
      const likelyPropertyKey = classTokens.find((token) => /^s\d+_/i.test(token) || /^unityApplicantId$/i.test(token));
      if (likelyPropertyKey) {
        return likelyPropertyKey;
      }
      const inputKey = getInputKey(wrapper);
      if (inputKey) {
        return inputKey;
      }
      return classTokens.length > 1 ? classTokens[classTokens.length - 1] : (classTokens[0] || `dom-${wrapper.id || Math.random().toString(16).slice(2)}`);
    }

    inferType(wrapper, key, meta) {
      if (meta && meta.type) {
        return String(meta.type).toLowerCase();
      }
      const rawTypes = Array.from(wrapper.classList)
        .filter((token) => token.startsWith('formio-component-'))
        .map((token) => token.slice('formio-component-'.length).toLowerCase())
        .filter((token) => !['label-hidden', 'multiple', 'file'].includes(token));
      const layoutType = rawTypes.find((token) => LAYOUT_TYPES.has(token));
      if (layoutType) {
        return layoutType;
      }
      const normalizedKey = String(key || '').toLowerCase();
      const classes = rawTypes.filter((token) => token !== normalizedKey);
      // A default-key component can legitimately repeat the same token for both
      // its type and key, for example:
      // formio-component-simplefile formio-component-simplefile.
      // Preserve the recognized type instead of filtering both copies away.
      const fallbackType = rawTypes.find((token) => token === normalizedKey) || rawTypes[0] || '';
      return String(classes[0] || (LAYOUT_TYPES.has(normalizedKey) ? normalizedKey : fallbackType)).toLowerCase();
    }

    stateId(wrapper, key) {
      return `${key}::${wrapper.id || 'no-id'}`;
    }

    cssEscape(value) {
      if (window.CSS && typeof window.CSS.escape === 'function') {
        return window.CSS.escape(String(value || ''));
      }
      return String(value || '').replace(/[^A-Za-z0-9_-]/g, '\\$&');
    }

    liveWrapper(descriptor) {
      if (descriptor && descriptor.wrapper && descriptor.wrapper.isConnected && isVisible(descriptor.wrapper)) {
        return descriptor.wrapper;
      }

      const wrapperId = descriptor && descriptor.wrapperId
        ? descriptor.wrapperId
        : descriptor && descriptor.wrapper && descriptor.wrapper.id
          ? descriptor.wrapper.id
          : '';
      if (wrapperId) {
        const exact = document.getElementById(wrapperId);
        if (exact && exact.isConnected && isVisible(exact)) {
          descriptor.wrapper = exact;
          descriptor.wrapperId = exact.id;
          return exact;
        }
      }

      const key = descriptor && descriptor.key ? descriptor.key : '';
      if (!key) {
        return descriptor ? descriptor.wrapper : null;
      }
      const candidates = Array.from(document.querySelectorAll(`.formio-component-${this.cssEscape(key)}`));
      const visible = candidates.find((candidate) => isVisible(candidate));
      const connected = visible || candidates.find((candidate) => candidate.isConnected) || null;
      if (descriptor && connected) {
        descriptor.wrapper = connected;
        descriptor.wrapperId = connected.id || descriptor.wrapperId || '';
      }
      return connected;
    }

    isFileWrapper(wrapper, type) {
      return Boolean(
        wrapper &&
        (
          String(type || '').includes('file') ||
          wrapper.classList.contains('formio-component-file') ||
          wrapper.classList.contains('formio-component-simplefile') ||
          wrapper.querySelector('[ref="fileDrop"], .fileSelector')
        )
      );
    }

    uploadedFileRows(wrapper, filename) {
      if (!wrapper) {
        return [];
      }
      const candidates = Array.from(wrapper.querySelectorAll(
        '.list-group > .list-group-item:not(.list-group-header), ' +
        '.list-group-item:not(.list-group-header), ' +
        'tbody tr:not(:first-child), [ref="fileLink"], [ref="fileName"], ' +
        '.file-name, .file-list a, a[download]'
      ));
      const matches = candidates.filter((element) => {
        const text = cleanText(element.textContent);
        const hasRemoveControl = Boolean(element.querySelector && element.querySelector(
          'button[ref*="remove"], button[aria-label*="remove" i], .fa-times, .fa-times-circle-o'
        ));
        if (filename && text.includes(filename)) {
          return true;
        }
        if (hasRemoveControl && text) {
          return true;
        }
        if (!text) {
          return false;
        }
        return !/^file\s*name\s*size$/i.test(text) && !/drop files to attach|browse to attach/i.test(text);
      });
      return Array.from(new Set(matches.map((element) =>
        element.closest('.list-group-item:not(.list-group-header), tbody tr') || element
      )));
    }

    inspectEmpty(wrapper, type) {
      if (this.isFileWrapper(wrapper, type)) {
        return this.uploadedFileRows(wrapper).length === 0;
      }
      if (type.includes('simpleday') || wrapper.querySelector('.formio-day-component-day, .formio-day-component-year')) {
        const month = wrapper.querySelector('select[ref="month"], select[name="month"], select[id$="-month"]');
        const day = wrapper.querySelector('.formio-day-component-day, input[ref="day"], input[id$="-day"]');
        const year = wrapper.querySelector('.formio-day-component-year, input[ref="year"], input[id$="-year"]');
        const controls = [month, day, year].filter((control) => control && !control.disabled);
        return controls.length === 0 || controls.some((control) => cleanText(control.value) === '');
      }
      const radios = Array.from(wrapper.querySelectorAll('input[type="radio"]')).filter((input) => !input.disabled);
      if (radios.length) {
        return !radios.some((input) => input.checked);
      }
      const checkboxes = Array.from(wrapper.querySelectorAll('input[type="checkbox"]')).filter((input) => !input.disabled);
      if (checkboxes.length) {
        return !checkboxes.some((input) => input.checked);
      }
      const select = wrapper.querySelector('select');
      if (select) {
        const selected = Array.from(select.selectedOptions || []).filter((option) => option.value && !/select|choose/i.test(cleanText(option.textContent)));
        return selected.length === 0;
      }
      const textarea = wrapper.querySelector('textarea');
      if (textarea) {
        return cleanText(textarea.value) === '';
      }
      const inputs = Array.from(wrapper.querySelectorAll('input:not([type="hidden"]):not([type="button"]):not([type="submit"])'))
        .filter((input) => !input.disabled);
      if (inputs.length) {
        return inputs.every((input) => cleanText(input.value) === '');
      }
      const choiceText = wrapper.querySelector('.choices__list--single .choices__item, .choices__list--multiple .choices__item');
      if (choiceText) {
        return cleanText(choiceText.textContent) === '';
      }
      return true;
    }

    isInvalid(wrapper) {
      if (wrapper.querySelector('[aria-invalid="true"], .is-invalid')) {
        return true;
      }
      const messages = Array.from(wrapper.querySelectorAll('.formio-errors, .invalid-feedback, .help-block'))
        .map((element) => cleanText(element.textContent))
        .filter(Boolean);
      return messages.length > 0 && wrapper.classList.contains('has-error');
    }

    isProtected(wrapper, key, type, meta) {
      if (wrapper.classList.contains('formio-component-hidden')) {
        return true;
      }
      if (LAYOUT_TYPES.has(type)) {
        return true;
      }
      if (PROTECTED_TYPES.has(type) && type !== 'button') {
        return true;
      }
      if (meta) {
        if (meta.input === false || meta.hidden || meta.disabled || meta.readOnly || meta.calculateValue) {
          return true;
        }
        if (meta.persistent === false || meta.persistent === 'client-only') {
          return true;
        }
      }
      const lowerKey = String(key).toLowerCase();
      if (/hidden|mappingtarget|extract|token|applicantagent|submissiondate/.test(lowerKey)) {
        return true;
      }
      // User-facing acknowledgements often contain "confirmation" in their
      // property names. Protect only actual system confirmation identifiers.
      if (/^(confirmationid|confirmationnumber|submissionconfirmationid|submissionconfirmationnumber)$/.test(lowerKey)) {
        return true;
      }
      const controls = wrapper.querySelectorAll('input, select, textarea, button');
      if (controls.length && Array.from(controls).every((control) => control.disabled || control.readOnly || control.type === 'hidden')) {
        return true;
      }
      return false;
    }

    hasOwnControl(wrapper) {
      const controls = Array.from(wrapper.querySelectorAll('input, select, textarea, .choices, [ref="fileDrop"]'));
      return controls.some((control) => control.closest('.formio-component') === wrapper);
    }

    isFillableDescriptor(descriptor) {
      if (!descriptor.visible || descriptor.protected) {
        return false;
      }
      if (descriptor.type === 'button') {
        return false;
      }
      if (descriptor.type.includes('datagrid') || descriptor.type.includes('editgrid')) {
        return true;
      }
      if (this.hasOwnControl(descriptor.wrapper)) {
        return true;
      }
      return this.isFileWrapper(descriptor.wrapper, descriptor.type);
    }

    async scanComponents() {
      await this.expandContainers();
      let bridgeResult = null;
      try {
        bridgeResult = await this.bridgeCommand('GET_COMPONENTS', {}, CONFIG.bridgeTimeoutMs);
      } catch (error) {
        await this.log('FORMIO_INSTANCE_SCAN_FAILED', { message: error.message });
      }
      const metadata = this.metadataMap(bridgeResult);
      const wrappers = Array.from(document.querySelectorAll('.formio-component[ref="component"], .formio-component'));
      const descriptors = [];
      const seen = new Set();

      for (const wrapper of wrappers) {
        if (seen.has(wrapper)) {
          continue;
        }
        seen.add(wrapper);
        const key = this.inferKey(wrapper, metadata);
        const meta = metadata.get(`__dom:${wrapper.id}`) || metadata.get(key) || null;
        const type = this.inferType(wrapper, key, meta);
        const id = this.stateId(wrapper, key);
        const visible = isVisible(wrapper);
        const descriptor = {
          id,
          key,
          type,
          label: getFieldLabel(wrapper) || (meta && meta.label) || '',
          description: getDescription(wrapper) || (meta && meta.description) || '',
          visible,
          enabled: !Boolean(wrapper.querySelector(':scope input:disabled, :scope select:disabled, :scope textarea:disabled')),
          protected: this.isProtected(wrapper, key, type, meta),
          required: Boolean((meta && meta.required) || wrapper.classList.contains('required') || wrapper.querySelector('[required]')),
          empty: this.inspectEmpty(wrapper, type),
          invalid: this.isInvalid(wrapper),
          meta,
          wrapper,
          wrapperId: wrapper.id || ''
        };
        const primaryControl = wrapper.querySelector('input:not([type="hidden"]), textarea, select');
        descriptor.maskPlan = this.resolveMaskPlan(descriptor, primaryControl, 1);
        descriptor.fillable = this.isFillableDescriptor(descriptor);
        descriptors.push(descriptor);

        if (!this.componentStates.has(id)) {
          this.componentStates.set(id, {
            id,
            key,
            type,
            label: descriptor.label,
            firstSeenPass: this.currentPass,
            lastSeenPass: this.currentPass,
            status: descriptor.protected ? 'protected' : descriptor.fillable ? 'discovered' : 'non-input',
            attempts: 0,
            fillStrategy: '',
            visibleAtLastScan: visible,
            lastError: '',
            maskSource: descriptor.maskPlan ? descriptor.maskPlan.source : '',
            resolvedMask: descriptor.maskPlan ? descriptor.maskPlan.mask : '',
            customRuleId: descriptor.maskPlan && descriptor.maskPlan.rule ? descriptor.maskPlan.rule.id : '',
            customRuleLabelMatch: descriptor.maskPlan && descriptor.maskPlan.rule ? descriptor.maskPlan.rule.labelMatch : '',
            maskPlanLoggedSignatures: []
          });
          await this.log('COMPONENT_DISCOVERED', {
            componentId: id,
            key,
            componentType: type,
            label: descriptor.label,
            visible,
            fillable: descriptor.fillable,
            protected: descriptor.protected,
            required: descriptor.required
          });
        } else {
          const state = this.componentStates.get(id);
          if (state.visibleAtLastScan !== visible) {
            await this.log(visible ? 'COMPONENT_BECAME_VISIBLE' : 'COMPONENT_BECAME_HIDDEN', {
              componentId: id,
              key,
              componentType: type,
              label: descriptor.label
            });
          }
          state.visibleAtLastScan = visible;
          state.lastSeenPass = this.currentPass;
        }
      }

      const fillable = descriptors.filter((item) => item.fillable && item.visible && !item.protected);
      const snapshot = descriptors.map((item) => {
        const state = this.componentStates.get(item.id) || {};
        return {
          componentId: item.id,
          key: item.key,
          label: item.label,
          description: item.description,
          componentType: item.type,
          visible: item.visible,
          enabled: item.enabled,
          protected: item.protected,
          fillable: item.fillable,
          required: item.required,
          empty: item.empty,
          invalid: item.invalid,
          status: state.status || '',
          attempts: state.attempts || 0,
          fillStrategy: state.fillStrategy || '',
          firstSeenPass: state.firstSeenPass,
          lastSeenPass: state.lastSeenPass,
          lastError: state.lastError || '',
          placeholder: item.meta && item.meta.placeholder ? item.meta.placeholder : '',
          inputMask: item.meta && item.meta.inputMask ? item.meta.inputMask : '',
          runtimeInputMask: item.meta && item.meta.runtimeInputMask ? item.meta.runtimeInputMask : '',
          maskSource: item.maskPlan ? item.maskPlan.source : '',
          resolvedMask: item.maskPlan ? item.maskPlan.mask : '',
          customRule: item.maskPlan && item.maskPlan.rule ? {
            matched: true,
            ruleId: item.maskPlan.rule.id,
            labelMatch: item.maskPlan.rule.labelMatch,
            matchMode: item.maskPlan.rule.matchMode,
            configuredMask: item.maskPlan.rule.mask
          } : { matched: false },
          maskGenerationStrategy: item.maskPlan ? 'input-mask' : '',
          widgetType: item.meta && item.meta.widgetType ? item.meta.widgetType : '',
          dataSrc: item.meta && item.meta.dataSrc ? item.meta.dataSrc : '',
          multiple: Boolean(item.meta && item.meta.multiple),
          minLength: item.meta && item.meta.minLength !== undefined ? item.meta.minLength : undefined,
          maxLength: item.meta && item.meta.maxLength !== undefined ? item.meta.maxLength : undefined,
          minWords: item.meta && item.meta.minWords !== undefined ? item.meta.minWords : undefined,
          maxWords: item.meta && item.meta.maxWords !== undefined ? item.meta.maxWords : undefined,
          minSelectedCount: item.meta && item.meta.minSelectedCount !== undefined ? item.meta.minSelectedCount : undefined,
          maxSelectedCount: item.meta && item.meta.maxSelectedCount !== undefined ? item.meta.maxSelectedCount : undefined,
          gridRowCount: item.type.includes('datagrid') || item.type.includes('editgrid')
            ? this.gridRows(item.wrapper, item.type).length
            : undefined,
          gridTargetRows: item.type.includes('datagrid') || item.type.includes('editgrid')
            ? this.gridTargetRows(item)
            : undefined
        };
      });
      this.progress.discovered = this.componentStates.size;
      this.progress.remaining = fillable.filter((item) => item.empty || item.invalid).length;
      this.progress.filled = Array.from(this.componentStates.values()).filter((state) => state.status === 'filled').length;
      this.progress.failed = Array.from(this.componentStates.values()).filter((state) => state.status === 'failed' || state.status === 'blocked').length;
      this.progress.unsupported = Array.from(this.componentStates.values()).filter((state) => state.status === 'unsupported').length;
      await this.indexSubmitLandmarks();
      return {
        descriptors,
        fillable,
        snapshot,
        formioInstanceFound: Boolean(bridgeResult && bridgeResult.formFound)
      };
    }

    async setSnapshot(name, snapshot) {
      await this.runtimeMessage({ type: 'SET_SNAPSHOT', runId: this.runId, name, snapshot });
    }

    async fillUntilStable() {
      let stablePasses = 0;
      const initialTabs = this.getTabEntries();
      this.passBudget = Math.min(
        CONFIG.hardMaxFillPasses,
        Math.max(CONFIG.maxFillPasses, 20 + (initialTabs.length * CONFIG.tabPassesPerTab))
      );
      await this.log('FILL_PASS_BUDGET_SET', {
        passBudget: this.passBudget,
        hardPassLimit: CONFIG.hardMaxFillPasses,
        tabCount: initialTabs.length
      });
      if (initialTabs.length) {
        await this.log('TAB_SET_DISCOVERED', {
          tabCount: initialTabs.length,
          tabs: initialTabs.map((entry) => ({ id: entry.id, label: entry.label, active: this.tabIsActive(entry) }))
        });
      }

      while (this.currentPass < CONFIG.hardMaxFillPasses) {
        if (this.currentPass >= this.passBudget) {
          const boundaryScan = await this.scanComponents();
          const unresolvedAtBoundary = boundaryScan.fillable.filter((item) => item.empty || item.invalid);
          if (unresolvedAtBoundary.length && this.lastPassHadProgress && this.passBudget < CONFIG.hardMaxFillPasses) {
            const oldBudget = this.passBudget;
            this.passBudget = Math.min(CONFIG.hardMaxFillPasses, this.passBudget + CONFIG.passExtensionIncrement);
            await this.log('FILL_PASS_BUDGET_EXTENDED', {
              oldBudget,
              newBudget: this.passBudget,
              unresolved: unresolvedAtBoundary.length,
              reason: 'The final allowed pass made progress or revealed additional fields.'
            });
          } else {
            break;
          }
        }
        if (await this.finalizeIfSubmitted(this.progress.submitAttempts, 'fill-loop-start')) {
          return;
        }
        if (this.stopRequested) {
          return;
        }
        if (Date.now() - this.lastProgressAt > CONFIG.overallStallMs) {
          const scan = await this.scanComponents();
          const unresolved = scan.fillable.filter((item) => item.empty || item.invalid);
          if (unresolved.length) {
            await this.failRun('stalled', 'Stalled', new Error('No successful form progress was recorded within the stall window.'), {
              reason: 'No progress',
              unresolved: unresolved.map((item) => ({ key: item.key, label: item.label, type: item.type }))
            });
            throw new Error('RUN_ALREADY_FAILED');
          }
          this.lastProgressAt = Date.now();
        }

        this.currentPass += 1;
        this.progress.pass = this.currentPass;
        const revisionBefore = this.domRevision;
        await this.setStatus('scanning', 'Scanning', `Scanning pass ${this.currentPass}`);
        await this.log('PASS_STARTED', { domRevision: this.domRevision });
        const scan = await this.scanComponents();
        await this.setSnapshot('lastKnown', scan.snapshot);
        await this.updateRun({ progress: Object.assign({}, this.progress) });

        const candidates = scan.fillable.filter((descriptor) => {
          const state = this.componentStates.get(descriptor.id);
          return (descriptor.empty || descriptor.invalid) && state && state.attempts < CONFIG.maxFieldAttempts;
        });

        await this.setStatus('filling', 'Filling', `Filling pass ${this.currentPass}`);
        let actions = 0;

        const gridCandidates = scan.fillable.filter((item) => item.type.includes('datagrid') || item.type.includes('editgrid'));
        for (const descriptor of gridCandidates) {
          actions += await this.handleGrid(descriptor);
        }
        if (actions > 0) {
          this.lastPassHadProgress = true;
          await this.setStatus('settling', 'Settling', 'Waiting for grid rows');
          await delay(CONFIG.settleDelayMs);
          stablePasses = 0;
          continue;
        }

        for (const descriptor of candidates) {
          if (this.stopRequested) {
            return;
          }
          if (!descriptor.wrapper.isConnected || !isVisible(descriptor.wrapper)) {
            continue;
          }
          if (descriptor.type.includes('datagrid') || descriptor.type.includes('editgrid')) {
            continue;
          }
          const result = await this.fillDescriptor(descriptor);
          actions += result ? 1 : 0;
        }

        actions += await this.handleLookupActions();
        await this.setStatus('settling', 'Settling', 'Waiting for conditional form changes');
        await delay(CONFIG.settleDelayMs);
        if (await this.finalizeIfSubmitted(this.progress.submitAttempts, 'fill-loop-settle')) {
          return;
        }

        const revisionChanged = this.domRevision !== revisionBefore;
        let navigationProgress = false;
        if (actions > 0 || revisionChanged) {
          stablePasses = 0;
        } else {
          const activatedTab = await this.activateNextUnvisitedTab();
          if (activatedTab) {
            navigationProgress = true;
            this.lastPassHadProgress = true;
            stablePasses = 0;
            await delay(CONFIG.settleDelayMs);
            continue;
          }
          const advancedWizard = await this.advanceWizard();
          if (advancedWizard) {
            navigationProgress = true;
            this.lastPassHadProgress = true;
            stablePasses = 0;
            await delay(CONFIG.settleDelayMs);
            continue;
          }
          stablePasses += 1;
        }
        this.lastPassHadProgress = actions > 0 || revisionChanged || navigationProgress;

        const afterScan = await this.scanComponents();
        await this.setSnapshot('lastKnown', afterScan.snapshot);
        await this.checkpoint('Fill pass completed', {
          actions,
          stablePasses,
          domRevisionBefore: revisionBefore,
          domRevisionAfter: this.domRevision
        });
        await this.log('PASS_COMPLETED', {
          actions,
          stablePasses,
          remaining: this.progress.remaining,
          domChanged: revisionChanged
        });

        if (stablePasses >= CONFIG.stablePassesRequired) {
          break;
        }
      }

      if (this.currentPass >= this.passBudget || this.currentPass >= CONFIG.hardMaxFillPasses) {
        const finalBudgetScan = await this.scanComponents();
        const unresolvedAfterBudget = finalBudgetScan.fillable.filter((item) => item.empty || item.invalid);
        const unvisitedTabsAfterBudget = this.getTabEntries().filter((entry) => !this.visitedTabs.has(entry.id));
        if (!unresolvedAfterBudget.length && !unvisitedTabsAfterBudget.length) {
          await this.log('FILL_PASS_BUDGET_REACHED_WITH_FORM_FULL', {
            passBudget: this.passBudget,
            pass: this.currentPass
          });
          return;
        }
        await this.failRun('safety_stop', 'Safety stop', new Error('The maximum fill-pass limit was reached.'), {
          reason: 'Maximum fill passes reached',
          passBudget: this.passBudget,
          hardPassLimit: CONFIG.hardMaxFillPasses,
          unresolved: unresolvedAfterBudget.map((item) => ({ key: item.key, label: item.label, type: item.type })),
          unvisitedTabs: unvisitedTabsAfterBudget.map((entry) => ({ id: entry.id, label: entry.label }))
        });
        throw new Error('RUN_ALREADY_FAILED');
      }
    }

    async fillDescriptor(descriptor) {
      const state = this.componentStates.get(descriptor.id);
      if (!state || state.attempts >= CONFIG.maxFieldAttempts) {
        return false;
      }
      state.attempts += 1;
      const strategy = this.chooseStrategy(descriptor);
      state.fillStrategy = strategy;
      this.currentAction = `Filling ${descriptor.key}`;
      await this.updateRun({ currentAction: this.currentAction, progress: Object.assign({}, this.progress) });
      const primaryControl = descriptor.wrapper.querySelector('input:not([type="hidden"]), textarea, select');
      const maskPlan = this.resolveMaskPlan(descriptor, primaryControl, state.attempts);
      if (maskPlan) {
        state.maskSource = maskPlan.source;
        state.resolvedMask = maskPlan.mask;
        state.customRuleId = maskPlan.rule ? maskPlan.rule.id : '';
        state.customRuleLabelMatch = maskPlan.rule ? maskPlan.rule.labelMatch : '';
        const maskPlanSignature = `${maskPlan.source}:${maskPlan.mask}:${maskPlan.rule ? maskPlan.rule.id : ''}`;
        state.maskPlanLoggedSignatures = Array.isArray(state.maskPlanLoggedSignatures) ? state.maskPlanLoggedSignatures : [];
        if (!state.maskPlanLoggedSignatures.includes(maskPlanSignature)) {
          if (maskPlan.rule) {
            this.progress.customRuleMatches += 1;
            await this.log('CUSTOM_RULE_MATCHED', {
              componentId: descriptor.id,
              key: descriptor.key,
              label: descriptor.label,
              ruleId: maskPlan.rule.id,
              labelMatch: maskPlan.rule.labelMatch,
              matchMode: maskPlan.rule.matchMode,
              configuredMask: maskPlan.rule.mask,
              fallbackDetectedMask: maskPlan.fallbackDetectedMask ? maskPlan.fallbackDetectedMask.mask : ''
            });
          } else {
            this.progress.detectedMasksUsed += 1;
            await this.log(maskPlan.source === 'runtime-inputmask' ? 'MASK_RUNTIME_DETECTED' : 'MASK_METADATA_DETECTED', {
              componentId: descriptor.id,
              key: descriptor.key,
              label: descriptor.label,
              maskSource: maskPlan.source,
              mask: maskPlan.mask
            });
          }
          state.maskPlanLoggedSignatures.push(maskPlanSignature);
        }
      }
      await this.log('FILL_ATTEMPT', {
        componentId: descriptor.id,
        key: descriptor.key,
        componentType: descriptor.type,
        label: descriptor.label,
        strategy,
        attempt: state.attempts,
        resolvedConstraints: this.constraints(descriptor, primaryControl),
        formatPlan: maskPlan ? {
          source: maskPlan.source,
          mask: maskPlan.mask,
          ruleId: maskPlan.rule ? maskPlan.rule.id : '',
          labelMatch: maskPlan.rule ? maskPlan.rule.labelMatch : ''
        } : null
      });
      await this.checkpoint('Fill attempt started', {
        fieldKey: descriptor.key,
        componentId: descriptor.id,
        strategy,
        attempt: state.attempts
      });

      try {
        const actionTimeout = strategy === 'formio-file-upload' ? CONFIG.uploadActionTimeoutMs : CONFIG.actionTimeoutMs;
        const outcome = await withTimeout(this.performFill(descriptor, strategy, state.attempts), actionTimeout, `Filling ${descriptor.key}`);
        if (outcome && outcome.success) {
          state.status = 'filled';
          state.lastError = '';
          this.progress.filled = Array.from(this.componentStates.values()).filter((item) => item.status === 'filled').length;
          if (maskPlan) {
            if (maskPlan.rule) {
              this.progress.customRuleAccepted += 1;
              await this.log('CUSTOM_RULE_VALUE_ACCEPTED', {
                componentId: descriptor.id,
                key: descriptor.key,
                ruleId: maskPlan.rule.id,
                configuredMask: maskPlan.mask,
                attempt: state.attempts
              });
            } else {
              await this.log('MASK_VALUE_PERSISTED', {
                componentId: descriptor.id,
                key: descriptor.key,
                maskSource: maskPlan.source,
                mask: maskPlan.mask,
                attempt: state.attempts
              });
            }
          }
          await this.log('FILL_SUCCEEDED', {
            componentId: descriptor.id,
            key: descriptor.key,
            componentType: descriptor.type,
            strategy,
            attempt: state.attempts,
            value: outcome.valueInfo || null
          });
          await this.markProgress(`Filled ${descriptor.key}`);
          await this.checkpoint('Fill succeeded', { fieldKey: descriptor.key, componentId: descriptor.id });
          return true;
        }
        throw new Error(outcome && outcome.message ? outcome.message : 'The generated value did not persist.');
      } catch (error) {
        state.lastError = error.message || String(error);
        state.status = state.attempts >= CONFIG.maxFieldAttempts ? 'blocked' : 'retry';
        if (maskPlan) {
          this.progress.maskValuesRejected += 1;
          if (maskPlan.rule) {
            this.progress.customRuleRejected += 1;
            await this.log('CUSTOM_RULE_VALUE_REJECTED', {
              componentId: descriptor.id,
              key: descriptor.key,
              ruleId: maskPlan.rule.id,
              configuredMask: maskPlan.mask,
              detectedFallbackMask: maskPlan.fallbackDetectedMask ? maskPlan.fallbackDetectedMask.mask : '',
              attempt: state.attempts,
              message: state.lastError
            });
          } else {
            await this.log('MASK_VALUE_REJECTED', {
              componentId: descriptor.id,
              key: descriptor.key,
              maskSource: maskPlan.source,
              mask: maskPlan.mask,
              attempt: state.attempts,
              message: state.lastError
            });
          }
        }
        await this.log(state.attempts >= CONFIG.maxFieldAttempts ? 'FILL_REJECTED' : 'VALUE_DID_NOT_PERSIST', {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type,
          strategy,
          attempt: state.attempts,
          message: state.lastError,
          stack: error.stack || ''
        });
        await this.checkpoint('Fill attempt failed', {
          fieldKey: descriptor.key,
          componentId: descriptor.id,
          message: state.lastError
        });
        return false;
      }
    }

    chooseStrategy(descriptor) {
      if (this.isFileWrapper(descriptor.wrapper, descriptor.type)) {
        return 'formio-file-upload';
      }
      if (descriptor.type.includes('simpleday') || descriptor.wrapper.querySelector('.formio-day-component-day, .formio-day-component-year')) {
        return 'day-component';
      }
      if (descriptor.wrapper.querySelector('input[type="radio"]')) {
        return 'radio-choice';
      }
      if (descriptor.wrapper.querySelector('input[type="checkbox"]')) {
        return 'checkbox-choice';
      }
      if (descriptor.wrapper.querySelector('.choices')) {
        return 'choices-select';
      }
      if (descriptor.wrapper.querySelector('select')) {
        return 'native-select';
      }
      const maskControl = this.primaryTextControl(descriptor);
      if (this.resolveMaskPlan(descriptor, maskControl, 1)) {
        return 'masked-input';
      }
      if (descriptor.wrapper.querySelector('textarea')) {
        return 'textarea';
      }
      if (descriptor.wrapper.querySelector('input[type="date"], input[type="datetime-local"], input[type="time"], .flatpickr-input')) {
        return 'date-time';
      }
      if (/phone|telephone|mobile|fax/i.test(`${descriptor.type} ${descriptor.key} ${descriptor.label}`)) {
        return 'phone-input';
      }
      if (descriptor.wrapper.querySelector('input')) {
        return 'input';
      }
      return 'formio-set-value';
    }

    async performFill(descriptor, strategy, attempt) {
      switch (strategy) {
        case 'formio-file-upload':
          return this.fillFile(descriptor);
        case 'radio-choice':
          return this.fillRadio(descriptor);
        case 'checkbox-choice':
          return this.fillCheckbox(descriptor);
        case 'choices-select':
          return this.fillChoices(descriptor);
        case 'native-select':
          return this.fillNativeSelect(descriptor);
        case 'day-component':
          return this.fillDayComponent(descriptor);
        case 'masked-input':
          return this.fillMaskedInput(descriptor, attempt);
        case 'textarea':
          return this.fillTextarea(descriptor, attempt);
        case 'date-time':
          return this.fillDateTime(descriptor, attempt);
        case 'phone-input':
          return this.fillPhone(descriptor, attempt);
        case 'input':
          return this.fillInput(descriptor, attempt);
        default:
          return this.fillViaBridge(descriptor, this.generateValue(descriptor, attempt));
      }
    }

    optionScore(optionText, descriptor) {
      const option = cleanText(optionText).toLowerCase();
      const context = `${descriptor.label} ${descriptor.description} ${descriptor.key}`.toLowerCase();
      if (!option || /please\s+select|select\.\.\.|choose|--/.test(option)) {
        return -1000;
      }
      let score = 10;
      if (/none|not applicable|prefer not|unknown/.test(option)) {
        score -= 80;
      }
      if (/british columbia/.test(option)) {
        score += 90;
      }
      if (/victoria/.test(option)) {
        score += 70;
      }
      if (/other/.test(option)) {
        score += 35;
      }
      const priorContext = /previous|previously|returning|existing|already|received.*grant|applied.*before|lookup/.test(context);
      if (option === 'no') {
        score += priorContext ? 120 : 10;
      }
      if (option === 'yes') {
        score += priorContext ? -20 : 80;
      }
      if (/local/.test(option)) {
        score += 20;
      }
      return score;
    }

    chooseOption(elements, descriptor) {
      return elements
        .map((element, index) => ({ element, index, score: this.optionScore(element.textContent || element.label || element.value, descriptor) }))
        .sort((a, b) => b.score - a.score || a.index - b.index)[0];
    }

    async fillRadio(descriptor) {
      const radios = Array.from(descriptor.wrapper.querySelectorAll('input[type="radio"]')).filter((input) => !input.disabled);
      const choices = radios.map((input) => {
        const label = input.closest('label');
        return { input, textContent: label ? label.textContent : input.value };
      });
      const chosen = this.chooseOption(choices, descriptor);
      if (!chosen) {
        return { success: false, message: 'No enabled radio option was found.' };
      }
      chosen.element.input.click();
      await delay(120);
      return {
        success: chosen.element.input.checked,
        valueInfo: safeValueInfo(chosen.element.input.value, true)
      };
    }

    checkboxSelectionBounds(descriptor, inputs) {
      const constraints = this.constraints(descriptor, null);
      const context = cleanText(`${descriptor.label || ''} ${descriptor.description || ''} ${descriptor.wrapper ? descriptor.wrapper.textContent : ''}`).toLowerCase();
      const maximumMatch = context.match(/(?:maximum|max\.?|up to|only select up to)\s*[:=-]?\s*(\d+)\s*(?:partners?|items?|options?|selections?)?/i) ||
        context.match(/can only select up to\s*(\d+)/i);
      const minimumMatch = context.match(/(?:minimum|min\.?|at least|select at least)\s*[:=-]?\s*(\d+)\s*(?:partners?|items?|options?|selections?)?/i);
      let maximum = constraints.maxSelectedCount;
      let minimum = constraints.minSelectedCount;
      let maximumSource = maximum !== null ? 'formio-schema' : '';
      let minimumSource = minimum !== null ? 'formio-schema' : '';

      if ((maximum === null || !Number.isFinite(maximum)) && maximumMatch) {
        maximum = Number(maximumMatch[1]);
        maximumSource = 'rendered-guidance';
      }
      if ((maximum === null || !Number.isFinite(maximum)) && /please select only one|select only one|only one (?:item|option|selection)|single selection/i.test(context)) {
        maximum = 1;
        maximumSource = 'rendered-single-selection-guidance';
      }
      if ((minimum === null || !Number.isFinite(minimum)) && minimumMatch) {
        minimum = Number(minimumMatch[1]);
        minimumSource = 'rendered-guidance';
      }
      if (!Number.isFinite(minimum)) {
        minimum = descriptor.required ? 1 : 0;
        minimumSource = descriptor.required ? 'required-default' : 'optional-default';
      }
      if (!Number.isFinite(maximum) || maximum <= 0) {
        maximum = inputs.length;
        maximumSource = 'all-available';
      }
      maximum = Math.max(0, Math.min(inputs.length, Math.floor(maximum)));
      minimum = Math.max(0, Math.min(maximum, Math.floor(minimum)));
      return { minimum, maximum, minimumSource, maximumSource };
    }

    async fillCheckbox(descriptor) {
      const wrapper = this.liveWrapper(descriptor) || descriptor.wrapper;
      const inputs = Array.from(wrapper.querySelectorAll('input[type="checkbox"]')).filter((input) => !input.disabled);
      if (!inputs.length) {
        return { success: false, message: 'No enabled checkbox was found.' };
      }

      const bounds = this.checkboxSelectionBounds(descriptor, inputs);
      const options = inputs.map((input, index) => {
        const labelElement = input.closest('label');
        const text = cleanText(labelElement ? labelElement.textContent : input.value);
        const disfavoured = /none|not applicable|prefer not|unknown/i.test(text) && inputs.length > 1;
        return {
          input,
          index,
          text,
          disfavoured,
          score: this.optionScore(text, descriptor)
        };
      });
      const preferred = options
        .filter((option) => !option.disfavoured)
        .sort((a, b) => b.score - a.score || a.index - b.index);
      const fallback = options
        .filter((option) => option.disfavoured)
        .sort((a, b) => b.score - a.score || a.index - b.index);
      const ordered = preferred.concat(fallback);
      const preferredCapacity = preferred.length > 0 ? preferred.length : fallback.length;
      const desiredCount = Math.max(bounds.minimum, Math.min(bounds.maximum, preferredCapacity));
      const chosen = new Set(ordered.slice(0, desiredCount).map((option) => option.input));
      const checkedBefore = inputs.filter((input) => input.checked).length;

      await this.log('CHECKBOX_SELECTION_LIMIT_DETECTED', {
        componentId: descriptor.id,
        key: descriptor.key,
        componentType: descriptor.type,
        optionCount: inputs.length,
        minimumSelected: bounds.minimum,
        maximumSelected: bounds.maximum,
        minimumSource: bounds.minimumSource,
        maximumSource: bounds.maximumSource
      });

      let changed = false;
      for (const input of inputs) {
        const shouldBeChecked = chosen.has(input);
        if (input.checked !== shouldBeChecked) {
          input.click();
          changed = true;
          await delay(60);
        }
      }

      const liveWrapper = this.liveWrapper(descriptor) || wrapper;
      const liveInputs = Array.from(liveWrapper.querySelectorAll('input[type="checkbox"]')).filter((input) => !input.disabled);
      const checkedAfter = liveInputs.filter((input) => input.checked).length;
      if (checkedBefore !== checkedAfter || checkedAfter > bounds.maximum) {
        await this.log('CHECKBOX_SELECTION_REPAIRED', {
          componentId: descriptor.id,
          key: descriptor.key,
          checkedBefore,
          checkedAfter,
          maximumSelected: bounds.maximum
        });
      }
      return {
        success: checkedAfter >= bounds.minimum && checkedAfter <= bounds.maximum,
        message: checkedAfter > bounds.maximum
          ? `The checkbox group still contains ${checkedAfter} selections, above the maximum of ${bounds.maximum}.`
          : checkedAfter < bounds.minimum
            ? `The checkbox group contains ${checkedAfter} selections, below the minimum of ${bounds.minimum}.`
            : '',
        valueInfo: safeValueInfo(checkedAfter, true),
        changed
      };
    }

    choiceValuePresent(wrapper) {
      if (!wrapper) {
        return false;
      }
      const select = wrapper.querySelector('select');
      if (select) {
        const selected = Array.from(select.selectedOptions || [])
          .filter((option) => option.value !== '' && !/please\s+select|choose/i.test(cleanText(option.textContent)));
        if (selected.length) {
          return true;
        }
      }
      const rendered = Array.from(wrapper.querySelectorAll(
        '.choices__list--single [data-item], .choices__list--multiple [data-item], ' +
        '.choices__list--single .choices__item:not([data-choice]), ' +
        '.choices__list--multiple .choices__item:not([data-choice])'
      )).filter((item) => {
        const text = cleanText(item.textContent);
        return text && !/please\s+select|choose|select\.\.\./i.test(text);
      });
      return rendered.length > 0;
    }

    dispatchPointerSequence(element) {
      const eventTypes = ['pointerdown', 'mousedown', 'pointerup', 'mouseup', 'click'];
      for (const type of eventTypes) {
        try {
          const EventCtor = type.startsWith('pointer') && typeof PointerEvent === 'function'
            ? PointerEvent
            : MouseEvent;
          element.dispatchEvent(new EventCtor(type, {
            bubbles: true,
            cancelable: true,
            composed: true,
            button: 0,
            buttons: type.endsWith('down') ? 1 : 0,
            pointerType: 'mouse'
          }));
        } catch (error) {
          element.dispatchEvent(new Event(type, { bubbles: true, cancelable: true, composed: true }));
        }
      }
    }

    async fillChoices(descriptor) {
      let wrapper = this.liveWrapper(descriptor);
      if (!wrapper) {
        return { success: false, message: 'The live Choices wrapper was not found.' };
      }
      const choicesRoot = wrapper.querySelector('.choices');
      const control = wrapper.querySelector(
        '.choices .form-control.ui.fluid.selection.dropdown, .choices[role="combobox"], .choices__inner'
      );
      if (!choicesRoot || !control) {
        return { success: false, message: 'Choices control was not found.' };
      }

      control.focus();
      this.dispatchPointerSequence(control);
      await delay(140);

      wrapper = this.liveWrapper(descriptor) || wrapper;
      const options = Array.from(wrapper.querySelectorAll(
        '.choices__item--choice[data-choice-selectable], .choices__item--choice:not(.is-disabled)'
      )).filter((option) => !option.classList.contains('is-disabled'));
      const chosen = this.chooseOption(options, descriptor);

      if (!chosen) {
        const select = wrapper.querySelector('select');
        if (select) {
          return this.fillNativeSelect(descriptor);
        }
        return { success: false, message: 'No selectable Choices option was found.' };
      }

      const chosenText = cleanText(chosen.element.textContent);
      this.dispatchPointerSequence(chosen.element);
      await delay(260);
      wrapper = this.liveWrapper(descriptor) || wrapper;

      if (!this.choiceValuePresent(wrapper)) {
        const search = wrapper.querySelector('.choices__input--cloned');
        if (search) {
          search.focus();
          const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
          setter.call(search, chosenText);
          search.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
          search.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'ArrowDown',
            code: 'ArrowDown',
            keyCode: 40,
            which: 40,
            bubbles: true,
            cancelable: true
          }));
          search.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true,
            cancelable: true
          }));
          await delay(260);
          wrapper = this.liveWrapper(descriptor) || wrapper;
        }
      }

      if (!this.choiceValuePresent(wrapper)) {
        const refreshedOptions = Array.from(wrapper.querySelectorAll(
          '.choices__item--choice[data-choice-selectable], .choices__item--choice:not(.is-disabled)'
        )).filter((option) => !option.classList.contains('is-disabled'));
        const exact = refreshedOptions.find((option) => cleanText(option.textContent) === chosenText);
        if (exact) {
          exact.click();
          await delay(220);
          wrapper = this.liveWrapper(descriptor) || wrapper;
        }
      }

      if (!this.choiceValuePresent(wrapper)) {
        const select = wrapper.querySelector('select');
        const dataValue = chosen.element.getAttribute('data-value');
        if (select && dataValue !== null) {
          let option = Array.from(select.options).find((item) => item.value === dataValue);
          if (!option && dataValue !== '[object Object]') {
            option = new Option(chosenText, dataValue, true, true);
            select.add(option);
          }
          if (option) {
            option.selected = true;
            select.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
            select.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
            select.dispatchEvent(new Event('blur', { bubbles: true, composed: true }));
            await delay(220);
            wrapper = this.liveWrapper(descriptor) || wrapper;
          }
        }
      }

      return {
        success: this.choiceValuePresent(wrapper),
        valueInfo: safeValueInfo(chosenText, true)
      };
    }

    async fillNativeSelect(descriptor) {
      const select = descriptor.wrapper.querySelector('select');
      if (!select || select.disabled) {
        return { success: false, message: 'No enabled select control was found.' };
      }
      const options = Array.from(select.options).filter((option) => !option.disabled && option.value !== '');
      const chosen = this.chooseOption(options, descriptor);
      if (!chosen) {
        return { success: false, message: 'No usable select option was found.' };
      }
      const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value').set;
      setter.call(select, chosen.element.value);
      select.dispatchEvent(new Event('input', { bubbles: true }));
      select.dispatchEvent(new Event('change', { bubbles: true }));
      select.dispatchEvent(new Event('blur', { bubbles: true }));
      await delay(160);
      return {
        success: select.value !== '',
        valueInfo: safeValueInfo(cleanText(chosen.element.textContent), true)
      };
    }

    clampNumber(value, minimum, maximum) {
      let result = Number(value);
      if (Number.isFinite(minimum)) {
        result = Math.max(result, minimum);
      }
      if (Number.isFinite(maximum)) {
        result = Math.min(result, maximum);
      }
      return result;
    }

    async fillDayComponent(descriptor) {
      const wrapper = this.liveWrapper(descriptor) || descriptor.wrapper;
      if (!wrapper) {
        return { success: false, message: 'The live day component wrapper was not found.' };
      }
      const month = wrapper.querySelector('select[ref="month"], select[name="month"], select[id$="-month"]');
      const day = wrapper.querySelector('.formio-day-component-day, input[ref="day"], input[id$="-day"]');
      const year = wrapper.querySelector('.formio-day-component-year, input[ref="year"], input[id$="-year"]');
      if (!month || !day || !year) {
        return { success: false, message: 'The day component did not expose month, day and year controls.' };
      }

      const monthOptions = Array.from(month.options || []).filter((option) => !option.disabled && option.value !== '');
      const preferredMonth = monthOptions.find((option) => String(option.value) === '6') || monthOptions[0];
      if (!preferredMonth) {
        return { success: false, message: 'The day component did not contain a usable month.' };
      }

      const dayMin = Number(day.min);
      const dayMax = Number(day.max);
      const yearMin = Number(year.min);
      const yearMax = Number(year.max);
      const generatedDay = this.clampNumber(20, Number.isFinite(dayMin) ? dayMin : 1, Number.isFinite(dayMax) ? dayMax : 31);
      const generatedYear = this.clampNumber(new Date().getFullYear(), Number.isFinite(yearMin) ? yearMin : undefined, Number.isFinite(yearMax) ? yearMax : undefined);
      const diagnostic = {
        componentId: descriptor.id,
        key: descriptor.key,
        componentType: descriptor.type
      };

      await this.nativeSetValue(month, preferredMonth.value, Object.assign({}, diagnostic, { controlPart: 'month' }));
      await this.nativeSetValue(day, String(generatedDay), Object.assign({}, diagnostic, { controlPart: 'day' }));
      await this.nativeSetValue(year, String(generatedYear), Object.assign({}, diagnostic, { controlPart: 'year' }));
      await delay(180);

      const live = this.liveWrapper(descriptor) || wrapper;
      const complete = !this.inspectEmpty(live, descriptor.type);
      return {
        success: complete,
        message: complete ? '' : 'Month, day and year did not all persist.',
        valueInfo: safeValueInfo(`${preferredMonth.value}/${generatedDay}/${generatedYear}`, true)
      };
    }

    async nativeSetValue(input, value, diagnostic) {
      const prototype = input instanceof HTMLTextAreaElement
        ? HTMLTextAreaElement.prototype
        : input instanceof HTMLSelectElement
          ? HTMLSelectElement.prototype
          : HTMLInputElement.prototype;
      const valueDescriptor = Object.getOwnPropertyDescriptor(prototype, 'value');
      const context = diagnostic || {};
      if (context.key) {
        this.currentAction = `Applying value to ${context.key}`;
        await this.updateRun({
          currentAction: this.currentAction,
          progress: Object.assign({}, this.progress)
        });
        await this.log('CONTROL_EVENT_SEQUENCE_STARTED', {
          componentId: context.componentId || '',
          key: context.key,
          componentType: context.componentType || '',
          controlPart: context.controlPart || '',
          controlType: String(input.type || input.tagName || '').toLowerCase(),
          value: safeValueInfo(value, true)
        });
      }
      if (valueDescriptor && valueDescriptor.set) {
        valueDescriptor.set.call(input, value);
      } else {
        input.value = value;
      }
      await delay(0);
      input.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
      await delay(0);
      input.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
      await delay(0);
      input.dispatchEvent(new Event('blur', { bubbles: true, composed: true }));
      await delay(0);
      if (context.key) {
        await this.log('CONTROL_EVENT_SEQUENCE_COMPLETED', {
          componentId: context.componentId || '',
          key: context.key,
          componentType: context.componentType || '',
          controlPart: context.controlPart || '',
          controlType: String(input.type || input.tagName || '').toLowerCase()
        });
      }
    }

    primaryTextControl(descriptor) {
      return descriptor && descriptor.wrapper
        ? descriptor.wrapper.querySelector('input:not([type="hidden"]):not([type="button"]):not([type="submit"]), textarea')
        : null;
    }

    customRuleForDescriptor(descriptor) {
      if (!cleanText(descriptor && descriptor.label || '')) {
        return null;
      }
      for (const rule of this.customFormatRules) {
        const normalizedLabel = normalizeRulePhrase(descriptor && descriptor.label || '', rule.caseSensitive);
        const normalizedRule = normalizeRulePhrase(rule.labelMatch, rule.caseSensitive);
        if (!normalizedRule) {
          continue;
        }
        const matched = rule.matchMode === 'exact'
          ? normalizedLabel === normalizedRule
          : normalizedLabel.includes(normalizedRule);
        if (matched) {
          return rule;
        }
      }
      return null;
    }

    runtimeMaskFromControl(control) {
      if (!control) {
        return '';
      }
      try {
        if (control.inputmask && control.inputmask.opts && control.inputmask.opts.mask) {
          const value = control.inputmask.opts.mask;
          return Array.isArray(value) ? String(value[0] || '') : String(value || '');
        }
      } catch (error) {
        // Runtime Inputmask expandos may be isolated from the content script.
      }
      const direct = control.getAttribute && (control.getAttribute('data-inputmask-mask') || control.getAttribute('data-mask'));
      if (direct) {
        return String(direct).trim();
      }
      const dataInputmask = control.getAttribute && control.getAttribute('data-inputmask');
      if (dataInputmask) {
        const match = String(dataInputmask).match(/(?:mask\s*[:=]\s*['"])([^'"]+)/i);
        if (match) {
          return match[1];
        }
      }
      return '';
    }

    detectedMask(descriptor, control) {
      const meta = descriptor.meta || {};
      const candidates = [
        { source: 'formio-component', value: meta.inputMask },
        { source: 'runtime-inputmask', value: meta.runtimeInputMask },
        { source: 'runtime-inputmask', value: this.runtimeMaskFromControl(control) }
      ];
      for (const candidate of candidates) {
        const value = Array.isArray(candidate.value) ? candidate.value[0] : candidate.value;
        const text = typeof value === 'string' ? value.trim() : '';
        if (text && /[9a*]/.test(text)) {
          return { source: candidate.source, mask: text };
        }
      }
      return null;
    }

    resolveMaskPlan(descriptor, control, attempt) {
      const customRule = this.customRuleForDescriptor(descriptor);
      const detected = this.detectedMask(descriptor, control);
      const attemptNumber = Number(attempt || 1);
      if (customRule && (attemptNumber === 1 || !detected || detected.mask === customRule.mask)) {
        return {
          source: 'custom-rule',
          mask: customRule.mask,
          rule: customRule,
          fallbackDetectedMask: detected && detected.mask !== customRule.mask ? detected : null
        };
      }
      if (detected) {
        return { source: detected.source, mask: detected.mask, rule: null, fallbackFromCustomRule: Boolean(customRule) };
      }
      if (customRule) {
        return { source: 'custom-rule', mask: customRule.mask, rule: customRule, fallbackDetectedMask: null };
      }
      return null;
    }

    maskTokenCharacters(descriptor, attempt) {
      const seed = `${this.runId}${descriptor.key}${attempt || 1}`.toUpperCase().replace(/[^A-Z0-9]/g, '');
      const digits = (seed.replace(/[^0-9]/g, '') + '12345678901234567890');
      const letters = (seed.replace(/[^A-Z]/g, '').replace(/[IOQ]/g, '') + 'ABCDEFGHJKLMNPRSTUVWXYZ');
      const alphaNumeric = `${letters}${digits}`;
      return { digits, letters, alphaNumeric };
    }

    generateMaskValue(mask, descriptor, attempt) {
      const source = String(mask || '');
      const unsupported = source.match(/[\[\]{}|?]/);
      if (unsupported) {
        return { supported: false, value: '', tokenCount: 0, reason: `Unsupported mask syntax: ${unsupported[0]}` };
      }
      const streams = this.maskTokenCharacters(descriptor, attempt);
      let digitIndex = 0;
      let letterIndex = 0;
      let alphaIndex = 0;
      let escaped = false;
      let value = '';
      let tokenCount = 0;
      for (const character of source) {
        if (escaped) {
          value += character;
          escaped = false;
          continue;
        }
        if (character === '\\') {
          escaped = true;
          continue;
        }
        if (character === '9') {
          value += streams.digits[digitIndex % streams.digits.length];
          digitIndex += 1;
          tokenCount += 1;
        } else if (character === 'a') {
          value += streams.letters[letterIndex % streams.letters.length];
          letterIndex += 1;
          tokenCount += 1;
        } else if (character === '*') {
          value += streams.alphaNumeric[alphaIndex % streams.alphaNumeric.length];
          alphaIndex += 1;
          tokenCount += 1;
        } else {
          value += character;
        }
      }
      if (escaped) {
        return { supported: false, value: '', tokenCount, reason: 'The mask ends with an incomplete escape.' };
      }
      return { supported: tokenCount > 0, value, tokenCount, reason: tokenCount > 0 ? '' : 'The mask has no supported tokens.' };
    }

    maskRegex(mask) {
      let pattern = '^';
      let escaped = false;
      for (const character of String(mask || '')) {
        if (escaped) {
          pattern += character.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
          escaped = false;
        } else if (character === '\\') {
          escaped = true;
        } else if (character === '9') {
          pattern += '[0-9]';
        } else if (character === 'a') {
          pattern += '[A-Za-z]';
        } else if (character === '*') {
          pattern += '[A-Za-z0-9]';
        } else {
          pattern += character.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
      }
      pattern += '$';
      try {
        return new RegExp(pattern);
      } catch (error) {
        return null;
      }
    }

    maskedValueAccepted(control, mask, generated, bridgeResult) {
      const rendered = cleanText(control && control.value || bridgeResult && bridgeResult.renderedValue || '');
      const regex = this.maskRegex(mask);
      const noPlaceholders = rendered && !/[_]/.test(rendered);
      const tokenCount = (String(mask).match(/[9a*]/g) || []).length;
      const renderedTokenCount = (rendered.match(/[A-Za-z0-9]/g) || []).length;
      const validByRegex = Boolean(regex && regex.test(rendered));
      const validByTokenCount = noPlaceholders && renderedTokenCount >= tokenCount;
      const inputmaskComplete = bridgeResult && bridgeResult.inputmaskComplete;
      const htmlValid = !control || typeof control.checkValidity !== 'function' || control.checkValidity();
      return Boolean((validByRegex || validByTokenCount || inputmaskComplete) && htmlValid && rendered);
    }

    async fillMaskedInput(descriptor, attempt) {
      const control = this.primaryTextControl(descriptor);
      const plan = this.resolveMaskPlan(descriptor, control, attempt);
      if (!plan) {
        return { success: false, message: 'No input mask was available.' };
      }
      const generated = this.generateMaskValue(plan.mask, descriptor, attempt);
      if (!generated.supported) {
        await this.log('MASK_SYNTAX_UNSUPPORTED', {
          componentId: descriptor.id,
          key: descriptor.key,
          maskSource: plan.source,
          mask: plan.mask,
          message: generated.reason
        });
        return { success: false, message: generated.reason };
      }
      await this.log('MASK_VALUE_GENERATED', {
        componentId: descriptor.id,
        key: descriptor.key,
        maskSource: plan.source,
        mask: plan.mask,
        ruleId: plan.rule ? plan.rule.id : '',
        tokenCount: generated.tokenCount,
        value: safeValueInfo(generated.value, true)
      });

      let bridgeResult = null;
      try {
        bridgeResult = await this.bridgeCommand('SET_MASKED_VALUE', {
          key: descriptor.key,
          wrapperId: descriptor.wrapperId,
          value: generated.value,
          mask: plan.mask
        }, CONFIG.bridgeTimeoutMs);
      } catch (error) {
        await this.log('MASK_BRIDGE_SET_FAILED', {
          componentId: descriptor.id,
          key: descriptor.key,
          message: error.message
        });
      }

      const liveWrapper = this.liveWrapper(descriptor) || descriptor.wrapper;
      const liveControl = liveWrapper.querySelector('input:not([type="hidden"]), textarea') || control;
      if (!this.maskedValueAccepted(liveControl, plan.mask, generated.value, bridgeResult) && liveControl) {
        liveControl.focus();
        await this.nativeSetValue(liveControl, generated.value, {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type
        });
        await delay(140);
      }
      const accepted = this.maskedValueAccepted(liveControl, plan.mask, generated.value, bridgeResult);
      if (!accepted) {
        return {
          success: false,
          message: `The value generated for mask ${plan.mask} did not persist as a complete valid value.`
        };
      }
      return {
        success: true,
        valueInfo: Object.assign(safeValueInfo(generated.value, true), {
          maskSource: plan.source,
          mask: plan.mask,
          ruleId: plan.rule ? plan.rule.id : '',
          tokenCount: generated.tokenCount
        }),
        maskPlan: plan
      };
    }

    constraints(descriptor, control) {
      const meta = descriptor.meta || {};
      const number = (value) => value === undefined || value === null || value === '' ? null : Number(value);
      const firstNumber = (...values) => {
        for (const value of values) {
          const parsed = number(value);
          if (parsed !== null && Number.isFinite(parsed)) {
            return parsed;
          }
        }
        return null;
      };
      const hasAttribute = (name) => Boolean(control && control.hasAttribute && control.hasAttribute(name));
      const wrapperText = cleanText(`${descriptor.label || ''} ${descriptor.description || ''} ${descriptor.wrapper ? descriptor.wrapper.textContent : ''}`);
      const explicitMaxCharacters = wrapperText.match(/(?:maximum|max\.?|up to)\s*[:=-]?\s*(\d+)\s*characters?\b/i);
      const explicitMinCharacters = wrapperText.match(/(?:minimum|min\.?|at least)\s*[:=-]?\s*(\d+)\s*characters?\b/i);
      const explicitMaxWords = wrapperText.match(/(?:maximum|max\.?|up to)\s*[:=-]?\s*(\d+)\s*words?\b/i);
      const explicitMinWords = wrapperText.match(/(?:minimum|min\.?|at least)\s*[:=-]?\s*(\d+)\s*words?\b/i);
      const remainingText = descriptor.wrapper
        ? cleanText((descriptor.wrapper.querySelector('[ref="charcount"], .form-text.text-right .text-muted, .form-text.text-right') || {}).textContent || '')
        : '';
      const remainingMatch = remainingText.match(/(-?\d+)\s*characters?\s+remaining/i);
      let counterMaximum = null;
      if (remainingMatch && control && typeof control.value === 'string') {
        counterMaximum = control.value.length + Number(remainingMatch[1]);
        if (!Number.isFinite(counterMaximum) || counterMaximum <= 0) {
          counterMaximum = null;
        }
      }
      return {
        minLength: firstNumber(
          hasAttribute('minlength') ? control.minLength : null,
          meta.minLength,
          explicitMinCharacters ? explicitMinCharacters[1] : null
        ),
        maxLength: firstNumber(
          hasAttribute('maxlength') ? control.maxLength : null,
          meta.maxLength,
          explicitMaxCharacters ? explicitMaxCharacters[1] : null,
          counterMaximum
        ),
        minWords: firstNumber(meta.minWords, explicitMinWords ? explicitMinWords[1] : null),
        maxWords: firstNumber(meta.maxWords, explicitMaxWords ? explicitMaxWords[1] : null),
        minSelectedCount: firstNumber(meta.minSelectedCount),
        maxSelectedCount: firstNumber(meta.maxSelectedCount),
        min: firstNumber(control && control.min !== '' ? control.min : null, meta.min),
        max: firstNumber(control && control.max !== '' ? control.max : null, meta.max),
        pattern: (control && control.pattern) || meta.pattern || '',
        placeholder: (control && control.placeholder) || meta.placeholder || '',
        inputMask: meta.inputMask || '',
        runtimeInputMask: meta.runtimeInputMask || this.runtimeMaskFromControl(control) || ''
      };
    }

    fitText(text, constraints) {
      let value = String(text);
      const minLength = constraints.minLength && constraints.minLength > 0 ? constraints.minLength : 0;
      const maxLength = constraints.maxLength && constraints.maxLength > 0 ? constraints.maxLength : 0;
      const minWords = constraints.minWords && constraints.minWords > 0 ? constraints.minWords : 0;
      const maxWords = constraints.maxWords && constraints.maxWords > 0 ? constraints.maxWords : 0;

      while (value.length < minLength) {
        value += ` Additional program information for run ${this.runId}.`;
      }
      while (minWords && cleanText(value).split(/\s+/).filter(Boolean).length < minWords) {
        value += ` Additional synthetic details for run ${this.runId}.`;
      }
      if (maxWords) {
        const words = cleanText(value).split(/\s+/).filter(Boolean);
        if (words.length > maxWords) {
          value = words.slice(0, maxWords).join(' ');
        }
      }
      if (maxLength && value.length > maxLength) {
        value = value.slice(0, maxLength).trimEnd();
      }
      if (!value && maxLength !== 0) {
        value = 'Test';
      }
      return value;
    }

    emailValue(descriptor) {
      const context = `${descriptor.key} ${descriptor.label}`.toLowerCase();
      let role = 'contact';
      if (/alternative|alternate/.test(context)) {
        role = 'alternative';
      } else if (/president|chair/.test(context)) {
        role = 'chair';
      } else if (/contact\s*1|contact1/.test(context)) {
        role = 'contact1';
      } else if (/contact\s*2|contact2/.test(context)) {
        role = 'contact2';
      } else if (/contact\s*3|contact3/.test(context)) {
        role = 'contact3';
      } else if (/contact\s*4|contact4/.test(context)) {
        role = 'contact4';
      } else {
        const keySlug = String(descriptor.key || 'contact')
          .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
          .replace(/[^a-z0-9]+/gi, '-')
          .replace(/^-+|-+$/g, '')
          .toLowerCase();
        role = keySlug.slice(-32) || 'contact';
      }
      return `${role}.${this.runId.toLowerCase()}@cedarridgecommunity.ca`;
    }

    generateText(descriptor, attempt, control) {
      const context = `${descriptor.key} ${descriptor.label} ${descriptor.description}`.toLowerCase();
      const constraints = this.constraints(descriptor, control);
      let value;
      if (/first\s*name/.test(context)) {
        value = 'Jordan';
      } else if (/last\s*name|surname/.test(context)) {
        value = 'Campbell';
      } else if (/contact\s*name|applicant\s*name|full\s*name|your\s*name/.test(context)) {
        value = `Jordan Campbell ${this.runId}`;
      } else if (/organization|organisation|society|legal\s*name|business\s*name/.test(context) && /name/.test(context)) {
        value = `Cedar Ridge Community Association ${this.runId}`;
      } else if (/email/.test(context)) {
        value = this.emailValue(descriptor);
      } else if (/phone|telephone|mobile|fax/.test(context)) {
        value = attempt === 1 ? '2505550142' : attempt === 2 ? '(250) 555-0142' : '6045550188';
      } else if (/postal|zip/.test(context)) {
        value = attempt > 1 ? 'V8W2B7' : 'V8W 2B7';
      } else if (/(?:address.*(?:unit|suite|apartment)|(?:unit|suite|apartment).*address)/.test(context)) {
        value = '200';
      } else if (/address.*(?:line\s*2|line2|address\s*2)|(?:line\s*2|line2).*address/.test(context)) {
        value = 'Building A';
      } else if (/address.*(?:line\s*1|line1|address\s*1)|(?:line\s*1|line1).*address|street\s*address/.test(context)) {
        value = '123 Douglas Street';
      } else if (/city|municipality/.test(context) && !/describe|list|serve/.test(context)) {
        value = 'Victoria';
      } else if (/province|state/.test(context)) {
        value = 'British Columbia';
      } else if (/country/.test(context)) {
        value = 'Canada';
      } else if (/mailing\s*address|physical\s*address|business\s*address|organization\s*address|organisation\s*address/.test(context)) {
        value = '123 Douglas Street';
      } else if (/website|web\s*site|url/.test(context)) {
        value = 'https://www2.gov.bc.ca';
      } else if (/business\s*number|cra/.test(context)) {
        value = '123456789RC0001';
      } else if (/society\s*number|incorporation\s*number|registration\s*number/.test(context)) {
        value = 'S12345';
      } else if (/submission\s*(?:number|#)/.test(context)) {
        value = this.runId.padEnd(8, 'A').slice(0, 8);
      } else if (/title|position|role/.test(context)) {
        value = 'Program Manager';
      } else if (/project\s*name|program\s*name|initiative\s*name/.test(context)) {
        value = `Community Access Program ${this.runId}`;
      } else if (/description|explain|provide details|summary|purpose|activities|outcome|need|rationale|comments|notes/.test(context)) {
        value = `Automated CHEFS run ${this.runId}. The organization delivers recurring community services, coordinates trained staff and volunteers, and tracks participation, service quality, and financial results. The requested information is synthetic and is intended solely to exercise this form field.`;
      } else {
        value = `Automated entry ${this.runId}`;
      }
      return this.fitText(value, constraints);
    }

    generateNumber(descriptor, attempt, control) {
      const context = `${descriptor.type} ${descriptor.key} ${descriptor.label} ${descriptor.description}`.toLowerCase();
      const constraints = this.constraints(descriptor, control);
      const isIdentifier = /(?:applicant|unity|business|society|registration|incorporation|submission)\s*(?:id|number|#)|l&g|lng/.test(context);
      const isCountQuestion = /how\s+many|number\s+of|count\b|total\s+(?:programs|projects|contacts|items|rows|people)/.test(context) &&
        !isIdentifier &&
        !/phone|telephone|mobile|fax|postal/.test(context);
      let value;
      if (/how\s+many\s+programs|number\s+of\s+programs|program\s+count/.test(context)) {
        value = 1;
      } else if (isCountQuestion) {
        value = 1;
      } else if (/year/.test(context) && !/amount|budget|currency/.test(context)) {
        value = 2024;
      } else if (/month/.test(context)) {
        value = 10;
      } else if (/day/.test(context)) {
        value = 31;
      } else if (/percentage|percent|%/.test(context)) {
        value = 25;
      } else if (/employee|staff|volunteer|participant|member|people|attendee/.test(context)) {
        value = 25 + attempt;
      } else if (/currency|amount|budget|revenue|expense|cost|dollar|funds?\s+requested|funding\s+amount|requested\s+funding/.test(context)) {
        value = 12500 + (attempt - 1) * 500;
      } else if (isIdentifier) {
        value = 900001 + attempt;
      } else {
        value = 10 + attempt;
      }
      if (constraints.min !== null && Number.isFinite(constraints.min) && value < constraints.min) {
        value = constraints.min;
      }
      if (constraints.max !== null && Number.isFinite(constraints.max) && value > constraints.max) {
        value = constraints.min !== null && Number.isFinite(constraints.min)
          ? constraints.min + Math.max(1, (constraints.max - constraints.min) / 2)
          : constraints.max;
      }
      return value;
    }

    generateDate(descriptor, attempt, control) {
      const context = `${descriptor.key} ${descriptor.label} ${descriptor.description}`.toLowerCase();
      const date = new Date();
      if (/birth/.test(context)) {
        date.setFullYear(1990, 5, 15);
      } else if (/start|future|begin/.test(context)) {
        date.setDate(date.getDate() + 30 + attempt);
      } else if (/end|completion|finish/.test(context)) {
        date.setDate(date.getDate() + 120 + attempt);
      } else {
        date.setDate(date.getDate() - 30 - attempt);
      }
      const yyyy = date.getFullYear();
      const mm = String(date.getMonth() + 1).padStart(2, '0');
      const dd = String(date.getDate()).padStart(2, '0');
      const type = control ? control.type : '';
      const placeholder = cleanText((control && control.placeholder) || '').toLowerCase();
      if (type === 'datetime-local') {
        return `${yyyy}-${mm}-${dd}T10:30`;
      }
      if (type === 'time') {
        return '10:30';
      }
      if (type === 'date') {
        return `${yyyy}-${mm}-${dd}`;
      }
      if (/mm\/dd\/yyyy/.test(placeholder)) {
        return `${mm}/${dd}/${yyyy}`;
      }
      if (/dd\/mm\/yyyy/.test(placeholder)) {
        return `${dd}/${mm}/${yyyy}`;
      }
      return `${yyyy}-${mm}-${dd}`;
    }

    generateValue(descriptor, attempt, control) {
      const inputType = control ? String(control.type || '').toLowerCase() : '';
      const context = `${descriptor.type} ${descriptor.key} ${descriptor.label}`.toLowerCase();
      if (/phone|telephone|mobile|fax/.test(context)) {
        return this.generateText(descriptor, attempt, control);
      }
      if (['number', 'range'].includes(inputType) || /number|currency|decimal|percent/.test(descriptor.type)) {
        return this.generateNumber(descriptor, attempt, control);
      }
      if (['date', 'datetime-local', 'time', 'month'].includes(inputType) || /date|time/.test(descriptor.type)) {
        return this.generateDate(descriptor, attempt, control);
      }
      return this.generateText(descriptor, attempt, control);
    }

    async fillTextarea(descriptor, attempt) {
      const textarea = descriptor.wrapper.querySelector('textarea:not(:disabled):not([readonly])');
      if (!textarea) {
        return { success: false, message: 'No writable textarea was found.' };
      }
      const value = this.generateText(descriptor, attempt, textarea);
      textarea.focus();
      await this.nativeSetValue(textarea, value, {
        componentId: descriptor.id,
        key: descriptor.key,
        componentType: descriptor.type
      });
      await delay(140);
      if (cleanText(textarea.value) !== '') {
        return { success: true, valueInfo: safeValueInfo(value, true) };
      }
      return this.fillViaBridge(descriptor, value);
    }

    async fillDateTime(descriptor, attempt) {
      const input = descriptor.wrapper.querySelector('input:not(:disabled):not([readonly]):not([type="hidden"])');
      const value = this.generateDate(descriptor, attempt, input);
      if (input) {
        input.focus();
        await this.nativeSetValue(input, value, {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type
        });
        await delay(180);
        if (cleanText(input.value) !== '') {
          return { success: true, valueInfo: safeValueInfo(value, true) };
        }
      }
      const bridgeValue = /T/.test(value) ? new Date(value).toISOString() : value;
      return this.fillViaBridge(descriptor, bridgeValue);
    }

    phoneDigitCount(input) {
      if (!input) {
        return 0;
      }
      try {
        if (input.inputmask && typeof input.inputmask.unmaskedvalue === 'function') {
          return String(input.inputmask.unmaskedvalue() || '').replace(/\D/g, '').length;
        }
      } catch (error) {
        // Fall back to the rendered value.
      }
      return String(input.value || '').replace(/\D/g, '').length;
    }

    async fillPhone(descriptor, attempt) {
      const input = descriptor.wrapper.querySelector('input:not(:disabled):not([readonly]):not([type="hidden"])');
      const digits = attempt >= 3 ? '6045550188' : '2505550142';
      const formatted = attempt === 2 ? '(250) 555-0142' : digits;

      if (input) {
        input.focus();
        await this.nativeSetValue(input, '', {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type
        });
        await delay(60);
        await this.nativeSetValue(input, formatted, {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type
        });
        await delay(260);
        const digitCount = this.phoneDigitCount(input);
        if (digitCount >= 10) {
          return {
            success: true,
            valueInfo: Object.assign(safeValueInfo(formatted, true), { digitCount, semanticType: 'phone' })
          };
        }
      }

      try {
        const bridgeValue = attempt === 1 ? digits : '(250) 555-0142';
        const bridgeResult = await this.bridgeCommand('SET_VALUE', { key: descriptor.key, value: bridgeValue }, CONFIG.bridgeTimeoutMs);
        await delay(260);
        const digitCount = this.phoneDigitCount(input);
        const bridgeHasValue = Boolean(bridgeResult && bridgeResult.hasValue);
        if (digitCount >= 10 || (bridgeHasValue && !input)) {
          return {
            success: true,
            valueInfo: Object.assign(safeValueInfo(bridgeValue, true), { digitCount, semanticType: 'phone' })
          };
        }
      } catch (error) {
        return { success: false, message: `Phone input rejected the generated number: ${error.message}` };
      }

      return {
        success: false,
        message: `Phone input remained incomplete after entry. Rendered digit count: ${this.phoneDigitCount(input)}.`
      };
    }

    async fillInput(descriptor, attempt) {
      const inputs = Array.from(descriptor.wrapper.querySelectorAll('input:not(:disabled):not([readonly]):not([type="hidden"]):not([type="button"]):not([type="submit"]):not([type="checkbox"]):not([type="radio"])'));
      if (!inputs.length) {
        return this.fillViaBridge(descriptor, this.generateValue(descriptor, attempt));
      }
      let success = false;
      let lastValue = '';
      for (const input of inputs) {
        const value = this.generateValue(descriptor, attempt, input);
        lastValue = value;
        input.focus();
        await this.nativeSetValue(input, String(value), {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type
        });
        await delay(100);
        success = success || cleanText(input.value) !== '';
      }
      if (success) {
        return { success: true, valueInfo: safeValueInfo(lastValue, true) };
      }
      return this.fillViaBridge(descriptor, lastValue);
    }

    async fillViaBridge(descriptor, value) {
      if (!descriptor.key || descriptor.key.startsWith('dom-')) {
        return { success: false, message: 'The component key is unavailable for Form.io setValue.' };
      }
      const result = await this.bridgeCommand('SET_VALUE', { key: descriptor.key, value }, CONFIG.bridgeTimeoutMs);
      return {
        success: Boolean(result && (result.hasValue || result.changed)),
        valueInfo: safeValueInfo(value, true)
      };
    }

    attachmentChoice(descriptor) {
      const context = `${descriptor.key} ${descriptor.label} ${descriptor.description} ${(descriptor.meta && descriptor.meta.filePattern) || ''}`.toLowerCase();
      if (/xlsx|spreadsheet|excel|budget/.test(context)) {
        return { filename: 'chefs-attachment.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' };
      }
      if (/docx|word/.test(context)) {
        return { filename: 'chefs-attachment.docx', mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' };
      }
      if (/csv/.test(context)) {
        return { filename: 'chefs-attachment.csv', mimeType: 'text/csv' };
      }
      if (/json/.test(context)) {
        return { filename: 'chefs-attachment.json', mimeType: 'application/json' };
      }
      if (/png|image|photo/.test(context)) {
        return { filename: 'chefs-attachment.png', mimeType: 'image/png' };
      }
      if (/jpe?g/.test(context)) {
        return { filename: 'chefs-attachment.jpg', mimeType: 'image/jpeg' };
      }
      if (/txt|text/.test(context)) {
        return { filename: 'chefs-attachment.txt', mimeType: 'text/plain' };
      }
      return { filename: 'chefs-attachment.pdf', mimeType: 'application/pdf' };
    }

    bytesToBase64(bytes) {
      let binary = '';
      const chunkSize = 0x8000;
      for (let index = 0; index < bytes.length; index += chunkSize) {
        binary += String.fromCharCode(...bytes.subarray(index, index + chunkSize));
      }
      return btoa(binary);
    }

    async sha256Hex(bytes) {
      const digest = await crypto.subtle.digest('SHA-256', bytes);
      return Array.from(new Uint8Array(digest)).map((value) => value.toString(16).padStart(2, '0')).join('');
    }

    createDragEvent(type, dataTransfer) {
      try {
        return new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer });
      } catch (error) {
        const event = new Event(type, { bubbles: true, cancelable: true });
        Object.defineProperty(event, 'dataTransfer', { value: dataTransfer });
        return event;
      }
    }

    async uploadFileByDomDrop(descriptor, file) {
      let wrapper = this.liveWrapper(descriptor);
      const dropTarget = wrapper && (wrapper.querySelector('[ref="fileDrop"], .fileSelector') || wrapper);
      if (!dropTarget) {
        throw new Error('The file drop target was not found.');
      }

      const baselineCount = this.uploadedFileRows(wrapper).length;
      const pending = this.fileUploadsInFlight.get(descriptor.id);
      if (!pending) {
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        this.fileUploadsInFlight.set(descriptor.id, {
          filename: file.name,
          startedAt: Date.now(),
          baselineCount,
          wrapperId: descriptor.wrapperId || (wrapper && wrapper.id) || ''
        });
        dropTarget.dispatchEvent(this.createDragEvent('dragenter', dataTransfer));
        dropTarget.dispatchEvent(this.createDragEvent('dragover', dataTransfer));
        dropTarget.dispatchEvent(this.createDragEvent('drop', dataTransfer));
      } else {
        await this.log('UPLOAD_PENDING_RECHECK', {
          componentId: descriptor.id,
          key: descriptor.key,
          filename: pending.filename,
          pendingForMs: Date.now() - pending.startedAt
        });
      }

      const started = Date.now();
      let wrapperReplaced = false;
      let lastPendingLogAt = 0;
      const expectedBaseline = pending ? pending.baselineCount : baselineCount;

      while (Date.now() - started < CONFIG.uploadTimeoutMs) {
        const live = this.liveWrapper(descriptor);
        if (live && wrapper && live !== wrapper) {
          wrapperReplaced = true;
          wrapper = live;
          await this.log('UPLOAD_WRAPPER_REPLACED', {
            componentId: descriptor.id,
            key: descriptor.key,
            filename: file.name,
            wrapperId: descriptor.wrapperId || (wrapper && wrapper.id) || ''
          });
        } else if (live) {
          wrapper = live;
        }

        const allRows = this.uploadedFileRows(wrapper);
        const namedRows = this.uploadedFileRows(wrapper, file.name);
        if (
          namedRows.length ||
          allRows.length > expectedBaseline ||
          (wrapper && cleanText(wrapper.textContent).includes(file.name))
        ) {
          this.fileUploadsInFlight.delete(descriptor.id);
          return {
            hasValue: true,
            valueCount: Math.max(1, allRows.length, namedRows.length),
            method: wrapperReplaced ? 'dom-drop-rerendered-wrapper' : 'dom-drop'
          };
        }

        const now = Date.now();
        if (now - lastPendingLogAt >= 10000) {
          lastPendingLogAt = now;
          const loader = wrapper && wrapper.querySelector('[ref="fileProcessingLoader"], .loader-wrapper, .progress, [role="progressbar"]');
          await this.log('UPLOAD_STILL_PENDING', {
            componentId: descriptor.id,
            key: descriptor.key,
            filename: file.name,
            pendingForMs: now - (this.fileUploadsInFlight.get(descriptor.id)?.startedAt || started),
            loaderVisible: Boolean(loader && isVisible(loader)),
            renderedFileRows: allRows.length
          });
        }

        const errorElement = wrapper && wrapper.querySelector('.formio-errors, .invalid-feedback');
        const message = cleanText(errorElement ? errorElement.textContent : '');
        if (message) {
          this.fileUploadsInFlight.delete(descriptor.id);
          throw new Error(message);
        }
        await delay(250);
      }

      // Keep the in-flight marker so a later pass monitors the existing upload
      // instead of dropping the same file a second time.
      throw new Error('The DOM drop is still pending and no uploaded file row has appeared yet.');
    }

    async fillFile(descriptor) {
      let liveWrapper = this.liveWrapper(descriptor);
      if (liveWrapper && this.uploadedFileRows(liveWrapper).length > 0) {
        this.fileHandled.add(descriptor.id);
        this.fileUploadsInFlight.delete(descriptor.id);
        return { success: true, valueInfo: safeValueInfo('existing-file', false) };
      }
      if (this.fileHandled.has(descriptor.id)) {
        return { success: false, message: 'The file component was previously marked complete but is now empty.' };
      }

      const chosen = this.attachmentChoice(descriptor);
      const response = await fetch(chrome.runtime.getURL(`attachments/${chosen.filename}`));
      if (!response.ok) {
        throw new Error(`Packaged attachment could not be read: ${chosen.filename}`);
      }
      const buffer = await response.arrayBuffer();
      const bytes = new Uint8Array(buffer);
      const hash = await this.sha256Hex(bytes);
      const file = new File([bytes], chosen.filename, {
        type: chosen.mimeType,
        lastModified: Date.now()
      });
      this.progress.attachmentsPending += 1;
      await this.log('UPLOAD_STARTED', {
        componentId: descriptor.id,
        key: descriptor.key,
        filename: chosen.filename,
        mimeType: chosen.mimeType,
        sizeBytes: bytes.length,
        sha256: hash
      });
      await this.updateRun({ progress: Object.assign({}, this.progress), currentAction: `Uploading ${descriptor.key}` });

      let result = null;
      let bridgeError = null;
      try {
        result = await this.bridgeCommand('UPLOAD_FILE', {
          key: descriptor.key,
          wrapperId: descriptor.wrapperId || (liveWrapper && liveWrapper.id) || '',
          filename: chosen.filename,
          mimeType: chosen.mimeType,
          base64: this.bytesToBase64(bytes)
        }, CONFIG.uploadTimeoutMs);
      } catch (error) {
        bridgeError = error;
        await this.log('UPLOAD_API_FALLBACK', {
          componentId: descriptor.id,
          key: descriptor.key,
          message: error.message,
          stack: error.stack || ''
        });
      }

      try {
        if (!result || (!result.hasValue && !result.valueCount)) {
          result = await this.uploadFileByDomDrop(descriptor, file);
        }
        const started = Date.now();
        while (Date.now() - started < CONFIG.uploadTimeoutMs) {
          liveWrapper = this.liveWrapper(descriptor);
          if (liveWrapper && !this.inspectEmpty(liveWrapper, descriptor.type)) {
            break;
          }
          await delay(250);
        }
        liveWrapper = this.liveWrapper(descriptor);
        if (!liveWrapper || this.inspectEmpty(liveWrapper, descriptor.type)) {
          throw new Error(bridgeError
            ? `File upload API and DOM drop did not produce a stored file. API error: ${bridgeError.message}`
            : 'The Form.io file component did not report or render a stored file value.');
        }

        this.fileHandled.add(descriptor.id);
        this.progress.attachmentsPending = Math.max(0, this.progress.attachmentsPending - 1);
        this.progress.attachmentsCompleted += 1;
        await this.runtimeMessage({
          type: 'ADD_ATTACHMENT_RECORD',
          runId: this.runId,
          attachment: {
            time: new Date().toISOString(),
            key: descriptor.key,
            filename: chosen.filename,
            mimeType: chosen.mimeType,
            sizeBytes: bytes.length,
            sha256: hash,
            outcome: 'completed',
            valueCount: result.valueCount || 1,
            method: result.method || result.uploadMethod || 'formio-api'
          }
        });
        await this.log('UPLOAD_COMPLETED', {
          componentId: descriptor.id,
          key: descriptor.key,
          filename: chosen.filename,
          valueCount: result.valueCount || 1,
          method: result.method || result.uploadMethod || 'formio-api'
        });
        return { success: true, valueInfo: safeValueInfo(chosen.filename, true) };
      } catch (error) {
        this.progress.attachmentsPending = Math.max(0, this.progress.attachmentsPending - 1);
        const stillPending = /still pending/i.test(error.message || '');
        if (stillPending) {
          await this.log('UPLOAD_PENDING_TIMEOUT', {
            componentId: descriptor.id,
            key: descriptor.key,
            filename: chosen.filename,
            message: error.message
          });
          return { success: false, message: error.message };
        }
        this.fileUploadsInFlight.delete(descriptor.id);
        await this.runtimeMessage({
          type: 'ADD_ATTACHMENT_RECORD',
          runId: this.runId,
          attachment: {
            time: new Date().toISOString(),
            key: descriptor.key,
            filename: chosen.filename,
            mimeType: chosen.mimeType,
            sizeBytes: bytes.length,
            sha256: hash,
            outcome: 'failed',
            message: error.message
          }
        });
        await this.log('UPLOAD_FAILED', {
          componentId: descriptor.id,
          key: descriptor.key,
          filename: chosen.filename,
          message: error.message,
          stack: error.stack || ''
        });
        throw error;
      }
    }

    gridRows(wrapper, type) {
      if (!wrapper) {
        return [];
      }
      const selectors = type.includes('editgrid')
        ? '.editgrid-row, [ref^="editgrid-"][ref$="-row"]'
        : '.datagrid-table > tbody > tr, [ref^="datagrid-"][ref$="-row"]';
      return Array.from(new Set(Array.from(wrapper.querySelectorAll(selectors))))
        .filter((row) => row.isConnected && !row.closest('[hidden], .formio-hidden'));
    }

    gridAddButton(wrapper) {
      if (!wrapper) {
        return null;
      }
      const direct = wrapper.querySelector('button.formio-button-add-row, [ref$="-addRow"]');
      if (direct && isVisible(direct) && !direct.disabled) {
        return direct;
      }
      return Array.from(wrapper.querySelectorAll('button, a.btn'))
        .find((element) => isVisible(element) && !element.disabled && GRID_ADD_ACTION.test(cleanText(element.textContent))) || null;
    }

    gridTargetRows(descriptor) {
      const configured = Math.max(2, Math.min(5, Number(this.settings.rowsPerGrid) || 2));
      const minimum = descriptor.meta && Number.isFinite(Number(descriptor.meta.minLength))
        ? Number(descriptor.meta.minLength)
        : 0;
      const maximum = descriptor.meta && Number.isFinite(Number(descriptor.meta.maxLength))
        ? Number(descriptor.meta.maxLength)
        : 5;
      return Math.max(minimum, Math.min(configured, maximum > 0 ? maximum : configured));
    }

    async handleGrid(descriptor) {
      let wrapper = this.liveWrapper(descriptor) || descriptor.wrapper;
      if (!wrapper) {
        return 0;
      }
      const targetRows = this.gridTargetRows(descriptor);
      let rowCount = this.gridRows(wrapper, descriptor.type).length;
      if (this.gridHandled.has(descriptor.id)) {
        return 0;
      }
      await this.log('GRID_INSPECTED', {
        componentId: descriptor.id,
        key: descriptor.key,
        componentType: descriptor.type,
        existingRows: rowCount,
        targetRows
      });
      if (rowCount >= targetRows) {
        this.gridHandled.add(descriptor.id);
        return 0;
      }

      let added = 0;
      while (rowCount < targetRows) {
        wrapper = this.liveWrapper(descriptor) || wrapper;
        const button = this.gridAddButton(wrapper);
        if (!button) {
          if (rowCount > 0) {
            await this.log('GRID_TARGET_UNAVAILABLE', {
              componentId: descriptor.id,
              key: descriptor.key,
              componentType: descriptor.type,
              reason: 'No enabled add-row control was found.',
              existingRows: rowCount,
              targetRows
            });
            this.gridHandled.add(descriptor.id);
            break;
          }
          const state = this.componentStates.get(descriptor.id);
          if (state) {
            state.status = 'unsupported';
            state.lastError = 'No add-row control was found.';
          }
          await this.log('COMPONENT_UNSUPPORTED', {
            componentId: descriptor.id,
            key: descriptor.key,
            componentType: descriptor.type,
            reason: 'No add-row control was found.',
            existingRows: rowCount,
            targetRows
          });
          this.gridHandled.add(descriptor.id);
          break;
        }

        const before = rowCount;
        await this.log('GRID_ROW_ADD_ATTEMPT', {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type,
          existingRows: before,
          targetRows
        });
        button.click();

        const started = Date.now();
        let after = before;
        while (Date.now() - started < 5000) {
          await delay(100);
          wrapper = this.liveWrapper(descriptor) || wrapper;
          after = this.gridRows(wrapper, descriptor.type).length;
          if (after > before) {
            break;
          }
        }
        if (after <= before) {
          await this.log('GRID_ROW_ADD_FAILED', {
            componentId: descriptor.id,
            key: descriptor.key,
            componentType: descriptor.type,
            existingRows: before,
            targetRows
          });
          break;
        }

        rowCount = after;
        added += after - before;
        this.progress.rowsAdded += after - before;
        await this.log('GRID_ROW_ADDED', {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type,
          rowsAdded: after - before,
          rowCount,
          targetRows
        });
      }

      if (rowCount >= targetRows) {
        this.gridHandled.add(descriptor.id);
        await this.log('GRID_TARGET_REACHED', {
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type,
          rowCount,
          targetRows
        });
      }
      if (added > 0) {
        await this.markProgress(`Added ${added} row(s) to ${descriptor.key}`);
      }
      return added;
    }

    async handleLookupActions() {
      const buttons = Array.from(document.querySelectorAll('.formio-form button[type="button"], .formio-form a.btn'))
        .filter((button) => isVisible(button));
      let actions = 0;
      for (const button of buttons) {
        const text = cleanText(button.textContent || button.title);
        if (!text || ACTION_EXCLUDE.test(text) || GRID_ADD_ACTION.test(text) || !LOOKUP_ACTION.test(text)) {
          continue;
        }
        const id = `${button.name || ''}::${text}::${button.id || ''}`;
        if (this.actionHandled.has(id)) {
          continue;
        }
        this.actionHandled.add(id);
        this.currentAction = `Running action: ${text}`;
        await this.log('LOOKUP_STARTED', { actionId: id, label: text });
        try {
          button.click();
          await delay(1800);
          await this.log('LOOKUP_COMPLETED', { actionId: id, label: text });
          await this.markProgress(`Ran action ${text}`);
          actions += 1;
        } catch (error) {
          await this.log('LOOKUP_FAILED', { actionId: id, label: text, message: error.message });
        }
      }
      return actions;
    }

    getTabEntries() {
      const rawTabs = Array.from(document.querySelectorAll(
        '.formio-form .formio-component-tabs .nav-tabs [role="tab"], ' +
        '.formio-form .formio-component-tabs .nav-tabs .nav-link, ' +
        '.formio-form [role="tablist"] [role="tab"], ' +
        '.formio-form [role="tablist"] .nav-link'
      ));
      const seenControls = new Set();
      const entries = [];
      for (const raw of rawTabs) {
        const host = raw.matches('[role="tab"]') ? raw : (raw.closest('[role="tab"]') || raw);
        const control = raw.matches('a, button')
          ? raw
          : (raw.querySelector('a.nav-link, button.nav-link, a[href], button') || raw);
        if (!control || seenControls.has(control) || !isVisible(host) || control.classList.contains('disabled') || control.disabled) {
          continue;
        }
        seenControls.add(control);
        const label = cleanText(control.textContent || host.textContent);
        const target = control.getAttribute('href') || control.getAttribute('data-bs-target') || control.getAttribute('data-target') || control.getAttribute('aria-controls') || '';
        const id = target || control.id || host.id || label;
        if (!id) {
          continue;
        }
        entries.push({ id, label, host, control, target });
      }
      return entries;
    }

    tabIsActive(entry) {
      return Boolean(
        entry && (
          entry.control.getAttribute('aria-selected') === 'true' ||
          entry.host.getAttribute('aria-selected') === 'true' ||
          entry.control.classList.contains('active') ||
          entry.host.classList.contains('active')
        )
      );
    }

    async waitForTabActivation(tabId, timeoutMs) {
      const started = Date.now();
      while (Date.now() - started < timeoutMs) {
        const current = this.getTabEntries().find((entry) => entry.id === tabId);
        if (current && this.tabIsActive(current)) {
          return true;
        }
        await delay(100);
      }
      return false;
    }

    async activateNextUnvisitedTab() {
      const tabs = this.getTabEntries();
      for (const tab of tabs) {
        if (this.tabIsActive(tab) && !this.visitedTabs.has(tab.id)) {
          this.visitedTabs.add(tab.id);
          await this.log('TAB_RECOGNIZED_ACTIVE', { tabId: tab.id, label: tab.label });
        }
      }

      for (const tab of tabs) {
        if (this.visitedTabs.has(tab.id) || this.tabIsActive(tab)) {
          continue;
        }
        const failures = this.tabActivationFailures.get(tab.id) || 0;
        await this.log('TAB_ACTIVATION_ATTEMPT', {
          tabId: tab.id,
          label: tab.label,
          attempt: failures + 1,
          controlTag: tab.control.tagName.toLowerCase(),
          controlHref: tab.control.getAttribute('href') || ''
        });
        tab.control.click();
        const activated = await this.waitForTabActivation(tab.id, 3000);
        if (activated) {
          this.visitedTabs.add(tab.id);
          this.tabActivationFailures.delete(tab.id);
          await this.log('TAB_ACTIVATED', { tabId: tab.id, label: tab.label, verified: true });
          await this.markProgress(`Activated tab ${tab.label}`);
          return true;
        }

        const nextFailureCount = failures + 1;
        this.tabActivationFailures.set(tab.id, nextFailureCount);
        await this.log('TAB_ACTIVATION_FAILED', {
          tabId: tab.id,
          label: tab.label,
          attempt: nextFailureCount
        });
        if (nextFailureCount >= 2) {
          this.visitedTabs.add(tab.id);
          await this.log('TAB_SKIPPED_AFTER_FAILURE', {
            tabId: tab.id,
            label: tab.label,
            attempts: nextFailureCount
          });
        }
        return false;
      }
      return false;
    }

    visibleFieldSignature() {
      return Array.from(document.querySelectorAll('.formio-component'))
        .filter((element) => isVisible(element))
        .map((element) => element.id || Array.from(element.classList).find((value) => value.startsWith('formio-component-s')) || '')
        .filter(Boolean)
        .slice(0, 100)
        .join('|');
    }

    async advanceWizard() {
      const tabs = this.getTabEntries();
      const unvisitedTabs = tabs.filter((entry) => !this.visitedTabs.has(entry.id));
      if (unvisitedTabs.length) {
        return false;
      }
      const signature = this.visibleFieldSignature();
      if (this.visitedWizardSignatures.has(signature)) {
        return false;
      }
      this.visitedWizardSignatures.add(signature);
      const buttons = Array.from(document.querySelectorAll('.formio-form button, .formio-form a.btn'))
        .filter((button) => isVisible(button) && !button.disabled);
      const next = buttons.find((button) => /^(next|continue|save\s+and\s+continue)$/i.test(cleanText(button.textContent)) || button.getAttribute('ref') === 'next');
      if (!next) {
        return false;
      }
      next.click();
      await this.log('WIZARD_NEXT', { label: cleanText(next.textContent) });
      await this.markProgress(`Advanced wizard using ${cleanText(next.textContent)}`);
      return true;
    }

    tabEntryForWrapper(wrapper) {
      if (!wrapper) {
        return null;
      }
      const tabsRoot = wrapper.closest('.formio-component-tabs');
      const pane = wrapper.closest('[ref="tab-tabs"], .tab-pane[role="tabpanel"]');
      if (!tabsRoot || !pane) {
        return null;
      }
      const panes = Array.from(tabsRoot.querySelectorAll(
        ':scope > .card > [ref="tab-tabs"], :scope > [ref="tab-tabs"], ' +
        ':scope > .card > .tab-pane[role="tabpanel"], :scope > .tab-pane[role="tabpanel"]'
      ));
      const paneIndex = panes.indexOf(pane);
      if (paneIndex < 0) {
        return null;
      }
      const entries = this.getTabEntries().filter((entry) => tabsRoot.contains(entry.host));
      return entries[paneIndex] || null;
    }

    async prepareValidationRepair(errors) {
      const scan = await this.scanComponents();
      const errorKeys = new Set((errors || []).map((error) => error.key).filter(Boolean));
      const invalidDescriptors = scan.descriptors.filter((descriptor) => descriptor.invalid || (descriptor.fillable && errorKeys.has(descriptor.key)));
      const repairTabs = [];
      const seenTabs = new Set();
      const resetFields = [];

      for (const descriptor of invalidDescriptors) {
        const state = this.componentStates.get(descriptor.id);
        if (state) {
          state.attempts = 0;
          state.status = 'retry';
          state.lastError = (errors || []).find((error) => error.key === descriptor.key)?.message || 'Rejected during submission validation.';
        }
        resetFields.push({
          componentId: descriptor.id,
          key: descriptor.key,
          componentType: descriptor.type,
          visible: descriptor.visible
        });
        const tab = this.tabEntryForWrapper(descriptor.wrapper);
        if (tab && !seenTabs.has(tab.id)) {
          seenTabs.add(tab.id);
          repairTabs.push(tab);
          this.visitedTabs.delete(tab.id);
        }
      }

      await this.log('VALIDATION_REPAIR_PREPARED', {
        errorCount: (errors || []).length,
        resetFieldCount: resetFields.length,
        resetFields,
        repairTabs: repairTabs.map((tab) => ({ id: tab.id, label: tab.label }))
      });

      const firstTab = repairTabs[0];
      if (firstTab && !this.tabIsActive(firstTab)) {
        await this.log('VALIDATION_REPAIR_TAB_ACTIVATION_ATTEMPT', {
          tabId: firstTab.id,
          label: firstTab.label
        });
        firstTab.control.click();
        const activated = await this.waitForTabActivation(firstTab.id, 3000);
        await this.log(activated ? 'VALIDATION_REPAIR_TAB_ACTIVATED' : 'VALIDATION_REPAIR_TAB_ACTIVATION_FAILED', {
          tabId: firstTab.id,
          label: firstTab.label
        });
      }
      this.lastProgressAt = Date.now();
      return { invalidDescriptors, repairTabs };
    }

    collectValidationErrors() {
      if (this.confirmationState().confirmed) {
        return [];
      }
      const errors = [];
      const now = new Date().toISOString();
      const componentWrappers = Array.from(document.querySelectorAll('.formio-component'));

      for (const wrapper of componentWrappers) {
        const ownInvalidControl = Array.from(wrapper.querySelectorAll('[aria-invalid="true"], .is-invalid'))
          .some((element) => element.closest('.formio-component') === wrapper);
        const ownMessageElements = Array.from(wrapper.querySelectorAll(
          '.formio-errors .error, .formio-errors, .invalid-feedback .error, .invalid-feedback'
        )).filter((element) => element.closest('.formio-component') === wrapper);
        const messages = ownMessageElements.map((element) => cleanText(element.textContent)).filter(Boolean);
        if (!ownInvalidControl && !messages.length && !wrapper.classList.contains('has-error')) {
          continue;
        }
        const message = Array.from(new Set(messages)).join(' ');
        if (!message) {
          continue;
        }
        const key = getInputKey(wrapper) || '';
        const type = this.inferType(wrapper, key, null);
        errors.push({
          time: now,
          pass: this.currentPass,
          key,
          type,
          message: message.slice(0, 2000),
          componentId: wrapper.id || ''
        });
      }

      const pageSelectors = [
        '.alert-danger',
        '.alert-error',
        '.v-alert--type-error',
        '[role="alert"].alert-danger',
        '[role="alert"].alert-error',
        '[role="alert"][data-test*="error" i]'
      ];
      const pageElements = Array.from(document.querySelectorAll(pageSelectors.join(', ')));
      for (const element of pageElements) {
        if (!isVisible(element) || element.closest('.formio-component')) {
          continue;
        }
        const message = cleanText(element.textContent);
        if (!message) {
          continue;
        }
        errors.push({
          time: now,
          pass: this.currentPass,
          key: '',
          type: '',
          message: message.slice(0, 2000),
          componentId: ''
        });
      }

      const unique = [];
      const seen = new Set();
      for (const error of errors) {
        const signature = `${error.key}|${error.message}`;
        if (!seen.has(signature)) {
          seen.add(signature);
          unique.push(error);
        }
      }
      return unique;
    }

    isGridAddControl(button) {
      if (!button) {
        return false;
      }
      const label = cleanText(button.textContent);
      const ref = button.getAttribute('ref') || '';
      return button.classList.contains('formio-button-add-row') ||
        /-addRow$/i.test(ref) ||
        GRID_ADD_ACTION.test(label);
    }

    isSubmitButtonCandidate(button) {
      if (!button || button.disabled) {
        return false;
      }
      const wrapper = button.closest('.formio-component');
      const label = cleanText(button.textContent);
      const name = button.getAttribute('name') || '';
      const wrapperClass = wrapper ? wrapper.className : '';
      if (this.isGridAddControl(button) || ACTION_EXCLUDE.test(label) || LOOKUP_ACTION.test(label)) {
        return false;
      }
      const explicitSubmit = /(?:^|\s)formio-component-submit(?:\s|$)/.test(wrapperClass) ||
        /^data\[submit\]$/i.test(name) ||
        String(button.type || '').toLowerCase() === 'submit';
      const submitLikeLabel = /^(submit|submit application|send application|complete application|complete submission|send request|submit request|apply)$/i.test(label);
      return explicitSubmit || submitLikeLabel;
    }

    submitButtonCandidates() {
      return Array.from(document.querySelectorAll('.formio-form button'))
        .filter((button) => this.isSubmitButtonCandidate(button));
    }

    submitLandmarkKey(button) {
      const wrapper = button ? button.closest('.formio-component') : null;
      return [
        wrapper && wrapper.id ? wrapper.id : '',
        button && button.getAttribute('name') ? button.getAttribute('name') : '',
        cleanText(button ? button.textContent : '')
      ].join('|');
    }

    async indexSubmitLandmarks() {
      const candidates = this.submitButtonCandidates();
      for (const button of candidates) {
        const wrapper = button.closest('.formio-component');
        const tab = this.tabEntryForWrapper(wrapper);
        const key = this.submitLandmarkKey(button);
        const visible = isVisible(button);
        const existing = this.submitLandmarks.get(key);
        const landmark = {
          key,
          wrapperId: wrapper && wrapper.id ? wrapper.id : '',
          buttonName: button.getAttribute('name') || '',
          buttonLabel: cleanText(button.textContent),
          buttonType: String(button.type || '').toLowerCase(),
          tabId: tab ? tab.id : '',
          tabLabel: tab ? tab.label : '',
          firstSeenPass: existing ? existing.firstSeenPass : this.currentPass,
          lastSeenPass: this.currentPass,
          lastVisiblePass: visible ? this.currentPass : (existing ? existing.lastVisiblePass : 0),
          visible
        };
        this.submitLandmarks.set(key, landmark);
        if (!existing) {
          await this.log('SUBMIT_LANDMARK_DISCOVERED', landmark);
        } else if (existing.visible !== visible) {
          await this.log(visible ? 'SUBMIT_LANDMARK_BECAME_VISIBLE' : 'SUBMIT_LANDMARK_BECAME_HIDDEN', landmark);
        }
      }
      return candidates;
    }

    resolveSubmitLandmark(landmark) {
      if (!landmark) {
        return null;
      }
      if (landmark.wrapperId) {
        const wrapper = document.getElementById(landmark.wrapperId);
        if (wrapper) {
          const match = Array.from(wrapper.querySelectorAll('button')).find((button) => this.isSubmitButtonCandidate(button));
          if (match) {
            return match;
          }
        }
      }
      return this.submitButtonCandidates().find((button) => {
        const sameName = landmark.buttonName && button.getAttribute('name') === landmark.buttonName;
        const sameLabel = landmark.buttonLabel && cleanText(button.textContent) === landmark.buttonLabel;
        return sameName || sameLabel;
      }) || null;
    }

    findSubmitButton() {
      return this.submitButtonCandidates()
        .find((button) => isVisible(button) && !button.disabled) || null;
    }

    async ensureSubmitButtonVisible() {
      await this.indexSubmitLandmarks();
      let visible = this.findSubmitButton();
      if (visible) {
        return visible;
      }

      const landmarks = Array.from(this.submitLandmarks.values())
        .sort((a, b) => (b.lastVisiblePass || 0) - (a.lastVisiblePass || 0) || (b.lastSeenPass || 0) - (a.lastSeenPass || 0));
      for (const landmark of landmarks) {
        const button = this.resolveSubmitLandmark(landmark);
        const wrapper = button ? button.closest('.formio-component') : (landmark.wrapperId ? document.getElementById(landmark.wrapperId) : null);
        const tab = (landmark.tabId ? this.getTabEntries().find((entry) => entry.id === landmark.tabId) : null) || this.tabEntryForWrapper(wrapper);
        if (!tab) {
          continue;
        }
        if (!this.tabIsActive(tab)) {
          await this.log('SUBMIT_TAB_ACTIVATION_ATTEMPT', {
            tabId: tab.id,
            label: tab.label,
            landmarkKey: landmark.key,
            submitLabel: landmark.buttonLabel
          });
          tab.control.click();
          const activated = await this.waitForTabActivation(tab.id, 3000);
          await this.log(activated ? 'SUBMIT_TAB_ACTIVATED' : 'SUBMIT_TAB_ACTIVATION_FAILED', {
            tabId: tab.id,
            label: tab.label,
            landmarkKey: landmark.key,
            submitLabel: landmark.buttonLabel
          });
          if (!activated) {
            continue;
          }
          await delay(250);
        }
        await this.indexSubmitLandmarks();
        visible = this.findSubmitButton();
        if (visible) {
          await this.log('SUBMIT_LANDMARK_REACQUIRED', {
            tabId: tab.id,
            tabLabel: tab.label,
            submitLabel: cleanText(visible.textContent),
            buttonName: visible.getAttribute('name') || ''
          });
          return visible;
        }
      }
      return null;
    }

    confirmationState(overrides) {
      const options = overrides || {};
      const text = cleanText(Object.prototype.hasOwnProperty.call(options, 'text')
        ? options.text
        : (document.body ? (document.body.innerText || document.body.textContent) : ''));
      const url = new URL(options.url || location.href);
      const successPath = /\/form\/success(?:\/|$)/i.test(url.pathname) || /\/submission\/success(?:\/|$)/i.test(url.pathname);
      const successPhrase = /\byour form has been submitted successfully\b|\bform submitted successfully\b|\bsubmission (?:was|has been) received\b|\bthank you for your submission\b/i.test(text);
      const labeledMatch = text.match(/confirmation\s*(?:id|number|#)?\s*[:#-]?\s*([0-9A-F]{8})\b/i);
      let confirmationId = labeledMatch ? labeledMatch[1].toUpperCase() : null;
      let confirmationSource = labeledMatch ? 'page-label' : '';

      if (!confirmationId && (successPath || successPhrase)) {
        const queryCandidates = [
          url.searchParams.get('confirmationId'),
          url.searchParams.get('confirmation'),
          url.searchParams.get('s')
        ].filter(Boolean);
        for (const candidate of queryCandidates) {
          const match = String(candidate).match(/^([0-9a-f]{8})(?:-[0-9a-f-]+)?$/i);
          if (match) {
            confirmationId = match[1].toUpperCase();
            confirmationSource = 'success-url';
            break;
          }
        }
      }

      if (!confirmationId && (successPath || successPhrase)) {
        const standaloneMatch = text.match(/\b([0-9A-F]{8})\b/i);
        if (standaloneMatch) {
          confirmationId = standaloneMatch[1].toUpperCase();
          confirmationSource = 'success-page-token';
        }
      }

      const detectedBy = [];
      if (successPath) {
        detectedBy.push('success-path');
      }
      if (successPhrase) {
        detectedBy.push('success-phrase');
      }
      if (confirmationId) {
        detectedBy.push(confirmationSource || 'confirmation-id');
      }

      return {
        confirmed: Boolean(successPath || successPhrase),
        confirmationId,
        detectedBy,
        url: location.href
      };
    }

    async finalizeSubmitted(confirmation, attempt, phase) {
      if (this.successFinalized) {
        return true;
      }
      this.successFinalized = true;
      const success = confirmation || this.confirmationState();
      const confirmationId = success && success.confirmationId ? success.confirmationId : null;
      this.progress.remaining = 0;
      this.currentAction = 'Submission completed';
      this.lastSuccessfulAction = 'CHEFS submission completed';

      await this.log('SUCCESS_STATE_DETECTED', {
        attempt: attempt || this.progress.submitAttempts || 0,
        phase: phase || 'unknown',
        confirmationId,
        detectedBy: success && success.detectedBy ? success.detectedBy : [],
        successUrl: location.href
      });
      await this.log('CONFIRMATION_DETECTED', { confirmationId });
      await this.log('SUBMIT_SUCCEEDED', {
        attempt: attempt || this.progress.submitAttempts || 0,
        confirmationId,
        phase: phase || 'unknown'
      });

      await this.updateRun({
        status: 'submitted',
        statusLabel: 'Submitted',
        currentAction: this.currentAction,
        lastSuccessfulAction: this.lastSuccessfulAction,
        confirmationId,
        message: '',
        failure: null,
        endedAt: new Date().toISOString(),
        progress: Object.assign({}, this.progress)
      });

      this.running = false;
      if (this.mutationObserver) {
        this.mutationObserver.disconnect();
      }

      try {
        const finalScan = await this.scanComponents();
        await this.setSnapshot('final', finalScan.snapshot || []);
      } catch (error) {
        await this.log('FINAL_SUCCESS_SNAPSHOT_FAILED', { message: error.message || String(error) });
      }
      await this.checkpoint('Submission success detected', {
        confirmationId,
        successUrl: location.href,
        detectedBy: success && success.detectedBy ? success.detectedBy : []
      });
      await this.runtimeMessage({
        type: 'RUN_FINALIZED',
        runId: this.runId,
        finalizedAt: new Date().toISOString()
      });
      return true;
    }

    async finalizeIfSubmitted(attempt, phase) {
      const confirmation = this.confirmationState();
      if (!confirmation.confirmed) {
        return false;
      }
      await this.finalizeSubmitted(confirmation, attempt, phase);
      return true;
    }

    async clickModalConfirmationIfPresent() {
      const dialogs = Array.from(document.querySelectorAll('[role="dialog"], .modal.show, .v-dialog'))
        .filter((dialog) => isVisible(dialog));
      for (const dialog of dialogs) {
        const button = Array.from(dialog.querySelectorAll('button'))
          .find((candidate) => isVisible(candidate) && /confirm|submit|yes,?\s+submit/i.test(cleanText(candidate.textContent)) && !/cancel|close/i.test(cleanText(candidate.textContent)));
        if (button) {
          button.click();
          await this.log('SUBMIT_CONFIRMATION_CLICKED', { label: cleanText(button.textContent) });
          return true;
        }
      }
      return false;
    }

    async waitForSubmitOutcome() {
      const started = Date.now();
      let lastErrorSignature = '';
      while (Date.now() - started < CONFIG.submitOutcomeTimeoutMs) {
        if (this.stopRequested) {
          return { type: 'stopped' };
        }
        const confirmation = this.confirmationState();
        if (confirmation.confirmed) {
          return {
            type: 'confirmed',
            confirmationId: confirmation.confirmationId,
            detectedBy: confirmation.detectedBy || []
          };
        }
        await this.clickModalConfirmationIfPresent();
        const errors = this.collectValidationErrors();
        const signature = errors.map((error) => `${error.key}:${error.message}`).join('|');
        if (errors.length && signature && signature === lastErrorSignature) {
          return { type: 'validation', errors };
        }
        if (errors.length) {
          lastErrorSignature = signature;
        }
        await delay(500);
      }
      const errors = this.collectValidationErrors();
      if (errors.length) {
        return { type: 'validation', errors };
      }
      return { type: 'timeout' };
    }

    async submitWithRecovery() {
      if (await this.finalizeIfSubmitted(this.progress.submitAttempts, 'before-submit-recovery')) {
        return;
      }

      for (let attempt = 1; attempt <= CONFIG.maxSubmitAttempts; attempt += 1) {
        if (this.stopRequested) {
          await this.finishStopped();
          return;
        }
        if (await this.finalizeIfSubmitted(attempt, 'submit-attempt-start')) {
          return;
        }

        this.progress.submitAttempts = attempt;
        await this.setStatus('validating', 'Validating', 'Checking form before submission');
        let bridgeValidity = null;
        try {
          bridgeValidity = await this.bridgeCommand('CHECK_VALIDITY', {}, CONFIG.bridgeTimeoutMs);
        } catch (error) {
          await this.log('FORMIO_VALIDATION_API_FAILED', { message: error.message });
        }
        if (await this.finalizeIfSubmitted(attempt, 'after-formio-validation')) {
          return;
        }
        await this.log('VALIDATION_CHECKED', {
          formioValid: bridgeValidity ? bridgeValidity.valid : null,
          formioErrors: bridgeValidity && bridgeValidity.errors ? bridgeValidity.errors : []
        });

        const bridgeErrors = bridgeValidity && Array.isArray(bridgeValidity.errors)
          ? bridgeValidity.errors.map((error) => ({
            time: new Date().toISOString(),
            pass: this.currentPass,
            key: error && error.key ? error.key : '',
            type: error && error.type ? error.type : '',
            message: cleanText(error && (error.message || error.error || error.text) ? (error.message || error.error || error.text) : ''),
            componentId: error && error.componentId ? error.componentId : ''
          })).filter((error) => error.message)
          : [];
        const domErrors = this.collectValidationErrors();
        const preSubmitErrors = [];
        const preSubmitErrorSignatures = new Set();
        for (const error of bridgeErrors.concat(domErrors)) {
          const signature = `${error.key || ''}|${error.message || ''}`;
          if (!preSubmitErrorSignatures.has(signature)) {
            preSubmitErrorSignatures.add(signature);
            preSubmitErrors.push(error);
          }
        }
        if (preSubmitErrors.length) {
          await this.runtimeMessage({
            type: 'ADD_VALIDATION_ERRORS',
            runId: this.runId,
            errors: preSubmitErrors
          });
          for (const error of preSubmitErrors) {
            await this.log('VALIDATION_ERROR', Object.assign({ phase: 'pre-submit' }, error));
          }
          await this.log('PRE_SUBMIT_VALIDATION_REPAIR_STARTED', {
            errorCount: preSubmitErrors.length,
            formioValid: bridgeValidity ? bridgeValidity.valid : null
          });
          await this.setStatus('filling', 'Repairing', 'Repairing fields rejected before submission');
          await this.prepareValidationRepair(preSubmitErrors);
          await this.fillUntilStable();
          if (!this.running) {
            return;
          }
          continue;
        }

        const preSubmit = await this.scanComponents();
        if (await this.finalizeIfSubmitted(attempt, 'after-pre-submit-scan')) {
          return;
        }
        await this.setSnapshot('lastKnown', preSubmit.snapshot);
        await this.checkpoint('Pre-submit checkpoint', { submitAttempt: attempt });

        const button = await this.ensureSubmitButtonVisible();
        if (!button) {
          if (await this.finalizeIfSubmitted(attempt, 'submit-button-missing')) {
            return;
          }
          await this.failRun('blocked', 'Blocked', new Error('A visible Form.io submit button could not be found.'), {
            reason: 'Submit button unavailable'
          });
          return;
        }
        this.currentAction = `Submitting attempt ${attempt}`;
        await this.setStatus('submitting', 'Submitting', this.currentAction);
        await this.log('SUBMIT_ATTEMPT', {
          attempt,
          label: cleanText(button.textContent),
          buttonName: button.name || '',
          buttonType: button.type || ''
        });
        button.click();
        const outcome = await this.waitForSubmitOutcome();
        if (outcome.type === 'confirmed') {
          await this.finalizeSubmitted({
            confirmed: true,
            confirmationId: outcome.confirmationId || null,
            detectedBy: outcome.detectedBy || ['submit-outcome'],
            url: location.href
          }, attempt, 'submit-outcome');
          return;
        }
        if (outcome.type === 'validation') {
          if (await this.finalizeIfSubmitted(attempt, 'before-validation-repair')) {
            return;
          }
          await this.runtimeMessage({
            type: 'ADD_VALIDATION_ERRORS',
            runId: this.runId,
            errors: outcome.errors
          });
          for (const error of outcome.errors) {
            await this.log('VALIDATION_ERROR', error);
          }
          this.lastProgressAt = Date.now();
          await this.setStatus('filling', 'Repairing', 'Repairing fields rejected by validation');
          await this.prepareValidationRepair(outcome.errors);
          await this.fillUntilStable();
          if (!this.running) {
            return;
          }
          continue;
        }
        if (outcome.type === 'stopped') {
          await this.finishStopped();
          return;
        }
        if (await this.finalizeIfSubmitted(attempt, 'submit-outcome-timeout')) {
          return;
        }
        await this.failRun('stalled', 'Stalled', new Error('Submission produced neither confirmation nor actionable validation feedback.'), {
          reason: 'Submission outcome timeout',
          submitAttempt: attempt
        });
        return;
      }
      if (await this.finalizeIfSubmitted(this.progress.submitAttempts, 'maximum-submit-attempts')) {
        return;
      }
      this.currentAction = 'Submission blocked by unresolved validation errors';
      await this.failRun('blocked', 'Blocked', new Error('The form did not submit after the maximum number of validation-repair attempts.'), {
        reason: 'Maximum submission attempts reached'
      });
    }

    async failRun(status, statusLabel, error, details) {
      if (this.runId && await this.finalizeIfSubmitted(this.progress.submitAttempts, 'failure-guard')) {
        return;
      }
      if (!this.runId) {
        this.running = false;
        return;
      }
      const scan = await this.scanComponents().catch(() => ({ snapshot: [], fillable: [] }));
      await this.setSnapshot('lastKnown', scan.snapshot || []);
      await this.setSnapshot('final', scan.snapshot || []);
      const validationErrors = this.collectValidationErrors();
      if (validationErrors.length) {
        await this.runtimeMessage({ type: 'ADD_VALIDATION_ERRORS', runId: this.runId, errors: validationErrors });
      }
      const failure = Object.assign({
        time: new Date().toISOString(),
        message: error && error.message ? error.message : String(error),
        stack: error && error.stack ? error.stack : '',
        currentAction: this.currentAction,
        lastSuccessfulAction: this.lastSuccessfulAction,
        pass: this.currentPass,
        scrollX: window.scrollX,
        scrollY: window.scrollY,
        url: location.href,
        visibleValidationErrors: validationErrors,
        unresolvedFields: (scan.fillable || [])
          .filter((item) => item.empty || item.invalid)
          .map((item) => ({ key: item.key, label: item.label, type: item.type, empty: item.empty, invalid: item.invalid }))
      }, details || {});
      await this.log(status === 'stalled' ? 'STALL_DETECTED' : 'RUN_FAILED', failure);
      await this.runtimeMessage({
        type: 'SET_FAILURE',
        runId: this.runId,
        status,
        statusLabel,
        failure
      });
      await this.updateRun({
        status,
        statusLabel,
        currentAction: this.currentAction,
        endedAt: new Date().toISOString(),
        progress: Object.assign({}, this.progress)
      });
      await this.checkpoint('Run failure finalized', {
        status,
        statusLabel,
        reason: failure.reason || failure.message
      });
      await this.runtimeMessage({
        type: 'RUN_FINALIZED',
        runId: this.runId,
        finalizedAt: new Date().toISOString()
      });
      this.running = false;
      if (this.mutationObserver) {
        this.mutationObserver.disconnect();
      }
    }

    async finishStopped() {
      const scan = await this.scanComponents().catch(() => ({ snapshot: [] }));
      await this.setSnapshot('lastKnown', scan.snapshot || []);
      await this.setSnapshot('final', scan.snapshot || []);
      await this.log('RUN_STOPPED', { currentAction: this.currentAction });
      await this.updateRun({
        status: 'stopped',
        statusLabel: 'Stopped',
        currentAction: 'Stopped by user',
        endedAt: new Date().toISOString(),
        progress: Object.assign({}, this.progress)
      });
      await this.checkpoint('Stopped run finalized', {
        status: 'stopped',
        reason: 'Stopped by user'
      });
      await this.runtimeMessage({
        type: 'RUN_FINALIZED',
        runId: this.runId,
        finalizedAt: new Date().toISOString()
      });
      this.running = false;
      if (this.mutationObserver) {
        this.mutationObserver.disconnect();
      }
    }

    stop() {
      this.stopRequested = true;
      return { ok: true };
    }
  }

  const controller = new ChefsTesterController();
  window.__CHEFS_TESTER_CONTENT_CONTROLLER__ = controller;

  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === 'CHEFS_TESTER_START') {
      controller.start(message)
        .then(sendResponse)
        .catch((error) => sendResponse({ ok: false, error: error.message || String(error) }));
      return true;
    }
    if (message.type === 'CHEFS_TESTER_STOP') {
      sendResponse(controller.stop());
      return false;
    }
    if (message.type === 'CHEFS_TESTER_STATUS') {
      sendResponse({
        ok: true,
        running: controller.running,
        runId: controller.runId,
        currentAction: controller.currentAction,
        progress: controller.progress
      });
      return false;
    }
    return false;
  });
})();
