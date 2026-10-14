const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const webRoot = path.resolve(__dirname, '../../../src/Unity.GrantManager.Web');
const previews = [
    { name: 'form configuration', file: 'wwwroot/js/formConfiguration/NotificationsPreview.js' },
    { name: 'notifications widget', file: 'Views/Shared/Components/Notifications/Default.js', privateRenderer: true }
];

// These adapters test the integration with the sanitizer, not DOMPurify itself.
// Parser fixtures explicitly supply the text a browser would return; no mock
// HTML parser or sanitizer is used to claim that an attack payload is safe.
function previewHarness(preview, { decodedText = new Map(), sanitizer = 'supported', sanitizedHtml = '' } = {}) {
    const htmlWrites = [];
    const sanitizerCalls = [];
    function element(tagName) {
        return {
            tagName, children: [], textContent: '',
            append(...children) { this.children.push(...children); },
            replaceChildren(...children) { this.children = children; },
            set innerHTML(value) { htmlWrites.push({ element: this, value }); }
        };
    }
    const context = {
        document: { createElement: element, addEventListener() {} },
        DOMParser: class {
            parseFromString(value, type) {
                assert.equal(type, 'text/html');
                assert.ok(decodedText.has(value), `Missing parser fixture: ${value}`);
                return { documentElement: { textContent: decodedText.get(value) } };
            }
        }
    };
    if (sanitizer !== 'missing') {
        context.DOMPurify = {
            isSupported: sanitizer === 'supported',
            sanitize(html, options) {
                assert.equal(sanitizer, 'supported', 'Unsupported sanitizer must not be called');
                sanitizerCalls.push({ html, options: JSON.parse(JSON.stringify(options)) });
                return sanitizedHtml;
            }
        };
    }

    let source = readFileSync(path.join(webRoot, preview.file), 'utf8');
    if (preview.privateRenderer) {
        // Expose the existing private renderer only inside this test's VM.
        // The production function body and its helpers execute unchanged.
        assert.ok(source.startsWith('(function () {'));
        source = source.replace('(function () {',
            '(function () { globalThis.renderNotificationTemplatePreview = renderTemplatePreview;');
    }
    vm.runInNewContext(source, context, { filename: preview.file });
    const container = element('div');
    return {
        container, htmlWrites, sanitizerCalls,
        render(template, target = container) { context.renderNotificationTemplatePreview(target, template); }
    };
}

const rawBody = '<p>Hello <strong>team</strong></p><img src="x" onerror="alert(1)"><a href="javascript:alert(1)">Link</a><script>alert(2)</script>';
const cleanBody = '<p>Hello <strong>team</strong></p><img src="x"><a>Link</a>';
const bodyText = 'Hello teamLinkalert(2)';
function encode(value) {
    return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
}
const encodedBody = encode(rawBody);
const doubleEncodedBody = encode(encodedBody);
const decodedText = new Map([
    [rawBody, bodyText], [bodyText, bodyText],
    [encodedBody, rawBody], [doubleEncodedBody, encodedBody]
]);

for (const preview of previews) {
    for (const [label, body] of [['raw', rawBody], ['encoded', encodedBody], ['double encoded', doubleEncodedBody]]) {
        test(`${preview.name}: sanitizes ${label} HTML after decoding and inserts only sanitizer output`, () => {
            const view = previewHarness(preview, { decodedText, sanitizedHtml: cleanBody });
            const subject = '<img src="x" onerror="alert(3)">';
            view.render({ subject, body });
            assert.deepEqual(view.sanitizerCalls, [{ html: rawBody, options: { USE_PROFILES: { html: true } } }]);
            assert.equal(view.htmlWrites.length, 1);
            assert.equal(view.htmlWrites[0].element, view.container.children[2]);
            assert.equal(view.htmlWrites[0].value, cleanBody);
            assert.equal(view.container.children[0].children[1].textContent, subject);
        });
    }

    for (const sanitizer of ['missing', 'unsupported']) {
        test(`${preview.name}: renders decoded markup as text when DOMPurify is ${sanitizer}`, () => {
            const view = previewHarness(preview, { decodedText, sanitizer });
            view.render({ body: doubleEncodedBody });
            assert.equal(view.htmlWrites.length, 0, 'Fallback must never write raw HTML');
            assert.equal(view.sanitizerCalls.length, 0);
            assert.equal(view.container.children[2].textContent, rawBody);
        });
    }

    test(`${preview.name}: preserves supported body and subject property names`, () => {
        for (const key of ['body', 'bodyHtml', 'bodyHTML', 'Body', 'BodyHtml', 'BodyHTML']) {
            const view = previewHarness(preview, { decodedText, sanitizedHtml: cleanBody });
            view.render({ Subject: 'Subject', [key]: rawBody });
            assert.equal(view.sanitizerCalls[0].html, rawBody, key);
            assert.equal(view.htmlWrites[0].value, cleanBody, key);
            assert.equal(view.container.children[0].children[1].textContent, 'Subject', key);
        }
    });

    test(`${preview.name}: preserves plain text and clears the previous preview`, () => {
        const plainText = 'Hello team';
        const view = previewHarness(preview, {
            decodedText: new Map([[plainText, plainText]]), sanitizedHtml: plainText
        });
        view.container.append({ stale: true });
        view.render({ body: plainText });
        assert.equal(view.container.children.length, 3);
        assert.equal(view.htmlWrites[0].value, plainText);
        assert.equal(view.sanitizerCalls[0].html, plainText);
        view.render(null);
        assert.equal(view.container.children.length, 0);
        assert.equal(view.sanitizerCalls.length, 1);
        assert.doesNotThrow(() => view.render({ body: rawBody }, null));
    });

    test(`${preview.name}: handles an empty template body`, () => {
        const view = previewHarness(preview);
        view.render({ subject: 'Empty body' });
        assert.equal(view.sanitizerCalls[0].html, '');
        assert.equal(view.htmlWrites[0].value, '');
    });
}
