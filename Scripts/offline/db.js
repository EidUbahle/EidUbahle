/**
 * EidUbahle ERP – Dexie.js (IndexedDB) Schema
 * Mirrors the MSSQL schema closely so the offline layer is a true replica.
 * Version-based migrations keep the schema forward-compatible.
 */
'use strict';

// ── EidDB class ───────────────────────────────────────────────────────────────
class EidDatabase extends Dexie {
  constructor() {
    super('EidUbahleERP');
    this._defineSchemas();
  }

  _defineSchemas() {
    // Version 1 – Phase 1: Core entities
    this.version(1).stores({
      // ── Auth / Session ──────────────────────────────────────────
      userSession: '++id, userId, tenantId, deviceId',
      translations: '[languageCode+key], languageCode, module',
      languages: 'code, isActive',
      featureFlags: '[tenantId+featureKey], tenantId',

      // ── SaaS / Tenants ──────────────────────────────────────────
      tenants: 'id, subdomain',
      companies: 'id, tenantId, isActive',
      branches: 'id, companyId, tenantId, isActive',

      // ── Users & Roles ────────────────────────────────────────────
      users: 'id, tenantId, username, email',
      roles: 'id, tenantId',
      permissions: 'id, permissionKey, module',
      userRoles: 'id, userId, roleId',
      userCompanyBranches: 'id, userId, companyId, branchId',

      // ── Sync Queue ───────────────────────────────────────────────
      // Every offline write is queued here and processed by the sync engine
      syncQueue: '++localId, entityType, entityId, operation, status, createdAt, retryCount',

      // ── Sync Conflicts ───────────────────────────────────────────
      syncConflicts: 'id, tenantId, entityType, entityId, resolution, detectedAt',

      // ── Sync Metadata ────────────────────────────────────────────
      syncMeta: 'key', // key-value store for sync pointers (lastSyncAt, serverTimestamp, etc.)

      // ── Settings ─────────────────────────────────────────────────
      settings: '[scope+key], scope',

      // ── Table Column Preferences (per user per table) ────────────
      tablePrefs: '[userId+tableId], userId',

      // ── Dashboard Layouts ────────────────────────────────────────
      dashboardLayouts: '[userId+dashboardId], userId',
    });

    // Future versions go here (non-breaking additions)
    // this.version(2).stores({ ... }).upgrade(tx => { ... });
  }
}

// ── Singleton DB instance ─────────────────────────────────────────────────────
const db = new EidDatabase();

// ── Open & error handling ─────────────────────────────────────────────────────
db.open().catch(err => {
  console.error('[DB] Failed to open IndexedDB:', err);
  // Notify app that offline storage is unavailable
  document.dispatchEvent(new CustomEvent('eid:db:error', { detail: err }));
});

// ── Sync Queue helpers ────────────────────────────────────────────────────────
const SyncOp = {
  CREATE: 'create',
  UPDATE: 'update',
  DELETE: 'delete',
};

const SyncStatus = {
  PENDING:  'pending',
  RETRYING: 'retrying',
  DONE:     'done',
  FAILED:   'failed',
  CONFLICT: 'conflict',
};

/**
 * Add an operation to the sync queue.
 * All offline writes must call this so the background sync engine can push them.
 */
async function enqueueSyncOp(entityType, entityId, operation, payload, baseVersion = null) {
  await db.syncQueue.add({
    entityType,
    entityId,
    operation,
    payloadJson: JSON.stringify(payload),
    baseVersion,
    status: SyncStatus.PENDING,
    retryCount: 0,
    createdAt: new Date().toISOString(),
    clientTimestamp: new Date().toISOString(),
  });

  // Request a background sync if supported
  if ('serviceWorker' in navigator && 'SyncManager' in window) {
    const reg = await navigator.serviceWorker.ready;
    await reg.sync.register('eid-sync').catch(() => {});
  }
}

/**
 * Get all pending sync queue items.
 */
async function getPendingSyncOps() {
  return db.syncQueue
    .where('status').anyOf([SyncStatus.PENDING, SyncStatus.RETRYING])
    .toArray();
}

/**
 * Mark a sync queue item as done.
 */
async function markSyncDone(localId) {
  await db.syncQueue.update(localId, { status: SyncStatus.DONE });
}

/**
 * Mark a sync queue item as failed with retry count increment.
 */
async function markSyncRetry(localId, maxRetries = 5) {
  const item = await db.syncQueue.get(localId);
  if (!item) return;
  if (item.retryCount >= maxRetries) {
    await db.syncQueue.update(localId, { status: SyncStatus.FAILED });
  } else {
    await db.syncQueue.update(localId, {
      status: SyncStatus.RETRYING,
      retryCount: item.retryCount + 1,
    });
  }
}

// ── Translation cache helpers ─────────────────────────────────────────────────
async function getCachedTranslations(langCode) {
  const rows = await db.translations.where('languageCode').equals(langCode).toArray();
  const map = {};
  for (const row of rows) map[row.key] = row.value;
  return map;
}

async function cacheTranslationBundle(langCode, bundle) {
  const rows = Object.entries(bundle).map(([key, value]) => ({
    languageCode: langCode,
    key,
    value,
    module: 'General',
    cachedAt: new Date().toISOString(),
  }));
  await db.translations.bulkPut(rows);
}

// ── Sync metadata ─────────────────────────────────────────────────────────────
async function getSyncMeta(key) {
  const row = await db.syncMeta.get(key);
  return row?.value ?? null;
}

async function setSyncMeta(key, value) {
  await db.syncMeta.put({ key, value });
}

// ── User session ──────────────────────────────────────────────────────────────
async function saveUserSession(claims, accessToken, refreshToken, accessExpiry, refreshExpiry) {
  await db.userSession.clear();
  await db.userSession.add({
    userId: claims.userId,
    tenantId: claims.tenantId,
    username: claims.username,
    fullName: claims.fullName,
    email: claims.email,
    avatarUrl: claims.avatarUrl,
    languageCode: claims.languageCode,
    themeMode: claims.themeMode,
    activeLayout: claims.activeLayout,
    accentColor: claims.accentColor,
    isTenantAdmin: claims.isTenantAdmin,
    isSuperAdmin: claims.isSuperAdmin,
    permissions: JSON.stringify(claims.permissions),
    companyBranches: JSON.stringify(claims.companyBranches),
    activeCompanyId: claims.activeCompanyId,
    activeBranchId: claims.activeBranchId,
    activeCompanyName: claims.activeCompanyName,
    activeBranchName: claims.activeBranchName,
    tenantLogoUrl: claims.tenantLogoUrl,
    tenantAccentColor: claims.tenantAccentColor,
    tenantLayout: claims.tenantLayout,
    accessToken,
    refreshToken,
    accessExpiry,
    refreshExpiry,
    deviceId: getDeviceId(),
    savedAt: new Date().toISOString(),
  });
}

async function getCurrentSession() {
  const rows = await db.userSession.toArray();
  return rows.length > 0 ? rows[0] : null;
}

async function clearSession() {
  await db.userSession.clear();
}

// ── Device ID (stable per browser) ───────────────────────────────────────────
function getDeviceId() {
  let id = localStorage.getItem('eid_device_id');
  if (!id) {
    id = 'dev-' + Date.now() + '-' + Math.random().toString(36).slice(2);
    localStorage.setItem('eid_device_id', id);
  }
  return id;
}

// ── Table preferences ─────────────────────────────────────────────────────────
async function getTablePrefs(userId, tableId) {
  return db.tablePrefs.get([userId, tableId]);
}

async function saveTablePrefs(userId, tableId, visibleColumns, pageSize, sortBy, sortDir) {
  await db.tablePrefs.put({ userId, tableId, visibleColumns, pageSize, sortBy, sortDir, updatedAt: new Date().toISOString() });
}

// ── Export ────────────────────────────────────────────────────────────────────
window.EidDB = {
  db,
  SyncOp,
  SyncStatus,
  enqueueSyncOp,
  getPendingSyncOps,
  markSyncDone,
  markSyncRetry,
  getCachedTranslations,
  cacheTranslationBundle,
  getSyncMeta,
  setSyncMeta,
  saveUserSession,
  getCurrentSession,
  clearSession,
  getDeviceId,
  getTablePrefs,
  saveTablePrefs,
};
