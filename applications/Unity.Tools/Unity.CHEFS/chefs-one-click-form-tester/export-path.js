'use strict';

(() => {
  const INVALID_COMPONENT_CHARACTERS = /[<>:"|?*\u0000-\u001F]/;
  const RESERVED_WINDOWS_NAME = /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?$/i;

  function normalizeExportFolder(value) {
    const text = String(value || '').trim();
    if (!text) {
      return '';
    }
    if (text.length > 240) {
      throw new Error('The export folder cannot exceed 240 characters.');
    }
    if (/^[A-Za-z]:/.test(text) || /^[\\/]/.test(text)) {
      throw new Error('Enter a folder relative to Downloads, not an absolute path or drive letter.');
    }

    const components = text.split(/[\\/]/);
    if (components.some((component) => component.length === 0)) {
      throw new Error('The export folder cannot contain repeated or trailing separators.');
    }

    for (const component of components) {
      if (component === '.' || component === '..') {
        throw new Error('The export folder cannot contain . or .. path traversal.');
      }
      if (component.length > 255) {
        throw new Error('An export folder component cannot exceed 255 characters.');
      }
      if (INVALID_COMPONENT_CHARACTERS.test(component)) {
        throw new Error(`The export folder component "${component}" contains an invalid Windows filename character.`);
      }
      if (/[. ]$/.test(component)) {
        throw new Error(`The export folder component "${component}" cannot end with a period or space.`);
      }
      if (RESERVED_WINDOWS_NAME.test(component)) {
        throw new Error(`The export folder component "${component}" is a reserved Windows name.`);
      }
    }

    return components.join('/');
  }

  function joinExportPath(subfolder, filename) {
    const normalized = normalizeExportFolder(subfolder);
    const safeFilename = String(filename || '').replace(/[\\/]/g, '');
    if (!safeFilename) {
      throw new Error('An export filename is required.');
    }
    return normalized ? `${normalized}/${safeFilename}` : safeFilename;
  }

  globalThis.ChefsExportPath = Object.freeze({
    normalizeExportFolder,
    joinExportPath
  });
})();
