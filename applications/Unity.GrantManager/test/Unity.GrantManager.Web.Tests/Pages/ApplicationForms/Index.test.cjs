const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const script = readFileSync(path.resolve(__dirname, '../../../../src/Unity.GrantManager.Web/Pages/ApplicationForms/Index.js'), 'utf8');

function initialize({ granted = true, confirmed = true, deletionFails = false } = {}) {
    const calls = { deletes: [], notifications: [], reloads: 0, permissions: [] };
    let configuration;
    const table = { ajax: { reload() { calls.reloads++; } } };
    const $ = argument => typeof argument === 'function' ? argument() : {};
    $.fn = { dataTable: { Buttons: { defaults: { dom: { button: {} } } } } };
    const context = {
        $,
        abp: {
            appPath: '/',
            localization: { getResource: () => (key, ...args) => [key, ...args].join(':') },
            auth: { isGranted(permission) { calls.permissions.push(permission); return granted; } },
            ModalManager: function () { this.onResult = () => {}; },
            message: { confirm: async () => confirmed },
            notify: { success(message) { calls.notifications.push(message); } }
        },
        unity: { grantManager: { applicationForms: { applicationForm: {
            getList() {},
            delete(id) {
                calls.deletes.push(id);
                // ABP presents request errors globally; a failed request never invokes the success handler.
                return { then(success) { if (!deletionFails) success(); } };
            }
        } } } },
        commonTableActionButtons: () => [],
        initializeDataTable(config) { configuration = config; return table; }
    };
    vm.runInNewContext(script, context);
    const action = configuration.listColumns[0].rowAction.items.find(item => item.text === 'ApplicationForms:DeleteForm');
    return { action, calls };
}

test('Delete Form visibility uses the Delete Forms permission', () => {
    const { action, calls } = initialize({ granted: false });
    assert.equal(action.visible, false);
    assert.deepEqual(calls.permissions, ['GrantManagerManagement.ApplicationForms.Delete']);
});

test('cancelling deletion keeps the row and makes no request', async () => {
    const { action, calls } = initialize({ confirmed: false });
    action.action({ record: { id: 'form-id', applicationFormName: 'Existing form' } });
    await new Promise(setImmediate);
    assert.deepEqual(calls.deletes, []);
    assert.equal(calls.reloads, 0);
});

test('confirmed deletion refreshes the list and shows success', async () => {
    const { action, calls } = initialize();
    action.action({ record: { id: 'form-id', applicationFormName: 'Existing form' } });
    await new Promise(setImmediate);
    assert.deepEqual(calls.deletes, ['form-id']);
    assert.equal(calls.reloads, 1);
    assert.deepEqual(calls.notifications, ['ApplicationForms:DeletedSuccessfully']);
});

test('a failed deletion keeps the row and does not show success', async () => {
    const { action, calls } = initialize({ deletionFails: true });
    action.action({ record: { id: 'form-id', applicationFormName: 'Existing form' } });
    await new Promise(setImmediate);
    assert.equal(calls.reloads, 0);
    assert.deepEqual(calls.notifications, []);
});
