<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Pages_Login" %>
<!DOCTYPE html>
<html id="htmlRoot">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
  <meta name="theme-color" content="#2563EB" />
  <link rel="manifest" href="/manifest.webmanifest" />
  <link rel="icon" href="/images/icons/icon-96.png" type="image/png" />
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
  <link rel="stylesheet" href="/Styles/main.css" />
  <link rel="stylesheet" href="/Styles/themes.css" />
  <title>Sign In – EidUbahle ERP</title>
  <style>
    .login-page {
      min-height: 100vh;
      background: var(--bg-base);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
    }
    .login-split {
      display: flex;
      width: 100%;
      max-width: 1000px;
      border-radius: var(--radius-xl);
      overflow: hidden;
      box-shadow: 0 30px 80px rgba(0,0,0,.35);
    }
    /* Left panel: branding */
    .login-brand-panel {
      flex: 1;
      background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dark) 100%);
      color: #fff;
      padding: 3rem;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      min-width: 0;
    }
    @media (max-width: 767px) { .login-brand-panel { display: none; } }

    .login-brand-features { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: .875rem; }
    .login-brand-features li { display: flex; align-items: center; gap: .75rem; font-size: .875rem; opacity: .95; }
    .login-brand-features li i { font-size: 1.25rem; }

    /* Right panel: form */
    .login-form-panel {
      width: 420px;
      flex-shrink: 0;
      background: var(--bg-surface);
      padding: 3rem;
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
    @media (max-width: 767px) { .login-form-panel { width: 100%; padding: 2rem 1.5rem; } }

    /* Offline banner */
    .offline-banner {
      background: var(--warning-light);
      border: 1px solid var(--warning);
      color: var(--warning);
      border-radius: var(--radius-md);
      padding: .625rem 1rem;
      font-size: .78rem;
      font-weight: 500;
      margin-bottom: 1.25rem;
      display: flex;
      align-items: center;
      gap: .5rem;
    }

    /* Password toggle */
    .password-wrapper { position: relative; }
    .password-toggle {
      position: absolute;
      inset-inline-end: .625rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      color: var(--text-muted);
      cursor: pointer;
      padding: .25rem;
      line-height: 1;
    }
    html[dir="rtl"] .password-toggle { inset-inline-end: auto; inset-inline-start: .625rem; }

    /* Language switcher */
    .lang-selector {
      display: flex;
      gap: .375rem;
      flex-wrap: wrap;
      margin-bottom: 1.5rem;
    }
    .lang-btn {
      padding: .25rem .625rem;
      border-radius: var(--radius-full);
      border: 1px solid var(--border-color);
      background: transparent;
      font-size: .72rem;
      cursor: pointer;
      color: var(--text-secondary);
      transition: var(--transition);
    }
    .lang-btn.active, .lang-btn:hover {
      border-color: var(--accent);
      color: var(--accent);
      background: var(--accent-light);
    }

    /* 2FA panel */
    #twoFaPanel { display: none; }
  </style>
</head>
<body>
  <div class="login-page">
    <div class="login-split">

      <!-- ── Brand Panel ─────────────────────────────────────────────────── -->
      <div class="login-brand-panel">
        <div>
          <div style="display:flex;align-items:center;gap:.875rem;margin-bottom:2.5rem;">
            <img src="/images/logo-default-white.svg" alt="EidUbahle ERP" style="width:44px;height:44px;border-radius:12px;background:rgba(255,255,255,.2);padding:6px;" />
            <div>
              <div style="font-size:1.25rem;font-weight:800;letter-spacing:-.02em;">EidUbahle ERP</div>
              <div style="font-size:.78rem;opacity:.8;">Enterprise Accounting SaaS</div>
            </div>
          </div>

          <h2 style="font-size:1.75rem;font-weight:800;line-height:1.2;margin-bottom:1rem;">
            Your Business.<br/>Your Data.<br/>Always Online.
          </h2>
          <p style="font-size:.9rem;opacity:.85;margin-bottom:2rem;line-height:1.7;">
            Works fully offline with automatic cloud sync.
            Never lose data. Never wait for a network.
          </p>

          <ul class="login-brand-features">
            <li><i class="bi bi-wifi-off"></i> <span>100% Offline-First — works without internet</span></li>
            <li><i class="bi bi-arrow-repeat"></i> <span>Auto-sync when connectivity returns</span></li>
            <li><i class="bi bi-shield-check"></i> <span>Enterprise security with JWT &amp; 2FA</span></li>
            <li><i class="bi bi-globe"></i> <span>8 languages, RTL/LTR, unlimited more</span></li>
            <li><i class="bi bi-building"></i> <span>Multi-company, multi-branch, multi-currency</span></li>
            <li><i class="bi bi-phone"></i> <span>PWA – installable on any device</span></li>
          </ul>
        </div>

        <div style="font-size:.72rem;opacity:.6;margin-top:2rem;">
          © <script>document.write(new Date().getFullYear())</script> EidUbahle ERP. All rights reserved.
        </div>
      </div>

      <!-- ── Login Form Panel ────────────────────────────────────────────── -->
      <div class="login-form-panel">

        <!-- Language switcher -->
        <div class="lang-selector" id="langSwitcher" role="group" aria-label="Language"></div>

        <!-- Offline notice -->
        <div class="offline-banner" id="offlineBanner" style="display:none;">
          <i class="bi bi-wifi-off"></i>
          <span data-i18n="login.offline_mode">You are working offline. Data is saved locally.</span>
        </div>

        <h1 style="font-size:1.5rem;font-weight:800;margin-bottom:.375rem;" data-i18n="login.title">Sign In</h1>
        <p style="color:var(--text-muted);font-size:.85rem;margin-bottom:1.75rem;" data-i18n="login.subtitle">Manage your business with confidence</p>

        <!-- Error message -->
        <div id="loginError" class="alert alert-danger" style="display:none;"></div>

        <!-- ── Login Form ──────────────────────────────────────────────── -->
        <div id="loginPanel">
          <div class="form-group">
            <label class="form-label required" data-i18n="login.username">Username or Email</label>
            <input type="text" id="username" name="username" class="form-control" autocomplete="username"
                   data-i18n-placeholder="login.username" autofocus />
          </div>

          <div class="form-group">
            <div style="display:flex;justify-content:space-between;align-items:center;">
              <label class="form-label required" style="margin-bottom:0;" data-i18n="login.password">Password</label>
              <a href="/Pages/ForgotPassword.aspx" style="font-size:.75rem;" data-i18n="login.forgot">Forgot Password?</a>
            </div>
            <div class="password-wrapper" style="margin-top:.5rem;">
              <input type="password" id="password" name="password" class="form-control" autocomplete="current-password"
                     data-i18n-placeholder="login.password" style="padding-inline-end:2.5rem;" />
              <button type="button" class="password-toggle" id="pwToggle" onclick="LoginPage.togglePassword()" aria-label="Toggle password">
                <i class="bi bi-eye" id="pwIcon"></i>
              </button>
            </div>
          </div>

          <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:1.5rem;flex-wrap:wrap;gap:.5rem;">
            <label style="display:flex;align-items:center;gap:.5rem;cursor:pointer;font-size:.825rem;">
              <input type="checkbox" id="rememberMe" style="width:16px;height:16px;accent-color:var(--accent);" />
              <span data-i18n="login.remember">Remember me</span>
            </label>
          </div>

          <button type="button" id="loginBtn" class="btn btn-primary w-100 btn-lg" onclick="LoginPage.submit()" style="gap:.75rem;">
            <span id="loginBtnText" data-i18n="login.signin">Sign In</span>
            <span id="loginSpinner" class="d-none"><i class="bi bi-arrow-repeat" style="animation:spin 1s linear infinite;"></i></span>
          </button>
        </div>

        <!-- ── 2FA Form ────────────────────────────────────────────────── -->
        <div id="twoFaPanel">
          <div class="text-center" style="margin-bottom:1.5rem;">
            <div style="font-size:2.5rem;margin-bottom:.75rem;">🔐</div>
            <h2 style="font-size:1.1rem;font-weight:700;" data-i18n="login.2fa_title">Two-Factor Authentication</h2>
            <p class="text-muted" style="font-size:.82rem;margin-top:.5rem;" data-i18n="login.2fa_label">Enter the 6-digit code from your authenticator app</p>
          </div>

          <div class="form-group">
            <input type="text" id="totpCode" name="totpCode" class="form-control text-center font-mono"
                   placeholder="000000" maxlength="6" inputmode="numeric" pattern="[0-9]*"
                   autocomplete="one-time-code"
                   style="font-size:1.5rem;letter-spacing:.5rem;text-align:center;" />
          </div>

          <button type="button" id="twoFaBtn" class="btn btn-primary w-100 btn-lg" onclick="LoginPage.submit2FA()">
            <span data-i18n="login.2fa_verify">Verify</span>
          </button>
          <button type="button" class="btn btn-ghost w-100 mt-2" onclick="LoginPage.backToLogin()" data-i18n="common.back">Back</button>
        </div>

        <!-- Install PWA hint -->
        <div id="installPwaBanner" style="display:none;margin-top:1.5rem;padding:.875rem;background:var(--accent-light);border-radius:var(--radius-md);font-size:.78rem;color:var(--accent);">
          <div style="display:flex;align-items:center;gap:.5rem;font-weight:600;margin-bottom:.35rem;">
            <i class="bi bi-phone"></i> Install EidUbahle as an App
          </div>
          <div style="opacity:.85;margin-bottom:.625rem;">Get full offline capabilities and faster performance</div>
          <button class="btn btn-primary btn-sm" onclick="LoginPage.installPwa()">Install</button>
          <button class="btn btn-ghost btn-sm" onclick="document.getElementById('installPwaBanner').style.display='none'">Maybe Later</button>
        </div>

      </div><!-- /login-form-panel -->

    </div><!-- /login-split -->
  </div><!-- /login-page -->

  <!-- Scripts -->
  <script src="https://cdn.jsdelivr.net/npm/dexie@3.2.7/dist/dexie.min.js"></script>
  <script src="/Scripts/offline/db.js"></script>
  <script src="/Scripts/modules/i18n.js"></script>
  <script src="/Scripts/modules/theme.js"></script>
  <style>@keyframes spin { from { transform: rotate(0); } to { transform: rotate(360deg); } }</style>

  <script>
  'use strict';
  const LoginPage = (() => {
    let _pendingUsername = null;
    let _pendingPassword = null;
    let _pendingRemember = false;
    let _deferredInstall = null;

    // ── Init ─────────────────────────────────────────────────────────────────
    async function init() {
      // Theme
      ThemeEngine.init(null);

      // i18n
      const lang = localStorage.getItem('eid_lang') || navigator.language.split('-')[0] || 'en';
      await i18n.init(lang);
      i18n.translatePage();
      buildLangSwitcher();

      // Online/offline
      updateOnlineStatus();
      window.addEventListener('online',  updateOnlineStatus);
      window.addEventListener('offline', updateOnlineStatus);

      // Check for existing valid offline session
      const session = await EidDB.getCurrentSession();
      if (session && session.accessToken) {
        const offlineInfo = parseTokenExpiry(session.accessToken);
        if (offlineInfo && !offlineInfo.isExpired) {
          // Redirect to dashboard (auth module will handle JWT validation)
          const rUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/Pages/Dashboard.aspx';
          window.location.href = rUrl;
          return;
        }
      }

      // Keyboard shortcut
      document.addEventListener('keydown', e => {
        if (e.key === 'Enter') submit();
      });

      // PWA install prompt
      window.addEventListener('beforeinstallprompt', e => {
        e.preventDefault();
        _deferredInstall = e;
        document.getElementById('installPwaBanner').style.display = 'block';
      });
    }

    // ── Submit login ──────────────────────────────────────────────────────────
    async function submit() {
      const username = document.getElementById('username').value.trim();
      const password = document.getElementById('password').value;

      clearError();

      if (!username || !password) {
        showError(i18n.t('login.error.empty'));
        shakeForm();
        return;
      }

      setLoading(true);

      const payload = {
        username,
        password,
        deviceId: EidDB.getDeviceId(),
        deviceInfo: navigator.userAgent,
        rememberMe: document.getElementById('rememberMe').checked,
        totpCode: null,
      };

      const xhr = new XMLHttpRequest();
      xhr.open('POST', '/Handlers/Auth.ashx?action=login', true);
      xhr.setRequestHeader('Content-Type', 'application/json');
      xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
      xhr.timeout = 15000;
      xhr.onload = async function() {
        setLoading(false);
        try {
          const res = JSON.parse(xhr.responseText);
          await handleLoginResponse(res, username, password, document.getElementById('rememberMe').checked);
        } catch {
          showError(i18n.t('common.error'));
        }
      };
      xhr.onerror = xhr.ontimeout = function() {
        setLoading(false);
        // Offline: attempt offline login
        attemptOfflineLogin(username, password);
      };
      xhr.send(JSON.stringify(payload));
    }

    // ── Handle server response ────────────────────────────────────────────────
    async function handleLoginResponse(res, username, password, rememberMe) {
      if (res.require2FA) {
        _pendingUsername = username;
        _pendingPassword = password;
        _pendingRemember = rememberMe;
        showTwoFaPanel();
        return;
      }

      if (!res.success) {
        showError(i18n.t(`login.error.${getErrorKey(res.errorCode)}`) || res.errorMessage);
        shakeForm();
        return;
      }

      await onLoginSuccess(res);
    }

    // ── 2FA submit ────────────────────────────────────────────────────────────
    async function submit2FA() {
      const totpCode = document.getElementById('totpCode').value.trim();
      if (!totpCode || totpCode.length !== 6) {
        showError(i18n.t('login.error.2fa'));
        return;
      }

      setLoading(true);

      const payload = {
        username: _pendingUsername,
        password: _pendingPassword,
        deviceId: EidDB.getDeviceId(),
        deviceInfo: navigator.userAgent,
        rememberMe: _pendingRemember,
        totpCode,
      };

      const xhr = new XMLHttpRequest();
      xhr.open('POST', '/Handlers/Auth.ashx?action=login', true);
      xhr.setRequestHeader('Content-Type', 'application/json');
      xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
      xhr.onload = async function() {
        setLoading(false);
        try {
          const res = JSON.parse(xhr.responseText);
          if (!res.success) { showError(i18n.t('login.error.2fa')); shakeForm(); return; }
          await onLoginSuccess(res);
        } catch { showError(i18n.t('common.error')); }
      };
      xhr.onerror = function() { setLoading(false); showError(i18n.t('common.error')); };
      xhr.send(JSON.stringify(payload));
    }

    // ── On successful login ───────────────────────────────────────────────────
    async function onLoginSuccess(res) {
      const claims = res.userClaims;

      // Persist tokens
      localStorage.setItem('eid_access_token',  res.accessToken);
      localStorage.setItem('eid_refresh_token', res.refreshToken);
      localStorage.setItem('eid_lang', claims.languageCode || 'en');

      // Save session to IndexedDB for offline use
      await EidDB.saveUserSession(
        claims, res.accessToken, res.refreshToken,
        res.accessTokenExpiry, res.refreshTokenExpiry
      );

      // Apply theme/layout from user prefs
      if (claims.themeMode)   localStorage.setItem('eid_theme_mode', claims.themeMode);
      if (claims.accentColor) localStorage.setItem('eid_accent_color', claims.accentColor);
      if (claims.activeLayout || claims.tenantLayout)
        localStorage.setItem('eid_layout', claims.activeLayout || claims.tenantLayout);

      // Redirect
      const returnUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/Pages/Dashboard.aspx';
      window.location.href = decodeURIComponent(returnUrl);
    }

    // ── Offline login ─────────────────────────────────────────────────────────
    async function attemptOfflineLogin(username, password) {
      // Can only log in offline if a session exists (token not expired)
      const session = await EidDB.getCurrentSession();
      if (session && session.username === username) {
        const info = parseTokenExpiry(session.accessToken);
        if (info && !info.isExpired) {
          // Restore tokens in localStorage
          localStorage.setItem('eid_access_token',  session.accessToken);
          localStorage.setItem('eid_refresh_token', session.refreshToken);
          localStorage.setItem('eid_lang', session.languageCode || 'en');
          window.location.href = '/Pages/Dashboard.aspx';
          return;
        }
      }
      showError(i18n.t('common.offline') + '. ' + i18n.t('login.error.invalid'));
    }

    // ── UI helpers ─────────────────────────────────────────────────────────────
    function showError(msg) {
      const el = document.getElementById('loginError');
      el.textContent = msg;
      el.style.display = 'flex';
    }
    function clearError() {
      const el = document.getElementById('loginError');
      el.style.display = 'none';
    }
    function setLoading(on) {
      const btn  = document.getElementById('loginBtn');
      const txt  = document.getElementById('loginBtnText');
      const spin = document.getElementById('loginSpinner');
      btn.disabled = on;
      txt.textContent = on ? i18n.t('login.signing_in') : i18n.t('login.signin');
      spin.classList.toggle('d-none', !on);
    }
    function shakeForm() {
      const panel = document.getElementById('loginPanel');
      panel.classList.remove('anim-shake');
      void panel.offsetWidth;
      panel.classList.add('anim-shake');
    }
    function showTwoFaPanel() {
      document.getElementById('loginPanel').style.display = 'none';
      document.getElementById('twoFaPanel').style.display = 'block';
      document.getElementById('totpCode').focus();
    }
    function backToLogin() {
      document.getElementById('twoFaPanel').style.display = 'none';
      document.getElementById('loginPanel').style.display = 'block';
    }
    function togglePassword() {
      const inp = document.getElementById('password');
      const ico = document.getElementById('pwIcon');
      if (inp.type === 'password') {
        inp.type = 'text';
        ico.className = 'bi bi-eye-slash';
      } else {
        inp.type = 'password';
        ico.className = 'bi bi-eye';
      }
    }
    function updateOnlineStatus() {
      const offline = !navigator.onLine;
      document.getElementById('offlineBanner').style.display = offline ? 'flex' : 'none';
    }
    function buildLangSwitcher() {
      const langs = [
        { code: 'en', label: 'EN 🇬🇧' }, { code: 'so', label: 'SO 🇸🇴' },
        { code: 'ar', label: 'AR 🇸🇦' }, { code: 'fr', label: 'FR 🇫🇷' },
        { code: 'sw', label: 'SW 🇰🇪' }, { code: 'es', label: 'ES 🇪🇸' },
        { code: 'de', label: 'DE 🇩🇪' }, { code: 'tr', label: 'TR 🇹🇷' },
      ];
      const current = localStorage.getItem('eid_lang') || 'en';
      const container = document.getElementById('langSwitcher');
      container.innerHTML = langs.map(l =>
        `<button class="lang-btn ${l.code===current?'active':''}" onclick="LoginPage.changeLang('${l.code}')">${l.label}</button>`
      ).join('');
    }
    async function changeLang(code) {
      await i18n.changeLanguage(code);
      i18n.translatePage();
      buildLangSwitcher();
    }
    function getErrorKey(errorCode) {
      const map = { ERR_INVALID_CREDENTIALS: 'invalid', ERR_ACCOUNT_LOCKED: 'locked', ERR_ACCOUNT_INACTIVE: 'inactive', ERR_INVALID_2FA: '2fa' };
      return map[errorCode] || 'invalid';
    }
    function parseTokenExpiry(token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));
        return { isExpired: Date.now() / 1000 > payload.exp };
      } catch { return null; }
    }
    async function installPwa() {
      if (_deferredInstall) {
        _deferredInstall.prompt();
        const { outcome } = await _deferredInstall.userChoice;
        if (outcome === 'accepted') document.getElementById('installPwaBanner').style.display = 'none';
      }
    }

    return { init, submit, submit2FA, backToLogin, togglePassword, changeLang, installPwa };
  })();

  document.addEventListener('DOMContentLoaded', LoginPage.init);
  </script>

  <!-- Service Worker registration -->
  <script>
    if ('serviceWorker' in navigator) {
      window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => {}));
    }
  </script>
</body>
</html>
