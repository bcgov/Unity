'use strict';

(() => {
  const PROBE_TIMEOUT_MS = 45000;
  const PROBE_VISIBILITY_ATTEMPTS = 10;

  function delay(milliseconds) {
    return new Promise((resolve) => setTimeout(resolve, milliseconds));
  }

  async function waitForDownload(downloads, downloadId, timeoutMs) {
    return await new Promise((resolve, reject) => {
      let settled = false;
      const finish = (error) => {
        if (settled) {
          return;
        }
        settled = true;
        clearTimeout(timer);
        downloads.onChanged.removeListener(onChanged);
        if (error) {
          reject(error);
        } else {
          resolve();
        }
      };
      const inspect = (item) => {
        if (!item) {
          return;
        }
        if (item.state === 'complete') {
          finish();
        } else if (item.state === 'interrupted') {
          finish(new Error(item.error || 'The validation download was interrupted.'));
        }
      };
      const onChanged = (delta) => {
        if (!delta || delta.id !== downloadId) {
          return;
        }
        if (delta.state && delta.state.current === 'complete') {
          finish();
        } else if (delta.state && delta.state.current === 'interrupted') {
          finish(new Error(delta.error && delta.error.current || 'The validation download was interrupted.'));
        }
      };
      const timer = setTimeout(
        () => finish(new Error('Timed out while validating the selected folder.')),
        timeoutMs
      );
      downloads.onChanged.addListener(onChanged);
      downloads.search({ id: downloadId })
        .then((items) => inspect(items && items[0]))
        .catch(finish);
    });
  }

  async function probeAppearsInSelectedFolder(directoryHandle, probeName, attempts, wait) {
    for (let attempt = 0; attempt < attempts; attempt += 1) {
      try {
        await directoryHandle.getFileHandle(probeName);
        return true;
      } catch (error) {
        if (!error || error.name !== 'NotFoundError') {
          throw error;
        }
      }
      if (attempt + 1 < attempts) {
        await wait(100);
      }
    }
    return false;
  }

  async function cleanupProbe(downloads, downloadId) {
    if (downloadId === null || downloadId === undefined) {
      return;
    }
    try {
      await downloads.removeFile(downloadId);
    } catch (error) {
      // The file may already be absent after an interrupted or policy-blocked download.
    }
    try {
      await downloads.erase({ id: downloadId });
    } catch (error) {
      // Cleanup failure must not hide the validation result.
    }
  }

  async function validateSelectedFolder(directoryHandle, dependencies) {
    const options = dependencies || {};
    const downloads = options.downloads || chrome.downloads;
    const exportPath = options.exportPath || ChefsExportPath;
    const randomUUID = options.randomUUID || (() => crypto.randomUUID());
    const wait = options.delay || delay;
    const probeAttempts = options.probeAttempts || PROBE_VISIBILITY_ATTEMPTS;
    const timeoutMs = options.timeoutMs || PROBE_TIMEOUT_MS;

    if (!directoryHandle || directoryHandle.kind !== 'directory') {
      throw new Error('Select a folder directly inside Downloads.');
    }

    const folder = exportPath.normalizeExportFolder(directoryHandle.name);
    if (!folder || folder.includes('/')) {
      throw new Error('Select a folder directly inside Downloads. Clear Export Folder to use Downloads itself.');
    }

    const probeName = `chefs-export-folder-validation-${randomUUID()}.txt`;
    const downloadPath = exportPath.joinExportPath(folder, probeName);
    const url = `data:text/plain;charset=utf-8,${encodeURIComponent('CHEFS export folder validation. This temporary file should be removed automatically.')}`;
    let downloadId = null;

    try {
      downloadId = await downloads.download({
        url,
        filename: downloadPath,
        conflictAction: 'uniquify',
        saveAs: false
      });
      await waitForDownload(downloads, downloadId, timeoutMs);
      const found = await probeAppearsInSelectedFolder(
        directoryHandle,
        probeName,
        probeAttempts,
        wait
      );
      if (!found) {
        const error = new Error(
          'The selected folder is not a direct child of Downloads. Select a folder directly inside Downloads, or type a validated relative path.'
        );
        error.code = 'NOT_DOWNLOADS_CHILD';
        throw error;
      }
      return folder;
    } finally {
      await cleanupProbe(downloads, downloadId);
    }
  }

  async function selectValidatedFolder(dependencies) {
    const options = dependencies || {};
    const showPicker = options.showDirectoryPicker || globalThis.showDirectoryPicker;
    if (typeof showPicker !== 'function') {
      throw new Error('Folder selection is unavailable in this browser. Type a Downloads-relative folder instead.');
    }
    const directoryHandle = await showPicker({
      id: 'chefs-export-folder',
      mode: 'read',
      startIn: 'downloads'
    });
    return await validateSelectedFolder(directoryHandle, options);
  }

  globalThis.ChefsExportFolderPicker = Object.freeze({
    selectValidatedFolder,
    validateSelectedFolder
  });
})();
