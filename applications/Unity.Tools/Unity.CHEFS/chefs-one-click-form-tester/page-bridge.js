'use strict';

(function installChefsTesterBridge() {
  if (window.__CHEFS_TESTER_PAGE_BRIDGE__) {
    window.postMessage({ channel: 'CHEFS_TESTER_BRIDGE', type: 'BRIDGE_READY' }, '*');
    return;
  }
  window.__CHEFS_TESTER_PAGE_BRIDGE__ = true;

  let cachedForm = null;

  function isObject(value) {
    return value !== null && (typeof value === 'object' || typeof value === 'function');
  }

  function isFormInstance(value) {
    return isObject(value) &&
      typeof value.everyComponent === 'function' &&
      typeof value.getComponent === 'function' &&
      (typeof value.checkValidity === 'function' || typeof value.checkData === 'function');
  }

  function normalizeFormCandidate(value) {
    if (isFormInstance(value)) {
      return value;
    }
    if (value && isFormInstance(value.root)) {
      return value.root;
    }
    if (value && isFormInstance(value.formio)) {
      return value.formio;
    }
    if (value && isFormInstance(value.webform)) {
      return value.webform;
    }
    return null;
  }

  function safeOwnValues(value) {
    const preferredKeys = [
      'formio', 'form', 'webform', 'instance', 'formInstance', 'formioForm',
      'root', 'currentForm', 'component', 'proxy', 'ctx', 'setupState',
      'exposed', 'subTree', 'provides', 'appContext', '_instance',
      '__vueParentComponent', '__vue_app__', '__vue__'
    ];
    const values = [];
    for (const key of preferredKeys) {
      try {
        if (key in value) {
          values.push(value[key]);
        }
      } catch (error) {
        // Ignore inaccessible properties.
      }
    }
    let keys = [];
    try {
      keys = Object.keys(value).slice(0, 80);
    } catch (error) {
      return values;
    }
    for (const key of keys) {
      if (preferredKeys.includes(key)) {
        continue;
      }
      try {
        const child = value[key];
        if (isObject(child)) {
          values.push(child);
        }
      } catch (error) {
        // Ignore getters that throw.
      }
    }
    return values;
  }

  function findFormInstance() {
    if (isFormInstance(cachedForm)) {
      return cachedForm;
    }

    const roots = [];
    const knownWindowKeys = [
      'formio', 'form', 'webform', 'formInstance', 'formioForm',
      '__formio', '__FORMIO_FORM__', 'chefsForm'
    ];
    for (const key of knownWindowKeys) {
      try {
        if (window[key]) {
          roots.push(window[key]);
        }
      } catch (error) {
        // Ignore inaccessible globals.
      }
    }

    const formElement = document.querySelector('[ref="webform"], .formio-form');
    const appElement = document.querySelector('#app');
    const candidates = [formElement, appElement, document.body, document.documentElement].filter(Boolean);
    for (const element of candidates) {
      roots.push(element);
      try {
        roots.push(element.__vueParentComponent, element.__vue__, element.__vue_app__);
      } catch (error) {
        // Ignore inaccessible framework internals.
      }
    }

    const queue = roots.filter(Boolean).map((value) => ({ value, depth: 0 }));
    const visited = new WeakSet();
    let inspected = 0;

    while (queue.length && inspected < 5000) {
      const item = queue.shift();
      const value = item.value;
      const depth = item.depth;
      if (!isObject(value)) {
        continue;
      }
      if (visited.has(value)) {
        continue;
      }
      visited.add(value);
      inspected += 1;

      const normalized = normalizeFormCandidate(value);
      if (normalized) {
        cachedForm = normalized;
        return cachedForm;
      }
      if (depth >= 6) {
        continue;
      }
      for (const child of safeOwnValues(value)) {
        if (isObject(child) && !visited.has(child)) {
          queue.push({ value: child, depth: depth + 1 });
        }
      }
    }
    return null;
  }

  function sanitizeValueType(value) {
    if (Array.isArray(value)) {
      return 'array';
    }
    if (value === null) {
      return 'null';
    }
    return typeof value;
  }

  function serializeMaskValue(value) {
    if (Array.isArray(value)) {
      return value.length ? String(value[0] || '') : '';
    }
    if (typeof value === 'string' || typeof value === 'number') {
      return String(value);
    }
    return '';
  }

  function runtimeInputMask(wrapper, instance) {
    const controls = [];
    if (wrapper) {
      controls.push(...Array.from(wrapper.querySelectorAll('input:not([type="hidden"]), textarea')));
    }
    if (instance && instance.refs) {
      for (const value of Object.values(instance.refs)) {
        if (value instanceof HTMLInputElement || value instanceof HTMLTextAreaElement) {
          controls.push(value);
        } else if (Array.isArray(value)) {
          controls.push(...value.filter((item) => item instanceof HTMLInputElement || item instanceof HTMLTextAreaElement));
        }
      }
    }
    for (const control of controls) {
      try {
        if (control.inputmask && control.inputmask.opts && control.inputmask.opts.mask) {
          const mask = serializeMaskValue(control.inputmask.opts.mask);
          if (mask) {
            return mask;
          }
        }
      } catch (error) {
        // Continue through rendered attributes.
      }
      const direct = control.getAttribute('data-inputmask-mask') || control.getAttribute('data-mask');
      if (direct) {
        return direct;
      }
      const encoded = control.getAttribute('data-inputmask');
      if (encoded) {
        const match = encoded.match(/(?:mask\s*[:=]\s*['"])([^'"]+)/i);
        if (match) {
          return match[1];
        }
      }
    }
    return '';
  }

  function sanitizeComponent(instance) {
    const component = instance && instance.component ? instance.component : {};
    const validate = component.validate || {};
    const values = Array.isArray(component.values)
      ? component.values.slice(0, 100).map((item) => ({
          label: item && item.label !== undefined ? String(item.label) : '',
          valueType: item ? sanitizeValueType(item.value) : 'undefined',
          value: item && ['string', 'number', 'boolean'].includes(typeof item.value) ? item.value : undefined
        }))
      : [];
    const element = instance && instance.element ? instance.element : null;
    const wrapper = element && element.closest ? element.closest('.formio-component') : null;
    return {
      key: component.key || instance.key || '',
      path: instance.path || '',
      domId: (wrapper && wrapper.id) || (element && element.id) || '',
      instanceId: instance.id || component.id || '',
      type: component.type || instance.type || '',
      label: component.label || '',
      description: component.description || '',
      placeholder: component.placeholder || '',
      input: component.input !== false,
      hidden: Boolean(component.hidden),
      disabled: Boolean(component.disabled || instance.disabled),
      readOnly: Boolean(component.readOnly),
      calculateValue: Boolean(component.calculateValue),
      customDefaultValue: Boolean(component.customDefaultValue),
      persistent: component.persistent,
      multiple: Boolean(component.multiple),
      dataSrc: component.dataSrc || '',
      widgetType: component.widget && component.widget.type ? component.widget.type : component.widget || '',
      filePattern: component.filePattern || '',
      fileMinSize: component.fileMinSize || '',
      fileMaxSize: component.fileMaxSize || '',
      currency: component.currency || '',
      delimiter: component.delimiter,
      inputMask: serializeMaskValue(component.inputMask || component.mask || ''),
      runtimeInputMask: runtimeInputMask(wrapper, instance),
      minLength: validate.minLength,
      maxLength: validate.maxLength,
      minWords: validate.minWords,
      maxWords: validate.maxWords,
      minSelectedCount: validate.minSelectedCount !== undefined
        ? validate.minSelectedCount
        : component.minSelectedCount,
      maxSelectedCount: validate.maxSelectedCount !== undefined
        ? validate.maxSelectedCount
        : component.maxSelectedCount,
      min: validate.min,
      max: validate.max,
      pattern: validate.pattern || '',
      required: Boolean(validate.required),
      validationMessage: validate.customMessage || '',
      values,
      hasValue: typeof instance.hasValue === 'function' ? Boolean(instance.hasValue()) : undefined,
      valueType: sanitizeValueType(instance.dataValue)
    };
  }

  function getComponents() {
    const form = findFormInstance();
    if (!form) {
      return { formFound: false, components: [] };
    }
    const components = [];
    form.everyComponent((instance) => {
      try {
        components.push(sanitizeComponent(instance));
      } catch (error) {
        components.push({
          key: instance && instance.key ? instance.key : '',
          type: instance && instance.type ? instance.type : '',
          metadataError: error && error.message ? error.message : String(error)
        });
      }
    });
    return {
      formFound: true,
      formType: form.display || (form.form && form.form.display) || '',
      formLoading: Boolean(form.loading),
      componentCount: components.length,
      components
    };
  }

  function findComponentCandidates(key, wrapperId) {
    const form = findFormInstance();
    if (!form || (!key && !wrapperId)) {
      return [];
    }
    const candidates = [];
    const seen = new Set();
    const add = (instance) => {
      if (!instance || seen.has(instance)) {
        return;
      }
      const component = instance.component || {};
      const instanceKey = component.key || instance.key || '';
      const path = instance.path || '';
      const element = instance.element || null;
      const wrapper = element && element.closest ? element.closest('.formio-component') : null;
      const instanceDomId = (wrapper && wrapper.id) || (element && element.id) || '';
      const instanceId = instance.id || component.id || '';
      const keyMatches = Boolean(
        key &&
        (
          instanceKey === key ||
          path === key ||
          path.endsWith(`.${key}`) ||
          path.endsWith(`[${key}]`)
        )
      );
      const idMatches = Boolean(wrapperId && (instanceDomId === wrapperId || instanceId === wrapperId));
      if (keyMatches || idMatches) {
        seen.add(instance);
        candidates.push(instance);
      }
    };
    if (key) {
      try {
        add(form.getComponent(key));
      } catch (error) {
        // Continue with full component traversal.
      }
    }
    try {
      form.everyComponent((instance) => add(instance));
    } catch (error) {
      // Return whatever was discovered.
    }
    return candidates;
  }

  function findComponent(key, predicate) {
    const candidates = findComponentCandidates(key);
    if (typeof predicate === 'function') {
      return candidates.find(predicate) || candidates[0] || null;
    }
    return candidates[0] || null;
  }

  function callableMethodNames(instance) {
    const names = new Set();
    let current = instance;
    let depth = 0;
    while (current && depth < 5) {
      let properties = [];
      try {
        properties = Object.getOwnPropertyNames(current);
      } catch (error) {
        properties = [];
      }
      for (const name of properties) {
        try {
          if (typeof instance[name] === 'function' && /file|upload|drop/i.test(name)) {
            names.add(name);
          }
        } catch (error) {
          // Ignore inaccessible functions.
        }
      }
      current = Object.getPrototypeOf(current);
      depth += 1;
    }
    return Array.from(names).sort();
  }

  async function setComponentValue(payload) {
    const candidates = findComponentCandidates(payload.key, payload.wrapperId);
    const component = candidates[0] || null;
    if (!component || typeof component.setValue !== 'function') {
      throw new Error(`Form.io component was not found for ${payload.key}.`);
    }
    const changed = component.setValue(payload.value, {
      modified: true,
      fromSubmission: false,
      noUpdateEvent: false
    });
    if (typeof component.triggerChange === 'function') {
      component.triggerChange({ modified: true });
    }
    if (component.root && typeof component.root.checkData === 'function') {
      component.root.checkData(component.root.data, { modified: true });
    }
    return {
      changed: Boolean(changed),
      valueType: sanitizeValueType(component.dataValue),
      hasValue: typeof component.hasValue === 'function' ? Boolean(component.hasValue()) : undefined
    };
  }

  async function setMaskedValue(payload) {
    const wrapper = findRenderedWrapper(payload.key, payload.wrapperId);
    const control = wrapper && wrapper.querySelector('input:not([type="hidden"]), textarea');
    let inputmaskUsed = false;
    let inputmaskComplete = false;
    if (control) {
      try {
        if (control.inputmask && typeof control.inputmask.setValue === 'function') {
          control.inputmask.setValue(payload.value);
          inputmaskUsed = true;
          inputmaskComplete = typeof control.inputmask.isComplete === 'function'
            ? Boolean(control.inputmask.isComplete())
            : false;
        } else {
          control.value = payload.value;
        }
      } catch (error) {
        control.value = payload.value;
      }
      control.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
      control.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
      control.dispatchEvent(new Event('blur', { bubbles: true, composed: true }));
    }

    const candidates = findComponentCandidates(payload.key, payload.wrapperId);
    const component = candidates[0] || null;
    let changed = false;
    if (component && typeof component.setValue === 'function') {
      changed = Boolean(component.setValue(payload.value, {
        modified: true,
        fromSubmission: false,
        noUpdateEvent: false
      }));
      if (typeof component.triggerChange === 'function') {
        component.triggerChange({ modified: true });
      }
      if (component.root && typeof component.root.checkData === 'function') {
        component.root.checkData(component.root.data, { modified: true });
      }
    }
    const liveWrapper = findRenderedWrapper(payload.key, payload.wrapperId) || wrapper;
    const liveControl = liveWrapper && liveWrapper.querySelector('input:not([type="hidden"]), textarea');
    if (liveControl && liveControl.inputmask && typeof liveControl.inputmask.isComplete === 'function') {
      try {
        inputmaskComplete = Boolean(liveControl.inputmask.isComplete());
      } catch (error) {
        // Keep the earlier completion state.
      }
    }
    return {
      changed,
      inputmaskUsed,
      inputmaskComplete,
      renderedValue: liveControl ? String(liveControl.value || '') : '',
      hasValue: component && typeof component.hasValue === 'function'
        ? Boolean(component.hasValue())
        : Boolean(liveControl && liveControl.value)
    };
  }

  function base64ToBytes(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i += 1) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
  }


  function createDragEvent(type, dataTransfer) {
    try {
      return new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer });
    } catch (error) {
      const event = new Event(type, { bubbles: true, cancelable: true });
      Object.defineProperty(event, 'dataTransfer', { value: dataTransfer });
      return event;
    }
  }

  function renderedWrapperIsVisible(item) {
    if (!item || !item.isConnected || item.closest('.formio-hidden, [hidden], [aria-hidden="true"]')) {
      return false;
    }
    const style = getComputedStyle(item);
    return style.display !== 'none' &&
      style.visibility !== 'hidden' &&
      Number(style.opacity) !== 0 &&
      item.getClientRects().length > 0;
  }

  function findRenderedWrapper(key, wrapperId) {
    if (wrapperId) {
      const exact = document.getElementById(wrapperId);
      if (renderedWrapperIsVisible(exact)) {
        return exact;
      }
    }
    if (!key) {
      return null;
    }
    const escapedKey = window.CSS && typeof window.CSS.escape === 'function'
      ? window.CSS.escape(key)
      : String(key).replace(/[^A-Za-z0-9_-]/g, '\\$&');
    const wrappers = Array.from(document.querySelectorAll(`.formio-component-${escapedKey}`));
    return wrappers.find((item) => renderedWrapperIsVisible(item)) ||
      wrappers.find((item) => item.isConnected) ||
      null;
  }

  function renderedFileRows(wrapper, filename) {
    if (!wrapper) {
      return [];
    }
    const candidates = Array.from(wrapper.querySelectorAll(
      '.list-group > .list-group-item:not(.list-group-header), ' +
      '.list-group-item:not(.list-group-header), tbody tr:not(:first-child), ' +
      '[ref="fileLink"], [ref="fileName"], .file-name, .file-list a, a[download]'
    ));
    const matches = candidates.filter((element) => {
      const text = String(element.textContent || '').replace(/\s+/g, ' ').trim();
      const hasRemoveControl = Boolean(element.querySelector && element.querySelector(
        'button[ref*="remove"], button[aria-label*="remove" i], .fa-times, .fa-times-circle-o'
      ));
      if (filename && text.includes(filename)) {
        return true;
      }
      if (hasRemoveControl && text) {
        return true;
      }
      if (!text) {
        return false;
      }
      return !/^file\s*name\s*size$/i.test(text) && !/drop files to attach|browse to attach/i.test(text);
    });
    return Array.from(new Set(matches.map((element) =>
      element.closest('.list-group-item:not(.list-group-header), tbody tr') || element
    )));
  }

  async function uploadFileByDomDrop(payload, file) {
    let wrapper = findRenderedWrapper(payload.key, payload.wrapperId);
    const dropTarget = wrapper && (wrapper.querySelector('[ref="fileDrop"], .fileSelector') || wrapper);
    if (!dropTarget) {
      throw new Error(`No rendered file drop target was found for ${payload.key}.`);
    }
    const baselineCount = renderedFileRows(wrapper).length;
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(file);
    dropTarget.dispatchEvent(createDragEvent('dragenter', dataTransfer));
    dropTarget.dispatchEvent(createDragEvent('dragover', dataTransfer));
    dropTarget.dispatchEvent(createDragEvent('drop', dataTransfer));

    const started = Date.now();
    let wrapperReplaced = false;
    while (Date.now() - started < 45000) {
      const liveWrapper = findRenderedWrapper(payload.key, payload.wrapperId);
      if (liveWrapper && wrapper && liveWrapper !== wrapper) {
        wrapperReplaced = true;
      }
      wrapper = liveWrapper || wrapper;
      const uploadedRows = renderedFileRows(wrapper, payload.filename);
      const allRows = renderedFileRows(wrapper);
      if (
        uploadedRows.length ||
        allRows.length > baselineCount ||
        (wrapper && String(wrapper.textContent || '').includes(payload.filename))
      ) {
        return {
          hasValue: true,
          valueCount: Math.max(1, uploadedRows.length, allRows.length),
          pendingUploads: 0,
          syncing: false,
          uploadMethod: wrapperReplaced ? 'page-dom-drop-rerendered-wrapper' : 'page-dom-drop',
          componentType: 'rendered-file',
          candidateCount: 0
        };
      }
      const errorElement = wrapper && wrapper.querySelector('.formio-errors, .invalid-feedback');
      const message = String(errorElement ? errorElement.textContent : '').trim();
      if (message) {
        throw new Error(message);
      }
      await new Promise((resolve) => setTimeout(resolve, 250));
    }
    throw new Error(`The rendered drop target for ${payload.key} did not produce an uploaded file row before timeout.`);
  }

  async function uploadFile(payload) {
    const candidates = findComponentCandidates(payload.key, payload.wrapperId);
    if (!candidates.length) {
      throw new Error(`No Form.io component instance was found for ${payload.key}.`);
    }
    const bytes = base64ToBytes(payload.base64);
    const file = new File([bytes], payload.filename, {
      type: payload.mimeType || 'application/octet-stream',
      lastModified: Date.now()
    });

    const attempts = [];
    for (const component of candidates) {
      const componentType = component && component.component ? component.component.type : component.type || '';
      const methods = callableMethodNames(component);
      const operations = [
        ['handleFilesToUpload', () => component.handleFilesToUpload([file])],
        ['uploadFile', () => component.uploadFile(file)],
        ['addFile', () => component.addFile(file)],
        ['onDrop', () => component.onDrop({
          dataTransfer: { files: [file], items: [{ kind: 'file', type: file.type, getAsFile: () => file }] },
          preventDefault() {},
          stopPropagation() {}
        })]
      ];
      for (const [method, invoke] of operations) {
        if (typeof component[method] !== 'function') {
          continue;
        }
        try {
          await invoke();
          if (typeof component.triggerChange === 'function') {
            component.triggerChange({ modified: true });
          }
          if (component.root && typeof component.root.checkData === 'function') {
            component.root.checkData(component.root.data, { modified: true });
          }
          return {
            hasValue: typeof component.hasValue === 'function' ? Boolean(component.hasValue()) : undefined,
            valueCount: Array.isArray(component.dataValue) ? component.dataValue.length : component.dataValue ? 1 : 0,
            pendingUploads: component.filesToSync && Array.isArray(component.filesToSync.filesToUpload)
              ? component.filesToSync.filesToUpload.length
              : 0,
            syncing: Boolean(component.isSyncing),
            uploadMethod: method,
            componentType,
            candidateCount: candidates.length
          };
        } catch (error) {
          attempts.push({ method, componentType, message: error && error.message ? error.message : String(error) });
        }
      }
      attempts.push({ method: 'none', componentType, callableMethods: methods });
    }
    try {
      return await uploadFileByDomDrop(payload, file);
    } catch (error) {
      attempts.push({ method: 'page-dom-drop', message: error && error.message ? error.message : String(error) });
    }
    throw new Error(`File component API was not usable for ${payload.key}. Diagnostics: ${JSON.stringify(attempts).slice(0, 3000)}`);
  }

  function checkValidity() {
    const form = findFormInstance();
    if (!form) {
      return { formFound: false, valid: false, errors: [] };
    }
    let valid = false;
    if (typeof form.checkValidity === 'function') {
      valid = Boolean(form.checkValidity(form.data, true, null, false));
    } else if (typeof form.checkData === 'function') {
      valid = Boolean(form.checkData(form.data, { dirty: true }));
    }
    const errors = Array.isArray(form.errors)
      ? form.errors.map((error) => ({
          message: error && error.message ? String(error.message) : String(error),
          key: error && error.component && error.component.key ? error.component.key : '',
          type: error && error.component && error.component.type ? error.component.type : ''
        }))
      : [];
    return { formFound: true, valid, errors };
  }

  async function executeCommand(command, payload) {
    switch (command) {
      case 'PING':
        return { ready: true, formFound: Boolean(findFormInstance()) };
      case 'GET_COMPONENTS':
        return getComponents();
      case 'SET_VALUE':
        return setComponentValue(payload || {});
      case 'SET_MASKED_VALUE':
        return setMaskedValue(payload || {});
      case 'UPLOAD_FILE':
        return uploadFile(payload || {});
      case 'CHECK_VALIDITY':
        return checkValidity();
      case 'RESET_CACHE':
        cachedForm = null;
        return { reset: true };
      default:
        throw new Error(`Unknown page bridge command: ${command}`);
    }
  }

  window.addEventListener('message', (event) => {
    if (event.source !== window || !event.data || event.data.channel !== 'CHEFS_TESTER_BRIDGE_REQUEST') {
      return;
    }
    const requestId = event.data.requestId;
    Promise.resolve()
      .then(() => executeCommand(event.data.command, event.data.payload))
      .then((result) => {
        window.postMessage({
          channel: 'CHEFS_TESTER_BRIDGE_RESPONSE',
          requestId,
          ok: true,
          result
        }, '*');
      })
      .catch((error) => {
        window.postMessage({
          channel: 'CHEFS_TESTER_BRIDGE_RESPONSE',
          requestId,
          ok: false,
          error: {
            message: error && error.message ? error.message : String(error),
            stack: error && error.stack ? error.stack : ''
          }
        }, '*');
      });
  });

  window.postMessage({ channel: 'CHEFS_TESTER_BRIDGE', type: 'BRIDGE_READY' }, '*');
})();
