const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const source = readFileSync(path.resolve(__dirname,
    '../../../src/Unity.Flex.Web/Views/Shared/Components/Scoresheet/Scoresheet.js'), 'utf8');

// Execute the actual ready/refresh handlers and capture the HTML written to the
// preview. These adapters do not parse HTML or simulate browser script execution.
function preview(options, previewId = 'scoresheet-preview') {
    const readyHandlers = [];
    const subscriptions = new Map();
    const htmlWrites = [];
    const section = {
        innerText: 'Section', dataset: { label: 'Section' },
        classList: { contains: name => name === 'section-item' }
    };
    const question = {
        innerText: 'Select question',
        dataset: {
            id: '11111111-1111-1111-1111-111111111111', questiontype: 'SelectList',
            required: 'True', questionlabel: '', questiondesc: '',
            definition: JSON.stringify({ options })
        },
        classList: { contains: () => false }
    };
    const expandedBody = {
        querySelectorAll(selector) {
            assert.equal(selector, '.list-group-item');
            return [section, question];
        }
    };
    const nodes = new Map([
        [previewId, { set innerHTML(html) { htmlWrites.push(html); } }],
        ['scoresheet-accordion', {}],
        ['previewSaveAllBtn', {}],
        ['previewDiscardAllBtn', {}]
    ]);
    const context = {
        $(handler) { assert.equal(typeof handler, 'function'); readyHandlers.push(handler); },
        document: {
            getElementById(id) { return nodes.get(id) || null; },
            querySelectorAll(selector) {
                if (selector === '#scoresheet-accordion .accordion-collapse.show') return [expandedBody];
                assert.ok(['[id^="sections-questions"]', '#scoresheet-accordion .accordion-button'].includes(selector));
                return [];
            }
        },
        Sortable: class {},
        abp: { ModalManager: class { onResult() {} } },
        PubSub: { subscribe(topic, handler) { subscriptions.set(topic, handler); } },
        updateSubtotal() {}
    };
    vm.runInNewContext(source, context, { filename: 'Scoresheet.js' });
    readyHandlers.forEach(handler => handler());
    return {
        get html() { return htmlWrites.at(-1); },
        get writes() { return htmlWrites.length; },
        refresh(nextOptions) {
            question.dataset.definition = JSON.stringify({ options: nextOptions });
            subscriptions.get('refresh_scoresheet_configuration_page')();
        }
    };
}

const placeholder = '<option data-numeric-value="0" value="">Please choose...</option>';

function assertOption(html, { value, label = value, score = '0' }) {
    assert.ok(html.includes(`<option data-numeric-value="${score}" value="${value}" title="${value}">${label}</option>`),
        'Expected encoded attributes and text in the generated option');
}

const textCases = [
    {
        name: 'literal punctuation',
        value: `A & B < C > D "quoted" 'single'`,
        encoded: `A &amp; B &lt; C &gt; D &quot;quoted&quot; 'single'`
    },
    {
        name: 'markup and attribute breakout',
        value: '"></option></select><img src=x onerror=alert(1)><select><option value="',
        encoded: '&quot;&gt;&lt;/option&gt;&lt;/select&gt;&lt;img src=x onerror=alert(1)&gt;&lt;select&gt;&lt;option value=&quot;'
    },
    {
        name: 'literal entity references',
        value: '&lt;tag&gt; &#34; &quot;',
        encoded: '&amp;lt;tag&amp;gt; &amp;#34; &amp;quot;'
    }
];

for (const { name, value, encoded } of textCases) {
    test(`select-list preview escapes ${name} in option attributes and text`, () => {
        const view = preview([{ value, numeric_value: 0 }]);
        assertOption(view.html, { value: encoded });
        assert.equal((view.html.match(/<option\b/g) || []).length, 2);
        assert.equal((view.html.match(/<select\b/g) || []).length, 1);
        assert.equal(view.html.includes('<img'), false);
    });
}

test('select-list preview escapes the numeric-value attribute even for malformed input', () => {
    const view = preview([{ value: 'Score', numeric_value: '0" onmouseover="alert(1)' }]);
    assertOption(view.html, { value: 'Score', score: '0&quot; onmouseover=&quot;alert(1)' });
    assert.equal(view.html.includes(' onmouseover="'), false);
});

test('select-list preview preserves option order, full values, and zero/negative/positive scores', () => {
    const view = preview([
        { value: 'Zero', numeric_value: 0 },
        { value: 'Negative', numeric_value: -5 },
        { value: 'Positive', numeric_value: 12 }
    ]);
    assertOption(view.html, { value: 'Zero', score: '0' });
    assertOption(view.html, { value: 'Negative', score: '-5' });
    assertOption(view.html, { value: 'Positive', score: '12' });
    assert.ok(view.html.indexOf(placeholder) < view.html.indexOf('title="Zero"'));
    assert.ok(view.html.indexOf('title="Zero"') < view.html.indexOf('title="Negative"'));
    assert.ok(view.html.indexOf('title="Negative"') < view.html.indexOf('title="Positive"'));
});

test('select-list preview does not truncate a 100-character label after escaping expands it', () => {
    const view = preview([{ value: '<'.repeat(100), numeric_value: 0 }]);
    assertOption(view.html, { value: '&lt;'.repeat(100) });
});

test('select-list preview truncates only the displayed label before escaping', () => {
    const value = '<'.repeat(101) + '"&';
    const view = preview([{ value, numeric_value: 0 }]);
    assertOption(view.html, {
        value: '&lt;'.repeat(101) + '&quot;&amp;',
        label: '&lt;'.repeat(100) + ' ...'
    });
});

test('empty select-list definitions keep the placeholder on the legacy preview page', () => {
    const view = preview([], 'preview');
    assert.ok(view.html.includes(placeholder));
    assert.equal((view.html.match(/<option\b/g) || []).length, 1);
});

test('refreshing the scoresheet rebuilds the preview with escaped new values', () => {
    const view = preview([{ value: 'Old option', numeric_value: 1 }]);
    view.refresh([{ value: 'New <option> & "title"', numeric_value: 2 }]);
    assert.equal(view.writes, 2);
    assert.equal(view.html.includes('Old option'), false);
    assertOption(view.html, { value: 'New &lt;option&gt; &amp; &quot;title&quot;', score: '2' });
});
