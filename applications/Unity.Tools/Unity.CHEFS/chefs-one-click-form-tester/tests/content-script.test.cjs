'use strict';

const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const { test } = require('node:test');
const { runInNewContext } = require('node:vm');
const { webcrypto } = require('node:crypto');

const source = readFileSync(join(__dirname, '..', 'content-script.js'), 'utf8');

class SyntheticEvent {
  constructor(type, options) {
    this.type = type;
    Object.assign(this, options);
  }
}

function controlClass() {
  return class {
    constructor() {
      this.events = [];
      this.currentValue = '';
    }
    get value() { return this.currentValue; }
    set value(value) { this.currentValue = value; }
    focus() {}
    dispatchEvent(event) {
      this.events.push(event);
      return true;
    }
  };
}

function loadController({ fastTimers = false } = {}) {
  const controls = { input: controlClass(), textarea: controlClass(), select: controlClass() };
  const document = {
    body: { textContent: '' },
    getElementById: () => null,
    querySelectorAll: () => [],
    querySelector: () => null
  };
  const window = { addEventListener() {} };
  runInNewContext(source, {
    document,
    window,
    chrome: { runtime: { onMessage: { addListener() {} }, sendMessage: async () => ({ ok: true }) } },
    crypto: webcrypto,
    location: new URL('https://chefs.example.test/form/123'),
    URL,
    TextEncoder,
    btoa,
    setTimeout: fastTimers ? (callback, ms) => {
      if (ms < 10000) {
        queueMicrotask(callback);
      }
      return 0;
    } : setTimeout,
    clearTimeout,
    Event: SyntheticEvent,
    KeyboardEvent: SyntheticEvent,
    HTMLInputElement: controls.input,
    HTMLTextAreaElement: controls.textarea,
    HTMLSelectElement: controls.select,
    Option: class {
      constructor(text, value, defaultSelected, selected) {
        Object.assign(this, { text, value, defaultSelected, selected });
      }
    },
    getComputedStyle: () => ({ display: 'block', visibility: 'visible', opacity: '1' })
  });
  const controller = window.__CHEFS_TESTER_CONTENT_CONTROLLER__;
  controller.runId = 'ABC123';
  return { controller, document, window, controls };
}

function wrapper(id, visible = true) {
  return {
    id,
    isConnected: true,
    closest: () => null,
    getClientRects: () => visible ? [{}] : [],
    querySelector: () => null,
    querySelectorAll: () => [],
    classList: { contains: () => false },
    dataset: {}
  };
}

function descriptor(patch = {}) {
  return {
    id: 'contact::field1', key: 'contact', type: 'textfield',
    label: 'Contact', description: '', visible: true, enabled: true,
    protected: false, fillable: true, required: true, empty: true, invalid: false,
    meta: null, maskPlan: null, wrapper: wrapper('field1'),
    ...patch
  };
}

function plain(value) {
  return JSON.parse(JSON.stringify(value));
}

function workflowController() {
  const { controller } = loadController({ fastTimers: true });
  const events = [];
  const messages = [];
  const checkpoints = [];
  controller.running = true;
  controller.lastProgressAt = Date.now();
  controller.log = async (name, data) => { events.push({ name, data }); };
  controller.runtimeMessage = async (message) => { messages.push(message); };
  controller.checkpoint = async (reason, data) => { checkpoints.push({ reason, data }); };
  return { controller, events, messages, checkpoints };
}

async function setupFill(plan) {
  const result = workflowController();
  const { controller, events } = result;
  const item = descriptor();
  await controller.trackDescriptor(item);
  events.length = 0;
  controller.chooseStrategy = () => 'input';
  controller.resolveMaskPlan = () => plan;
  return { ...result, item, state: controller.componentStates.get(item.id) };
}

test('fillDescriptor preserves custom-mask success counters and event order', async () => {
  const plan = { source: 'custom-rule', mask: '999', rule: { id: 'r1', labelMatch: 'Contact', matchMode: 'exact', mask: '999' } };
  const { controller, item, state, events } = await setupFill(plan);
  controller.performFill = async () => ({ success: true, valueInfo: { generatedByExtension: true } });
  assert.equal(await controller.fillDescriptor(item), true);
  assert.equal(state.status, 'filled');
  assert.equal(state.attempts, 1);
  assert.equal(controller.progress.customRuleMatches, 1);
  assert.equal(controller.progress.customRuleAccepted, 1);
  assert.equal(controller.progress.filled, 1);
  assert.deepEqual(events.map((event) => event.name), ['CUSTOM_RULE_MATCHED', 'FILL_ATTEMPT', 'CUSTOM_RULE_VALUE_ACCEPTED', 'FILL_SUCCEEDED']);
});

test('fillDescriptor retries three times, reports a mask only once, then blocks', async () => {
  const plan = { source: 'custom-rule', mask: '999', rule: { id: 'r1', labelMatch: 'Contact', mask: '999' } };
  const { controller, item, state, events } = await setupFill(plan);
  controller.performFill = async () => ({ success: false, message: 'Rejected' });
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    assert.equal(await controller.fillDescriptor(item), false);
    assert.equal(state.status, attempt === 3 ? 'blocked' : 'retry');
  }
  assert.equal(await controller.fillDescriptor(item), false);
  assert.equal(state.attempts, 3);
  assert.equal(state.lastError, 'Rejected');
  assert.equal(controller.progress.customRuleMatches, 1);
  assert.equal(controller.progress.customRuleRejected, 3);
  assert.equal(controller.progress.maskValuesRejected, 3);
  assert.equal(events.filter((event) => event.name === 'FILL_ATTEMPT').length, 3);
  assert.equal(events.at(-1).name, 'FILL_REJECTED');
});

test('fillDescriptor distinguishes detected masks from custom rules', async () => {
  const { controller, item, events } = await setupFill({ source: 'runtime-inputmask', mask: '999' });
  controller.performFill = async () => ({ success: true });
  assert.equal(await controller.fillDescriptor(item), true);
  assert.equal(controller.progress.detectedMasksUsed, 1);
  assert.equal(controller.progress.customRuleAccepted, 0);
  assert.deepEqual(events.map((event) => event.name), ['MASK_RUNTIME_DETECTED', 'FILL_ATTEMPT', 'MASK_VALUE_PERSISTED', 'FILL_SUCCEEDED']);
});

test('fillDescriptor propagates cancellation without recording a rejection', async () => {
  const { controller, item, state, events } = await setupFill(null);
  const stopped = Object.assign(new Error('Stopped'), { code: 'CHEFS_TESTER_STOPPED' });
  controller.performFill = async () => { throw stopped; };
  await assert.rejects(controller.fillDescriptor(item), (error) => error === stopped);
  assert.equal(state.status, 'discovered');
  assert.deepEqual(events.map((event) => event.name), ['FILL_ATTEMPT']);
});

function setupPass(controller) {
  const scan = { fillable: [], snapshot: [] };
  controller.scanComponents = async () => scan;
  controller.finalizeIfSubmitted = async () => false;
  controller.handleLookupActions = async () => 0;
  controller.commitOpenEditGridRows = async () => 0;
  controller.activateNextUnvisitedTab = async () => false;
  controller.advanceWizard = async () => false;
  return scan;
}

test('fill pass rescans after grid progress without filling stale candidates', async () => {
  const { controller, events } = workflowController();
  setupPass(controller);
  controller.fillGridCandidates = async () => 1;
  controller.fillPassCandidates = async () => { assert.fail('Stale candidates must not be filled'); };
  assert.equal(await controller.runFillPass(1), 0);
  assert.equal(controller.lastPassHadProgress, true);
  assert.equal(controller.currentPass, 1);
  assert.equal(events.some((event) => event.name === 'PASS_COMPLETED'), false);
});

test('fill pass navigates tabs before wizard steps and resets stability', async () => {
  const { controller } = workflowController();
  setupPass(controller);
  controller.activateNextUnvisitedTab = async () => true;
  controller.advanceWizard = async () => { assert.fail('Wizard navigation must follow tabs'); };
  assert.equal(await controller.runFillPass(1), 0);
  assert.equal(controller.lastPassHadProgress, true);
});

test('fill pass preserves the DOM revision captured before its final scan', async () => {
  const { controller, events } = workflowController();
  const scan = setupPass(controller);
  let scans = 0;
  controller.scanComponents = async () => {
    scans += 1;
    if (scans === 2) {
      controller.domRevision += 1;
    }
    return scan;
  };
  assert.equal(await controller.runFillPass(1), 2);
  assert.equal(events.find((event) => event.name === 'PASS_COMPLETED').data.domChanged, false);
});

test('fillUntilStable stops after two unchanged passes', async () => {
  const { controller } = workflowController();
  setupPass(controller);
  controller.getTabEntries = () => [];
  await controller.fillUntilStable();
  assert.equal(controller.currentPass, 2);
  assert.equal(controller.lastPassHadProgress, false);
});

test('fill budget extends only with unresolved fields and progress', async () => {
  const { controller } = workflowController();
  controller.currentPass = 40;
  controller.passBudget = 40;
  controller.scanComponents = async () => ({ fillable: [descriptor()] });
  assert.equal(await controller.extendFillBudget(), false);
  controller.lastPassHadProgress = true;
  assert.equal(await controller.extendFillBudget(), true);
  assert.equal(controller.passBudget, 50);
  controller.currentPass = 200;
  controller.passBudget = 200;
  assert.equal(await controller.extendFillBudget(), false);
});

test('stall guard fails unresolved fields but refreshes progress for full forms', async () => {
  const { controller } = workflowController();
  let failures = 0;
  controller.failRun = async () => { failures += 1; };
  controller.lastProgressAt = 0;
  controller.scanComponents = async () => ({ fillable: [descriptor()] });
  await assert.rejects(controller.checkFillStall(), /RUN_ALREADY_FAILED/);
  assert.equal(failures, 1);
  controller.scanComponents = async () => ({ fillable: [] });
  await controller.checkFillStall();
  assert.ok(controller.lastProgressAt > 0);
});

test('Choices retries keyboard, text, then native select with short-circuiting', async () => {
  for (const successfulRetry of ['keyboard', 'text', 'native']) {
    const { controller } = workflowController();
    const item = descriptor();
    const calls = [];
    item.wrapper.querySelector = () => ({ focus() {} });
    controller.dispatchPointerSequence = () => {};
    controller.autocompleteOptions = () => [{ textContent: 'Choice', scrollIntoView() {}, click() {} }];
    controller.waitForChoiceValue = async () => null;
    for (const [name, method] of [['keyboard', 'retryChoiceByKeyboard'], ['text', 'retryChoiceByText'], ['native', 'retryChoiceByNativeSelect']]) {
      controller[method] = async () => {
        calls.push(name);
        return name === successfulRetry ? item.wrapper : null;
      };
    }
    const result = await controller.fillChoices(item, false);
    assert.equal(result.success, true);
    assert.deepEqual(calls, ['keyboard', 'text', 'native'].slice(0, ['keyboard', 'text', 'native'].indexOf(successfulRetry) + 1));
  }
});

test('native Choices fallback distinguishes empty and missing data values', async () => {
  const { controller } = workflowController();
  const events = [];
  const select = { options: [], add(option) { this.options.push(option); }, dispatchEvent(event) { events.push(event.type); } };
  const item = descriptor();
  item.wrapper.querySelector = () => select;
  controller.waitForChoiceValue = async () => item.wrapper;
  const choice = { wrapper: item.wrapper, text: 'Empty value', element: { dataset: {} } };
  assert.equal(await controller.retryChoiceByNativeSelect(item, choice), null);
  assert.equal(select.options.length, 0);
  choice.element.dataset.value = '';
  assert.equal(await controller.retryChoiceByNativeSelect(item, choice), item.wrapper);
  assert.equal(select.options[0].value, '');
  assert.deepEqual(events, ['input', 'change', 'blur']);
  select.options.length = 0;
  choice.element.dataset.value = '[object Object]';
  assert.equal(await controller.retryChoiceByNativeSelect(item, choice), null);
  assert.equal(select.options.length, 0);
});

test('OrgBook fallback accepts only a value that persists', async () => {
  const { controller, events } = workflowController();
  const item = descriptor({ type: 'orgbook' });
  controller.selectOrgbookResult = async () => ({ applied: true, resultCount: 1, lookup: { formRootCount: 0, inspectedComponentCount: 2 } });
  controller.waitForChoiceValue = async () => null;
  assert.equal(await controller.selectOrgbookValue(item), null);
  controller.waitForChoiceValue = async () => item.wrapper;
  assert.equal((await controller.selectOrgbookValue(item)).success, true);
  assert.equal(events.at(-1).data.formioRootCount, 0);
  assert.equal(events.at(-1).data.formioComponentCount, 2);
});

test('pre-submit errors preserve bridge priority and remove duplicates', () => {
  const { controller } = workflowController();
  controller.collectValidationErrors = () => [{ key: 'a', message: 'Required' }, { key: 'b', message: 'Invalid' }];
  const errors = controller.preSubmitErrors({ errors: [null, { key: 'a', error: '  Required  ', componentId: 'bridge' }] });
  assert.equal(errors.length, 2);
  assert.equal(errors[0].componentId, 'bridge');
  assert.equal(errors[0].message, 'Required');
  assert.equal(errors[1].key, 'b');
});

function setupSubmission(controller) {
  controller.finalizeIfSubmitted = async () => false;
  controller.bridgeCommand = async () => ({ valid: true, errors: [] });
  controller.collectValidationErrors = () => [];
  controller.scanComponents = async () => ({ snapshot: [] });
  controller.prepareValidationRepair = async () => {};
  controller.fillUntilStable = async () => {};
  controller.failRun = async () => { controller.running = false; };
}

test('pre-submit validation repairs never click submit and stop after three attempts', async () => {
  const { controller, events } = workflowController();
  setupSubmission(controller);
  let repairs = 0;
  controller.bridgeCommand = async () => ({ valid: false, errors: [{ key: 'a', message: 'Required' }] });
  controller.prepareValidationRepair = async () => { repairs += 1; };
  controller.ensureSubmitButtonVisible = async () => { assert.fail('Do not submit unresolved validation'); };
  await controller.submitWithRecovery();
  assert.equal(repairs, 3);
  assert.equal(controller.progress.submitAttempts, 3);
  assert.equal(controller.running, false);
  assert.equal(events.filter((event) => event.name === 'PRE_SUBMIT_VALIDATION_REPAIR_STARTED').length, 3);
});

test('submission validation retries then finalizes success exactly once', async () => {
  const { controller } = workflowController();
  setupSubmission(controller);
  let clicks = 0;
  let repairs = 0;
  let finalized = 0;
  controller.ensureSubmitButtonVisible = async () => ({ textContent: 'Submit', click() { clicks += 1; } });
  controller.waitForSubmitOutcome = async () => clicks === 1
    ? { type: 'validation', errors: [{ key: 'a', message: 'Required' }] }
    : { type: 'confirmed', confirmationId: '1234ABCD' };
  controller.prepareValidationRepair = async () => { repairs += 1; };
  controller.finalizeSubmitted = async (confirmation) => {
    assert.equal(confirmation.confirmationId, '1234ABCD');
    finalized += 1;
    controller.running = false;
  };
  await controller.submitWithRecovery();
  assert.equal(clicks, 2);
  assert.equal(repairs, 1);
  assert.equal(finalized, 1);
});

test('submission timeout and cancellation are terminal outcomes', async () => {
  for (const type of ['timeout', 'stopped']) {
    const { controller } = workflowController();
    setupSubmission(controller);
    let clicks = 0;
    let stopped = 0;
    let failed = 0;
    controller.ensureSubmitButtonVisible = async () => ({ textContent: 'Submit', click() { clicks += 1; } });
    controller.waitForSubmitOutcome = async () => ({ type });
    controller.finishStopped = async () => { stopped += 1; controller.running = false; };
    controller.failRun = async () => { failed += 1; controller.running = false; };
    await controller.submitWithRecovery();
    assert.equal(clicks, 1);
    assert.equal(stopped, type === 'stopped' ? 1 : 0);
    assert.equal(failed, type === 'timeout' ? 1 : 0);
  }
});

const attachment = { chosen: { filename: 'test.pdf', mimeType: 'application/pdf' }, bytes: new Uint8Array([1]), hash: 'test-hash', file: { name: 'test.pdf' } };

test('pending uploads do not invoke the API fallback or clear in-flight markers', async () => {
  const { controller, events } = workflowController();
  const item = descriptor({ type: 'file' });
  controller.fileUploadsInFlight.set(item.id, { filename: 'test.pdf' });
  controller.loadAttachment = async () => attachment;
  controller.inspectEmpty = () => true;
  controller.uploadFileByDomDrop = async () => { throw new Error('The DOM drop is still pending'); };
  controller.bridgeCommand = async () => { assert.fail('Pending upload must not be duplicated'); };
  const result = await controller.fillFile(item);
  assert.equal(result.success, false);
  assert.equal(controller.fileUploadsInFlight.has(item.id), true);
  assert.equal(controller.progress.attachmentsPending, 0);
  assert.equal(events.at(-1).name, 'UPLOAD_PENDING_TIMEOUT');
});

test('completed file uploads record hashes and counters once', async () => {
  const { controller, messages } = workflowController();
  const item = descriptor({ type: 'file' });
  controller.inspectEmpty = () => true;
  controller.loadAttachment = async () => attachment;
  controller.uploadFileByDomDrop = async () => ({ hasValue: true, valueCount: 1, method: 'dom-drop' });
  controller.waitForStoredFile = async () => {};
  const result = await controller.fillFile(item);
  assert.equal(result.success, true);
  assert.equal(controller.progress.attachmentsPending, 0);
  assert.equal(controller.progress.attachmentsCompleted, 1);
  assert.equal(controller.fileHandled.has(item.id), true);
  const record = messages.find((message) => message.type === 'ADD_ATTACHMENT_RECORD').attachment;
  assert.equal(record.sha256, 'test-hash');
  assert.equal(record.method, 'dom-drop');
  assert.equal(record.outcome, 'completed');
});

test('API upload failures retain both errors and propagate cancellation', async () => {
  const { controller } = workflowController();
  const item = descriptor({ type: 'file' });
  controller.uploadFileByDomDrop = async () => { throw new Error('DOM failed'); };
  controller.bytesToBase64 = () => 'AQ==';
  controller.bridgeCommand = async () => { throw new Error('API failed'); };
  const result = await controller.uploadFileWithFallback(item, attachment, item.wrapper);
  assert.equal(result.result, null);
  assert.match(result.bridgeError.message, /DOM failed.*API failed/);
  const stopped = Object.assign(new Error('Stopped'), { code: 'CHEFS_TESTER_STOPPED' });
  controller.bridgeCommand = async () => { throw stopped; };
  await assert.rejects(controller.uploadFileWithFallback(item, attachment, item.wrapper), (error) => error === stopped);
});

test('DOM upload completion requires pending indicators to clear', () => {
  const { controller } = workflowController();
  const item = descriptor({ type: 'file' });
  controller.uploadedFileRows = () => [{}];
  controller.fileUploadPending = () => true;
  assert.equal(controller.domUploadState(item.wrapper, attachment.file, 0).completed, false);
  controller.fileUploadPending = () => false;
  assert.equal(controller.domUploadState(item.wrapper, attachment.file, 0).completed, true);
});

test('grid diagnostics preserve open editors versus unsupported empty grids', async () => {
  const { controller, events } = workflowController();
  const item = descriptor({ type: 'editgrid' });
  controller.editGridSaveButtons = () => [{}];
  await controller.recordGridUnavailable(item, item.wrapper, 1, 2);
  assert.equal(events.at(-1).name, 'GRID_ROW_EDITOR_PENDING');
  assert.equal(controller.gridHandled.has(item.id), false);
  await controller.trackDescriptor(item);
  await controller.recordGridUnavailable(item, item.wrapper, 0, 2);
  assert.equal(events.at(-1).name, 'COMPONENT_UNSUPPORTED');
  assert.equal(controller.componentStates.get(item.id).status, 'unsupported');
  assert.equal(controller.gridHandled.has(item.id), true);
});

test('grid additions stop at the target and increment only added rows', async () => {
  const { controller } = workflowController();
  const item = descriptor({ type: 'datagrid' });
  let rows = 0;
  controller.gridRows = () => Array.from({ length: rows }, () => ({}));
  controller.gridTargetRows = () => 2;
  controller.gridAddButton = () => ({ click() { rows += 1; } });
  assert.equal(await controller.handleGrid(item), 2);
  assert.equal(controller.progress.rowsAdded, 2);
  assert.equal(controller.gridHandled.has(item.id), true);
  assert.equal(await controller.handleGrid(item), 0);
});

test('date picker success avoids DOM fallback and picker failure permits it', async () => {
  const { controller, events } = workflowController();
  const item = descriptor({ type: 'datetime' });
  const state = { wrapper: item.wrapper };
  const plan = { date: new Date(2027, 5, 1), value: '2027-06-01', min: null, max: null };
  controller.inspectEmpty = () => false;
  assert.equal(await controller.applyDatePicker(item, { _flatpickr: { setDate() {} } }, plan, state), true);
  assert.equal(events.at(-1).data.method, 'flatpickr');
  const brokenPicker = { _flatpickr: { setDate() { throw new Error('Picker failed'); } } };
  assert.equal(await controller.applyDatePicker(item, brokenPicker, plan, state), false);
  assert.equal(events.at(-1).name, 'DATE_FLATPICKR_FALLBACK_FAILED');
});

test('native setters retain input, change, and blur ordering for each control type', async () => {
  const { controller, controls } = loadController({ fastTimers: true });
  for (const Control of Object.values(controls)) {
    const control = new Control();
    assert.equal(controller.nativeValuePrototype(control), Control.prototype);
    await controller.nativeSetValue(control, 'test value');
    assert.equal(control.value, 'test value');
    assert.deepEqual(control.events.map((event) => event.type), ['input', 'change', 'blur']);
    assert.ok(control.events.every((event) => event.bubbles && event.composed));
  }
});

test('keyboard Choices retry preserves the legacy event sequence', async () => {
  const { controller, controls } = loadController({ fastTimers: true });
  const search = new controls.input();
  const item = descriptor();
  const choice = { text: 'Victoria', wrapper: item.wrapper };
  let clicks = 0;
  item.wrapper.querySelector = (selector) => selector === '.choices__input--cloned' ? search : { click() { clicks += 1; } };
  controller.waitForChoiceValue = async () => item.wrapper;
  assert.equal(await controller.retryChoiceByKeyboard(item, choice, false), item.wrapper);
  assert.equal(search.value, 'Victoria');
  assert.equal(clicks, 1);
  assert.deepEqual(search.events.map((event) => [event.type, event.key]), [
    ['input', undefined], ['keydown', 'ArrowDown'], ['keydown', 'Enter'], ['keyup', 'Enter']
  ]);
});

test('checkbox messages distinguish underflow, overflow, and valid counts', () => {
  const { controller } = loadController();
  const bounds = { minimum: 1, maximum: 2 };
  assert.match(controller.checkboxSelectionMessage(0, bounds), /below the minimum of 1/);
  assert.equal(controller.checkboxSelectionMessage(1, bounds), '');
  assert.match(controller.checkboxSelectionMessage(3, bounds), /above the maximum of 2/);
});

test('masked value validation retains HTML validity and placeholder checks', () => {
  const { controller } = loadController();
  assert.equal(controller.maskedValueAccepted({ value: '123', checkValidity: () => true }, '999', null), true);
  assert.equal(controller.maskedValueAccepted({ value: '123', checkValidity: () => false }, '999', null), false);
  assert.equal(controller.maskedValueAccepted({ value: '1__' }, '999', null), false);
  assert.equal(controller.maskedValueAccepted(null, '999', { renderedValue: '123', inputmaskComplete: true }), true);
});

test('numeric guidance retains signed, decimal, currency, and reversed range captures', () => {
  const { controller } = loadController();
  const cases = [
    ['Amount between CAD $100 and CA $500', 100, 500],
    ['Amount from $500 to $100', 100, 500],
    ['Maximum of CAD $1,250.50', null, 1250.5],
    ['Minimum: -12.75', -12.75, null],
    ['Must not exceed CA $20', null, 20],
    ['Must be at least 0', 0, null],
    ['An amount without guidance', null, null]
  ];
  for (const [label, minimum, maximum] of cases) {
    const bounds = controller.constraints(descriptor({ label }), null);
    assert.equal(bounds.min, minimum, label);
    assert.equal(bounds.max, maximum, label);
  }
});

test('field names retain the last complete nonempty bracketed key', () => {
  const { controller } = loadController();
  const cases = [
    ['data[contacts][0][name]', 'name'],
    ['data[name][]', 'name'],
    ['data[][name]', 'name'],
    ['data[[nested]]', '[nested'],
    ['data[name][unfinished', 'name'],
    ['data[][]', 'dom-field'],
    ['data' + '['.repeat(2000), 'dom-field'],
    ['data[ ]', ' ']
  ];
  for (const [name, expected] of cases) {
    const field = wrapper('field');
    field.classList = [];
    field.classList.contains = () => false;
    field.querySelector = () => ({ name });
    assert.equal(controller.inferKey(field, new Map()), expected);
  }
});

test('character counters retain zero, signed, and first valid numeric suffix semantics', () => {
  const { controller } = loadController();
  const cases = [
    ['0 characters remaining', 5],
    ['-2 characters remaining', 3],
    ['-9 characters remaining', null],
    ['+7 CHARACTERS remaining', 12],
    ['X12characters remaining', 17],
    ['99 invalid, 1 character remaining', 6],
    ['9'.repeat(2000) + ' invalid, 2 characters remaining', 7],
    ['No counter', null]
  ];
  for (const [textContent, expected] of cases) {
    const item = descriptor();
    item.wrapper.querySelector = () => ({ textContent });
    const bounds = controller.constraints(item, { value: 'abcde', hasAttribute: () => false });
    assert.equal(bounds.maxLength, expected);
  }
});

test('checkbox guidance ignores optional unit suffixes without changing bounds', () => {
  const { controller } = loadController();
  const inputs = Array.from({ length: 5 }, () => ({}));
  for (const suffix of ['', ' partners', ' items', ' options', ' selections']) {
    const item = descriptor({ label: `Minimum: 1${suffix}. Maximum: 3${suffix}.` });
    const bounds = controller.checkboxSelectionBounds(item, inputs);
    assert.equal(bounds.minimum, 1);
    assert.equal(bounds.maximum, 3);
  }
});

test('option ordering remains stable without modifying the supplied array', () => {
  const { controller } = loadController();
  const first = { textContent: 'Choice one' };
  const second = { textContent: 'Choice two' };
  const options = [first, second];
  assert.equal(controller.chooseOption(options, descriptor()).element, first);
  second.textContent = 'British Columbia';
  assert.equal(controller.chooseOption(options, descriptor()).element, second);
  assert.deepEqual(options, [first, second]);
});

test('mask parsing retains captures, escaped literals, and first unsupported symbols', () => {
  const { controller } = loadController();
  assert.equal(controller.runtimeMaskFromControl({ dataset: { inputmask: 'mask = "aa-999"' } }), 'aa-999');
  const item = descriptor();
  assert.equal(controller.generateMaskValue('aa?9[', item, 1).reason, 'Unsupported mask syntax: ?');
  const mask = String.raw`\9-a*(9)`;
  const generated = controller.generateMaskValue(mask, item, 1);
  assert.equal(generated.supported, true);
  assert.equal(controller.maskRegex(mask).test(generated.value), true);
});

test('excluded actions cannot be selected even inside a submit wrapper', () => {
  const { controller } = loadController();
  const button = {
    textContent: 'Submit',
    classList: { contains: () => false },
    getAttribute: (name) => name === 'name' ? 'data[submit]' : '',
    closest: (selector) => selector === '.formio-component' ? { className: 'formio-component-submit' } : null
  };
  assert.equal(controller.isSubmitButtonCandidate(button), true);
  for (const label of ['Save as draft', 'CREATE PDF', 'Print', 'Delete', 'Reset', 'Cancel', 'Previous', 'Back', 'Logout', 'View my drafts', 'Wide layout']) {
    button.textContent = label;
    assert.equal(controller.isSubmitButtonCandidate(button), false, label);
  }
});

test('confirmation-key protection excludes system IDs but allows acknowledgements', () => {
  const { controller } = loadController();
  const field = wrapper('field');
  for (const key of ['confirmationId', 'confirmationNumber', 'submissionConfirmationId', 'submissionConfirmationNumber']) {
    assert.equal(controller.isProtected(field, key, 'textfield', null), true, key);
  }
  for (const key of ['confirmation', 'confirmationAcknowledgement', 'customerConfirmation']) {
    assert.equal(controller.isProtected(field, key, 'textfield', null), false, key);
  }
});

test('class field initializers keep mutable state and bound handlers instance-local', () => {
  const { controller, window } = loadController();
  const second = new controller.constructor();
  for (const key of ['settings', 'customFormatRules', 'customRuleSet', 'componentStates', 'gridHandled', 'fileHandled', 'fileUploadsInFlight', 'actionHandled', 'editGridCommitAttempts', 'visitedTabs', 'tabActivationFailures', 'submitLandmarks', 'visitedWizardSignatures', 'bridgeRequests', 'progress']) {
    assert.notEqual(controller[key], second[key], key);
  }
  controller.progress.filled = 99;
  controller.componentStates.set('first-only', {});
  controller.customFormatRules.push({ id: 'first-only' });
  assert.equal(second.progress.filled, 0);
  assert.equal(second.componentStates.size, 0);
  assert.equal(second.customFormatRules.length, 0);
  assert.equal(second.currentAction, 'Idle');
  assert.equal(second.passBudget, 40);
  second.boundBridgeMessage({ source: window, data: { channel: 'CHEFS_TESTER_BRIDGE', type: 'BRIDGE_READY' } });
  assert.equal(second.bridgeReady, true);
  assert.equal(controller.bridgeReady, false);
});

test('bounded guidance patterns retain compact and normalized whitespace forms', () => {
  const { controller } = loadController();
  for (const space of ['', ' ', '\t', '\r\n', '\u00a0', ' '.repeat(2000)]) {
    const item = descriptor({
      label: `Minimum${space}:${space}2${space}characters. Maximum${space}:${space}20${space}characters.`,
      description: `Minimum${space}:${space}5${space}words. Maximum${space}:${space}10${space}words.`
    });
    const constraints = controller.constraints(item, null);
    assert.equal(constraints.minLength, 2);
    assert.equal(constraints.maxLength, 20);
    assert.equal(constraints.minWords, 5);
    assert.equal(constraints.maxWords, 10);
    const selection = descriptor({ label: `Minimum${space}:${space}2 options. Maximum${space}:${space}3 options.` });
    const bounds = controller.checkboxSelectionBounds(selection, [{}, {}, {}, {}]);
    assert.equal(bounds.minimum, 2);
    assert.equal(bounds.maximum, 3);
    const currency = controller.constraints(descriptor({ label: `Amount between CAD${space}$1,250.50 and CA${space}$2,000.75` }), null);
    assert.equal(currency.min, 1250.5);
    assert.equal(currency.max, 2000.75);
  }
});

test('bounded confirmation pattern preserves normalized labels and rejects invalid IDs', () => {
  const { controller } = loadController();
  for (const space of ['', ' ', '\t', '\r\n', '\u00a0', ' '.repeat(2000)]) {
    const state = controller.confirmationState({
      text: `Confirmation${space}number${space}:${space}abc123ef`,
      url: 'https://chefs.example.test/form/success'
    });
    assert.equal(state.confirmationId, 'ABC123EF');
    assert.deepEqual(plain(state.detectedBy), ['success-path', 'page-label']);
  }
  assert.equal(controller.confirmationState({ text: 'Confirmation' + ' '.repeat(2000) + 'unknown' }).confirmationId, null);
  assert.equal(controller.confirmationState({ text: 'Confirmation: abc123ef0' }).confirmationId, null);
});

test('inferType keeps first differing type, repeated default keys, and layout precedence', () => {
  const { controller } = loadController();
  const cases = [
    [['contact', 'email', 'number'], 'contact', 'email'],
    [['textfield', 'contact', 'email'], 'contact', 'textfield'],
    [['simplefile', 'simplefile'], 'simplefile', 'simplefile'],
    [['contact', 'panel'], 'contact', 'panel'],
    [['', 'contact', 'email'], 'contact', 'contact']
  ];
  for (const [tokens, key, expected] of cases) {
    const field = { classList: tokens.map((token) => `formio-component-${token}`) };
    assert.equal(controller.inferType(field, key, null), expected);
  }
});

test('simplified address patterns retain compact, spaced, and multiline line numbers', () => {
  const { controller } = loadController();
  for (const space of ['', ' ', '\t', '\n']) {
    assert.equal(controller.addressText(`address line${space}1`, 1), '123 Douglas Street');
    assert.equal(controller.addressText(`line${space}1 address`, 1), '123 Douglas Street');
    assert.equal(controller.addressText(`address line${space}2`, 1), 'Building A');
    assert.equal(controller.addressText(`line${space}2 address`, 1), 'Building A');
  }
  assert.equal(controller.addressText('street address', 1), '123 Douglas Street');
  assert.equal(controller.addressText('address\nline1', 1), null);
});

test('raw replacement strings retain CSS escaping and literal mask metacharacters', () => {
  const { controller } = loadController();
  assert.equal(controller.cssEscape('a:b.c [x]'), String.raw`a\:b\.c\ \[x\]`);
  const regex = controller.maskRegex(String.raw`\9.\*`);
  assert.equal(regex.test('9.*'), true);
  assert.equal(regex.test('9X*'), false);
  assert.equal(controller.maskRegex('999').test('123'), true);
  assert.equal(controller.maskRegex('999').test('abc'), false);
});

test('email role matching and slug trimming preserve prior values', () => {
  const { controller } = loadController();
  for (const label of ['Contact1', 'Contact 1', 'Contact\t1']) {
    assert.equal(controller.emailValue(descriptor({ label })), 'contact1.abc123@cedarridgecommunity.ca');
  }
  assert.equal(controller.emailValue(descriptor({ key: '**customEmail**', label: 'Email' })), 'custom-email.abc123@cedarridgecommunity.ca');
});

const textCases = [
  ['First name and email', 'Jordan'],
  ['Surname', 'Campbell'],
  ['Full name', 'Jordan Campbell ABC123'],
  ['Organization name and address', 'Cedar Ridge Community Association ABC123'],
  ['Phone', '2505550142'],
  ['Postal code and country', 'V8W 2B7'],
  ['Address suite', '200'],
  ['Address line 2', 'Building A'],
  ['Address line 1', '123 Douglas Street'],
  ['City', 'Victoria'],
  ['Province', 'British Columbia'],
  ['Country', 'Canada'],
  ['Mailing address', '123 Douglas Street'],
  ['Website', 'https://www2.gov.bc.ca'],
  ['Business number', '123456789RC0001'],
  ['Society number', 'S12345'],
  ['Submission number', 'ABC123AA'],
  ['Position', 'Program Manager'],
  ['Project name', 'Community Access Program ABC123'],
  ['Unspecified', 'Automated entry ABC123']
];
for (const [label, expected] of textCases) {
  test(`text generation preserves ${label} precedence`, () => {
    const { controller } = loadController();
    assert.equal(controller.generateText(descriptor({ key: 'field', label }), 1, null), expected);
  });
}

test('text generation preserves retry formats and narrative fallback', () => {
  const { controller } = loadController();
  assert.equal(controller.generateText(descriptor({ label: 'Phone' }), 2, null), '(250) 555-0142');
  assert.equal(controller.generateText(descriptor({ label: 'Phone' }), 3, null), '6045550188');
  assert.equal(controller.generateText(descriptor({ label: 'Postal code' }), 2, null), 'V8W2B7');
  assert.match(controller.generateText(descriptor({ label: 'Describe the city served' }), 1, null), /^Automated entry/);
  assert.match(controller.generateText(descriptor({ label: 'Project description' }), 1, null), /^Automated CHEFS run ABC123\./);
  assert.equal(controller.generateText(descriptor({ label: 'Contact email' }), 1, null), 'contact.abc123@cedarridgecommunity.ca');
});

test('number generation preserves count, identifier, and bounded amount defaults', () => {
  const { controller } = loadController();
  const cases = [
    ['How many programs', 1], ['Number of contacts', 1], ['Year', 2024],
    ['Month', 10], ['Day', 31], ['Percentage', 25], ['Employees', 27],
    ['Funding amount', 13000], ['Applicant id', 900003], ['Unspecified', 12]
  ];
  for (const [label, expected] of cases) {
    assert.equal(controller.generateNumber(descriptor({ label }), 2, null), expected);
  }
  const bounded = descriptor({ label: 'Funding amount', meta: { min: 100, max: 200 } });
  assert.equal(controller.generateNumber(bounded, 1, null), 150);
});

test('date generation clamps retry offsets and preserves control formatting', () => {
  const { controller } = loadController();
  const item = descriptor({ label: 'End between June 1, 2027 and June 3, 2027' });
  assert.equal(controller.dateValuePlan(item, 10, { type: 'date' }).value, '2027-06-01');
  assert.equal(controller.dateValuePlan(item, 1, { type: 'datetime-local' }).value, '2027-06-03T10:30');
  assert.equal(controller.dateValuePlan(item, 1, { type: 'time' }).value, '10:30');
  assert.equal(controller.dateValuePlan(item, 1, { placeholder: 'MM/DD/YYYY' }).value, '06/03/2027');
  assert.equal(controller.dateValuePlan(item, 1, { placeholder: 'DD/MM/YYYY' }).value, '03/06/2027');
  assert.equal(controller.dateValuePlan(item, 1, { type: 'date', placeholder: 'DD/MM/YYYY' }).value, '2027-06-03');
});

test('bounded dates copy their bounds without mutating them', () => {
  const { controller } = loadController();
  const min = new Date(2027, 5, 1, 12);
  const max = new Date(2027, 5, 3, 12);
  const originalMin = min.getTime();
  const originalMax = max.getTime();
  const cases = [
    ['start', 2, { min }, new Date(2027, 5, 2, 12)],
    ['end', 2, { max }, new Date(2027, 5, 2, 12)],
    ['start', 10, { min, max }, max],
    ['end', 10, { min, max }, min]
  ];
  for (const [context, attempt, bounds, expected] of cases) {
    const result = controller.boundedDateValue(context, attempt, bounds);
    assert.equal(result.getTime(), expected.getTime());
    assert.notEqual(result, min);
    assert.notEqual(result, max);
    assert.equal(min.getTime(), originalMin);
    assert.equal(max.getTime(), originalMax);
  }
});

test('bytesToBase64 preserves every byte across chunk boundaries', () => {
  const { controller } = loadController();
  for (const length of [0, 256, 0x8000, 0x8001, 0x10001]) {
    const bytes = Uint8Array.from({ length }, (_, index) => index % 256);
    assert.equal(controller.bytesToBase64(bytes), Buffer.from(bytes).toString('base64'));
  }
});

test('confirmation IDs alone do not declare success', () => {
  const { controller } = loadController();
  const label = controller.confirmationState({ text: 'Confirmation: abcdef12' });
  assert.equal(label.confirmed, false);
  assert.equal(label.confirmationId, 'ABCDEF12');
  const query = controller.confirmationState({ text: '', url: 'https://chefs.example.test/form/123?confirmationId=abcdef12' });
  assert.equal(query.confirmed, false);
  assert.equal(query.confirmationId, null);
});

test('confirmation detection preserves label, query, then token precedence', () => {
  const { controller } = loadController();
  const url = 'https://chefs.example.test/form/success?confirmationId=abcdef12&confirmation=11111111';
  const labeled = controller.confirmationState({ url, text: 'Confirmation: 22222222' });
  assert.equal(labeled.confirmationId, '22222222');
  assert.deepEqual(plain(labeled.detectedBy), ['success-path', 'page-label']);
  const query = controller.confirmationState({ url, text: '33333333' });
  assert.equal(query.confirmationId, 'ABCDEF12');
  const token = controller.confirmationState({ text: 'Thank you for your submission 33333333' });
  assert.equal(token.confirmed, true);
  assert.equal(token.confirmationId, '33333333');
  assert.deepEqual(plain(token.detectedBy), ['success-phrase', 'success-page-token']);
  const noToken = controller.confirmationState({ url: 'https://chefs.example.test/form/success', text: '' });
  assert.equal(noToken.confirmed, true);
  assert.equal(noToken.confirmationId, null);
});

test('rule normalization preserves complete tags and malformed delimiters', () => {
  const { controller } = loadController();
  const cases = [
    ['A <b>B</b> C', 'a b c'], ['A <b<c>B', 'a b'],
    ['A > B <C', 'a b c'], ['<<< unfinished', 'unfinished'],
    ['** : - A > B - : **', 'a b']
  ];
  for (const [label, labelMatch] of cases) {
    const rule = { labelMatch, matchMode: 'exact', caseSensitive: false };
    controller.customFormatRules = [rule];
    assert.equal(controller.customRuleForDescriptor({ label }), rule);
  }
});

for (const minimum of [undefined, null, 0, -1, 1, 20]) {
  test(`fitText handles minimum word count ${minimum}`, () => {
    const { controller } = loadController();
    const value = controller.fitText('Test', { minWords: minimum });
    const count = value.trim().split(/\s+/).length;
    assert.ok(count >= Math.max(1, minimum || 0));
    if (!(minimum > 1)) {
      assert.equal(value, 'Test');
    }
  });
}

test('fitText treats absent, negative, and NaN bounds as unconstrained', () => {
  const { controller } = loadController();
  for (const value of [undefined, null, 0, -1, Number.NaN]) {
    const constraints = { minLength: value, maxLength: value, minWords: value, maxWords: value };
    assert.equal(controller.fitText('one two three', constraints), 'one two three');
    assert.equal(controller.fitText('', constraints), '');
  }
});

test('fitText preserves padding text and maximum constraints', () => {
  const { controller } = loadController();
  assert.equal(controller.fitText('Test', { minWords: 2 }),
    'Test Additional synthetic details for run ABC123.');
  assert.equal(controller.fitText('Test', { minLength: 5 }),
    'Test Additional program information for run ABC123.');
  assert.equal(controller.fitText('one two three', { maxWords: 2 }), 'one two');
  assert.equal(controller.fitText('one two three', { maxLength: 4 }), 'one');
  assert.equal(controller.fitText('', {}), '');
  assert.equal(controller.fitText('', { maxLength: 3 }), 'Test');
});

test('custom rule normalization preserves punctuation and case semantics', () => {
  const { controller } = loadController();
  controller.customFormatRules = [{ labelMatch: 'Contact Name', matchMode: 'exact', caseSensitive: false }];
  assert.equal(controller.customRuleForDescriptor({ label: '* : <b>CONTACT</b> Name -- *' }), controller.customFormatRules[0]);
  controller.customFormatRules[0].caseSensitive = true;
  assert.equal(controller.customRuleForDescriptor({ label: '* CONTACT Name *' }), null);
  assert.equal(controller.customRuleForDescriptor({ label: '* Contact Name *' }), controller.customFormatRules[0]);
});

const months = [
  ['January', 'Jan'], ['February', 'Feb'], ['March', 'Mar'], ['April', 'Apr'],
  ['May'], ['June', 'Jun'], ['July', 'Jul'], ['August', 'Aug'],
  ['September', 'Sep', 'Sept'], ['October', 'Oct'], ['November', 'Nov'], ['December', 'Dec']
];
for (const [month, aliases] of months.entries()) {
  test(`date guidance accepts month ${month + 1} names and abbreviations`, () => {
    const { controller } = loadController();
    for (const alias of aliases) {
      const item = descriptor({ label: `Start on or after ${alias.toUpperCase()}. 2nd, 2027` });
      const plan = controller.dateValuePlan(item, 1, { type: 'date' });
      assert.equal(plan.value, `2027-${String(month + 1).padStart(2, '0')}-02`);
    }
  });
}

test('date guidance retains date validation and range ordering', () => {
  const { controller } = loadController();
  const invalid = controller.constraints(descriptor({ label: 'On or after February 30, 2027' }), null);
  assert.equal(invalid.minDate, null);
  const plan = controller.dateValuePlan(descriptor({ label: 'Start between June 30, 2027 and June 1, 2027' }), 1, { type: 'date' });
  assert.equal(plan.value, '2027-06-01');
});

test('liveWrapper retains visible wrappers and reacquires replacements by id', () => {
  const { controller, document } = loadController();
  const item = descriptor();
  assert.equal(controller.liveWrapper(null), null);
  assert.equal(controller.liveWrapper(item), item.wrapper);
  item.wrapper.isConnected = false;
  const replacement = wrapper('field1');
  document.getElementById = () => replacement;
  assert.equal(controller.liveWrapper(item), replacement);
  assert.equal(item.wrapper, replacement);
  assert.equal(item.wrapperId, 'field1');
});

test('liveWrapper prefers visible key matches then connected hidden matches', () => {
  const { controller, document } = loadController();
  const item = descriptor({ wrapper: null });
  const hidden = wrapper('hidden', false);
  const visible = wrapper('visible');
  document.querySelectorAll = () => [hidden, visible];
  assert.equal(controller.liveWrapper(item), visible);
  visible.isConnected = false;
  assert.equal(controller.liveWrapper(item), hidden);
});

test('runtime masks retain expando and dataset priority', () => {
  const { controller } = loadController();
  const control = { dataset: { inputmaskMask: ' 999 ', mask: 'aaa', inputmask: "mask: '***'" } };
  assert.equal(controller.runtimeMaskFromControl(control), '999');
  delete control.dataset.inputmaskMask;
  assert.equal(controller.runtimeMaskFromControl(control), 'aaa');
  delete control.dataset.mask;
  assert.equal(controller.runtimeMaskFromControl(control), '***');
  control.inputmask = { opts: { mask: ['99-99', 'aaaa'] } };
  assert.equal(controller.runtimeMaskFromControl(control), '99-99');
  assert.equal(controller.runtimeMaskFromControl(null), '');
});

test('constraints preserve DOM, schema, and guidance precedence including zero', () => {
  const { controller } = loadController();
  const item = descriptor({ label: 'Minimum 5 characters, maximum 50 characters', meta: { minLength: 2, maxLength: 30, min: 7, max: 20 } });
  const control = { hasAttribute: (name) => name === 'maxlength', maxLength: 10, min: '0', max: '' };
  const constraints = controller.constraints(item, control);
  assert.equal(constraints.minLength, 2);
  assert.equal(constraints.maxLength, 10);
  assert.equal(constraints.min, 0);
  assert.equal(constraints.max, 20);
  const fallback = controller.constraints(descriptor({ label: 'Minimum 5 characters, maximum 50 characters' }), null);
  assert.equal(fallback.minLength, 5);
  assert.equal(fallback.maxLength, 50);
});

test('trackDescriptor logs discovery once and preserves attempts on visibility changes', async () => {
  const { controller } = loadController();
  const events = [];
  controller.log = async (...args) => events.push(args);
  const item = descriptor();
  controller.currentPass = 1;
  await controller.trackDescriptor(item);
  const state = controller.componentStates.get(item.id);
  assert.equal(state.status, 'discovered');
  state.attempts = 2;
  item.visible = false;
  controller.currentPass = 2;
  await controller.trackDescriptor(item);
  await controller.trackDescriptor(item);
  assert.deepEqual(events.map(([name]) => name), ['COMPONENT_DISCOVERED', 'COMPONENT_BECAME_HIDDEN']);
  assert.equal(state.attempts, 2);
  assert.equal(state.firstSeenPass, 1);
  assert.equal(state.lastSeenPass, 2);
  assert.equal(controller.initialComponentStatus(descriptor({ protected: true })), 'protected');
  assert.equal(controller.initialComponentStatus(descriptor({ fillable: false })), 'non-input');
});

test('descriptorSnapshot preserves masks, zero constraints, and grid metadata', () => {
  const { controller } = loadController();
  const rule = { id: 'r1', labelMatch: 'Contact', matchMode: 'exact', mask: '999' };
  const item = descriptor({ type: 'datagrid', meta: { minLength: 0, maxLength: null, minWords: 0 }, maskPlan: { source: 'custom-rule', mask: '999', rule } });
  controller.gridRows = () => [{}, {}];
  controller.gridTargetRows = () => 3;
  const snapshot = controller.descriptorSnapshot(item);
  assert.equal(snapshot.minLength, 0);
  assert.equal(snapshot.maxLength, null);
  assert.equal(snapshot.minWords, 0);
  assert.equal(snapshot.maxWords, undefined);
  assert.equal(snapshot.gridRowCount, 2);
  assert.equal(snapshot.gridTargetRows, 3);
  assert.deepEqual(plain(snapshot.customRule), { matched: true, ruleId: 'r1', labelMatch: 'Contact', matchMode: 'exact', configuredMask: '999' });
  const unmasked = controller.descriptorSnapshot(descriptor());
  assert.deepEqual(plain(unmasked.customRule), { matched: false });
  assert.equal(unmasked.gridRowCount, undefined);
});

test('scanComponents deduplicates wrappers, tracks descriptors, and updates progress', async () => {
  const { controller, document } = loadController();
  const item = descriptor();
  document.querySelectorAll = () => [item.wrapper, item.wrapper];
  controller.expandContainers = async () => {};
  controller.bridgeCommand = async () => ({ components: [], formFound: true });
  controller.createDescriptor = () => item;
  let indexed = 0;
  controller.indexSubmitLandmarks = async () => { indexed += 1; };
  const result = await controller.scanComponents();
  assert.equal(result.descriptors.length, 1);
  assert.equal(result.fillable.length, 1);
  assert.equal(result.snapshot[0].status, 'discovered');
  assert.equal(result.formioInstanceFound, true);
  assert.equal(controller.progress.discovered, 1);
  assert.equal(controller.progress.remaining, 1);
  assert.equal(indexed, 1);
});

test('bridgeCommand targets the current origin and cleans up its request', async () => {
  const { controller, window } = loadController();
  controller.bridgeReady = true;
  window.postMessage = (message, origin) => {
    assert.equal(origin, 'https://chefs.example.test');
    assert.equal(message.command, 'PING');
    controller.handleBridgeMessage({ source: window, data: { channel: 'CHEFS_TESTER_BRIDGE_RESPONSE', requestId: message.requestId, ok: true, result: 'ready' } });
  };
  assert.equal(await controller.bridgeCommand('PING', {}, 100), 'ready');
  assert.equal(controller.bridgeRequests.size, 0);
});

test('finalization remains idempotent and the confirmation guard returns a boolean', async () => {
  const { controller } = loadController();
  const messages = [];
  controller.runtimeMessage = async (message) => { messages.push(message); };
  controller.confirmationState = () => ({ confirmed: true, confirmationId: '1234ABCD', detectedBy: ['success-path'] });
  controller.scanComponents = async () => ({ snapshot: [] });
  controller.running = true;
  assert.equal(await controller.finalizeIfSubmitted(1, 'test'), true);
  assert.equal(await controller.finalizeIfSubmitted(1, 'test'), true);
  assert.equal(messages.filter((message) => message.type === 'RUN_FINALIZED').length, 1);
  assert.equal(controller.running, false);
  controller.confirmationState = () => ({ confirmed: false });
  assert.equal(await controller.finalizeIfSubmitted(1, 'test'), false);
});
