const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const moduleDirectory = path.resolve(__dirname, '../../..');
const componentDirectory = path.join(moduleDirectory, 'src/Unity.Flex.Web/Views/Shared/Components');
const detailsSource = readFileSync(path.resolve(moduleDirectory,
    '../../src/Unity.GrantManager.Web/Pages/GrantApplications/Details.js'), 'utf8');
const widgets = [
    ['DefaultFieldWidget', 1], ['YesNoWidget', 1], ['CheckboxWidget', 2],
    ['CurrencyWidget', 1], ['DateWidget', 1], ['RadioWidget', 2],
    ['SelectListWidget', 1], ['CheckboxGroupWidget', 2], ['TextAreaWidget', 1]
].map(([name, expectedBranches]) => ({
    name, expectedBranches,
    source: readFileSync(path.join(componentDirectory, name, 'Default.cshtml'), 'utf8')
}));

function controlsFor(widget) {
    // Match source control boundaries without interpreting embedded Razor quotes.
    return [...widget.source.matchAll(/<(?:input|select|textarea)\b[\s\S]*?(?:\/>|>)/g)]
        .map(match => match[0]).filter(markup => markup.includes('notifyFieldChange'));
}

function handlerFor(markup) {
    const handler = markup.match(/\bonchange="([^"]*)"/)?.[1];
    assert.ok(handler, 'The field must have a change handler');
    assert.doesNotMatch(handler, /@/, 'Metadata must not be interpolated into executable code');
    return handler;
}

function invokeHandler(context, handler, field) {
    context.field = field;
    vm.runInContext(`(function () { ${handler} }).call(field);`, context);
    assert.equal(context.xssExecuted, false, 'Metadata must not execute as JavaScript');
}

const metadataCases = [
    { worksheetName: 'Worksheet-v1', uiAnchor: 'ProjectInfo' },
    { worksheetName: "Reviewer's worksheet", uiAnchor: "Reviewer's tab" },
    { worksheetName: 'A & B < C > D "quoted"', uiAnchor: '"><img src=x onerror=alert(1)>' },
    { worksheetName: 'Café &lt;name&gt; &#39; \\ line\nnext', uiAnchor: 'Custom\\Tab\nNext' },
    { worksheetName: "');globalThis.xssExecuted = true;//", uiAnchor: 'ProjectInfo' },
    { worksheetName: 'Worksheet-v1', uiAnchor: "');globalThis.xssExecuted = true;//" },
    { worksheetName: '', uiAnchor: '' }
];

// These checks inspect Razor bindings and run actual handler source with decoded
// dataset values. They do not render Razor or parse HTML in a browser.
for (const widget of widgets) {
    test(`${widget.name} forwards metadata as data in every field branch`, () => {
        const controls = controlsFor(widget);
        assert.equal(controls.length, widget.expectedBranches);
        for (const markup of controls) {
            assert.match(markup, /\bdata-worksheet-name="@Model\.Name"/);
            assert.match(markup, /\bdata-ui-anchor="@Model\.Field\??\.UiAnchor"/);
            const handler = handlerFor(markup);
            for (const dataset of metadataCases) {
                const calls = [];
                const context = vm.createContext({
                    xssExecuted: false,
                    notifyFieldChange(...args) { calls.push(args); }
                });
                const field = {
                    dataset, id: 'custom_field.worksheet.field-id.worksheet-id',
                    value: 'original value', checked: /\schecked\b/.test(markup)
                };
                invokeHandler(context, handler, field);
                assert.equal(calls.length, 1);
                assert.deepEqual(calls[0], [dataset.worksheetName, dataset.uiAnchor, field]);
                assert.equal(field.value, 'original value');
                assert.equal(field.checked, /\schecked\b/.test(markup));
            }
        }
    });
}

function notificationHarness() {
    const events = [];
    const fields = new Map();
    const context = vm.createContext({
        xssExecuted: false,
        $() {}, // Skip page initialization; exercise the real notification functions.
        document: { getElementById(id) { return fields.get(id); } },
        PubSub: {
            publish(topic, payload) {
                events.push({ topic, payload: typeof payload === 'object' ? { ...payload } : payload });
            }
        }
    });
    vm.runInContext(detailsSource, context, { filename: 'Details.js' });
    return {
        events,
        change(handler, field) {
            fields.set(field.id, field);
            invokeHandler(context, handler, field);
        }
    };
}

for (const anchor of ['ProjectInfo', 'ApplicantInfo', 'AssessmentInfo', 'PaymentInfo', 'FundingAgreementInfo']) {
    test(`all field handlers preserve ${anchor} notification routing and current values`, () => {
        const harness = notificationHarness();
        for (const widget of widgets) {
            for (const [index, markup] of controlsFor(widget).entries()) {
                const field = {
                    id: `${widget.name}-${index}`, value: `changed ${widget.name}-${index}`,
                    dataset: { worksheetName: 'Worksheet-v1', uiAnchor: anchor }
                };
                const count = harness.events.length;
                harness.change(handlerFor(markup), field);
                assert.equal(harness.events.length, count + 1);
                assert.deepEqual(harness.events.at(-1), {
                    topic: `fields_${anchor.toLowerCase()}`, payload: field.value
                });
            }
        }
    });
}

test('custom and preview anchors retain the exact worksheet name and field ID in notifications', () => {
    const harness = notificationHarness();
    const datasets = metadataCases.filter(dataset => dataset.uiAnchor !== 'ProjectInfo')
        .concat([{ worksheetName: 'Preview worksheet', uiAnchor: 'Preview' }]);
    for (const widget of widgets) {
        for (const markup of controlsFor(widget)) {
            for (const dataset of datasets) {
                const field = { dataset, id: `${widget.name}.${dataset.worksheetName}.field-id`, value: 'value' };
                const count = harness.events.length;
                harness.change(handlerFor(markup), field);
                assert.equal(harness.events.length, count + 1);
                assert.deepEqual(harness.events.at(-1), {
                    topic: 'fields_tab', payload: { worksheet: dataset.worksheetName, fieldId: field.id }
                });
            }
        }
    }
});

test('a refreshed field uses its own worksheet metadata and current value', () => {
    const harness = notificationHarness();
    const handler = handlerFor(controlsFor(widgets[0])[0]);
    harness.change(handler, {
        id: 'field-id', value: 'old', dataset: { worksheetName: 'Old-v1', uiAnchor: 'ProjectInfo' }
    });
    harness.change(handler, {
        id: 'field-id', value: 'new', dataset: { worksheetName: 'New-v2', uiAnchor: 'PaymentInfo' }
    });
    assert.deepEqual(harness.events, [
        { topic: 'fields_projectinfo', payload: 'old' },
        { topic: 'fields_paymentinfo', payload: 'new' }
    ]);
});
