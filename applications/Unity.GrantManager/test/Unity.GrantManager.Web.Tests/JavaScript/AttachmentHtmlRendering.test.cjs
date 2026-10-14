const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const webRoot = path.resolve(__dirname, '../../../src/Unity.GrantManager.Web');
const components = path.join(webRoot, 'Views/Shared/Components');
const sharedSource = readFileSync(path.join(components, '_Shared/Attachments.js'), 'utf8');
const dataTablesSource = readFileSync(path.join(webRoot, 'node_modules/datatables.net/js/dataTables.js'), 'utf8');

// Use the installed DataTables text renderer and cell writer. DOM/table/modal
// adapters capture output and callbacks; they do not parse browser HTML, run
// ABP's table normalization, persist changes, or download files.
const escapeSource = dataTablesSource.match(/var _escapeHtml = function \( d \) \{[\s\S]*?\n\t\};/)?.[0];
const textSource = dataTablesSource.match(/text: function \(\) \{[\s\S]*?\n\t\t\}/)?.[0];
const writeCellSource = dataTablesSource.match(/function _fnWriteCell\(td, val\)[\s\S]*?\n\t\}/)?.[0];
assert.ok(escapeSource && textSource && writeCellSource, 'Locate the installed DataTables implementations');

const owners = {
    Assessment: ['#AssessmentId', 'assessment-owner'],
    Application: ['#DetailsViewApplicationId', 'application-owner'],
    Applicant: ['#DetailsViewApplicantId', 'applicant-owner']
};

function harness() {
    const ready = [];
    const handlers = new Map();
    const subscriptions = new Map();
    const modals = [];
    const events = [];
    let configuration;
    let reloads = 0;
    const table = { on() { return this; }, ajax: { reload() { reloads++; } } };
    const document = {};
    const nodes = new Map();
    function $(selector) {
        if (typeof selector === 'function') { ready.push(selector); return; }
        if (selector?.attributes) return { attr(name) { return selector.attributes[name]; } };
        if (!nodes.has(selector)) {
            nodes.set(selector, {
                length: 0,
                on(event, delegatedSelector, callback) {
                    if (selector === document) handlers.set(delegatedSelector, callback);
                    return this;
                },
                one() { return this; },
                DataTable(options) { configuration = options; return table; },
                val() { return Object.values(owners).find(([id]) => id === selector)?.[1]; }
            });
        }
        return nodes.get(selector);
    }
    const context = vm.createContext({
        $, document, console, xssExecuted: false,
        abp: {
            currentUser: { id: 'current-user' },
            localization: { getResource() { return key => key; } },
            libs: { datatables: { normalizeConfiguration: value => value, createAjax() {} } },
            notify: { success() {} },
            ModalManager: class {
                constructor(options) { this.options = options; modals.push(this); }
                onResult(callback) { this.result = callback; }
                open(args) { this.args = { ...args }; }
            }
        },
        unity: { grantManager: { attachments: { attachment: {} } } },
        PubSub: {
            subscribe(topic, callback) { subscriptions.set(topic, callback); },
            publish(topic) { events.push(topic); }
        },
        initializeFilterRowPlugin() {},
        getSelectColumn() { return {}; }
    });
    vm.runInContext(`${escapeSource}\n${writeCellSource}\nglobalThis.textRendererFactory = ({${textSource}}).text;`, context);
    $.fn = { dataTable: { render: { text: context.textRendererFactory } } };
    vm.runInContext(sharedSource, context, { filename: 'Attachments.js' });
    return {
        context, modals, events, subscriptions,
        get configuration() { return configuration; },
        get reloads() { return reloads; },
        load(widget) {
            const file = `${widget}/${widget}.js`;
            vm.runInContext(readFileSync(path.join(components, file), 'utf8'), context, { filename: file });
            ready.splice(0).forEach(callback => callback());
        },
        click(button) {
            context.button = button;
            if (button.attributes.class?.includes('js-preview-attachment')) {
                handlers.get('.js-preview-attachment').call(button);
            } else {
                assert.ok(button.attributes.onclick, 'An enabled action must have a handler');
                assert.match(button.attributes.onclick, /^\w+\(this\.dataset\./);
                vm.runInContext(`(function () { ${button.attributes.onclick} }).call(button);`, context);
            }
            assert.equal(context.xssExecuted, false);
        }
    };
}

// Decode only the entities emitted by the production attribute encoder, once.
// This is a test adapter for generated attributes, not a sanitizer or HTML parser.
function decode(value) {
    const entities = { '&amp;': '&', '&lt;': '<', '&gt;': '>', '&quot;': '"', '&#39;': "'" };
    return value.replace(/&(?:amp|lt|gt|quot|#39);/g, entity => entities[entity]);
}

function elements(html, tag) {
    return [...html.matchAll(new RegExp(`<${tag}\\b([^>]*)>([\\s\\S]*?)<\\/${tag}>`, 'g'))].map(match => {
        const attributes = Object.fromEntries([...match[1].matchAll(/([\w-]+)="([^"]*)"/g)]
            .map(([, name, value]) => [name, decode(value)]));
        const dataset = Object.fromEntries(Object.entries(attributes)
            .filter(([name]) => name.startsWith('data-'))
            .map(([name, value]) => [name.slice(5).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase()), value]));
        return { attributes, dataset, content: match[2], disabled: /\sdisabled(?:\s|$)/.test(match[1]) };
    });
}

const specialRows = [
    { fileName: "Reviewer's report.pdf", displayName: 'A & B < C > D "quoted"', s3ObjectKey: 'object-key', id: 'attachment-id' },
    { fileName: "');globalThis.xssExecuted = true;//.pdf", displayName: '<img src=x onerror=alert(1)>', s3ObjectKey: 'key" onmouseover="alert(1)', id: 'attachment-id' },
    { fileName: '&lt;report&gt; &#39; résumé.pdf', displayName: '', s3ObjectKey: 'key & "quoted"', id: 'id" onclick="alert(1)' }
];

for (const [attachmentType, [, ownerId]] of Object.entries(owners)) {
    test(`${attachmentType} attachment actions preserve special metadata as data`, () => {
        for (const row of specialRows) {
            const view = harness();
            const html = view.context.generateAttachmentButtonContent(row.s3ObjectKey, 'display', {
                ...row, creatorId: 'current-user'
            }, {}, attachmentType);
            assert.equal((html.match(/<button\b/g) || []).length, 5);
            assert.equal((html.match(/<a\b/g) || []).length, 1);
            assert.doesNotMatch(html, /<img|onmouseover="|onclick="alert/);
            const buttons = elements(html, 'button');
            const preview = buttons.find(button => button.content.includes('Preview Attachment'));
            const edit = buttons.find(button => button.content.includes('Edit Attachment'));
            const remove = buttons.find(button => button.content.includes('Delete Attachment'));
            const download = elements(html, 'a')[0];
            assert.equal(download.attributes.download, row.s3ObjectKey);
            assert.equal(download.attributes.href,
                `/api/app/attachment/${attachmentType}/${ownerId}/download/${encodeURIComponent(row.fileName)}`);
            view.click(preview);
            view.click(edit);
            view.click(remove);
            assert.deepEqual(view.modals.map(modal => modal.args), [
                { attachmentType, ownerId, fileName: row.fileName, displayName: row.displayName || row.fileName },
                { attachmentType, attachmentId: row.id },
                { s3ObjectKey: row.s3ObjectKey, fileName: row.fileName, attachmentType, attachmentTypeId: ownerId }
            ]);
            assert.deepEqual(view.modals.map(modal => modal.options.viewUrl), [
                '../Attachments/PreviewAttachmentModal', '../Attachments/UpdateAttachmentModal', '../Attachments/DeleteAttachmentModal'
            ]);
            view.modals[1].result();
            view.modals[2].result();
            assert.deepEqual(view.events, Array(2).fill(`refresh_${attachmentType.toLowerCase()}_attachment_list`));
        }
    });
}

test('attachment deletion remains disabled for a different creator', () => {
    const view = harness();
    const html = view.context.generateAttachmentButtonContent('key', 'display', {
        ...specialRows[0], creatorId: 'another-user'
    }, {}, 'Assessment');
    const button = elements(html, 'button').find(item => item.content.includes('Delete Attachment'));
    assert.equal(button.disabled, true);
    assert.equal(button.attributes.onclick, undefined);
});

function render(column, data, type) {
    if (typeof column.render === 'function') return column.render(data, type, {}, {});
    return column.render?.[type] ? column.render[type](data) : data;
}

const widgets = [
    ['AssessmentResultAttachments', 'assessment'], ['ApplicationAttachments', 'application'],
    ['ApplicantAttachments', 'applicant'], ['ChefsAttachments', 'chefs']
];
for (const [widget, refreshType] of widgets) {
    test(`${widget} encodes display/filter text while preserving raw sort values`, () => {
        const view = harness();
        view.load(widget);
        const textColumns = view.configuration.columnDefs.filter(column => ['fileName', 'displayName', 'attachedBy'].includes(column.data));
        assert.equal(textColumns.length, widget === 'ChefsAttachments' ? 2 : 3);
        const raw = 'O\'Brien & <img src=x onerror=alert(1) title="quote"> &lt;literal&gt;';
        const escaped = 'O\'Brien &amp; &lt;img src=x onerror=alert(1) title=&quot;quote&quot;&gt; &amp;lt;literal&amp;gt;';
        for (const column of textColumns) {
            for (const type of ['display', 'filter']) assert.equal(render(column, raw, type), escaped);
            for (const type of ['sort', 'type', undefined]) assert.equal(render(column, raw, type), raw);
            // Capture the installed DataTables writer's HTML assignment: safe text
            // is what enters the table and is subsequently copied into print HTML.
            const cell = {};
            view.context._fnWriteCell(cell, render(column, raw, 'display'));
            assert.equal(cell.innerHTML, escaped);
        }
        view.subscriptions.get(`refresh_${refreshType}_attachment_list`)();
        assert.equal(view.reloads, 1);
    });

    test(`${widget} preserves label placeholders, empty labels, and ordinary text`, () => {
        const view = harness();
        view.load(widget);
        const label = view.configuration.columnDefs.find(column => column.data === 'displayName');
        for (const [value, expected] of [[null, '—'], [undefined, '—'], ['', ''], ['Ordinary label', 'Ordinary label']]) {
            for (const type of ['display', 'filter', 'sort', 'type']) assert.equal(render(label, value, type), expected);
        }
    });
}

test('CHEFS edit actions use encoded metadata and retain the CHEFS refresh event', () => {
    const view = harness();
    view.load('ChefsAttachments');
    const row = {
        ...specialRows[2], chefsSubmissionId: 'submission/id', chefsFileId: 'file/id'
    };
    const column = view.configuration.columnDefs.find(item => item.data === 'chefsFileId');
    const html = column.render(row.chefsFileId, 'display', row, {});
    const buttons = elements(html, 'button');
    assert.equal(buttons.length, 4);
    const preview = buttons.find(button => button.content.includes('Preview Attachment'));
    assert.equal(preview.attributes['chefs-file-name'], row.fileName);
    assert.equal(preview.attributes['chefs-display-name'], row.fileName);
    assert.equal(preview.attributes['chefs-submission-id'], encodeURIComponent(row.chefsSubmissionId));
    const edit = buttons.find(button => button.content.includes('Edit Attachment'));
    view.click(edit);
    assert.deepEqual(view.modals[0].args, { attachmentType: 'CHEFS', attachmentId: row.id });
    view.modals[0].result();
    assert.deepEqual(view.events, ['refresh_chefs_attachment_list']);
});
