const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

const source = readFileSync(path.join(__dirname,
    '../../../src/Unity.GrantManager.Web/Views/Shared/Components/ReviewList/ReviewList.js'), 'utf8');
const permissionReason = 'You need the "Start Assessment" permission to create an assessment and move this application to Under Assessment.';

function fixture(getAvailability, { hasAi = true, hasButtons = true } = {}) {
    const reason = {
        value: '', hidden: true,
        text(value) { this.value = value; return this; },
        toggleClass(name, hidden) { this.hidden = hidden; return this; }
    };
    function button() {
        const attributes = {};
        const node = { attr(name, value) { attributes[name] = value; return this; } };
        return {
            enabled: false, attributes,
            any() { return hasButtons; },
            disable() { this.enabled = false; return this; },
            enable(enabled = true) { this.enabled = enabled; return this; },
            node() { return node; }
        };
    }
    const create = button();
    const clone = button();
    const table = {
        button(name) { return name === 'Create:name' ? create : clone; },
        rows() { return { data: () => ({ toArray: () => hasAi ? [{ isAiAssessment: true }] : [] }) }; }
    };
    const context = {
        abp: {
            localization: { getResource: () => key => key },
            auth: { isGranted: () => true }
        },
        document: { querySelector: selector => ({ value: selector === '#DetailsViewApplicationId' ? 'application-id' : 'True' }) },
        // Do not initialize DataTables: these tests exercise the production button-state functions.
        $: value => typeof value === 'function' ? undefined
            : value === '#AssessmentCreationDisabledReason' ? reason : value,
        unity: { grantManager: { assessments: { assessment: { getCreationAvailability: getAvailability } } } },
        console: { error() {} }
    };
    vm.runInNewContext(source, context);
    return { create, clone, reason, update: () => context.updateAssessmentCreationButtons(table) };
}

test('disables Create and Clone with the Start Assessment explanation', async () => {
    const ui = fixture(async applicationId => {
        assert.equal(applicationId, 'application-id');
        return { canCreate: false, reason: permissionReason };
    });

    await ui.update();

    assert.equal(ui.create.enabled, false);
    assert.equal(ui.clone.enabled, false);
    assert.equal(ui.reason.value, permissionReason);
    assert.equal(ui.reason.hidden, false);
    assert.equal(ui.create.attributes.title, permissionReason);
    assert.equal(ui.clone.attributes['aria-describedby'], 'AssessmentCreationDisabledReason');
});

test('allows both actions when the server allows creation', async () => {
    const ui = fixture(async () => ({ canCreate: true, reason: null }));

    await ui.update();

    assert.equal(ui.create.enabled, true);
    assert.equal(ui.clone.enabled, true);
    assert.equal(ui.reason.hidden, true);
    assert.equal(ui.create.attributes.title, '');
});

test('keeps Clone disabled when no AI assessment is available', async () => {
    const ui = fixture(async () => ({ canCreate: true }), { hasAi: false });

    await ui.update();

    assert.equal(ui.create.enabled, true);
    assert.equal(ui.clone.enabled, false);
});

test('keeps both actions disabled if the availability request fails', async () => {
    const ui = fixture(async () => { throw new Error('request failed'); });

    await ui.update();

    assert.equal(ui.create.enabled, false);
    assert.equal(ui.clone.enabled, false);
    assert.equal(ui.reason.value, 'Assessment:UnableToCheckCreation');
    assert.equal(ui.reason.hidden, false);
});

test('ignores an outdated response that would re-enable restricted actions', async () => {
    let resolveFirst;
    let resolveSecond;
    const first = new Promise(resolve => { resolveFirst = resolve; });
    const second = new Promise(resolve => { resolveSecond = resolve; });
    let calls = 0;
    const ui = fixture(() => ++calls === 1 ? first : second);

    const firstUpdate = ui.update();
    const secondUpdate = ui.update();
    assert.equal(ui.create.enabled, false);
    assert.equal(ui.clone.enabled, false);
    resolveSecond({ canCreate: false, reason: permissionReason });
    await secondUpdate;
    resolveFirst({ canCreate: true });
    await firstUpdate;

    assert.equal(ui.create.enabled, false);
    assert.equal(ui.clone.enabled, false);
    assert.equal(ui.reason.value, permissionReason);
});

test('does not request creation availability when no creation buttons exist', async () => {
    let calls = 0;
    const ui = fixture(async () => { calls++; return { canCreate: true }; }, { hasButtons: false });

    await ui.update();

    assert.equal(calls, 0);
});
