<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="TenantSettings.aspx.cs" Inherits="Admin_TenantSettings" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Tenant Settings – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <span class="breadcrumb-item">Admin</span>
    <span class="breadcrumb-item active">Tenant Settings</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Tenant Settings</h1>
      <p class="page-subtitle" id="tenantSubtitle">Loading…</p>
    </div>
  </div>

  <div class="page-body">
    <div style="display:grid;grid-template-columns:minmax(0,2fr) minmax(0,1fr);gap:1.5rem;align-items:start;" class="settings-layout">

      <!-- General settings form -->
      <form id="settingsForm" onsubmit="TenantSettings.save(event)">
        <div class="card mb-3">
          <div class="card-header"><span class="card-title">General</span></div>
          <div class="card-body" style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;">
            <div class="form-group" style="grid-column:span 2;">
              <label class="form-label required">Organisation Name</label>
              <input type="text" id="tsName" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label">Default Language</label>
              <select id="tsLang" class="form-select">
                <option value="en">English</option>
                <option value="so">Somali</option>
                <option value="ar">Arabic</option>
                <option value="fr">French</option>
                <option value="sw">Swahili</option>
                <option value="om">Oromo</option>
                <option value="ti">Tigrinya</option>
                <option value="ha">Hausa</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Default Currency</label>
              <select id="tsCurrency" class="form-select">
                <option value="USD">USD – US Dollar</option>
                <option value="EUR">EUR – Euro</option>
                <option value="GBP">GBP – British Pound</option>
                <option value="AED">AED – UAE Dirham</option>
                <option value="SAR">SAR – Saudi Riyal</option>
                <option value="KES">KES – Kenyan Shilling</option>
                <option value="SOS">SOS – Somali Shilling</option>
                <option value="ETB">ETB – Ethiopian Birr</option>
              </select>
            </div>
            <div class="form-group" style="grid-column:span 2;">
              <label class="form-label">Time Zone</label>
              <select id="tsTZ" class="form-select">
                <option value="UTC">UTC</option>
                <option value="Africa/Nairobi">Africa/Nairobi (EAT +3)</option>
                <option value="Africa/Mogadishu">Africa/Mogadishu (EAT +3)</option>
                <option value="Asia/Dubai">Asia/Dubai (GST +4)</option>
                <option value="Asia/Riyadh">Asia/Riyadh (AST +3)</option>
                <option value="Europe/London">Europe/London (GMT/BST)</option>
                <option value="America/New_York">America/New_York (EST/EDT)</option>
              </select>
            </div>
          </div>
        </div>

        <div class="card mb-3">
          <div class="card-header"><span class="card-title">Branding</span></div>
          <div class="card-body" style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;">
            <div class="form-group">
              <label class="form-label">Accent Colour</label>
              <div class="input-group">
                <input type="color" id="tsAccentPicker" style="width:44px;height:38px;padding:2px;border:1px solid var(--border);border-radius:var(--radius-sm) 0 0 var(--radius-sm);"
                       oninput="document.getElementById('tsAccent').value=this.value;TenantSettings.previewAccent(this.value)" />
                <input type="text" id="tsAccent" class="form-input" maxlength="7" placeholder="#2563EB"
                       oninput="TenantSettings.syncColorPicker(this.value)" style="border-radius:0 var(--radius-sm) var(--radius-sm) 0;" />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Theme Mode</label>
              <select id="tsTheme" class="form-select">
                <option value="light">Light</option>
                <option value="dark">Dark</option>
                <option value="auto">Auto (System)</option>
              </select>
            </div>
            <div class="form-group" style="grid-column:span 2;">
              <label class="form-label">Logo URL</label>
              <input type="url" id="tsLogo" class="form-input" placeholder="https://…/logo.png" />
              <p class="form-hint">Shown in the sidebar. Recommended: 160×48px PNG with transparent background.</p>
            </div>
            <div class="form-group" style="grid-column:span 2;">
              <label class="form-label">Default Layout</label>
              <div class="d-flex gap-3 mt-1" id="layoutPicker">
                <label class="form-check d-flex align-items-center gap-2 border p-2 rounded" style="cursor:pointer;">
                  <input type="radio" name="layout" value="classic" class="form-check-input" />
                  <div><div class="fw-600">Classic</div><div class="text-muted small">Collapsible sidebar</div></div>
                </label>
                <label class="form-check d-flex align-items-center gap-2 border p-2 rounded" style="cursor:pointer;">
                  <input type="radio" name="layout" value="topnav" class="form-check-input" />
                  <div><div class="fw-600">Top Nav</div><div class="text-muted small">Horizontal navigation</div></div>
                </label>
                <label class="form-check d-flex align-items-center gap-2 border p-2 rounded" style="cursor:pointer;">
                  <input type="radio" name="layout" value="compact" class="form-check-input" />
                  <div><div class="fw-600">Compact</div><div class="text-muted small">Icon-only sidebar</div></div>
                </label>
              </div>
            </div>
          </div>
        </div>

        <div class="d-flex justify-content-end gap-2">
          <button type="button" class="btn btn-ghost" onclick="TenantSettings.load()">Discard</button>
          <button type="submit" class="btn btn-primary">
            <i class="bi bi-floppy me-1"></i> Save Settings
          </button>
        </div>
      </form>

      <!-- Right sidebar: subscription & usage -->
      <div>
        <div class="card mb-3">
          <div class="card-header"><span class="card-title">Subscription</span></div>
          <div class="card-body" id="subscriptionInfo">
            <div class="skeleton skeleton-text"></div>
            <div class="skeleton skeleton-text mt-2" style="width:70%;"></div>
          </div>
        </div>
        <div class="card">
          <div class="card-header"><span class="card-title">Usage</span></div>
          <div class="card-body" id="usageInfo">
            <div class="skeleton skeleton-text"></div>
          </div>
        </div>
      </div>

    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const TenantSettings = (() => {

  async function load() {
    const res = await App.apiGet('/Handlers/Tenant.ashx?resource=settings');
    const s = res?.data;
    if (!s) { App.showToast('Failed to load settings', 'error'); return; }

    document.getElementById('tenantSubtitle').textContent = s.subdomain || '';
    setVal('tsName', s.name);
    setVal('tsLang', s.defaultLanguageCode);
    setVal('tsCurrency', s.defaultCurrencyCode);
    setVal('tsTZ', s.timeZone);
    setVal('tsAccent', s.accentColor);
    setVal('tsLogo', s.logoUrl);
    document.getElementById('tsAccentPicker').value = s.accentColor || '#2563EB';
    document.getElementById('tsTheme').value = s.themeMode || 'auto';

    const layout = s.activeLayout || 'classic';
    document.querySelectorAll('input[name="layout"]').forEach(r => r.checked = r.value === layout);

    // Subscription
    const sub = s.subscription;
    const subEl = document.getElementById('subscriptionInfo');
    if (sub) {
      subEl.innerHTML = `
        <div class="d-flex align-items-center justify-content-between mb-2">
          <span class="fw-600">${esc(sub.planName)}</span>
          <span class="badge badge-${sub.status==='Active'?'success':'warning'}">${esc(sub.status)}</span>
        </div>
        <div class="text-muted small">
          Expires: ${fmtDate(sub.endDate)}<br>
          Max Users: ${sub.maxUsers} · Max Companies: ${sub.maxCompanies}
        </div>`;
    } else {
      subEl.innerHTML = '<p class="text-muted small">No subscription info available</p>';
    }

    // Usage
    const usageEl = document.getElementById('usageInfo');
    usageEl.innerHTML = `
      <div class="mb-3">
        <div class="d-flex justify-content-between mb-1">
          <span class="small">Users</span>
          <span class="small fw-600">${s.currentUserCount} / ${s.maxUsers}</span>
        </div>
        <div class="progress" style="height:6px;background:var(--border);border-radius:3px;">
          <div class="progress-bar" style="width:${Math.min(100,s.currentUserCount/Math.max(1,s.maxUsers)*100).toFixed(1)}%;height:100%;background:var(--accent);border-radius:3px;"></div>
        </div>
      </div>
      <div>
        <div class="d-flex justify-content-between mb-1">
          <span class="small">Companies</span>
          <span class="small fw-600">${s.currentCompanyCount} / ${s.maxCompanies}</span>
        </div>
        <div class="progress" style="height:6px;background:var(--border);border-radius:3px;">
          <div class="progress-bar" style="width:${Math.min(100,s.currentCompanyCount/Math.max(1,s.maxCompanies)*100).toFixed(1)}%;height:100%;background:var(--accent);border-radius:3px;"></div>
        </div>
      </div>`;
  }

  async function save(e) {
    e.preventDefault();
    const layout = document.querySelector('input[name="layout"]:checked')?.value || 'classic';
    const dto = {
      name: val('tsName'),
      logoUrl: val('tsLogo'),
      accentColor: val('tsAccent'),
      themeMode: document.getElementById('tsTheme').value,
      activeLayout: layout,
      defaultLanguageCode: document.getElementById('tsLang').value,
      defaultCurrencyCode: document.getElementById('tsCurrency').value,
      timeZone: document.getElementById('tsTZ').value
    };
    const res = await App.apiPut('/Handlers/Tenant.ashx?resource=settings', dto);
    if (res?.success) {
      App.showToast('Settings saved', 'success');
      // Apply accent colour live
      document.documentElement.style.setProperty('--accent', dto.accentColor);
    } else {
      App.showToast(res?.message || 'Error saving settings', 'error');
    }
  }

  function previewAccent(color) {
    if (/^#[0-9a-fA-F]{6}$/.test(color))
      document.documentElement.style.setProperty('--accent', color);
  }

  function syncColorPicker(value) {
    if (/^#[0-9a-fA-F]{6}$/.test(value)) {
      document.getElementById('tsAccentPicker').value = value;
      previewAccent(value);
    }
  }

  const val = id => (document.getElementById(id)?.value || '').trim();
  const setVal = (id, v) => { const el = document.getElementById(id); if (el) el.value = v || ''; };
  const esc = s => (!s ? '' : String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;'));
  const fmtDate = d => d ? new Date(d).toLocaleDateString() : '—';

  document.addEventListener('DOMContentLoaded', load);
  return { load, save, previewAccent, syncColorPicker };
})();
</script>
</asp:Content>
