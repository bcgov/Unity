'use strict';

(function installChefsTesterBridge() {
  const MESSAGE_ORIGIN = window.location.origin;
  if (window.__CHEFS_TESTER_PAGE_BRIDGE__) {
    window.postMessage({ channel: 'CHEFS_TESTER_BRIDGE', type: 'BRIDGE_READY' }, MESSAGE_ORIGIN);
    return;
  }
  window.__CHEFS_TESTER_PAGE_BRIDGE__ = true;

  let cachedForms = [];

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
    if (isFormInstance(value?.root)) {
      return value.root;
    }
    if (isFormInstance(value?.formio)) {
      return value.formio;
    }
    if (isFormInstance(value?.webform)) {
      return value.webform;
    }
    return null;
  }

  function safeOwnValues(value) {
    const preferredKeys = new Set([
      'formio', 'form', 'webform', 'instance', 'formInstance', 'formioForm',
      'root', 'currentForm', 'component', 'proxy', 'ctx', 'setupState',
      'exposed', 'subTree', 'provides', 'appContext', '_instance',
      '__vueParentComponent', '__vue_app__', '__vue__'
    ]);
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
      if (preferredKeys.has(key)) {
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

  function recordFormCandidate(value, seenForms, forms) {
    const normalized = normalizeFormCandidate(value);
    if (normalized && !seenForms.has(normalized)) {
      seenForms.add(normalized);
      forms.push(normalized);
    }
  }

  function enqueueChildObjects(queue, value, depth, visited) {
    if (depth >= 8) {
      return;
    }
    for (const child of safeOwnValues(value)) {
      if (isObject(child) && !visited.has(child)) {
        queue.push({ value: child, depth: depth + 1 });
      }
    }
  }

  function elementSearchRoots(element) {
    const roots = [];
    let current = element;
    let hops = 0;
    while (current && hops < 12) {
      roots.push(current);
      try {
        roots.push(current.__vueParentComponent, current.__vue__, current.__vue_app__);
      } catch (error) {
        // Ignore inaccessible framework internals.
      }
      current = current.parentElement;
      hops += 1;
    }
    return roots.filter(Boolean);
  }

  function initialFormSearchRoots(wrapperId) {
    const targetWrapper = wrapperId ? document.getElementById(wrapperId) : null;
    const roots = targetWrapper ? elementSearchRoots(targetWrapper) : [];
    roots.push(...cachedForms.filter((form) => isFormInstance(form)));
    for (const key of [
      'formio', 'form', 'webform', 'formInstance', 'formioForm',
      '__formio', '__FORMIO_FORM__', 'chefsForm'
    ]) {
      try {
        if (window[key]) {
          roots.push(window[key]);
        }
      } catch {
        // Inaccessible framework globals are excluded from discovery.
      }
    }
    const formElements = Array.from(document.querySelectorAll('[ref="webform"], .formio-form')).slice(0, 100);
    roots.push(...formElements, document.querySelector('#app'), document.body, document.documentElement);
    return { roots: roots.filter(Boolean), targetWrapper };
  }

  function traverseForForms(roots) {
    const queue = roots.map((value) => ({ value, depth: 0 }));
    const visited = new WeakSet();
    const seenForms = new Set();
    const forms = [];
    let inspected = 0;
    while (queue.length && inspected < 8000) {
      const current = queue.shift();
      const value = current?.value;
      if (!isObject(value) || visited.has(value)) {
        continue;
      }
      visited.add(value);
      inspected += 1;
      recordFormCandidate(value, seenForms, forms);
      enqueueChildObjects(queue, value, current.depth, visited);
    }
    return { forms, inspected };
  }

  function discoverFormInstances(wrapperId) {
    const { roots, targetWrapper } = initialFormSearchRoots(wrapperId);
    const { forms, inspected } = traverseForForms(roots);
    cachedForms = forms;
    return {
      forms,
      inspectedObjects: inspected,
      wrapperFound: Boolean(targetWrapper)
    };
  }

  function findFormInstance() {
    const cached = cachedForms.find((form) => isFormInstance(form));
    if (cached) {
      return cached;
    }
    const discovery = discoverFormInstances('');
    return discovery.forms[0] || null;
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

  function controlsForMaskInspection(wrapper, instance) {
    const controls = wrapper
      ? Array.from(wrapper.querySelectorAll('input:not([type="hidden"]), textarea'))
      : [];
    for (const value of Object.values(instance?.refs || {})) {
      if (value instanceof HTMLInputElement || value instanceof HTMLTextAreaElement) {
        controls.push(value);
      } else if (Array.isArray(value)) {
        controls.push(...value.filter((item) => item instanceof HTMLInputElement || item instanceof HTMLTextAreaElement));
      }
    }
    return controls;
  }

  function maskFromControl(control) {
    try {
      const runtimeMask = serializeMaskValue(control.inputmask?.opts?.mask);
      if (runtimeMask) {
        return runtimeMask;
      }
    } catch {
      // Some input-mask libraries expose getters that can throw; rendered metadata remains usable.
    }
    const direct = control.dataset.inputmaskMask || control.dataset.mask;
    if (direct) {
      return direct;
    }
    const match = control.dataset.inputmask?.match(/(?:mask\s*[:=]\s*['"])([^'"]+)/i);
    return match?.[1] || '';
  }

  function runtimeInputMask(wrapper, instance) {
    for (const control of controlsForMaskInspection(wrapper, instance)) {
      const mask = maskFromControl(control);
      if (mask) {
        return mask;
      }
    }
    return '';
  }

  function sanitizeComponent(instance) {
    const component = instance?.component || {};
    const validate = component.validate || {};
    const values = Array.isArray(component.values)
      ? component.values.slice(0, 100).map((item) => ({
          label: item?.label !== undefined ? String(item.label) : '',
          valueType: item ? sanitizeValueType(item.value) : 'undefined',
          value: ['string', 'number', 'boolean'].includes(typeof item?.value) ? item.value : undefined
        }))
      : [];
    const element = instance?.element || null;
    const wrapper = element?.closest?.('.formio-component') || null;
    return {
      key: component.key || instance.key || '',
      path: instance.path || '',
      domId: wrapper?.id || element?.id || '',
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
      widgetType: component.widget?.type || component.widget || '',
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
          key: instance?.key || '',
          type: instance?.type || '',
          metadataError: error?.message || String(error)
        });
      }
    });
    return {
      formFound: true,
      formType: form.display || form.form?.display || '',
      formLoading: Boolean(form.loading),
      componentCount: components.length,
      components
    };
  }

  function componentCandidateMatch(instance, key, wrapperId, targetWrapper, formIndex) {
    const component = instance.component || {};
    const instanceKey = component.key || instance.key || '';
    const path = instance.path || '';
    const element = instance.element || null;
    const wrapper = element?.closest?.('.formio-component') || null;
    const instanceDomId = wrapper?.id || element?.id || '';
    const instanceId = instance.id || component.id || '';
    const keyMatches = Boolean(key && (
      instanceKey === key ||
      path === key ||
      path.endsWith(`.${key}`) ||
      path.endsWith(`[${key}]`)
    ));
    const idMatches = Boolean(wrapperId && (instanceDomId === wrapperId || instanceId === wrapperId));
    if (!keyMatches && !idMatches) {
      return null;
    }
    const ownsWrapper = Boolean(targetWrapper && element && (
      element === targetWrapper ||
      targetWrapper.contains?.(element) ||
      element.contains?.(targetWrapper)
    ));
    return {
      instance,
      score: (idMatches ? 1000 : 0) + (ownsWrapper ? 800 : 0) + (keyMatches ? 100 : 0) +
        (element?.isConnected ? 10 : 0) - formIndex,
      ownsWrapper
    };
  }

  function findComponentCandidates(key, wrapperId) {
    const discovery = discoverFormInstances(wrapperId);
    const forms = discovery.forms;
    if (!forms.length || (!key && !wrapperId)) {
      const empty = [];
      empty.lookupDiagnostics = {
        formRootCount: forms.length,
        inspectedComponentCount: 0,
        inspectedObjectCount: discovery.inspectedObjects,
        wrapperFound: discovery.wrapperFound,
        wrapperMatched: false
      };
      return empty;
    }
    const targetWrapper = wrapperId ? document.getElementById(wrapperId) : null;
    const matches = [];
    const seen = new Set();
    let inspectedComponentCount = 0;
    const add = (instance, formIndex) => {
      if (!instance || seen.has(instance)) {
        return;
      }
      inspectedComponentCount += 1;
      const match = componentCandidateMatch(instance, key, wrapperId, targetWrapper, formIndex);
      if (match) {
        seen.add(instance);
        matches.push(match);
      }
    };
    forms.forEach((form, formIndex) => {
      if (key) {
        try {
          add(form.getComponent(key), formIndex);
        } catch (error) {
          // Continue with full component traversal.
        }
      }
      try {
        form.everyComponent((instance) => add(instance, formIndex));
      } catch (error) {
        // Return whatever was discovered from this root.
      }
    });
    matches.sort((left, right) => right.score - left.score);
    const candidates = matches.map((match) => match.instance);
    candidates.lookupDiagnostics = {
      formRootCount: forms.length,
      inspectedComponentCount,
      inspectedObjectCount: discovery.inspectedObjects,
      wrapperFound: discovery.wrapperFound,
      wrapperMatched: matches.some((match) => match.ownsWrapper),
      candidateCount: candidates.length
    };
    return candidates;
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
    return Array.from(names).sort((left, right) => left.localeCompare(right));
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
    if (typeof component.root?.checkData === 'function') {
      component.root.checkData(component.root.data, { modified: true });
    }
    return {
      changed: Boolean(changed),
      valueType: sanitizeValueType(component.dataValue),
      hasValue: typeof component.hasValue === 'function' ? Boolean(component.hasValue()) : undefined
    };
  }

  async function selectOrgbookResult(payload) {
    const candidates = findComponentCandidates(payload.key, payload.wrapperId);
    const lookup = candidates.lookupDiagnostics || {};
    const component = candidates.find((candidate) => {
      const type = String(candidate.component?.type || candidate.type || '').toLowerCase();
      return type.includes('orgbook') &&
        typeof candidate.triggerUpdate === 'function' &&
        typeof candidate.setValue === 'function';
    });
    if (!component) {
      throw new Error(
        `Form.io OrgBook component was not found for ${payload.key} after inspecting ` +
        `${lookup.formRootCount || 0} Form.io roots and ${lookup.inspectedComponentCount || 0} components.`
      );
    }

    const configuredUrl = String(component.component?.data?.url || '');
    const endpoint = new URL(configuredUrl, window.location.href);
    if (endpoint.protocol !== 'https:' || endpoint.hostname !== 'orgbook.gov.bc.ca' ||
        endpoint.pathname !== '/api/v3/search/autocomplete') {
      throw new Error('The Form.io OrgBook component is not configured with the approved autocomplete endpoint.');
    }

    const query = String(payload.query || '').slice(0, 100);
    const preferredValue = String(payload.preferredValue || '').trim();
    component.triggerUpdate(query, true);
    if (typeof component.itemsLoaded?.then === 'function') {
      await Promise.race([
        component.itemsLoaded,
        new Promise((resolve, reject) => setTimeout(() => reject(new Error('Form.io OrgBook items did not load in time.')), 8000))
      ]);
    }

    const options = Array.isArray(component.selectOptions) ? component.selectOptions : [];
    const hasPreferredValue = options.some((option) =>
      typeof option?.value === 'string' && option.value.trim() === preferredValue
    );
    if (!hasPreferredValue) {
      return {
        applied: false,
        resultCount: options.length,
        hasValue: false,
        lookup
      };
    }

    const changed = component.setValue(preferredValue, {
      modified: true,
      fromSubmission: false,
      noUpdateEvent: false
    });
    if (typeof component.triggerChange === 'function') {
      component.triggerChange({ modified: true });
    }
    if (typeof component.root?.checkData === 'function') {
      component.root.checkData(component.root.data, { modified: true });
    }
    return {
      applied: true,
      changed: Boolean(changed),
      resultCount: options.length,
      source: 'formio-select-lifecycle',
      lookup,
      valueType: sanitizeValueType(component.dataValue),
      hasValue: typeof component.hasValue === 'function' ? Boolean(component.hasValue()) : undefined
    };
  }

  function dispatchValueEvents(control) {
    for (const type of ['input', 'change', 'blur']) {
      control.dispatchEvent(new Event(type, { bubbles: true, composed: true }));
    }
  }

  function setRenderedMaskedValue(control, value) {
    if (!control) {
      return { inputmaskUsed: false, inputmaskComplete: false };
    }
    let inputmaskUsed = false;
    let inputmaskComplete = false;
    try {
      if (typeof control.inputmask?.setValue === 'function') {
        control.inputmask.setValue(value);
        inputmaskUsed = true;
        inputmaskComplete = typeof control.inputmask.isComplete === 'function' &&
          Boolean(control.inputmask.isComplete());
      } else {
        control.value = value;
      }
    } catch {
      control.value = value;
    }
    dispatchValueEvents(control);
    return { inputmaskUsed, inputmaskComplete };
  }

  function setFormComponentValue(component, value) {
    if (typeof component?.setValue !== 'function') {
      return false;
    }
    const changed = Boolean(component.setValue(value, {
      modified: true,
      fromSubmission: false,
      noUpdateEvent: false
    }));
    if (typeof component.triggerChange === 'function') {
      component.triggerChange({ modified: true });
    }
    if (typeof component.root?.checkData === 'function') {
      component.root.checkData(component.root.data, { modified: true });
    }
    return changed;
  }

  function currentMaskCompletion(control, fallback) {
    if (typeof control?.inputmask?.isComplete !== 'function') {
      return fallback;
    }
    try {
      return Boolean(control.inputmask.isComplete());
    } catch {
      return fallback;
    }
  }

  async function setMaskedValue(payload) {
    const wrapper = findRenderedWrapper(payload.key, payload.wrapperId);
    const control = wrapper?.querySelector('input:not([type="hidden"]), textarea');
    const rendered = setRenderedMaskedValue(control, payload.value);

    const candidates = findComponentCandidates(payload.key, payload.wrapperId);
    const component = candidates[0] || null;
    const changed = setFormComponentValue(component, payload.value);
    const liveWrapper = findRenderedWrapper(payload.key, payload.wrapperId) || wrapper;
    const liveControl = liveWrapper?.querySelector('input:not([type="hidden"]), textarea');
    return {
      changed,
      inputmaskUsed: rendered.inputmaskUsed,
      inputmaskComplete: currentMaskCompletion(liveControl, rendered.inputmaskComplete),
      renderedValue: liveControl ? String(liveControl.value || '') : '',
      hasValue: typeof component?.hasValue === 'function'
        ? Boolean(component.hasValue())
        : Boolean(liveControl?.value)
    };
  }

  function base64ToBytes(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i += 1) {
      bytes[i] = binary.codePointAt(i);
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
    if (!item?.isConnected || item.closest?.('.formio-hidden, [hidden], [aria-hidden="true"]')) {
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
    const escapedKey = typeof window.CSS?.escape === 'function'
      ? window.CSS.escape(key)
      : String(key).replace(/[^A-Za-z0-9_-]/g, String.raw`\$&`);
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
      const hasRemoveControl = Boolean(element.querySelector?.(
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

  function completedDomUpload(wrapper, filename, baselineCount, wrapperReplaced) {
    const uploadedRows = renderedFileRows(wrapper, filename);
    const allRows = renderedFileRows(wrapper);
    const filenameVisible = String(wrapper?.textContent || '').includes(filename);
    if (!uploadedRows.length && allRows.length <= baselineCount && !filenameVisible) {
      return null;
    }
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

  async function uploadFileByDomDrop(payload, file) {
    let wrapper = findRenderedWrapper(payload.key, payload.wrapperId);
    const dropTarget = wrapper?.querySelector('[ref="fileDrop"], .fileSelector') || wrapper;
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
      const completed = completedDomUpload(wrapper, payload.filename, baselineCount, wrapperReplaced);
      if (completed) {
        return completed;
      }
      const errorElement = wrapper?.querySelector('.formio-errors, .invalid-feedback');
      const message = String(errorElement?.textContent || '').trim();
      if (message) {
        throw new Error(message);
      }
      await new Promise((resolve) => setTimeout(resolve, 250));
    }
    throw new Error(`The rendered drop target for ${payload.key} did not produce an uploaded file row before timeout.`);
  }

  function uploadOperations(component, file) {
    return [
      ['handleFilesToUpload', () => component.handleFilesToUpload([file])],
      ['uploadFile', () => component.uploadFile(file)],
      ['addFile', () => component.addFile(file)],
      ['onDrop', () => component.onDrop({
        dataTransfer: { files: [file], items: [{ kind: 'file', type: file.type, getAsFile: () => file }] },
        preventDefault() {},
        stopPropagation() {}
      })]
    ];
  }

  function componentValueCount(value) {
    if (Array.isArray(value)) {
      return value.length;
    }
    return value ? 1 : 0;
  }

  async function tryComponentUpload(component, file, candidateCount, attempts) {
    const componentType = component.component?.type || component.type || '';
    for (const [method, invoke] of uploadOperations(component, file)) {
      if (typeof component[method] !== 'function') {
        continue;
      }
      try {
        await invoke();
        if (typeof component.triggerChange === 'function') {
          component.triggerChange({ modified: true });
        }
        if (typeof component.root?.checkData === 'function') {
          component.root.checkData(component.root.data, { modified: true });
        }
        return {
          hasValue: typeof component.hasValue === 'function' ? Boolean(component.hasValue()) : undefined,
          valueCount: componentValueCount(component.dataValue),
          pendingUploads: Array.isArray(component.filesToSync?.filesToUpload)
            ? component.filesToSync.filesToUpload.length
            : 0,
          syncing: Boolean(component.isSyncing),
          uploadMethod: method,
          componentType,
          candidateCount
        };
      } catch (error) {
        attempts.push({ method, componentType, message: error?.message || String(error) });
      }
    }
    attempts.push({ method: 'none', componentType, callableMethods: callableMethodNames(component) });
    return null;
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
      const result = await tryComponentUpload(component, file, candidates.length, attempts);
      if (result) {
        return result;
      }
    }
    try {
      return await uploadFileByDomDrop(payload, file);
    } catch (error) {
      attempts.push({ method: 'page-dom-drop', message: error?.message || String(error) });
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
          message: error?.message ? String(error.message) : String(error),
          key: error?.component?.key || '',
          type: error?.component?.type || ''
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
      case 'SELECT_ORGBOOK_RESULT':
        return selectOrgbookResult(payload || {});
      case 'SET_MASKED_VALUE':
        return setMaskedValue(payload || {});
      case 'UPLOAD_FILE':
        return uploadFile(payload || {});
      case 'CHECK_VALIDITY':
        return checkValidity();
      case 'RESET_CACHE':
        cachedForms = [];
        return { reset: true };
      default:
        throw new Error(`Unknown page bridge command: ${command}`);
    }
  }

  async function handleBridgeMessage(event) {
    const requestId = event.data.requestId;
    try {
      const result = await executeCommand(event.data.command, event.data.payload);
      window.postMessage({
        channel: 'CHEFS_TESTER_BRIDGE_RESPONSE',
        requestId,
        ok: true,
        result
      }, MESSAGE_ORIGIN);
    } catch (error) {
      window.postMessage({
        channel: 'CHEFS_TESTER_BRIDGE_RESPONSE',
        requestId,
        ok: false,
        error: {
          message: error?.message || String(error),
          stack: error?.stack || ''
        }
      }, MESSAGE_ORIGIN);
    }
  }

  window.addEventListener('message', (event) => {
    if (event.source !== window ||
        event.origin !== MESSAGE_ORIGIN ||
        event.data?.channel !== 'CHEFS_TESTER_BRIDGE_REQUEST') {
      return;
    }
    void handleBridgeMessage(event);
  });

  window.postMessage({ channel: 'CHEFS_TESTER_BRIDGE', type: 'BRIDGE_READY' }, MESSAGE_ORIGIN);
})();
