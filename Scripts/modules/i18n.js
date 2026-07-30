/**
 * EidUbahle ERP – i18n (Internationalization) Module
 *
 * Features:
 *  - Downloads translation bundle from server on login
 *  - Caches in IndexedDB (EidDB) for offline use
 *  - Supports RTL/LTR dynamic switching
 *  - Provides t(key, ...args) helper used everywhere in the UI
 *  - Zero hardcoded strings anywhere in the front-end
 */
'use strict';

const i18n = (() => {
  let _bundle = {};
  let _lang   = 'en';
  let _dir    = 'ltr';
  let _ready  = false;

  // ── Init ────────────────────────────────────────────────────────────────────
  async function init(preferredLang) {
    _lang = preferredLang || localStorage.getItem('eid_lang') || 'en';
    await load(_lang);
  }

  // ── Load bundle ─────────────────────────────────────────────────────────────
  async function load(langCode) {
    // Try IndexedDB first (offline)
    const cached = await EidDB.getCachedTranslations(langCode);
    if (cached && Object.keys(cached).length > 0) {
      _bundle = cached;
      _lang   = langCode;
      _dir    = cached['__dir'] ?? 'ltr';
      applyDirection();
      _ready = true;
      _emit('ready');
      // Refresh from server in background
      fetchFromServer(langCode).then(bundle => {
        if (bundle) EidDB.cacheTranslationBundle(langCode, bundle);
      });
      return;
    }
    // No cache – fetch from server
    const bundle = await fetchFromServer(langCode);
    if (bundle) {
      _bundle = bundle;
      _lang   = langCode;
      _dir    = bundle['__dir'] ?? 'ltr';
      applyDirection();
      await EidDB.cacheTranslationBundle(langCode, bundle);
    } else {
      // Fallback to English embedded defaults
      _bundle = getEmbeddedFallback();
    }
    _ready = true;
    _emit('ready');
  }

  // ── Fetch from server ────────────────────────────────────────────────────────
  async function fetchFromServer(langCode) {
    try {
      return await new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', `/Handlers/Translations.ashx?lang=${encodeURIComponent(langCode)}`, true);
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        const token = localStorage.getItem('eid_access_token');
        if (token) xhr.setRequestHeader('X-Auth-Token', token);
        xhr.timeout = 10000;
        xhr.onload = () => {
          try {
            const data = JSON.parse(xhr.responseText);
            resolve(data.success ? data.data?.translations : null);
          } catch { resolve(null); }
        };
        xhr.onerror  = () => resolve(null);
        xhr.ontimeout = () => resolve(null);
        xhr.send();
      });
    } catch { return null; }
  }

  // ── Translate ────────────────────────────────────────────────────────────────
  /**
   * Get translation for a key, with optional {0}, {1} placeholder substitution.
   * Falls back to the key itself so the UI never shows blank text.
   */
  function t(key, ...args) {
    let text = _bundle[key] ?? key;
    if (args.length) {
      args.forEach((arg, i) => {
        text = text.replace(new RegExp(`\\{${i}\\}`, 'g'), arg ?? '');
      });
    }
    return text;
  }

  // ── Change language at runtime ────────────────────────────────────────────────
  async function changeLanguage(langCode) {
    if (langCode === _lang) return;
    await load(langCode);
    localStorage.setItem('eid_lang', langCode);
    // Re-render all elements with data-i18n attributes
    translatePage();
    document.dispatchEvent(new CustomEvent('eid:i18n:changed', { detail: { lang: langCode, dir: _dir } }));
  }

  // ── Translate page ────────────────────────────────────────────────────────────
  /**
   * Apply translations to all elements with:
   *   data-i18n="key"               → sets innerText
   *   data-i18n-placeholder="key"   → sets placeholder attribute
   *   data-i18n-title="key"         → sets title attribute
   *   data-i18n-aria-label="key"    → sets aria-label
   */
  function translatePage() {
    document.querySelectorAll('[data-i18n]').forEach(el => {
      el.textContent = t(el.dataset.i18n);
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
      el.placeholder = t(el.dataset.i18nPlaceholder);
    });
    document.querySelectorAll('[data-i18n-title]').forEach(el => {
      el.title = t(el.dataset.i18nTitle);
    });
    document.querySelectorAll('[data-i18n-aria-label]').forEach(el => {
      el.setAttribute('aria-label', t(el.dataset.i18nAriaLabel));
    });
  }

  // ── RTL/LTR ──────────────────────────────────────────────────────────────────
  function applyDirection() {
    document.documentElement.setAttribute('dir', _dir);
    document.documentElement.setAttribute('lang', _lang);
    document.documentElement.classList.toggle('rtl', _dir === 'rtl');
    document.documentElement.classList.toggle('ltr', _dir === 'ltr');
  }

  // ── Embedded fallback (minimal English) ─────────────────────────────────────
  function getEmbeddedFallback() {
    return {
      'login.title': 'Sign In', 'login.username': 'Username or Email',
      'login.password': 'Password', 'login.signin': 'Sign In',
      'common.loading': 'Loading…', 'common.error': 'An error occurred.',
      'common.offline': 'Offline', 'common.sync_now': 'Sync Now',
      '__dir': 'ltr',
    };
  }

  // ── Event bus ────────────────────────────────────────────────────────────────
  const _listeners = {};
  function on(event, handler) {
    if (!_listeners[event]) _listeners[event] = [];
    _listeners[event].push(handler);
  }
  function _emit(event, data) {
    (_listeners[event] || []).forEach(h => h(data));
  }

  return { init, load, t, changeLanguage, translatePage, applyDirection, on, getLang: () => _lang, getDir: () => _dir, isReady: () => _ready };
})();

window.i18n = i18n;
