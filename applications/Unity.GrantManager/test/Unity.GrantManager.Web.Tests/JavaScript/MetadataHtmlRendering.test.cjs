const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const root = path.resolve(__dirname, '../../..');
const abpSource = readFileSync(path.join(root,
    'src/Unity.GrantManager.Web/node_modules/@abp/core/src/abp.js'), 'utf8');
const reportingRoot = 'modules/Unity.Reporting/src/Unity.Reporting.Web/Views/Shared/Components/';
const tenantFile = 'modules/Unity.TenantManagement/src/Unity.TenantManagement.Web/Pages/TenantManagement/Tenants/Index.js';

// Isolate existing functions without starting unrelated page initialization.
// Read their complete declarations, including nested callbacks, from production
// source. These files indent function-closing braces by four spaces.
function loadFunctions(context, file, names) {
    const source = readFileSync(path.join(root, file), 'utf8').replaceAll('\r\n', '\n');
    for (const name of names) {
        const start = source.indexOf(`    function ${name}(`);
        assert.ok(start >= 0, `Missing production function: ${name}`);
        const end = source.indexOf('\n    }\n', start);
        assert.ok(end > start, `Missing end of production function: ${name}`);
        vm.runInContext(source.slice(start, end + 6), context, { filename: file });
    }
}

// Use the installed ABP encoder, not a mock sanitizer. The DOM/AJAX adapters
// capture writes and callbacks; they do not simulate browser script execution.
function harness(response = {}, { fail = false } = {}) {
    const nodes = new Map();
    const requests = [];
    let modalShows = 0;
    const context = vm.createContext({
        document: { querySelector: () => null, createElement: () => ({ classList: { add() {} } }) }
    });
    vm.runInContext(abpSource, context, { filename: 'abp.js' });
    function node(selector) {
        if (!nodes.has(selector)) {
            const item = {
                0: {}, textValue: '', htmlValue: '', htmlWrites: [], visible: false,
                text(value) { this.textValue = String(value); return this; },
                html(value) {
                    if (!arguments.length) return context.abp.utils.htmlEscape(this.textValue);
                    this.htmlValue = value;
                    this.htmlWrites.push(value);
                    return this;
                },
                show() { this.visible = true; return this; },
                hide() { this.visible = false; return this; }
            };
            nodes.set(selector, item);
        }
        return nodes.get(selector);
    }
    context.$ = node;
    context.bootstrap = { Modal: class { show() { modalShows++; } } };
    context.getCorrelationProvider = () => 'formversion';
    context._createFeatureProviderKey = 'create-provider-key';
    context.abp.ajax = options => {
        requests.push(options);
        const result = {
            done(callback) { if (!fail) callback(response); return result; },
            fail(callback) { if (fail) callback({ status: 500 }); return result; }
        };
        return result;
    };
    return { context, node, requests, get modalShows() { return modalShows; } };
}

function reportingPreview(response) {
    const view = harness();
    loadFunctions(view.context, reportingRoot + 'ReportingConfigurationViewStatus/Default.js', ['showViewDataModal']);
    view.context.showViewDataModal(response);
    return view;
}

test('reporting delete dialog treats an API-provided view name as literal text', () => {
    const viewName = '<img src=x onerror=alert(1)> & "view"';
    const view = harness({ viewName });
    loadFunctions(view.context, reportingRoot + 'ReportingConfiguration/Default.js', ['updateDeleteModalContent']);
    let callbacks = 0;
    view.context.updateDeleteModalContent('correlation-id', () => callbacks++);
    const item = view.node('#deleteViewListItem');
    assert.equal(item.textValue, `The associated database view (${viewName})`);
    assert.equal(item.htmlWrites.length, 0);
    assert.equal(item.visible, true);
    assert.equal(callbacks, 1);
    assert.equal(view.requests[0].data.correlationProvider, 'formversion');
});

test('reporting delete dialog hides the view entry when absent or the request fails', () => {
    for (const [response, options] of [[{}, {}], [{ viewName: '  ' }, {}], [{}, { fail: true }]]) {
        const view = harness(response, options);
        loadFunctions(view.context, reportingRoot + 'ReportingConfiguration/Default.js', ['updateDeleteModalContent']);
        view.node('#deleteViewListItem').show();
        let callbacks = 0;
        view.context.updateDeleteModalContent('correlation-id', () => callbacks++);
        assert.equal(view.node('#deleteViewListItem').visible, false);
        assert.equal(callbacks, 1);
    }
});

test('reporting preview escapes column headers and still looks up rows by the original key', () => {
    const column = '<img src=x onerror=alert(1)> & "column"';
    const view = reportingPreview({ viewName: 'View', columns: [column], data: [{ [column]: 'Correct cell' }], totalCount: 1 });
    const html = view.node('#viewDataPreviewContent').htmlValue;
    assert.ok(html.includes('<th>&lt;img src=x onerror=alert(1)&gt; &amp; &quot;column&quot;</th>'));
    assert.ok(html.includes('<td>Correct cell</td>'));
    assert.equal(html.includes('<img'), false);
    assert.equal(view.modalShows, 1);
});

test('reporting preview escapes a malformed record count', () => {
    const view = reportingPreview({ viewName: 'View', columns: ['column'], data: [{ column: 5 }], totalCount: '<img src=x onerror=alert(1)>' });
    const html = view.node('#viewDataPreviewContent').htmlValue;
    assert.ok(html.includes('&lt;img src=x onerror=alert(1)&gt; records'));
    assert.equal(html.includes('<img'), false);
});

test('reporting preview preserves numeric counts, singular/plural labels, and empty results', () => {
    for (const totalCount of [1, 2]) {
        const view = reportingPreview({ viewName: 'View', columns: ['column'], data: [{ column: 0 }], totalCount });
        const html = view.node('#viewDataPreviewContent').htmlValue;
        assert.ok(html.includes(`${totalCount} record${totalCount === 1 ? '' : 's'} from this application`));
        assert.ok(html.includes('<td>0</td>'));
    }
    const empty = reportingPreview({ viewName: '<b>View</b>', columns: [], data: [], totalCount: 0 });
    assert.ok(empty.node('#viewDataPreviewContent').htmlValue.includes('No preview data available.'));
    assert.equal(empty.node('#viewDataPreviewModalLabel').textValue, 'View Data Preview: <b>View</b> (Sample Data)');
});

test('reporting preview keeps existing data-cell escaping and null handling', () => {
    const view = reportingPreview({ viewName: 'View', columns: ['a', 'b'], data: [{ a: '<img src=x>', b: null }], totalCount: 1 });
    const html = view.node('#viewDataPreviewContent').htmlValue;
    assert.ok(html.includes('<td>&lt;img src=x&gt;</td><td></td>'));
});

function featureView(groups) {
    const view = harness({ groups });
    loadFunctions(view.context, tenantFile, ['_renderFeatureItem', '_renderFeatureGroups', '_loadFeaturesTab', '_loadCreateFeaturesTab']);
    return view;
}

test('tenant feature rendering escapes group and feature display names', () => {
    const view = featureView([]);
    const html = view.context._renderFeatureGroups([{
        name: 'Group', displayName: '<img src=x onerror=alert(1)>',
        features: [{ name: 'Unity.Flex', displayName: '<b>A & B</b>', value: 'True' }]
    }]);
    assert.ok(html.includes('&lt;img src=x onerror=alert(1)&gt;</h6>'));
    assert.ok(html.includes('&lt;b&gt;A &amp; B&lt;/b&gt;</label>'));
    assert.equal(html.includes('<img'), false);
    assert.equal(html.includes('<b>'), false);
});

test('tenant feature rendering escapes identifiers in double-quoted attributes', () => {
    const view = featureView([]);
    const html = view.context._renderFeatureGroups([{
        name: 'Group" onclick="alert(1)', displayName: 'Group',
        features: [{ name: 'Unity.Flex" onfocus="alert(2)', displayName: 'Feature', value: 'false' }]
    }]);
    assert.ok(html.includes('data-feature-group="Group&quot; onclick=&quot;alert(1)"'));
    assert.ok(html.includes('data-feature-name="Unity.Flex&quot; onfocus=&quot;alert(2)"'));
    assert.ok(html.includes('id="ft-Unity-Flex&quot; onfocus=&quot;alert(2)"'));
    assert.ok(html.includes('for="ft-Unity-Flex&quot; onfocus=&quot;alert(2)"'));
    assert.equal(html.includes(' onclick="'), false);
    assert.equal(html.includes(' onfocus="'), false);
});

test('tenant feature rendering escapes fallback names and literal entity references', () => {
    const view = featureView([]);
    const html = view.context._renderFeatureGroups([{
        name: '<Group>', features: [{ name: 'A &lt; B', value: 'false' }]
    }]);
    assert.ok(html.includes('&lt;Group&gt;</h6>'));
    assert.ok(html.includes('A &amp;lt; B</label>'));
});

test('tenant feature rendering preserves normal identifiers, checked state, and empty groups', () => {
    const view = featureView([]);
    const html = view.context._renderFeatureGroups([{
        name: 'Specializations', features: [
            { name: 'Unity.Onboarding', value: 'True' },
            { name: 'Unity.Other', value: 'false' }
        ]
    }]);
    assert.ok(html.includes('data-feature-group="Specializations"'));
    assert.ok(html.includes('id="ft-Unity-Onboarding" data-feature-name="Unity.Onboarding" checked>'));
    assert.ok(html.includes('for="ft-Unity-Onboarding"'));
    assert.ok(html.includes('data-feature-name="Unity.Other">'));
    assert.ok(view.context._renderFeatureGroups([]).includes('No features available.'));
    assert.ok(view.context._renderFeatureGroups(null).includes('No features available.'));
});

for (const mode of ['config', 'create']) {
    test(`tenant ${mode} feature loading inserts escaped metadata from the API`, () => {
        const view = featureView([{ name: 'Group', features: [{ name: 'Unity.Flex', displayName: '<img src=x>', value: 'true' }] }]);
        if (mode === 'config') view.context._loadFeaturesTab('tenant-id');
        else view.context._loadCreateFeaturesTab();
        const content = view.node(`#${mode}-features-content`);
        assert.equal(content.htmlWrites[0], '');
        assert.ok(content.htmlValue.includes('&lt;img src=x&gt;</label>'));
        assert.equal(content.htmlValue.includes('<img'), false);
        assert.equal(view.requests[0].data.providerName, 'T');
        assert.equal(view.requests[0].data.providerKey, mode === 'config' ? 'tenant-id' : 'create-provider-key');
    });
}
