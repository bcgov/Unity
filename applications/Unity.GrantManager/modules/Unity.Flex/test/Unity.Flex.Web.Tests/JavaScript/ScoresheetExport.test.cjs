const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { before, test } = require('node:test');
const vm = require('node:vm');

const componentDirectory = path.resolve(__dirname,
    '../../../src/Unity.Flex.Web/Views/Shared/Components/Scoresheet');
const view = readFileSync(path.join(componentDirectory, 'Default.cshtml'), 'utf8');
const source = readFileSync(path.join(componentDirectory, 'Scoresheet.js'), 'utf8');
const buttonMarkup = view.match(/<button\b[^>]*\bid="export_scoresheet_btn"[^>]*>/s)?.[0];
const clickHandler = buttonMarkup?.match(/\bonclick="([^"]*)"/)?.[1];

before(() => {
    assert.ok(buttonMarkup, 'The shared scoresheet view must contain the export button');
    assert.ok(clickHandler, 'The export button must have a click handler');
});

test('export metadata uses Razor-encoded data attributes instead of JavaScript interpolation', () => {
    assert.match(buttonMarkup, /\bdata-scoresheet-id="@scoresheet\.Id"/);
    assert.match(buttonMarkup, /\bdata-scoresheet-name="@scoresheet\.Name"/);
    assert.match(buttonMarkup, /\bdata-scoresheet-title="@scoresheet\.Title"/);
    assert.doesNotMatch(clickHandler, /@/);
});

// Verify the view's fixed handler, then call the production export function with
// decoded dataset values. These adapters do not render Razor, parse HTML, or
// simulate browser downloads. Attribute encoding remains a manual check.
function exportsHarness() {
    const requests = [];
    const downloads = [];
    const revokedUrls = [];
    const context = {
        xssExecuted: false,
        $() {}, // Export does not require the preview's ready callbacks.
        abp: { ModalManager: class { onResult() {} } },
        console,
        document: {
            createElement(tag) {
                assert.equal(tag, 'a');
                return {
                    style: {},
                    click() { downloads.push({ href: this.href, name: this.download }); }
                };
            },
            body: { appendChild() {} }
        },
        window: {
            URL: {
                createObjectURL(blob) {
                    assert.equal(blob, 'scoresheet-json');
                    return 'blob:scoresheet-export';
                },
                revokeObjectURL(url) { revokedUrls.push(url); }
            }
        },
        async fetch(url) {
            requests.push(url);
            return { ok: true, async blob() { return 'scoresheet-json'; } };
        }
    };
    vm.createContext(context);
    // Trusted test input: only Scoresheet.js from the fixed repository path above.
    // VM separates browser globals; it is not a sandbox for untrusted source code.
    vm.runInContext(source, context, { filename: 'Scoresheet.js' });
    return {
        requests, downloads, revokedUrls,
        async click(dataset) {
            assert.equal(clickHandler,
                'exportScoresheet(this.dataset.scoresheetId, this.dataset.scoresheetName, this.dataset.scoresheetTitle);');
            context.exportScoresheet(dataset.scoresheetId, dataset.scoresheetName, dataset.scoresheetTitle);
            await new Promise(resolve => setImmediate(resolve));
            assert.equal(context.xssExecuted, false, 'Metadata must not execute as JavaScript');
        }
    };
}

const scoresheetId = '11111111-1111-1111-1111-111111111111';
const cases = [
    { label: 'ordinary metadata', name: 'application-v1', title: 'Application Review' },
    { label: 'apostrophes', name: "reviewer's-v1", title: "Reviewer's scoresheet" },
    { label: 'quotes and markup', name: 'review-v1', title: 'A & B < C > D "quoted"' },
    { label: 'literal entities and Unicode', name: 'review-v2', title: 'Café &lt;review&gt; &#39;' },
    { label: 'script-shaped title', name: 'review-v1', title: "');globalThis.xssExecuted = true;//" },
    { label: 'script-shaped name', name: "');globalThis.xssExecuted = true;//", title: 'Review' },
    { label: 'attribute-shaped title', name: 'review-v1', title: '"><img src=x onerror=alert(1)>' }
];

for (const { label, name, title } of cases) {
    test(`export preserves ${label} as filename data`, async () => {
        const harness = exportsHarness();
        await harness.click({ scoresheetId, scoresheetName: name, scoresheetTitle: title });
        assert.deepEqual(harness.requests, [`/api/app/scoresheet/export/${scoresheetId}`]);
        assert.deepEqual(harness.downloads, [{
            href: 'blob:scoresheet-export', name: `scoresheet_${title}_${name}.json`
        }]);
        assert.deepEqual(harness.revokedUrls, ['blob:scoresheet-export']);
    });
}

test('a refreshed button exports its own updated metadata and ID once per click', async () => {
    const harness = exportsHarness();
    await harness.click({ scoresheetId, scoresheetName: 'old-v1', scoresheetTitle: 'Old' });
    const refreshedId = '22222222-2222-2222-2222-222222222222';
    await harness.click({
        scoresheetId: refreshedId, scoresheetName: 'new-v2', scoresheetTitle: "Updated reviewer's title"
    });
    assert.deepEqual(harness.requests, [
        `/api/app/scoresheet/export/${scoresheetId}`, `/api/app/scoresheet/export/${refreshedId}`
    ]);
    assert.deepEqual(harness.downloads.map(download => download.name), [
        'scoresheet_Old_old-v1.json', "scoresheet_Updated reviewer's title_new-v2.json"
    ]);
});
