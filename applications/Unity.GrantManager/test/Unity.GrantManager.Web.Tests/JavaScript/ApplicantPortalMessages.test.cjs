const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const source = readFileSync(path.resolve(__dirname,
    '../../../src/Unity.GrantManager.Web/Pages/ApplicantPortalSettings/Messages.js'), 'utf8');
const defaultMessage = {
    html: '<p>Multiple applicants. Contact {inboxEmail}.</p>',
    text: 'Multiple applicants. Contact {inboxEmail}.'
};
const defaultHtml = defaultMessage.html;
const savedCustomMessage = { html: '<p>Saved custom</p>', text: 'Saved custom' };

// Run the actual page handlers with adapters for jQuery, TinyMCE, and the API.
function page({ useDefault = true, message = defaultMessage, customMessage = useDefault ? null : message } = {}) {
    const nodes = new Map();
    const requests = [];
    const editorEvents = new Map();
    // TinyMCE supplies HTML and plain text separately. Fixtures provide both;
    // this stub does not parse or sanitize HTML.
    const plainTextByHtml = new Map();
    function registerMessage({ html, text }) {
        assert.equal(typeof html, 'string', 'Message fixtures must provide HTML');
        assert.equal(typeof text, 'string', 'Message fixtures must provide plain text');
        plainTextByHtml.set(html, text);
    }
    registerMessage(defaultMessage);
    registerMessage(message);
    if (customMessage) registerMessage(customMessage);
    let pending;
    function node(selector) {
        if (nodes.has(selector)) return nodes.get(selector);
        const events = new Map();
        const item = {
            id: selector.substring(1), value: '', textValue: '', attrs: {}, props: {}, classes: new Set(),
            val(value) { if (!arguments.length) return this.value; this.value = value; return this; },
            text(value) { if (!arguments.length) return this.textValue; this.textValue = value; return this; },
            attr(key, value) { if (arguments.length === 1) return this.attrs[key]; this.attrs[key] = value; return this; },
            prop(key, value) { if (arguments.length === 1) return this.props[key]; this.props[key] = value; return this; },
            toggleClass(key, enabled) { if (enabled) this.classes.add(key); else this.classes.delete(key); return this; },
            on(names, handler) { for (const name of names.split(' ')) events.set(name, handler); return this; },
            trigger(name) { events.get(name)?.call(this, { preventDefault() {} }); return this; }
        };
        nodes.set(selector, item);
        return item;
    }
    function $(selector) {
        if (typeof selector !== 'string') return selector;
        if (!selector.includes(',')) return node(selector);
        const group = selector.split(',').map(value => node(value.trim()));
        return { on(event, handler) { group.forEach(item => item.on(event, handler)); return this; } };
    }
    function deferred(promise) {
        return {
            then(fn) { return deferred(promise.then(fn)); },
            catch(fn) { return deferred(promise.catch(fn)); },
            always(fn) { return deferred(promise.finally(fn)); }
        };
    }
    $('#PortalMessageForm').attr('data-use-default', String(useDefault)).attr('data-max-html-length', '2048');
    $('#MultipleIdentitiesMessage').val(message.html);
    $('#DefaultMultipleIdentitiesMessage').val(defaultHtml);
    $('#CustomMultipleIdentitiesMessage').val(customMessage?.html ?? '');
    $('#UseDefaultMultipleIdentitiesMessage').prop('checked', useDefault);
    const editor = {
        html: message.html, currentMode: 'design', undoClears: 0,
        getContent(options) {
            if (options?.format !== 'text') return this.html;
            assert.ok(plainTextByHtml.has(this.html), 'Missing plain-text fixture for editor content');
            return plainTextByHtml.get(this.html);
        },
        setContent(value) { this.html = value; },
        on(names, handler) { for (const name of names.split(' ')) editorEvents.set(name, handler); },
        getBody() { return { setAttribute() {} }; },
        focus() {},
        mode: { set(value) { editor.currentMode = value; } },
        undoManager: { clear() { editor.undoClears++; } }
    };
    const context = {
        jQuery: $, Promise,
        abp: {
            localization: { getResource: () => (key, ...args) => [key, ...args].join(' ') },
            ui: { setBusy() {}, clearBusy() {} },
            notify: { success() {} }
        },
        tinymce: { init(options) { options.setup(editor); editorEvents.get('init')(); return Promise.resolve([editor]); } },
        unity: { grantManager: { applicantPortal: { applicantPortalMessage: {
            updateMultipleIdentities(input) {
                requests.push(input);
                return deferred(new Promise((resolve, reject) => { pending = { resolve, reject }; }));
            }
        } } } }
    };
    vm.runInNewContext(source, context);
    $('#manage-messages-menu-item').trigger('click');
    return {
        $, editor, requests,
        type(html, text) {
            registerMessage({ html, text });
            editor.setContent(html);
            editorEvents.get('input')();
        },
        toggle(value) { $('#UseDefaultMultipleIdentitiesMessage').prop('checked', value).trigger('change'); },
        save() { $('#PortalMessageForm').trigger('submit'); },
        async success() { pending.resolve(requests.at(-1)); await new Promise(setImmediate); },
        async failure() { pending.reject({ message: 'Save failed' }); await new Promise(setImmediate); }
    };
}

test('default mode is read-only and switching off starts with the default text', () => {
    const view = page();
    assert.equal(view.editor.currentMode, 'readonly');
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);
    view.$('#ResetPortalMessageButton').trigger('click');
    assert.equal(view.$('#UseDefaultMultipleIdentitiesMessage').prop('checked'), true);
    assert.equal(view.editor.currentMode, 'readonly');
    view.toggle(false);
    assert.equal(view.editor.currentMode, 'design');
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), false);
});

test('switching default on and off restores the latest custom draft and clears undo history', () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.type('<p>Unsaved custom</p>', 'Unsaved custom');
    const previousClears = view.editor.undoClears;
    view.toggle(true);
    assert.equal(view.editor.html, defaultHtml);
    view.toggle(false);
    assert.equal(view.editor.html, '<p>Unsaved custom</p>');
    assert.ok(view.editor.undoClears > previousClears);
    assert.equal(view.requests.length, 0);
});

test('a page loaded in default mode restores the retained custom message when switched off', () => {
    const view = page({ customMessage: savedCustomMessage });
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.editor.currentMode, 'readonly');
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);
    view.toggle(false);
    assert.equal(view.editor.html, '<p>Saved custom</p>');
    assert.equal(view.editor.currentMode, 'design');
});

test('saving default mode retains the latest custom draft across a page reload', async () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.type('<p>Edited custom</p>', 'Edited custom');
    view.toggle(true);
    view.save();
    assert.equal(view.requests[0].useDefaultMessage, true);
    assert.equal(view.requests[0].messageHtml, defaultHtml);
    assert.equal(view.requests[0].customMessageHtml, '<p>Edited custom</p>');
    await view.success();
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);

    const reloaded = page({ useDefault: true, customMessage: {
        html: view.requests[0].customMessageHtml, text: 'Edited custom'
    } });
    reloaded.toggle(false);
    assert.equal(reloaded.editor.html, '<p>Edited custom</p>');
});

test('Reset in default mode leaves the retained custom message intact', () => {
    const view = page({ customMessage: savedCustomMessage });
    view.$('#ResetPortalMessageButton').trigger('click');
    assert.equal(view.$('#UseDefaultMultipleIdentitiesMessage').prop('checked'), true);
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);
    view.toggle(false);
    assert.equal(view.editor.html, '<p>Saved custom</p>');
});

test('Cancel restores both default mode and the previously saved custom template', () => {
    const view = page({ customMessage: savedCustomMessage });
    view.toggle(false);
    view.type('<p>Edited custom</p>', 'Edited custom');
    view.toggle(true);
    view.$('#CancelPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#UseDefaultMultipleIdentitiesMessage').prop('checked'), true);
    view.toggle(false);
    assert.equal(view.editor.html, '<p>Saved custom</p>');
});

test('Reset in custom mode replaces the retained draft with default text across toggles', () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.$('#ResetPortalMessageButton').trigger('click');
    view.toggle(true);
    view.toggle(false);
    assert.equal(view.editor.html, defaultHtml);
    view.$('#CancelPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, '<p>Saved custom</p>');
});

test('invalid retained custom text blocks Save and explains how to correct it', () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.type('<p>' + 'a'.repeat(2048) + '</p>', 'a'.repeat(2048));
    view.toggle(true);
    view.save();
    assert.equal(view.requests.length, 0);
    assert.match(view.$('#portal-message-error').text(), /RetainedMessageValidationHelp/);
    view.toggle(false);
    assert.equal(view.editor.html, '<p>' + 'a'.repeat(2048) + '</p>');
});

test('Reset restores default text while keeping custom mode, and Cancel restores the saved custom message', () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.$('#ResetPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, defaultHtml);
    assert.equal(view.$('#UseDefaultMultipleIdentitiesMessage').prop('checked'), false);
    assert.equal(view.editor.currentMode, 'design');
    assert.equal(view.requests.length, 0);
    view.$('#CancelPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, '<p>Saved custom</p>');
    assert.equal(view.editor.currentMode, 'design');
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);
});

test('Save persists the current draft and Cancel subsequently returns to that new baseline', async () => {
    const view = page();
    view.toggle(false);
    view.type('<p><strong>Custom</strong></p>', 'Custom');
    view.save();
    assert.equal(view.requests[0].useDefaultMessage, false);
    assert.equal(view.requests[0].messageHtml, '<p><strong>Custom</strong></p>');
    assert.equal(view.editor.currentMode, 'readonly');
    await view.success();
    assert.equal(view.editor.currentMode, 'design');
    view.type('<p>Another draft</p>', 'Another draft');
    view.$('#CancelPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, '<p><strong>Custom</strong></p>');
});

test('Reset followed by Save persists default text in custom mode', async () => {
    const view = page({ useDefault: false, message: savedCustomMessage });
    view.$('#ResetPortalMessageButton').trigger('click');
    view.save();
    assert.equal(view.requests[0].useDefaultMessage, false);
    assert.equal(view.requests[0].messageHtml, defaultHtml);
    await view.success();
    assert.equal(view.$('#UseDefaultMultipleIdentitiesMessage').prop('checked'), false);
    assert.equal(view.editor.currentMode, 'design');
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), true);
});

test('Save rejects visually empty HTML and HTML above the storage limit', () => {
    const view = page();
    view.toggle(false);
    for (const [html, text] of [
        ['', ''],
        [' ', ' '],
        ['<p><br></p>', ''],
        ['<p>&nbsp;\u200B</p>', '\u00A0\u200B'],
        ['<p>' + 'a'.repeat(2048) + '</p>', 'a'.repeat(2048)]
    ]) {
        view.type(html, text);
        view.save();
        assert.equal(view.$('#portal-message-error').prop('hidden'), false);
    }
    assert.equal(view.requests.length, 0);
    view.type('<p>' + 'a'.repeat(2041) + '</p>', 'a'.repeat(2041));
    view.save();
    assert.equal(view.requests.length, 1);
});

test('failed Save preserves the draft, shows an error, and re-enables editing', async () => {
    const view = page();
    view.toggle(false);
    view.type('<p>Keep this draft</p>', 'Keep this draft');
    view.save();
    await view.failure();
    assert.equal(view.editor.html, '<p>Keep this draft</p>');
    assert.equal(view.editor.currentMode, 'design');
    assert.equal(view.$('#portal-message-error').text(), 'Save failed');
    assert.equal(view.$('#SavePortalMessageButton').prop('disabled'), false);
    view.$('#CancelPortalMessageButton').trigger('click');
    assert.equal(view.editor.html, defaultHtml);
});

test('switching configuration sections preserves the message draft and sends no save request', () => {
    const view = page({ useDefault: false, message: { html: '<p>Saved</p>', text: 'Saved' } });
    view.type('<p>Draft</p>', 'Draft');
    view.$('#manage-statuses-menu-item').trigger('click');
    assert.equal(view.$('#portal-messages-div').classes.has('d-none'), true);
    view.$('#manage-messages-menu-item').trigger('click');
    assert.equal(view.editor.html, '<p>Draft</p>');
    assert.equal(view.requests.length, 0);
});
