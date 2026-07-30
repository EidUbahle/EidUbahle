/**
 * EidUbahle ERP – Offline Sync Engine
 *
 * Responsibilities:
 *  1. Monitor online/offline status
 *  2. Push queued operations to server (with exponential backoff)
 *  3. Pull changes from server (incremental, timestamp-based)
 *  4. Detect and route conflicts to the conflict resolver
 *  5. Provide manual sync trigger
 *  6. Emit events for UI updates (status bar, notifications)
 */
'use strict';

const SyncEngine = (() => {
  // ── Config ──────────────────────────────────────────────────────────────────
  const CFG = {
    pushUrl:          '/Handlers/SyncPush.ashx',
    pullUrl:          '/Handlers/SyncPull.ashx',
    maxBatchSize:     50,       // records per push batch
    maxRetries:       5,
    backoffBase:      1000,     // ms
    backoffMax:       60000,    // 1 min cap
    autoSyncInterval: 30000,    // 30s when online
    fullResyncThreshold: 7 * 24 * 60 * 60 * 1000, // 7 days
  };

  // ── State ───────────────────────────────────────────────────────────────────
  let _isOnline    = navigator.onLine;
  let _isSyncing   = false;
  let _syncTimer   = null;
  let _lastSyncAt  = null;
  let _conflictCount = 0;

  // ── Init ────────────────────────────────────────────────────────────────────
  function init() {
    window.addEventListener('online',  onOnline);
    window.addEventListener('offline', onOffline);

    // Listen for SW-triggered sync
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.addEventListener('message', msg => {
        if (msg.data?.type === 'SW_SYNC_TRIGGER') sync();
      });
    }

    // Restore last sync time from IndexedDB
    EidDB.getSyncMeta('lastSyncAt').then(v => {
      _lastSyncAt = v ? new Date(v) : null;
      _emit('status', buildStatus());
    });

    _emit('status', buildStatus());
    if (_isOnline) scheduleAutoSync();
  }

  // ── Online / Offline handlers ────────────────────────────────────────────────
  function onOnline() {
    _isOnline = true;
    _emit('online');
    _emit('status', buildStatus());
    sync(); // immediate sync when back online
    scheduleAutoSync();
  }

  function onOffline() {
    _isOnline = false;
    clearTimeout(_syncTimer);
    _emit('offline');
    _emit('status', buildStatus());
  }

  // ── Auto-sync scheduler ──────────────────────────────────────────────────────
  function scheduleAutoSync() {
    clearTimeout(_syncTimer);
    _syncTimer = setTimeout(() => {
      if (_isOnline) sync().then(() => scheduleAutoSync());
    }, CFG.autoSyncInterval);
  }

  // ── Main sync entry point ────────────────────────────────────────────────────
  async function sync(force = false) {
    if (_isSyncing && !force) return;
    if (!_isOnline) {
      _emit('status', buildStatus());
      return;
    }

    _isSyncing = true;
    _emit('syncing');
    _emit('status', buildStatus());

    const start = Date.now();
    let pushed = 0, pulled = 0, conflicts = 0;

    try {
      // Check if a full re-sync is needed
      if (needsFullResync()) {
        const confirmed = await _confirm('sync.confirm_full_resync');
        if (confirmed) {
          await fullResync();
          return;
        }
      }

      pushed    = await pushPendingOps();
      pulled    = await pullChanges();

    } catch (err) {
      console.error('[Sync] Error:', err);
      _emit('error', err.message);
    } finally {
      _isSyncing = false;
      _lastSyncAt = new Date();
      await EidDB.setSyncMeta('lastSyncAt', _lastSyncAt.toISOString());
      _emit('done', { pushed, pulled, conflicts, durationMs: Date.now() - start });
      _emit('status', buildStatus());
    }
  }

  // ── Push pending operations ──────────────────────────────────────────────────
  async function pushPendingOps() {
    const pending = await EidDB.getPendingSyncOps();
    if (!pending.length) return 0;

    let totalPushed = 0;

    // Process in batches
    for (let i = 0; i < pending.length; i += CFG.maxBatchSize) {
      const batch = pending.slice(i, i + CFG.maxBatchSize);
      const payload = {
        deviceId: EidDB.getDeviceId(),
        lastSyncAt: _lastSyncAt?.toISOString() ?? new Date(0).toISOString(),
        records: batch.map(item => ({
          entityType:      item.entityType,
          entityId:        item.entityId,
          operation:       item.operation,
          payloadJson:     item.payloadJson,
          baseVersion:     item.baseVersion,
          clientTimestamp: item.clientTimestamp,
        })),
      };

      const res = await ajaxPost(CFG.pushUrl, payload, 3);
      if (!res) continue;

      if (res.accepted) totalPushed += res.accepted;

      // Mark accepted records as done
      for (const item of batch) {
        if (!res.conflicts?.find(c => c.entityId === item.entityId)) {
          await EidDB.markSyncDone(item.localId);
        }
      }

      // Handle conflicts
      if (res.conflicts?.length) {
        for (const conflict of res.conflicts) {
          await storeConflict(conflict, batch);
        }
        _conflictCount += res.conflicts.length;
        _emit('conflicts', _conflictCount);
      }
    }

    return totalPushed;
  }

  // ── Pull changes from server ─────────────────────────────────────────────────
  async function pullChanges() {
    const since = await EidDB.getSyncMeta('serverTimestamp') ?? new Date(0).toISOString();
    let pulled = 0;
    let cursor = null;

    do {
      const params = new URLSearchParams({ since, deviceId: EidDB.getDeviceId() });
      if (cursor) params.append('cursor', cursor);

      const res = await ajaxGet(`${CFG.pullUrl}?${params}`, 3);
      if (!res?.success) break;

      // Apply changes to IndexedDB
      await applyServerChanges(res.records || []);
      pulled += (res.records || []).length;

      if (res.serverTimestamp) {
        await EidDB.setSyncMeta('serverTimestamp', res.serverTimestamp);
      }

      cursor = res.hasMore ? res.continuationToken : null;

    } while (cursor);

    return pulled;
  }

  // ── Apply server changes to local DB ────────────────────────────────────────
  async function applyServerChanges(records) {
    for (const record of records) {
      try {
        const payload = JSON.parse(record.payloadJson);
        const tbl = entityToTable(record.entityType);
        if (!tbl) continue;

        if (record.operation === EidDB.SyncOp.DELETE) {
          await EidDB.db[tbl].where('id').equals(record.entityId).delete();
        } else {
          await EidDB.db[tbl].put({ ...payload, id: record.entityId });
        }
      } catch (err) {
        console.error('[Sync] Failed to apply record:', record.entityType, err);
      }
    }
  }

  // ── Full Re-sync ────────────────────────────────────────────────────────────
  async function fullResync() {
    _emit('status', buildStatus('full-resync'));
    await EidDB.setSyncMeta('serverTimestamp', new Date(0).toISOString());
    await pullChanges();
  }

  function needsFullResync() {
    if (!_lastSyncAt) return false;
    return (Date.now() - _lastSyncAt.getTime()) > CFG.fullResyncThreshold;
  }

  // ── Conflict handling ────────────────────────────────────────────────────────
  async function storeConflict(serverConflict, batch) {
    const queueItem = batch.find(b => b.entityId === serverConflict.entityId);
    await EidDB.db.syncConflicts.put({
      id:          serverConflict.conflictId,
      entityType:  serverConflict.entityType,
      entityId:    serverConflict.entityId,
      clientJson:  serverConflict.clientJson,
      serverJson:  serverConflict.serverJson,
      resolution:  'Pending',
      detectedAt:  new Date().toISOString(),
    });
    if (queueItem) {
      await EidDB.db.syncQueue.update(queueItem.localId, { status: EidDB.SyncStatus.CONFLICT });
    }
  }

  /**
   * Resolve a conflict and push the decision to the server.
   * resolution: 'ServerWins' | 'ClientWins' | 'ManualMerge'
   * mergedPayload: only needed for ManualMerge
   */
  async function resolveConflict(conflictId, resolution, mergedPayload = null) {
    const conflict = await EidDB.db.syncConflicts.get(conflictId);
    if (!conflict) throw new Error('Conflict not found');

    const payload = {
      conflictId,
      entityType: conflict.entityType,
      entityId:   conflict.entityId,
      resolution,
      mergedPayload: mergedPayload ? JSON.stringify(mergedPayload) : null,
    };

    const res = await ajaxPost('/Handlers/SyncResolveConflict.ashx', payload, 2);
    if (res?.success) {
      await EidDB.db.syncConflicts.update(conflictId, { resolution, resolvedAt: new Date().toISOString() });
      _conflictCount = Math.max(0, _conflictCount - 1);
      _emit('status', buildStatus());
    }
    return res;
  }

  // ── AJAX helpers ─────────────────────────────────────────────────────────────
  async function ajaxPost(url, data, maxRetries = 3) {
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        return await new Promise((resolve, reject) => {
          const xhr = new XMLHttpRequest();
          xhr.open('POST', url, true);
          xhr.setRequestHeader('Content-Type', 'application/json');
          xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
          const token = localStorage.getItem('eid_access_token');
          if (token) xhr.setRequestHeader('X-Auth-Token', token);
          xhr.timeout = 30000;
          xhr.onload = () => {
            try { resolve(JSON.parse(xhr.responseText)); }
            catch { reject(new Error('Invalid JSON response')); }
          };
          xhr.onerror = () => reject(new Error('Network error'));
          xhr.ontimeout = () => reject(new Error('Request timeout'));
          xhr.send(JSON.stringify(data));
        });
      } catch (err) {
        if (attempt === maxRetries) return null;
        await sleep(Math.min(CFG.backoffBase * Math.pow(2, attempt), CFG.backoffMax));
      }
    }
    return null;
  }

  async function ajaxGet(url, maxRetries = 3) {
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        return await new Promise((resolve, reject) => {
          const xhr = new XMLHttpRequest();
          xhr.open('GET', url, true);
          xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
          const token = localStorage.getItem('eid_access_token');
          if (token) xhr.setRequestHeader('X-Auth-Token', token);
          xhr.timeout = 30000;
          xhr.onload = () => {
            try { resolve(JSON.parse(xhr.responseText)); }
            catch { reject(new Error('Invalid JSON response')); }
          };
          xhr.onerror  = () => reject(new Error('Network error'));
          xhr.ontimeout = () => reject(new Error('Request timeout'));
          xhr.send();
        });
      } catch (err) {
        if (attempt === maxRetries) return null;
        await sleep(Math.min(CFG.backoffBase * Math.pow(2, attempt), CFG.backoffMax));
      }
    }
    return null;
  }

  // ── Event bus ────────────────────────────────────────────────────────────────
  const _listeners = {};

  function on(event, handler) {
    if (!_listeners[event]) _listeners[event] = [];
    _listeners[event].push(handler);
  }

  function off(event, handler) {
    if (!_listeners[event]) return;
    _listeners[event] = _listeners[event].filter(h => h !== handler);
  }

  function _emit(event, data) {
    (_listeners[event] || []).forEach(h => { try { h(data); } catch {} });
    // Also dispatch DOM event for decoupled UI components
    document.dispatchEvent(new CustomEvent(`eid:sync:${event}`, { detail: data }));
  }

  // ── Status object ─────────────────────────────────────────────────────────────
  function buildStatus(overrideState = null) {
    const state = overrideState ?? (_isSyncing ? 'syncing' : (_isOnline ? 'online' : 'offline'));
    return {
      state,
      isOnline:       _isOnline,
      isSyncing:      _isSyncing,
      lastSyncAt:     _lastSyncAt?.toISOString() ?? null,
      conflictCount:  _conflictCount,
    };
  }

  function getStatus() { return buildStatus(); }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

  function entityToTable(entityType) {
    const map = {
      User: 'users', Role: 'roles', Company: 'companies', Branch: 'branches',
      Language: 'languages', Translation: 'translations',
    };
    return map[entityType] ?? entityType.toLowerCase() + 's';
  }

  async function _confirm(translationKey) {
    const msg = window.i18n?.t(translationKey) ?? 'Continue?';
    return window.confirm(msg);
  }

  // ── Public API ────────────────────────────────────────────────────────────────
  return { init, sync, resolveConflict, on, off, getStatus, getConflictCount: () => _conflictCount };
})();

// Auto-init when DOM is ready
document.addEventListener('DOMContentLoaded', () => SyncEngine.init());

window.SyncEngine = SyncEngine;
