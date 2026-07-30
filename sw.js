/**
 * EidUbahle ERP – Service Worker
 * Strategy:
 *   - App Shell (HTML, CSS, JS, fonts): Cache-First, fallback to network
 *   - API / AJAX calls: Network-First, fallback to cached response
 *   - Background Sync: queued operations pushed when back online
 */
'use strict';

const CACHE_VERSION   = 'eid-v1.0.0';
const STATIC_CACHE    = `${CACHE_VERSION}-static`;
const DYNAMIC_CACHE   = `${CACHE_VERSION}-dynamic`;
const SYNC_TAG        = 'eid-sync';

// ── App Shell assets (cache on install) ────────────────────────────────────
const APP_SHELL = [
  '/',
  '/Pages/Login.aspx',
  '/Scripts/app.js',
  '/Scripts/offline/dexie.min.js',
  '/Scripts/offline/db.js',
  '/Scripts/sync/sync-engine.js',
  '/Scripts/modules/i18n.js',
  '/Scripts/modules/theme.js',
  '/Scripts/modules/table.js',
  '/Scripts/modules/notifications.js',
  '/Styles/main.css',
  '/Styles/themes.css',
  '/Styles/components.css',
  '/offline.html',
];

// ── Install: cache app shell ─────────────────────────────────────────────────
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(STATIC_CACHE).then(cache => {
      console.log('[SW] Caching app shell');
      return Promise.allSettled(
        APP_SHELL.map(url =>
          cache.add(url).catch(err => console.warn(`[SW] Failed to cache ${url}:`, err))
        )
      );
    }).then(() => self.skipWaiting())
  );
});

// ── Activate: clean up old caches ────────────────────────────────────────────
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys.filter(k => k !== STATIC_CACHE && k !== DYNAMIC_CACHE)
            .map(k => { console.log('[SW] Deleting old cache:', k); return caches.delete(k); })
      )
    ).then(() => self.clients.claim())
  );
});

// ── Fetch strategy ───────────────────────────────────────────────────────────
self.addEventListener('fetch', event => {
  const req = event.request;
  const url = new URL(req.url);

  // Skip non-GET and non-same-origin
  if (req.method !== 'GET' || url.origin !== self.location.origin) return;

  // Skip AJAX / API calls (handled by network-first below)
  const isAPI = req.headers.get('X-Requested-With') === 'XMLHttpRequest'
             || req.headers.get('X-Auth-Token')
             || url.pathname.endsWith('.ashx');

  if (isAPI) {
    // Network-first for API calls
    event.respondWith(
      fetch(req.clone()).then(res => {
        if (res && res.status === 200) {
          const clone = res.clone();
          caches.open(DYNAMIC_CACHE).then(c => c.put(req, clone));
        }
        return res;
      }).catch(() =>
        caches.match(req).then(cached => cached || new Response(
          JSON.stringify({ success: false, errorCode: 'ERR_OFFLINE', message: 'You are offline' }),
          { status: 503, headers: { 'Content-Type': 'application/json' } }
        ))
      )
    );
    return;
  }

  // Cache-first for static assets (CSS, JS, fonts, images)
  const isStatic = /\.(css|js|woff2?|ttf|eot|png|jpg|jpeg|svg|ico|webp)$/i.test(url.pathname);
  if (isStatic) {
    event.respondWith(
      caches.match(req).then(cached => cached ||
        fetch(req).then(res => {
          if (res && res.status === 200) {
            const clone = res.clone();
            caches.open(STATIC_CACHE).then(c => c.put(req, clone));
          }
          return res;
        })
      )
    );
    return;
  }

  // HTML pages: network-first, fall back to cached, then offline.html
  event.respondWith(
    fetch(req).then(res => {
      if (res && res.status === 200) {
        const clone = res.clone();
        caches.open(DYNAMIC_CACHE).then(c => c.put(req, clone));
      }
      return res;
    }).catch(() =>
      caches.match(req).then(cached => cached || caches.match('/offline.html'))
    )
  );
});

// ── Background Sync ──────────────────────────────────────────────────────────
self.addEventListener('sync', event => {
  if (event.tag === SYNC_TAG) {
    console.log('[SW] Background sync triggered');
    event.waitUntil(triggerSync());
  }
});

async function triggerSync() {
  // Notify all clients to run their sync engine
  const clients = await self.clients.matchAll({ type: 'window' });
  for (const client of clients) {
    client.postMessage({ type: 'SW_SYNC_TRIGGER', timestamp: Date.now() });
  }
}

// ── Push Notifications ───────────────────────────────────────────────────────
self.addEventListener('push', event => {
  if (!event.data) return;
  let data;
  try { data = event.data.json(); } catch { data = { title: 'EidUbahle ERP', body: event.data.text() }; }

  event.waitUntil(
    self.registration.showNotification(data.title || 'EidUbahle ERP', {
      body: data.body || '',
      icon: '/images/icons/icon-192.png',
      badge: '/images/icons/badge-72.png',
      tag: data.tag || 'eid-notification',
      data: data.url || '/',
      requireInteraction: data.requireInteraction || false
    })
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data || '/';
  event.waitUntil(
    clients.matchAll({ type: 'window' }).then(cs => {
      const match = cs.find(c => c.url === url);
      return match ? match.focus() : clients.openWindow(url);
    })
  );
});

// ── Message handling ─────────────────────────────────────────────────────────
self.addEventListener('message', event => {
  if (event.data?.type === 'SKIP_WAITING') self.skipWaiting();
  if (event.data?.type === 'REQUEST_SYNC') {
    self.registration.sync?.register(SYNC_TAG).catch(() => triggerSync());
  }
});
