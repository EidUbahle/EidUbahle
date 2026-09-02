const CACHE_NAME = 'wamo-cache-v1';
const OFFLINE_URL = '/PWA/offline.html';
const ASSETS = ['/Default.aspx','/Content/css/wamo-themes.css','/Content/js/wamo.js',OFFLINE_URL];
self.addEventListener('install', event => event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(ASSETS)).then(() => self.skipWaiting())));
self.addEventListener('activate', event => event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))).then(() => self.clients.claim())));
self.addEventListener('fetch', event => { if (event.request.method !== 'GET') return; event.respondWith(fetch(event.request).then(response => { const copy = response.clone(); caches.open(CACHE_NAME).then(cache => cache.put(event.request, copy)); return response; }).catch(() => caches.match(event.request).then(match => match || caches.match(OFFLINE_URL)))); });
