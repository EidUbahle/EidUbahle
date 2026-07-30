/**
 * EidUbahle ERP – Main Application Module (app.js)
 *
 * Bootstraps the application: initializes theme, i18n, sync engine,
 * user session, navigation, toast notifications, global search,
 * language switcher, and all shared UI interactions.
 */
'use strict';

const App = (() => {
  let _session = null;

  // ── Bootstrap ────────────────────────────────────────────────────────────────
  async function boot() {
    // 1. Theme (applies instantly from localStorage – no flash)
    const session = await EidDB.getCurrentSession();
    _session = session;
    ThemeEngine.init(session);

    // 2. i18n
    const lang = session?.languageCode || localStorage.getItem('eid_lang') || 'en';
    await i18n.init(lang);

    // 3. Translate the current page
    i18n.translatePage();

    // 4. Populate user UI
    if (session) renderUserUI(session);

    // 5. Active nav item
    markActiveNav();

    // 6. Language switcher in topbar
    buildLangDropdown();

    // 7. Company/Branch selector
    buildBranchDropdown(session);

    // 8. Theme icon
    updateThemeIcon();

    // 9. Sync Engine events
    SyncEngine.on('status', onSyncStatus);
    SyncEngine.on('error',  msg => showToast(msg, 'error'));

    // 10. Token refresh timer
    scheduleTokenRefresh(session);

    // 11. Footer version
    const el = document.getElementById('footerVersion');
    if (el) el.textContent = `v${window.__APP_VERSION__ || '1.0.0'}`;

    // 12. Global keyboard shortcuts
    document.addEventListener('keydown', onKeyDown);

    // 13. Close dropdowns on outside click
    document.addEventListener('click', onDocumentClick);

    // 14. PWA update prompt
    listenForSwUpdate();
  }

  // ── User UI ───────────────────────────────────────────────────────────────────
  function renderUserUI(session) {
    // Avatars
    const initials = (session.fullName || session.username || 'U').split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
    const logoUrl = session.avatarUrl || null;

    document.querySelectorAll('#topbarAvatar, #sidebarAvatar').forEach(el => {
      if (logoUrl) {
        el.innerHTML = `<img src="${escHtml(logoUrl)}" alt="${escHtml(initials)}" />`;
      } else {
        el.textContent = initials;
        el.style.background = `hsl(${stringToHue(session.username || '')}, 60%, 45%)`;
        el.style.color = '#fff';
      }
    });

    // Names
    setTextIfExists('sidebarUserName',  session.fullName || session.username);
    setTextIfExists('sidebarBranchName', session.activeBranchName || '');
    setTextIfExists('userMenuName',  session.fullName || session.username);
    setTextIfExists('userMenuEmail', session.email || '');

    // Tenant branding
    if (session.tenantLogoUrl) {
      document.querySelectorAll('#tenantLogo').forEach(el => { el.src = session.tenantLogoUrl; });
    }
    if (session.tenantAccentColor) {
      ThemeEngine.setAccentColor(session.accentColor || session.tenantAccentColor);
    }
    setTextIfExists('tenantName', window.__EID_APP_NAME__ || 'EidUbahle ERP');
    setTextIfExists('activeBranchLabel', session.activeBranchName || 'Branch');
  }

  // ── Active navigation ─────────────────────────────────────────────────────────
  function markActiveNav() {
    const path = window.location.pathname.toLowerCase();
    document.querySelectorAll('.nav-item[data-page]').forEach(el => {
      el.classList.remove('active');
    });
    // Match by href prefix
    document.querySelectorAll('.nav-item').forEach(el => {
      const href = (el.getAttribute('href') || '').toLowerCase();
      if (href && href !== '/' && path.startsWith(href.split('?')[0])) {
        el.classList.add('active');
      }
    });
  }

  // ── Language dropdown ─────────────────────────────────────────────────────────
  function buildLangDropdown() {
    const container = document.getElementById('langDropdown');
    if (!container) return;
    const langs = [
      { code: 'en', label: 'English',   flag: '🇬🇧' },
      { code: 'so', label: 'Soomaali',  flag: '🇸🇴' },
      { code: 'ar', label: 'العربية',   flag: '🇸🇦' },
      { code: 'fr', label: 'Français',  flag: '🇫🇷' },
      { code: 'sw', label: 'Kiswahili', flag: '🇰🇪' },
      { code: 'es', label: 'Español',   flag: '🇪🇸' },
      { code: 'de', label: 'Deutsch',   flag: '🇩🇪' },
      { code: 'tr', label: 'Türkçe',    flag: '🇹🇷' },
    ];
    const current = i18n.getLang();
    container.innerHTML = langs.map(l =>
      `<a class="dropdown-item${l.code===current?' fw-600':''}" href="#" onclick="App.changeLang('${l.code}');return false;">
        <span>${l.flag}</span> <span>${escHtml(l.label)}</span>
        ${l.code===current ? '<i class="bi bi-check ms-auto"></i>' : ''}
      </a>`
    ).join('');
  }

  async function changeLang(code) {
    await i18n.changeLanguage(code);
    i18n.translatePage();
    buildLangDropdown();
    toggleDropdown('langDropdown');

    // Persist to server (best-effort)
    apiPost('/Handlers/UserPreference.ashx', { key: 'languageCode', value: code }).catch(() => {});
  }

  // ── Branch dropdown ────────────────────────────────────────────────────────────
  function buildBranchDropdown(session) {
    const container = document.getElementById('branchDropdown');
    if (!container || !session) return;
    try {
      const branches = JSON.parse(session.companyBranches || '[]');
      if (!branches.length) return;
      container.innerHTML = branches.map(b =>
        `<a class="dropdown-item${b.branchId===session.activeBranchId?' fw-600':''}" href="#"
            onclick="App.switchBranch('${b.companyId}','${b.branchId}');return false;">
          <i class="bi bi-building"></i>
          <span>${escHtml(b.companyName)} / ${escHtml(b.branchName)}</span>
          ${b.branchId===session.activeBranchId ? '<i class="bi bi-check ms-auto"></i>' : ''}
        </a>`
      ).join('');
    } catch { }
  }

  async function switchBranch(companyId, branchId) {
    toggleDropdown('branchDropdown');
    const res = await apiPost('/Handlers/UserPreference.ashx', { key: 'activeBranchId', value: branchId, companyId });
    if (res?.success) window.location.reload();
  }

  // ── Theme ──────────────────────────────────────────────────────────────────────
  function toggleTheme() {
    const newMode = ThemeEngine.isDark() ? 'light' : 'dark';
    ThemeEngine.setMode(newMode);
    updateThemeIcon();
  }

  function updateThemeIcon() {
    const icon = document.getElementById('themeIcon');
    if (!icon) return;
    icon.className = ThemeEngine.isDark() ? 'bi bi-moon-stars' : 'bi bi-sun';
  }

  // ── Sync status ────────────────────────────────────────────────────────────────
  function onSyncStatus(status) {
    const badge = document.getElementById('syncStatusBadge');
    const text  = document.getElementById('syncStatusText');
    if (!badge) return;

    badge.className = `sync-status-bar ${status.state}`;
    if (text) {
      if (status.state === 'syncing') text.textContent = i18n.t('sync.status.syncing');
      else if (!status.isOnline)      text.textContent = i18n.t('sync.status.offline');
      else if (status.conflictCount)  text.textContent = i18n.t('sync.status.conflict');
      else                            text.textContent = i18n.t('sync.status.online');
    }

    // Update notification badge
    const notif = document.getElementById('notifBadge');
    if (notif && status.conflictCount > 0) {
      notif.textContent = status.conflictCount;
      notif.style.display = 'block';
    } else if (notif) {
      notif.style.display = 'none';
    }
  }

  function showSyncPanel() {
    const overlay = document.getElementById('syncPanelOverlay');
    if (!overlay) return;
    overlay.classList.remove('d-none');
    renderSyncPanel();
  }

  async function renderSyncPanel() {
    const body = document.getElementById('syncPanelBody');
    if (!body) return;
    const status = SyncEngine.getStatus();
    const pending = await EidDB.getPendingSyncOps();
    const conflicts = await EidDB.db.syncConflicts.where('resolution').equals('Pending').count();

    body.innerHTML = `
      <div style="display:grid;gap:1rem;">
        <div class="d-flex align-items-center gap-3">
          <div class="sync-status-bar ${status.state}" style="pointer-events:none;">
            <span class="sync-dot"></span>
            <span>${status.isOnline ? i18n.t('sync.status.online') : i18n.t('sync.status.offline')}</span>
          </div>
          ${status.lastSyncAt ? `<span class="text-muted" style="font-size:.75rem;">${i18n.t('sync.last_sync', timeAgo(status.lastSyncAt))}</span>` : ''}
        </div>
        <div class="d-flex gap-3 flex-wrap">
          <div class="kpi-card" style="flex:1;min-width:120px;">
            <div class="kpi-label">${i18n.t('sync.pending_records', '')}</div>
            <div class="kpi-value">${pending.length}</div>
          </div>
          <div class="kpi-card" style="flex:1;min-width:120px;">
            <div class="kpi-label">${i18n.t('sync.conflict_count', '')}</div>
            <div class="kpi-value" style="${conflicts>0?'color:var(--danger)':''}">${conflicts}</div>
          </div>
        </div>
        ${conflicts > 0 ? `<a href="/Admin/SyncConflicts.aspx" class="btn btn-danger btn-sm"><i class="bi bi-exclamation-triangle"></i> Review Conflicts</a>` : ''}
      </div>`;
  }

  // ── Sidebar ────────────────────────────────────────────────────────────────────
  function toggleSidebar() {
    const sb = document.getElementById('sidebar');
    if (!sb) return;
    const collapsed = sb.classList.toggle('collapsed');
    const icon = document.getElementById('sidebarCollapseIcon');
    if (icon) icon.className = collapsed ? 'bi bi-chevron-right' : 'bi bi-chevron-left';
    localStorage.setItem('eid_sidebar_collapsed', collapsed ? '1' : '0');
  }

  function openSidebar() {
    const sb = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    if (sb) sb.classList.add('mobile-open');
    if (overlay) overlay.classList.remove('d-none');
  }

  function closeSidebar() {
    const sb = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    if (sb) sb.classList.remove('mobile-open');
    if (overlay) overlay.classList.add('d-none');
  }

  function toggleCompactSidebar() {
    const sb = document.getElementById('compactSidebar');
    if (sb) sb.classList.toggle('expanded');
  }

  function toggleMobileMenu() {
    const menu = document.getElementById('mobileMenu');
    if (menu) menu.classList.toggle('d-none');
  }

  // ── Dropdowns ──────────────────────────────────────────────────────────────────
  function toggleDropdown(id) {
    const menu = document.getElementById(id);
    if (!menu) return;
    const isOpen = menu.classList.toggle('open');
    if (isOpen) {
      // Close all others
      document.querySelectorAll('.dropdown-menu.open').forEach(m => {
        if (m.id !== id) m.classList.remove('open');
      });
    }
  }

  function onDocumentClick(e) {
    if (!e.target.closest('.dropdown')) {
      document.querySelectorAll('.dropdown-menu.open').forEach(m => m.classList.remove('open'));
    }
    if (!e.target.closest('#globalSearchOverlay') && !e.target.closest('[onclick*="openGlobalSearch"]')) {
      document.getElementById('globalSearchOverlay')?.classList.add('d-none');
    }
  }

  // ── Global Search ──────────────────────────────────────────────────────────────
  function openGlobalSearch() {
    const overlay = document.getElementById('globalSearchOverlay');
    if (!overlay) return;
    overlay.classList.remove('d-none');
    const input = document.getElementById('globalSearchInput');
    if (input) { input.value = ''; input.focus(); }
  }

  // Debounced global search
  let _searchTimer;
  document.addEventListener('input', e => {
    if (e.target.id !== 'globalSearchInput') return;
    clearTimeout(_searchTimer);
    _searchTimer = setTimeout(() => runGlobalSearch(e.target.value), 300);
  });

  async function runGlobalSearch(query) {
    const resultsEl = document.getElementById('globalSearchResults');
    if (!resultsEl) return;
    if (!query || query.length < 2) { resultsEl.innerHTML = ''; return; }

    resultsEl.innerHTML = '<div class="skeleton skeleton-text" style="height:40px;"></div>'.repeat(3);

    const res = await apiGet(`/Handlers/Search.ashx?q=${encodeURIComponent(query)}`);
    if (!res?.success) { resultsEl.innerHTML = `<div class="text-muted p-3">${i18n.t('common.no_data')}</div>`; return; }

    const items = res.data || [];
    if (!items.length) { resultsEl.innerHTML = `<div class="text-muted p-3">${i18n.t('common.no_data')}</div>`; return; }

    resultsEl.innerHTML = items.map(item =>
      `<a href="${escHtml(item.url)}" class="dropdown-item" style="padding:.625rem .75rem;">
        <i class="bi bi-${escHtml(item.icon || 'file-text')} text-muted"></i>
        <div style="min-width:0;">
          <div class="fw-500 truncate">${escHtml(item.title)}</div>
          <div class="text-muted truncate" style="font-size:.72rem;">${escHtml(item.subtitle || '')}</div>
        </div>
      </a>`
    ).join('');
  }

  // ── Toast Notifications ────────────────────────────────────────────────────────
  function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const icons = { success: 'check-circle-fill', warning: 'exclamation-triangle-fill', error: 'x-circle-fill', info: 'info-circle-fill' };
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
      <i class="bi bi-${icons[type] || icons.info}" style="flex-shrink:0;font-size:1.1rem;color:var(--${type==='error'?'danger':type});"></i>
      <div style="flex:1;">${escHtml(message)}</div>
      <button onclick="this.closest('.toast').remove()" style="background:none;border:none;color:var(--text-muted);cursor:pointer;padding:0;flex-shrink:0;"><i class="bi bi-x"></i></button>`;
    container.appendChild(toast);
    if (duration > 0) setTimeout(() => {
      toast.classList.add('hiding');
      setTimeout(() => toast.remove(), 300);
    }, duration);
    return toast;
  }

  function toggleNotifications() { toggleDropdown('notificationsDropdown'); }

  // ── Logout ─────────────────────────────────────────────────────────────────────
  async function logout() {
    const refreshToken = localStorage.getItem('eid_refresh_token');
    try {
      await apiPost('/Handlers/Auth.ashx?action=logout', { refreshToken });
    } catch { }
    localStorage.removeItem('eid_access_token');
    localStorage.removeItem('eid_refresh_token');
    await EidDB.clearSession();
    window.location.href = '/Pages/Login.aspx';
  }

  // ── Token refresh ──────────────────────────────────────────────────────────────
  function scheduleTokenRefresh(session) {
    if (!session?.accessExpiry) return;
    const expiry = new Date(session.accessExpiry).getTime();
    const now    = Date.now();
    const refreshAt = expiry - 120_000; // 2 min before expiry
    const delay = Math.max(0, refreshAt - now);

    setTimeout(async () => {
      const refreshToken = localStorage.getItem('eid_refresh_token')
                        || (await EidDB.getCurrentSession())?.refreshToken;
      if (!refreshToken) return;

      const res = await apiPost('/Handlers/Auth.ashx?action=refresh', {
        refreshToken,
        deviceId: EidDB.getDeviceId(),
      }).catch(() => null);

      if (res?.success) {
        localStorage.setItem('eid_access_token',  res.accessToken);
        localStorage.setItem('eid_refresh_token', res.refreshToken);
        await EidDB.saveUserSession(res.userClaims, res.accessToken, res.refreshToken, res.accessTokenExpiry, res.refreshTokenExpiry);
        scheduleTokenRefresh({ accessExpiry: res.accessTokenExpiry });
      }
    }, delay);
  }

  // ── SW update prompt ───────────────────────────────────────────────────────────
  function listenForSwUpdate() {
    if (!('serviceWorker' in navigator)) return;
    navigator.serviceWorker.ready.then(reg => {
      reg.addEventListener('updatefound', () => {
        const newWorker = reg.installing;
        newWorker.addEventListener('statechange', () => {
          if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
            const toast = showToast('A new version is available. Click to update.', 'info', 0);
            if (toast) toast.style.cursor = 'pointer';
            if (toast) toast.addEventListener('click', () => {
              newWorker.postMessage({ type: 'SKIP_WAITING' });
              window.location.reload();
            });
          }
        });
      });
    });
  }

  // ── Keyboard shortcuts ─────────────────────────────────────────────────────────
  function onKeyDown(e) {
    // Ctrl+K → global search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); openGlobalSearch(); }
    // Escape → close overlays
    if (e.key === 'Escape') {
      document.querySelectorAll('.dropdown-menu.open').forEach(m => m.classList.remove('open'));
      document.getElementById('globalSearchOverlay')?.classList.add('d-none');
      document.getElementById('syncPanelOverlay')?.classList.add('d-none');
    }
  }

  // ── AJAX helpers ───────────────────────────────────────────────────────────────
  function apiPost(url, data) {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open('POST', url, true);
      xhr.setRequestHeader('Content-Type', 'application/json');
      xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
      const token = localStorage.getItem('eid_access_token');
      if (token) xhr.setRequestHeader('X-Auth-Token', token);
      xhr.timeout = 15000;
      xhr.onload  = () => { try { resolve(JSON.parse(xhr.responseText)); } catch { resolve(null); } };
      xhr.onerror = xhr.ontimeout = () => resolve(null);
      xhr.send(JSON.stringify(data));
    });
  }

  function apiGet(url) {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open('GET', url, true);
      xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
      const token = localStorage.getItem('eid_access_token');
      if (token) xhr.setRequestHeader('X-Auth-Token', token);
      xhr.timeout = 15000;
      xhr.onload  = () => { try { resolve(JSON.parse(xhr.responseText)); } catch { resolve(null); } };
      xhr.onerror = xhr.ontimeout = () => resolve(null);
      xhr.send();
    });
  }

  // ── Utility ────────────────────────────────────────────────────────────────────
  function escHtml(s) {
    if (!s && s !== 0) return '';
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }
  function setTextIfExists(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }
  function stringToHue(s) {
    let h = 0;
    for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) & 0xFFFFFFFF;
    return Math.abs(h) % 360;
  }
  function timeAgo(iso) {
    const secs = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
    if (secs < 60)  return 'just now';
    if (secs < 3600) return `${Math.floor(secs/60)}m ago`;
    if (secs < 86400) return `${Math.floor(secs/3600)}h ago`;
    return `${Math.floor(secs/86400)}d ago`;
  }

  // ── Auto-init ──────────────────────────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', boot);

  // Restore sidebar state
  document.addEventListener('DOMContentLoaded', () => {
    const sb = document.getElementById('sidebar');
    if (sb && localStorage.getItem('eid_sidebar_collapsed') === '1') {
      sb.classList.add('collapsed');
      const icon = document.getElementById('sidebarCollapseIcon');
      if (icon) icon.className = 'bi bi-chevron-right';
    }
  });

  return {
    boot, logout, toggleTheme, toggleSidebar, openSidebar, closeSidebar,
    toggleCompactSidebar, toggleMobileMenu,
    toggleDropdown, showSyncPanel, openGlobalSearch, toggleNotifications,
    showToast, changeLang, switchBranch, apiPost, apiGet,
  };
})();

window.App = App;
