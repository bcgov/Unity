const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const sourcePath = process.env.REPORTING_SCRIPT_PATH || path.resolve(__dirname,
    '../../../modules/Unity.Reporting/src/Unity.Reporting.Web/Views/Shared/Components/ReportingConfiguration/Default.js');
const source = readFileSync(sourcePath, 'utf8');

function harness({ present = true, provider = 'formversion_consolidated' } = {}) {
    const requests = [];
    const tableReady = new Error('Table configuration captured');
    let tableOptions;
    const nodes = new Map();
    const document = { getElementById: () => present ? {} : null };

    function $(selector) {
        if (typeof selector === 'function') {
            selector();
            return;
        }
        if (!nodes.has(selector)) {
            const node = {
                val() {
                    return selector === '#reportingProvider' ? provider : 'form-version-id';
                },
                on() { return this; },
                off() { return this; },
                prop() { return this; },
                show() { return this; },
                hide() { return this; },
                text() { return this; },
                removeClass() { return this; },
                addClass() { return this; }
            };
            nodes.set(selector, node);
        }
        return nodes.get(selector);
    }

    const context = {
        $, document, console,
        abp: {
            appPath: '/',
            ajax(options) {
                requests.push(options);
                return { done() { return this; }, fail() { return this; } };
            }
        },
        commonTableActionButtons: () => [],
        UnityJsonEditor: class {},
        initializeDataTable(options) {
            tableOptions = options;
            throw tableReady;
        }
    };
    return {
        run: () => vm.runInNewContext(source, context, { filename: sourcePath }),
        requests, tableReady,
        get tableOptions() { return tableOptions; }
    };
}

test('reporting warning without form controls does not initialize the table or request metadata', () => {
    const view = harness({ present: false });
    view.run();
    assert.equal(view.requests.length, 0);
    assert.equal(view.tableOptions, undefined);
});

for (const provider of ['formversion', 'formversion_consolidated', 'worksheet', 'worksheet_consolidated', 'scoresheet']) {
    test(`reporting initialization can read ${provider} configuration and reach table setup`, () => {
        const view = harness({ provider });
        assert.throws(view.run, error => error === view.tableReady);
        assert.equal(view.requests[0].data.correlationProvider, provider);
        assert.equal(view.tableOptions.listColumns.find(column => column.name === 'label').title,
            provider.startsWith('formversion') ? 'CHEFS Label' :
                provider.startsWith('worksheet') ? 'Worksheet Label' : 'Scoresheet Label');
    });
}
