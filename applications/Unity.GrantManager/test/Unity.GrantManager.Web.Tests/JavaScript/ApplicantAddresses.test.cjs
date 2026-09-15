const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const source = readFileSync(path.resolve(__dirname,
    '../../../src/Unity.GrantManager.Web/Views/Shared/Components/ApplicantAddresses/Default.js'), 'utf8');
const zoneFormSource = readFileSync(path.resolve(__dirname,
    '../../../modules/Unity.Theme.UX2/src/Unity.Theme.UX2/wwwroot/themes/ux2/zone-extensions.js'), 'utf8');
const emptyId = '00000000-0000-0000-0000-000000000000';
const applicationId = '11111111-1111-1111-1111-111111111111';
const addressId = '22222222-2222-2222-2222-222222222222';

// Exercise the widget and real change tracker with small DOM/AJAX/DataTables adapters.
// The table adapter invokes the configured renderers on initialization and redraw.
function widget({ addresses = [], values = {} } = {}) {
    const nodes = new Map();
    const fields = [];
    const calls = [];
    const warnings = [];
    const errors = [];
    const tableRows = [];
    let renderedRows = [];
    let tableConfig;
    let tracking;
    let pending;

    function element(selector, value = '') {
        const attributes = {};
        const handlers = new Map();
        const classes = new Set();
        const node = Object.assign(Object.create($.prototype), {
            length: 1, value, disabled: false, removed: false,
            val(next) {
                if (arguments.length === 0) return this.value;
                this.value = next;
                return this;
            },
            prop(name, next) {
                if (arguments.length === 1) return this[name];
                this[name] = next;
                return this;
            },
            attr(name, next) {
                if (arguments.length === 1) return attributes[name];
                attributes[name] = next;
                return this;
            },
            removeAttr(name) { delete attributes[name]; return this; },
            addClass(name) { classes.add(name); return this; },
            removeClass(name) { classes.delete(name); return this; },
            is(query) { return query === 'form' && selector === '#ApplicantAddressesForm'; },
            on(event, handler) {
                if (!handlers.has(event)) handlers.set(event, []);
                handlers.get(event).push(handler);
                return this;
            },
            trigger(event) {
                for (const handler of handlers.get(event) || []) {
                    handler.call(this, { target: this, preventDefault() {} });
                }
                return this;
            },
            remove() { this.removed = true; }
        });
        nodes.set(selector, node);
        return node;
    }

    const form = element('#ApplicantAddressesForm');
    const save = element('#saveApplicantAddressesBtn');
    element('#ApplicantAddresses_Data', JSON.stringify(addresses));
    element('#ApplicantAddresses_ApplicantId', '33333333-3333-3333-3333-333333333333');
    element('#ApplicantAddresses_ExpectedApplicationId', applicationId);
    for (const prefix of ['PrimaryPhysicalAddress', 'PrimaryMailingAddress']) {
        element(`#ApplicantAddresses_${prefix}Id`, emptyId);
        element(`#${prefix}_CreationHint`);
        for (const field of ['Street', 'Street2', 'Unit', 'City', 'Province', 'PostalCode']) {
            const name = `${prefix}.${field}`;
            fields.push(element(`[name="${name}"]`, values[name] || '').attr('name', name));
        }
    }
    function collection(elements) {
        return {
            each(callback) { elements.forEach((node, index) => callback(index, node)); return this; },
            on(event, handler) { elements.forEach(node => node.on(event, handler)); return this; },
            prop(name, value) { elements.forEach(node => node.prop(name, value)); return this; }
        };
    }
    form.find = selector => {
        if (selector.startsWith('input:enabled')) {
            return collection(fields.filter(field => !field.disabled));
        }
        if (selector === 'input, select, textarea' || selector === 'input, textarea') {
            return collection(fields);
        }
        assert.ok(nodes.has(selector), `Unexpected selector: ${selector}`);
        return nodes.get(selector);
    };

    function $(selector) {
        if (typeof selector === 'function') return selector();
        if (selector instanceof $) return selector;
        assert.ok(nodes.has(selector), `Unexpected selector: ${selector}`);
        return nodes.get(selector);
    }
    $.fn = $.prototype;
    $.fn.DataTable = function () {};

    function renderTable() {
        renderedRows = tableRows.map(row => tableConfig.columnDefs.map(column =>
            column.render ? column.render(row[column.data], 'display', row) : row[column.data]));
        tableConfig.drawCallback.call({ api: () => table });
    }

    const table = {
        columns: { adjust() {} },
        row: { add(row) { tableRows.push(row); } },
        rows() {
            return {
                every(callback) {
                    tableRows.forEach((row, index) => callback.call({
                        data(value) {
                            if (arguments.length) tableRows[index] = value;
                            return tableRows[index];
                        }
                    }));
                },
                invalidate() { return { draw: renderTable }; }
            };
        }
    };
    element('#ApplicantAddressesTable').DataTable = config => {
        tableConfig = config;
        tableRows.push(...config.data);
        renderTable();
        return table;
    };

    const context = vm.createContext({
        $, jQuery: $, console, setTimeout() {},
        captureTracking(instance) { tracking = instance; },
        abp: {
            notify: { warn: message => warnings.push(message), error: message => errors.push(message), success() {} },
            libs: { datatables: { normalizeConfiguration: config => config } },
            utils: {
                // ABP exposes htmlEscape, not htmlEncode.
                htmlEscape: value => value.replace(/&/g, '&amp;').replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;').replace(/"/g, '&quot;')
            }
        },
        unity: { grantManager: { applicants: { applicant: {
            updateApplicantContactAddresses(applicant, payload) {
                calls.push({ applicant, payload });
                const callbacks = {};
                const request = {
                    done(callback) { callbacks.done = callback; return request; },
                    fail(callback) { callbacks.fail = callback; return request; },
                    always(callback) { callbacks.always = callback; return request; }
                };
                pending = {
                    resolve(result) { callbacks.done(result); callbacks.always(); },
                    reject(error) { callbacks.fail(error); callbacks.always(); }
                };
                return request;
            }
        } } } }
    });
    vm.runInContext(zoneFormSource, context);
    vm.runInContext(`
        const OriginalZoneForm = UnityZoneForm;
        UnityZoneForm = class extends OriginalZoneForm {
            constructor(...args) { super(...args); captureTracking(this); }
            // This address form has no numeric/currency fields.
            initializeNumericFields() {}
        };
    `, context);
    vm.runInContext(source, context);

    return {
        context, form, nodes, fields, calls, warnings, errors, tableRows,
        get renderedRows() { return renderedRows; },
        get tracking() { return tracking; },
        get pending() { return pending; },
        change(name, value) {
            nodes.get(`[name="${name}"]`).val(value).trigger('change');
        },
        type(name, value) { nodes.get(`[name="${name}"]`).val(value).trigger('input'); },
        save() { save.trigger('click'); }
    };
}

test('an untouched or whitespace-only new section creates no record', () => {
    const view = widget();
    view.save();
    view.change('PrimaryPhysicalAddress.Street', '   ');
    view.save();
    assert.equal(view.calls.length, 0);
});

test('creation sends the expected application and omits the untouched type', () => {
    const view = widget();
    view.change('PrimaryPhysicalAddress.Street', ' 123 Main St ');
    view.save();
    const payload = view.calls[0].payload;
    assert.equal(payload.expectedApplicationId, applicationId);
    assert.equal(payload.primaryPhysicalAddress.id, emptyId);
    assert.equal(payload.primaryPhysicalAddress.street, '123 Main St');
    assert.equal(payload.primaryMailingAddress, undefined);
});

test('Street 2 alone is sufficient and both types share the expected application', () => {
    const view = widget();
    view.change('PrimaryPhysicalAddress.Street', 'Physical');
    view.change('PrimaryMailingAddress.Street2', ' PO Box 123 ');
    view.save();
    assert.equal(view.calls[0].payload.primaryMailingAddress.street2, 'PO Box 123');
    assert.equal(view.calls[0].payload.expectedApplicationId, applicationId);
});

test('partial creation without a street line blocks the whole save and preserves changes', () => {
    const view = widget();
    view.change('PrimaryPhysicalAddress.Street', 'Physical');
    view.change('PrimaryMailingAddress.City', 'Victoria');
    view.save();
    assert.equal(view.calls.length, 0);
    assert.match(view.warnings[0], /Street or Street 2.*mailing/);
    assert.equal(view.tracking.modifiedFields.size, 2);
});

test('creation without an eligible application is blocked', () => {
    const view = widget();
    view.nodes.get('#ApplicantAddresses_ExpectedApplicationId').val('');
    view.change('PrimaryPhysicalAddress.Street', 'Physical');
    view.save();
    assert.equal(view.calls.length, 0);
    assert.equal(view.warnings[0], 'A submission is required before you can add an address.');
});

test('updates allow clearing an existing address without a creation application', () => {
    const view = widget({ values: { 'PrimaryPhysicalAddress.Street': 'Existing address' } });
    view.nodes.get('#ApplicantAddresses_ExpectedApplicationId').val('');
    view.nodes.get('#ApplicantAddresses_PrimaryPhysicalAddressId').val(addressId);
    view.change('PrimaryPhysicalAddress.Street', '');
    view.save();
    assert.equal(view.calls[0].payload.primaryPhysicalAddress.id, addressId);
    assert.equal(view.calls[0].payload.primaryPhysicalAddress.street, '');
    assert.equal(view.calls[0].payload.expectedApplicationId, undefined);
});

test('saving prevents repeat requests and restores only editable fields on failure', () => {
    const view = widget();
    const disabledField = view.nodes.get('[name="PrimaryMailingAddress.City"]');
    disabledField.disabled = true;
    view.change('PrimaryPhysicalAddress.Street', 'Physical');
    view.save();
    view.save();
    assert.equal(view.calls.length, 1);
    assert.ok(view.fields.every(field => field.disabled));
    view.pending.reject({ message: 'The latest submission has changed. Reload the page.' });
    assert.match(view.errors[0], /latest submission has changed/);
    assert.equal(view.tracking.modifiedFields.size, 1);
    assert.equal(view.nodes.get('[name="PrimaryPhysicalAddress.Street"]').value, 'Physical');
    assert.equal(view.nodes.get('[name="PrimaryPhysicalAddress.Street"]').disabled, false);
    assert.equal(disabledField.disabled, true);
    assert.equal(view.nodes.get('#saveApplicantAddressesBtn').disabled, false);
});

test('a successful creation stores the ID and normalized values for subsequent updates', () => {
    const view = widget();
    view.change('PrimaryPhysicalAddress.Street', ' Physical ');
    view.save();
    view.pending.resolve({ primaryPhysicalAddress: {
        id: addressId, street: 'Physical', postal: 'V8W 1A1', applicationId, referenceNo: 'TEST-123'
    } });
    assert.equal(view.nodes.get('#ApplicantAddresses_PrimaryPhysicalAddressId').value, addressId);
    assert.equal(view.nodes.get('[name="PrimaryPhysicalAddress.PostalCode"]').value, 'V8W 1A1');
    assert.equal(view.nodes.get('#PrimaryPhysicalAddress_CreationHint').removed, true);
    assert.equal(view.nodes.get('#PrimaryMailingAddress_CreationHint').removed, false);
    assert.equal(view.tracking.modifiedFields.size, 0);
    assert.equal(view.nodes.get('#saveApplicantAddressesBtn').disabled, true);

    view.change('PrimaryPhysicalAddress.City', 'Victoria');
    view.save();
    assert.equal(view.calls[1].payload.primaryPhysicalAddress.id, addressId);
    assert.equal(view.calls[1].payload.expectedApplicationId, undefined);
});

test('saved table rows are inserted then updated with their application link and no duplicates', () => {
    const view = widget();
    const rows = view.tableRows;
    const saved = { primaryPhysicalAddress: { id: addressId, street: 'Physical', applicationId, referenceNo: 'TEST-123', postal: 'V8W 1A1' } };
    view.change('PrimaryPhysicalAddress.Street', 'Physical');
    view.save();
    view.pending.resolve(saved);
    assert.equal(rows.length, 1);
    assert.equal(rows[0].applicationId, applicationId);
    assert.equal(rows[0].referenceNo, 'TEST-123');
    assert.equal(rows[0].addressType, 'Physical');
    assert.equal(rows[0].postal, 'V8W 1A1');
    assert.match(view.renderedRows[0][6], /TEST-123<\/a>/);
    view.change('PrimaryPhysicalAddress.Street', 'Updated');
    view.save();
    saved.primaryPhysicalAddress.street = 'Updated';
    view.pending.resolve(saved);
    assert.equal(rows.length, 1);
    assert.equal(rows[0].street, 'Updated');
});

for (const prefix of ['PrimaryPhysicalAddress', 'PrimaryMailingAddress']) {
    for (const field of ['Street', 'Street2']) {
        test(`typing ${prefix}.${field} enables Save without leaving the field`, () => {
            const view = widget();
            const save = view.nodes.get('#saveApplicantAddressesBtn');
            assert.equal(save.disabled, true);
            view.type(`${prefix}.${field}`, '123 Main St');
            assert.equal(save.disabled, false);
            assert.ok(view.tracking.modifiedFields.has(`${prefix}.${field}`));
            view.type(`${prefix}.${field}`, '');
            assert.equal(save.disabled, true);
        });
    }
}

test('existing rows render submission links without preventing Save initialization', () => {
    const view = widget({ addresses: [{
        id: addressId, addressType: 'Physical', street: 'Existing',
        applicationId, referenceNo: 'TEST-123'
    }] });
    assert.equal(view.tableRows.length, 1);
    assert.equal(view.renderedRows[0][6],
        `<a href="/GrantApplications/Details?ApplicationId=${applicationId}">TEST-123</a>`);
    view.change('PrimaryPhysicalAddress.Street', 'Changed');
    assert.equal(view.nodes.get('#saveApplicantAddressesBtn').disabled, false);
});

test('saved Street 2-only addresses are visible and submission labels are escaped', () => {
    const view = widget();
    view.change('PrimaryMailingAddress.Street2', 'PO Box 123');
    view.save();
    view.pending.resolve({ primaryMailingAddress: {
        id: addressId, street2: 'PO Box 123', applicationId, referenceNo: '<TEST & 123>'
    } });
    assert.equal(view.tableRows.length, 1);
    assert.equal(view.renderedRows[0][1], 'PO Box 123');
    assert.match(view.renderedRows[0][6], /&lt;TEST &amp; 123&gt;<\/a>/);
    assert.equal(view.nodes.get('#saveApplicantAddressesBtn').disabled, true);
});

test('saving both address types renders both rows and restores editing', () => {
    const view = widget();
    view.type('PrimaryPhysicalAddress.Street', '123 Main St');
    view.type('PrimaryMailingAddress.Street2', 'PO Box 123');
    view.save();
    view.pending.resolve({
        primaryPhysicalAddress: {
            id: addressId, street: '123 Main St', applicationId, referenceNo: 'TEST-123'
        },
        primaryMailingAddress: {
            id: '44444444-4444-4444-4444-444444444444', street2: 'PO Box 123',
            applicationId, referenceNo: 'TEST-123'
        }
    });

    assert.equal(view.tableRows.length, 2);
    assert.equal(view.renderedRows[0][1], '123 Main St');
    assert.equal(view.renderedRows[1][1], 'PO Box 123');
    assert.ok(view.fields.every(field => !field.disabled));
    assert.equal(view.tracking.modifiedFields.size, 0);
    view.type('PrimaryPhysicalAddress.Street', '456 Main St');
    assert.equal(view.nodes.get('#saveApplicantAddressesBtn').disabled, false);
});
