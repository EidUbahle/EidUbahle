/**
 * EidUbahle ERP – Theme Engine
 *
 * Features:
 *  - Dark / Light / Auto (follows system preference) mode
 *  - Custom accent color per tenant and per user
 *  - Company branding (logo, name in sidebar)
 *  - CSS custom property injection (all colors, radii, shadows)
 *  - Persisted preference in localStorage + IndexedDB
 *  - Instant switching without page reload
 *  - Supports 3 layout types: classic | topnav | compact
 */
'use strict';

const ThemeEngine = (() => {
  const STORAGE_KEY_MODE   = 'eid_theme_mode';
  const STORAGE_KEY_ACCENT = 'eid_accent_color';
  const STORAGE_KEY_LAYOUT = 'eid_layout';

  const DEFAULTS = {
    accentColor: '#2563EB',
    mode: 'auto',
    layout: 'classic',
  };

  let _mode    = DEFAULTS.mode;
  let _accent  = DEFAULTS.accentColor;
  let _layout  = DEFAULTS.layout;
  let _mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  // ── Init ────────────────────────────────────────────────────────────────────
  function init(session) {
    _mode   = session?.themeMode   ?? localStorage.getItem(STORAGE_KEY_MODE)   ?? DEFAULTS.mode;
    _accent = session?.accentColor ?? localStorage.getItem(STORAGE_KEY_ACCENT) ?? session?.tenantAccentColor ?? DEFAULTS.accentColor;
    _layout = session?.activeLayout ?? localStorage.getItem(STORAGE_KEY_LAYOUT) ?? session?.tenantLayout ?? DEFAULTS.layout;

    applyTheme();
    applyLayout(_layout);

    // React to OS theme changes when mode = auto
    _mediaQuery.addEventListener('change', () => {
      if (_mode === 'auto') applyTheme();
    });
  }

  // ── Apply theme ──────────────────────────────────────────────────────────────
  function applyTheme() {
    const isDark = resolveIsDark();
    document.documentElement.classList.toggle('theme-dark',  isDark);
    document.documentElement.classList.toggle('theme-light', !isDark);
    injectCSSProperties(isDark);
    // Signal body to become visible (prevents FOUC)
    document.body.setAttribute('data-theme-ready', '1');
  }

  function resolveIsDark() {
    if (_mode === 'dark')  return true;
    if (_mode === 'light') return false;
    return _mediaQuery.matches;
  }

  // ── CSS custom properties ─────────────────────────────────────────────────────
  function injectCSSProperties(isDark) {
    const accent = _accent || DEFAULTS.accentColor;
    const accentDark   = shiftColor(accent, isDark ? 20 : -20);
    const accentLight  = hexToRgba(accent, 0.12);
    const accentText   = getContrastColor(accent);

    const vars = isDark ? {
      '--bg-base':        '#0f172a',
      '--bg-surface':     '#1e293b',
      '--bg-elevated':    '#273549',
      '--bg-overlay':     '#334155',
      '--text-primary':   '#f1f5f9',
      '--text-secondary': '#94a3b8',
      '--text-muted':     '#64748b',
      '--border-color':   '#334155',
      '--divider-color':  '#1e293b',
      '--shadow-color':   '0,0,0',
    } : {
      '--bg-base':        '#f8fafc',
      '--bg-surface':     '#ffffff',
      '--bg-elevated':    '#f1f5f9',
      '--bg-overlay':     '#e2e8f0',
      '--text-primary':   '#0f172a',
      '--text-secondary': '#475569',
      '--text-muted':     '#94a3b8',
      '--border-color':   '#e2e8f0',
      '--divider-color':  '#f1f5f9',
      '--shadow-color':   '148,163,184',
    };

    // Accent colors (same for both modes, adjusted slightly)
    vars['--accent']        = accent;
    vars['--accent-dark']   = accentDark;
    vars['--accent-light']  = accentLight;
    vars['--accent-text']   = accentText;

    // Semantic colors
    vars['--success']  = '#10b981';
    vars['--warning']  = '#f59e0b';
    vars['--danger']   = '#ef4444';
    vars['--info']     = '#3b82f6';
    vars['--success-light'] = hexToRgba('#10b981', 0.12);
    vars['--warning-light'] = hexToRgba('#f59e0b', 0.12);
    vars['--danger-light']  = hexToRgba('#ef4444', 0.12);
    vars['--info-light']    = hexToRgba('#3b82f6', 0.12);

    // Design tokens
    vars['--radius-sm']   = '6px';
    vars['--radius-md']   = '10px';
    vars['--radius-lg']   = '16px';
    vars['--radius-xl']   = '24px';
    vars['--radius-full'] = '9999px';
    vars['--sidebar-width']          = '260px';
    vars['--sidebar-width-collapsed'] = '64px';
    vars['--topnav-height']          = '60px';
    vars['--transition']             = 'all 0.2s cubic-bezier(0.4,0,0.2,1)';
    vars['--font-sans']  = "'Inter', system-ui, -apple-system, sans-serif";
    vars['--font-mono']  = "'JetBrains Mono', 'Fira Code', monospace";

    const root = document.documentElement;
    for (const [k, v] of Object.entries(vars)) root.style.setProperty(k, v);
  }

  // ── Layout ────────────────────────────────────────────────────────────────────
  function applyLayout(layout) {
    _layout = layout;
    document.documentElement.setAttribute('data-layout', layout);
    document.body.setAttribute('data-layout', layout);
    localStorage.setItem(STORAGE_KEY_LAYOUT, layout);

    // Show/hide layout-specific master page containers
    document.querySelectorAll('[data-layout-only]').forEach(el => {
      el.style.display = el.dataset.layoutOnly === layout ? '' : 'none';
    });
  }

  // ── Public setters ────────────────────────────────────────────────────────────
  function setMode(mode) {  // 'auto' | 'light' | 'dark'
    _mode = mode;
    localStorage.setItem(STORAGE_KEY_MODE, mode);
    applyTheme();
    syncPref('themeMode', mode);
  }

  function setAccentColor(color) {
    _accent = color;
    localStorage.setItem(STORAGE_KEY_ACCENT, color);
    applyTheme();
    syncPref('accentColor', color);
  }

  function setLayout(layout) {
    applyLayout(layout);
    syncPref('activeLayout', layout);
  }

  // ── Sync preference to server (best-effort) ───────────────────────────────────
  function syncPref(key, value) {
    const token = localStorage.getItem('eid_access_token');
    if (!token) return;
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/Handlers/UserPreference.ashx', true);
    xhr.setRequestHeader('Content-Type', 'application/json');
    xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
    xhr.setRequestHeader('X-Auth-Token', token);
    xhr.send(JSON.stringify({ key, value }));
  }

  // ── Color utilities ───────────────────────────────────────────────────────────
  function hexToRgb(hex) {
    const r = parseInt(hex.slice(1,3),16), g = parseInt(hex.slice(3,5),16), b = parseInt(hex.slice(5,7),16);
    return { r, g, b };
  }

  function hexToRgba(hex, alpha) {
    const { r, g, b } = hexToRgb(hex);
    return `rgba(${r},${g},${b},${alpha})`;
  }

  function shiftColor(hex, amount) {
    let { r, g, b } = hexToRgb(hex);
    r = Math.max(0, Math.min(255, r + amount));
    g = Math.max(0, Math.min(255, g + amount));
    b = Math.max(0, Math.min(255, b + amount));
    return `#${r.toString(16).padStart(2,'0')}${g.toString(16).padStart(2,'0')}${b.toString(16).padStart(2,'0')}`;
  }

  function getContrastColor(hex) {
    const { r, g, b } = hexToRgb(hex);
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminance > 0.5 ? '#0f172a' : '#ffffff';
  }

  return {
    init,
    setMode,
    setAccentColor,
    setLayout,
    applyLayout,
    isDark: () => resolveIsDark(),
    getMode:   () => _mode,
    getAccent: () => _accent,
    getLayout: () => _layout,
  };

  function resolveIsDark() {
    if (_mode === 'dark')  return true;
    if (_mode === 'light') return false;
    return _mediaQuery.matches;
  }
})();

window.ThemeEngine = ThemeEngine;
