'use strict';

(function exposeDashboardModel(globalScope) {
  const SCHEMA_VERSION = 1;
  const HISTORY_LIMIT = 200;
  const HISTORY_MAX_AGE_MS = 90 * 24 * 60 * 60 * 1000;
  const RESULT_VALUES = new Set([
    'submitted', 'completed', 'failed', 'stalled', 'blocked', 'safety_stop', 'stopped'
  ]);
  const FAILURE_VALUES = new Set([
    'none', 'submission', 'validation', 'watchdog', 'safety', 'user_stop',
    'blocked', 'runtime'
  ]);
  const STRATEGY_VALUES = new Set([
    'input', 'textarea', 'checkbox', 'radio', 'select', 'choices-select',
    'button', 'formio-set-value', 'formio-file-upload', 'simple-day',
    'contenteditable', 'unknown'
  ]);
  const COMPONENT_TYPES = new Set([
    'textfield', 'simpletextfield', 'textarea', 'simpletextarea', 'number',
    'simplenumber', 'email', 'simpleemail', 'phoneNumber', 'simplephonenumber',
    'checkbox', 'simplecheckbox', 'selectboxes', 'simpleselectboxes', 'radio',
    'simpleradio', 'select', 'simpleselect', 'simpleselectadvanced', 'datetime',
    'simpledatetime', 'day', 'simpleday', 'file', 'simplefile', 'datagrid',
    'editgrid', 'tabs', 'button', 'unknown'
  ]);

  function finiteNumber(value, fallback) {
    const number = Number(value);
    return Number.isFinite(number) ? number : (fallback === undefined ? 0 : fallback);
  }

  function boundedInteger(value, maximum) {
    return Math.max(0, Math.min(maximum || Number.MAX_SAFE_INTEGER, Math.round(finiteNumber(value, 0))));
  }

  function safeIso(value) {
    const time = Date.parse(String(value || ''));
    return Number.isFinite(time) ? new Date(time).toISOString() : null;
  }

  function safeResult(value) {
    const result = String(value || '').toLowerCase();
    return RESULT_VALUES.has(result) ? result : 'failed';
  }

  function opaqueRunRef(value) {
    const text = String(value || '');
    if (!text) return 'run-unknown';
    let hash = 2166136261;
    for (let index = 0; index < text.length; index += 1) {
      hash ^= text.charCodeAt(index);
      hash = Math.imul(hash, 16777619);
    }
    return `run-${(hash >>> 0).toString(16).padStart(8, '0')}`;
  }

  function opaqueFormRef(urlText) {
    try {
      const url = new URL(String(urlText || ''));
      const value = String(url.searchParams.get('f') || '');
      const match = value.match(/^[0-9a-f]{8}-[0-9a-f-]{27,}$/i);
      return match ? `form-${match[0].slice(0, 8).toLowerCase()}` : 'form-unknown';
    } catch (error) {
      return 'form-unknown';
    }
  }

  function safeVersion(value) {
    const clean = String(value || '').match(/^\d+\.\d+\.\d+$/);
    return clean ? clean[0] : '';
  }

  function safeBuild(value) {
    const clean = String(value || '').match(/^\d{4}\.\d{2}\.\d{2}\.\d+$/);
    return clean ? clean[0] : '';
  }

  function safeStrategy(value) {
    const strategy = String(value || '').toLowerCase();
    return STRATEGY_VALUES.has(strategy) ? strategy : 'unknown';
  }

  function safeComponentType(value) {
    const type = String(value || '');
    return COMPONENT_TYPES.has(type) ? type : 'unknown';
  }

  function failureCategory(run) {
    const status = safeResult(run && run.status);
    if (status === 'submitted' || status === 'completed') {
      return 'none';
    }
    const reason = String(run && run.failure && run.failure.reason || '').toLowerCase();
    if (reason.includes('submit')) return 'submission';
    if (reason.includes('validation')) return 'validation';
    if (reason.includes('watchdog') || status === 'stalled') return 'watchdog';
    if (reason.includes('payment') || status === 'safety_stop') return 'safety';
    if (status === 'stopped') return 'user_stop';
    if (status === 'blocked') return 'blocked';
    return 'runtime';
  }

  function durationMs(run) {
    const start = Date.parse(String(run && run.startedAt || ''));
    const end = Date.parse(String(
      run && (run.endedAt || run.finalizedAt || run.updatedAt) || ''
    ));
    return Number.isFinite(start) && Number.isFinite(end)
      ? Math.max(0, Math.min(24 * 60 * 60 * 1000, end - start))
      : 0;
  }

  function buildPassSeries(run) {
    const byPass = new Map();
    for (const checkpoint of Array.isArray(run && run.checkpoints) ? run.checkpoints : []) {
      const pass = boundedInteger(checkpoint && checkpoint.pass, 500);
      if (!pass || !checkpoint || checkpoint.reason !== 'Fill pass completed') {
        continue;
      }
      const progress = checkpoint.progress || {};
      byPass.set(pass, {
        pass,
        filled: boundedInteger(progress.filled, 100000),
        remaining: boundedInteger(progress.remaining, 100000),
        actions: boundedInteger(checkpoint.actions, 100000),
        elapsedMs: Math.max(0, Date.parse(String(checkpoint.time || '')) -
          Date.parse(String(run.startedAt || '')))
      });
    }
    return Array.from(byPass.values()).sort((left, right) => left.pass - right.pass).slice(0, 500);
  }

  function buildStrategyStats(run) {
    const pending = new Map();
    const stats = new Map();
    const events = Array.isArray(run && run.events) ? run.events : [];
    for (const event of events) {
      const eventType = String(event && event.event || '');
      const strategy = safeStrategy(event && event.strategy);
      const identity = `${String(event && event.componentId || '')}:${boundedInteger(event && event.attempt, 100)}`;
      if (eventType === 'FILL_ATTEMPT') {
        pending.set(identity, {
          strategy,
          time: Date.parse(String(event.time || ''))
        });
        if (!stats.has(strategy)) {
          stats.set(strategy, { strategy, attempts: 0, successes: 0, failures: 0, latencyMs: [] });
        }
        stats.get(strategy).attempts += 1;
      } else if (eventType === 'FILL_SUCCEEDED' || eventType === 'FILL_FAILED') {
        const attempt = pending.get(identity);
        const resolvedStrategy = attempt ? attempt.strategy : strategy;
        if (!stats.has(resolvedStrategy)) {
          stats.set(resolvedStrategy, {
            strategy: resolvedStrategy,
            attempts: 0,
            successes: 0,
            failures: 0,
            latencyMs: []
          });
        }
        const item = stats.get(resolvedStrategy);
        if (eventType === 'FILL_SUCCEEDED') item.successes += 1;
        if (eventType === 'FILL_FAILED') item.failures += 1;
        const end = Date.parse(String(event.time || ''));
        if (attempt && Number.isFinite(attempt.time) && Number.isFinite(end)) {
          item.latencyMs.push(Math.max(0, Math.min(10 * 60 * 1000, end - attempt.time)));
          item.latencyMs = item.latencyMs.slice(-200);
        }
        pending.delete(identity);
      }
    }
    return Array.from(stats.values()).sort((left, right) => right.attempts - left.attempts).slice(0, 20);
  }

  function buildComponentStats(run) {
    const snapshot = run && run.snapshots &&
      (run.snapshots.final || run.snapshots.lastKnown || run.snapshots.initial);
    const statuses = {};
    const types = {};
    for (const component of Array.isArray(snapshot) ? snapshot : []) {
      const status = String(component && component.status || 'unknown').toLowerCase();
      const safeStatus = [
        'filled', 'protected', 'failed', 'unsupported', 'empty', 'pending', 'unknown'
      ].includes(status) ? status : 'unknown';
      const type = safeComponentType(component && component.componentType);
      statuses[safeStatus] = boundedInteger(statuses[safeStatus] || 0, 100000) + 1;
      types[type] = boundedInteger(types[type] || 0, 100000) + 1;
    }
    return { statuses, types };
  }

  function buildPhaseDurations(run) {
    const start = Date.parse(String(run && run.startedAt || ''));
    const end = start + durationMs(run);
    const events = (Array.isArray(run && run.events) ? run.events : [])
      .map((event) => ({
        event: String(event && event.event || ''),
        time: Date.parse(String(event && event.time || ''))
      }))
      .filter((event) => Number.isFinite(event.time))
      .sort((left, right) => left.time - right.time);
    const firstTime = (names) => {
      const found = events.find((event) => names.includes(event.event));
      return found ? found.time : null;
    };
    const scan = firstTime(['INITIAL_SCAN_COMPLETED']);
    const validation = firstTime(['VALIDATION_CHECKED', 'SUBMIT_ATTEMPT']);
    const submission = firstTime(['SUBMIT_ATTEMPT', 'SUBMIT_CLICKED']);
    const boundaries = [
      { key: 'initialization', start, end: scan || validation || submission || end },
      { key: 'filling', start: scan || start, end: validation || submission || end },
      { key: 'validation', start: validation || submission || end, end: submission || end },
      { key: 'submission', start: submission || end, end }
    ];
    return Object.fromEntries(boundaries.map((phase) => [
      phase.key,
      Math.max(0, Math.min(durationMs(run), finiteNumber(phase.end) - finiteNumber(phase.start)))
    ]));
  }

  function buildRunSummary(run, context) {
    const progress = run && run.progress || {};
    const componentStats = buildComponentStats(run);
    const result = safeResult(run && run.status);
    const summary = {
      schemaVersion: SCHEMA_VERSION,
      runRef: opaqueRunRef(run && run.runId),
      formRef: opaqueFormRef(run && run.formUrl),
      extensionVersion: safeVersion(run && run.extensionVersion),
      buildNumber: safeBuild(run && run.buildNumber),
      result,
      failureCategory: failureCategory(run),
      startedAt: safeIso(run && run.startedAt),
      endedAt: safeIso(run && (run.endedAt || run.finalizedAt || run.updatedAt)),
      durationMs: durationMs(run),
      confirmationCaptured: Boolean(run && run.confirmationId),
      metrics: {
        passes: boundedInteger(progress.pass, 10000),
        discovered: boundedInteger(progress.discovered, 100000),
        filled: boundedInteger(progress.filled, 100000),
        remaining: boundedInteger(progress.remaining, 100000),
        failed: boundedInteger(progress.failed, 100000),
        unsupported: boundedInteger(progress.unsupported, 100000),
        rowsAdded: boundedInteger(progress.rowsAdded, 100000),
        attachmentsCompleted: boundedInteger(progress.attachmentsCompleted, 100000),
        attachmentsPending: boundedInteger(progress.attachmentsPending, 100000),
        submitAttempts: boundedInteger(progress.submitAttempts, 1000),
        validationErrors: boundedInteger(
          Array.isArray(run && run.validationErrors) ? run.validationErrors.length : 0,
          100000
        )
      },
      phases: buildPhaseDurations(run),
      passSeries: buildPassSeries(run),
      strategies: buildStrategyStats(run),
      componentOutcomes: componentStats.statuses,
      componentTypes: componentStats.types,
      issueCounts: {
        fieldFailures: boundedInteger(progress.failed, 100000),
        unsupported: boundedInteger(progress.unsupported, 100000),
        validation: boundedInteger(
          Array.isArray(run && run.validationErrors) ? run.validationErrors.length : 0,
          100000
        ),
        screenshotFailure: (Array.isArray(run && run.events) ? run.events : [])
          .some((event) => event && event.event === 'SCREENSHOT_CAPTURE_FAILED') ? 1 : 0
      },
      batch: context && context.suiteId ? {
        suiteRef: opaqueRunRef(context.suiteId),
        index: (String(context.index || '').match(/^\d{1,6}/) || [''])[0]
      } : null
    };
    return JSON.parse(JSON.stringify(summary));
  }

  function hasOnlyKeys(value, allowed) {
    return Boolean(value) && typeof value === 'object' && !Array.isArray(value) &&
      Object.keys(value).every((key) => allowed.has(key));
  }

  function hasExactKeys(value, allowed) {
    return hasOnlyKeys(value, allowed) && Object.keys(value).length === allowed.size;
  }

  function numericObject(value, allowed) {
    return hasExactKeys(value, allowed) &&
      Object.values(value).every((item) => Number.isFinite(Number(item)));
  }

  function isDashboardSummary(record) {
    const topLevel = new Set([
      'schemaVersion', 'runRef', 'formRef', 'extensionVersion', 'buildNumber',
      'result', 'failureCategory', 'startedAt', 'endedAt', 'durationMs',
      'confirmationCaptured', 'metrics', 'phases', 'passSeries', 'strategies',
      'componentOutcomes', 'componentTypes', 'issueCounts', 'batch'
    ]);
    const metricKeys = new Set([
      'passes', 'discovered', 'filled', 'remaining', 'failed', 'unsupported',
      'rowsAdded', 'attachmentsCompleted', 'attachmentsPending', 'submitAttempts',
      'validationErrors'
    ]);
    const phaseKeys = new Set(['initialization', 'filling', 'validation', 'submission']);
    const passKeys = new Set(['pass', 'filled', 'remaining', 'actions', 'elapsedMs']);
    const strategyKeys = new Set([
      'strategy', 'attempts', 'successes', 'failures', 'latencyMs'
    ]);
    const outcomeKeys = new Set([
      'filled', 'protected', 'failed', 'unsupported', 'empty', 'pending', 'unknown'
    ]);
    const issueKeys = new Set([
      'fieldFailures', 'unsupported', 'validation', 'screenshotFailure'
    ]);
    if (
      !hasExactKeys(record, topLevel) ||
      record.schemaVersion !== SCHEMA_VERSION ||
      !/^run-[0-9a-f]{8}$/.test(String(record.runRef || '')) ||
      !/^form-(?:[0-9a-f]{8}|unknown)$/.test(String(record.formRef || '')) ||
      !RESULT_VALUES.has(String(record.result || '')) ||
      !FAILURE_VALUES.has(String(record.failureCategory || '')) ||
      !/^(?:|\d+\.\d+\.\d+)$/.test(String(record.extensionVersion || '')) ||
      !/^(?:|\d{4}\.\d{2}\.\d{2}\.\d+)$/.test(String(record.buildNumber || '')) ||
      !Number.isFinite(Date.parse(String(record.startedAt || ''))) ||
      !Number.isFinite(Date.parse(String(record.endedAt || ''))) ||
      !Number.isFinite(Number(record.durationMs)) ||
      typeof record.confirmationCaptured !== 'boolean' ||
      !numericObject(record.metrics, metricKeys) ||
      !numericObject(record.phases, phaseKeys) ||
      !numericObject(record.issueCounts, issueKeys) ||
      !hasOnlyKeys(record.componentOutcomes, outcomeKeys) ||
      !Object.values(record.componentOutcomes).every((item) => Number.isFinite(Number(item))) ||
      !hasOnlyKeys(record.componentTypes, COMPONENT_TYPES) ||
      !Object.values(record.componentTypes).every((item) => Number.isFinite(Number(item))) ||
      !Array.isArray(record.passSeries) ||
      !Array.isArray(record.strategies)
    ) {
      return false;
    }
    if (!record.passSeries.every((item) => numericObject(item, passKeys))) return false;
    if (!record.strategies.every((item) =>
      hasExactKeys(item, strategyKeys) &&
      STRATEGY_VALUES.has(String(item.strategy || '')) &&
      ['attempts', 'successes', 'failures'].every((key) => Number.isFinite(Number(item[key]))) &&
      Array.isArray(item.latencyMs) &&
      item.latencyMs.every((value) => Number.isFinite(Number(value)))
    )) return false;
    if (record.batch !== null && !(
      hasExactKeys(record.batch, new Set(['suiteRef', 'index'])) &&
      /^run-[0-9a-f]{8}$/.test(String(record.batch.suiteRef || '')) &&
      /^\d{0,6}$/.test(String(record.batch.index || ''))
    )) return false;
    return pidForbiddenKeys(record, 'dashboard', []).length === 0;
  }

  function trimHistory(records, nowValue) {
    const now = Number.isFinite(Number(nowValue)) ? Number(nowValue) : Date.now();
    const minimum = now - HISTORY_MAX_AGE_MS;
    const unique = new Map();
    for (const record of Array.isArray(records) ? records : []) {
      if (!isDashboardSummary(record)) continue;
      const ended = Date.parse(String(record.endedAt || ''));
      if (!Number.isFinite(ended) || ended < minimum || ended > now + 60000) continue;
      unique.set(record.runRef, record);
    }
    return Array.from(unique.values())
      .sort((left, right) => Date.parse(left.endedAt) - Date.parse(right.endedAt))
      .slice(-HISTORY_LIMIT);
  }

  function pidForbiddenKeys(value, path, findings) {
    const forbidden = /(^|_)(value|label|name|email|address|filename|content|screenshot|stack|url|confirmationid|useragent|events|checkpoints|snapshot)(_|$)/i;
    if (Array.isArray(value)) {
      value.forEach((item, index) => pidForbiddenKeys(item, `${path}[${index}]`, findings));
      return findings;
    }
    if (!value || typeof value !== 'object') return findings;
    for (const [key, child] of Object.entries(value)) {
      if (forbidden.test(key)) findings.push(`${path}.${key}`);
      pidForbiddenKeys(child, `${path}.${key}`, findings);
    }
    return findings;
  }

  globalScope.ChefsDashboardModel = Object.freeze({
    SCHEMA_VERSION,
    HISTORY_LIMIT,
    HISTORY_MAX_AGE_MS,
    buildRunSummary,
    isDashboardSummary,
    trimHistory,
    pidForbiddenKeys: (value) => pidForbiddenKeys(value, 'dashboard', [])
  });
})(globalThis);
