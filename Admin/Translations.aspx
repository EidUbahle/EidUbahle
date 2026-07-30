<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Translations.aspx.cs" Inherits="Admin_Translations" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Translation Management – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-sep">/</span>
    <span class="breadcrumb-item active" data-i18n="admin.translations.title">Translation Management</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title" data-i18n="admin.translations.title">Translation Management</h1>
      <p class="page-subtitle" data-i18n="admin.translations.direction_note">Add languages, edit translations inline, export/import JSON or Excel.</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-secondary btn-sm" onclick="TransAdmin.showAddLangModal()">
        <i class="bi bi-plus-circle"></i>
        <span data-i18n="admin.translations.add_language">Add Language</span>
      </button>
      <div class="dropdown">
        <button class="btn btn-secondary btn-sm" onclick="App.toggleDropdown('exportDropdown')">
          <i class="bi bi-download"></i> Export <i class="bi bi-chevron-down" style="font-size:.65rem;"></i>
        </button>
        <div class="dropdown-menu" id="exportDropdown">
          <a class="dropdown-item" href="#" onclick="TransAdmin.exportJson();return false;"><i class="bi bi-filetype-json"></i> Export JSON</a>
        </div>
      </div>
      <div class="dropdown">
        <button class="btn btn-secondary btn-sm" onclick="App.toggleDropdown('importDropdown')">
          <i class="bi bi-upload"></i> Import <i class="bi bi-chevron-down" style="font-size:.65rem;"></i>
        </button>
        <div class="dropdown-menu" id="importDropdown">
          <a class="dropdown-item" href="#" onclick="TransAdmin.showImportModal('json');return false;"><i class="bi bi-filetype-json"></i> Import JSON</a>
          <a class="dropdown-item" href="#" onclick="TransAdmin.showImportModal('excel');return false;"><i class="bi bi-file-earmark-excel"></i> Import Excel</a>
        </div>
      </div>
    </div>
  </div>

  <div class="page-body">

    <!-- Language tabs -->
    <div class="card" style="margin-bottom:1rem;">
      <div class="card-body" style="padding:.875rem 1.25rem;">
        <div class="d-flex align-items-center gap-3 flex-wrap">
          <span class="fw-500 text-muted" style="font-size:.8rem;">Language:</span>
          <div class="tabs" id="langTabs" style="border:none;flex:1;gap:.35rem;flex-wrap:wrap;"></div>
        </div>
      </div>
    </div>

    <!-- Filter / Search toolbar -->
    <div class="data-table-wrapper">
      <div class="data-table-toolbar">
        <div class="input-group" style="max-width:320px;">
          <span style="background:var(--bg-elevated);border:1px solid var(--border-color);border-inline-end:none;border-radius:var(--radius-md) 0 0 var(--radius-md);padding:.4rem .75rem;display:flex;align-items:center;color:var(--text-muted);">
            <i class="bi bi-search"></i>
          </span>
          <input type="text" id="translationSearch" class="form-control" data-i18n-placeholder="common.search"
                 style="border-inline-start:none;border-radius:0 var(--radius-md) var(--radius-md) 0;"
                 oninput="TransAdmin.search(this.value)" />
        </div>
        <select class="form-select" id="moduleFilter" onchange="TransAdmin.filterModule(this.value)" style="max-width:160px;">
          <option value="">All Modules</option>
          <option value="Auth">Auth</option>
          <option value="Navigation">Navigation</option>
          <option value="Common">Common</option>
          <option value="Admin">Admin</option>
          <option value="Sync">Sync</option>
          <option value="Accounting">Accounting</option>
          <option value="Sales">Sales</option>
          <option value="Inventory">Inventory</option>
        </select>
        <button class="btn btn-ghost btn-sm" onclick="TransAdmin.addRow()">
          <i class="bi bi-plus"></i> Add Key
        </button>
        <span id="translationCount" class="text-muted" style="font-size:.75rem;margin-inline-start:auto;"></span>
      </div>

      <!-- Translation table -->
      <div style="overflow-x:auto;">
        <table class="data-table" id="translationsTable">
          <thead>
            <tr>
              <th style="width:30%;" data-i18n="admin.translations.key">Key</th>
              <th style="width:12%;" data-i18n="admin.translations.module">Module</th>
              <th id="transValueHeader">Value (en)</th>
              <th style="width:90px;" data-i18n="common.actions">Actions</th>
            </tr>
          </thead>
          <tbody id="translationsBody">
            <tr><td colspan="4" style="text-align:center;padding:2.5rem;">
              <div class="skeleton skeleton-text"></div>
              <div class="skeleton skeleton-text" style="margin-top:.5rem;width:80%;"></div>
              <div class="skeleton skeleton-text" style="margin-top:.5rem;width:60%;"></div>
            </td></tr>
          </tbody>
        </table>
      </div>

      <div class="data-table-footer">
        <span id="transPageInfo" class="text-muted"></span>
        <div class="data-table-pagination" id="transPagination"></div>
      </div>
    </div>

  </div><!-- /page-body -->

  <!-- Add Language Modal -->
  <div id="addLangOverlay" class="modal-overlay d-none" role="dialog" aria-modal="true">
    <div class="modal" style="max-width:440px;">
      <div class="modal-header">
        <span class="modal-title" data-i18n="admin.translations.add_language">Add Language</span>
        <button class="btn btn-ghost btn-icon" onclick="TransAdmin.closeAddLangModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label class="form-label required">Language Code (ISO 639-1)</label>
          <input type="text" id="newLangCode" class="form-control" placeholder="e.g. it, zh, ja" maxlength="10" />
        </div>
        <div class="form-group">
          <label class="form-label required">Language Name (English)</label>
          <input type="text" id="newLangName" class="form-control" placeholder="Italian" />
        </div>
        <div class="form-group">
          <label class="form-label">Native Name</label>
          <input type="text" id="newLangNative" class="form-control" placeholder="Italiano" />
        </div>
        <div class="form-group">
          <label class="form-label">Text Direction</label>
          <select id="newLangDir" class="form-select">
            <option value="ltr">LTR – Left to Right</option>
            <option value="rtl">RTL – Right to Left</option>
          </select>
        </div>
        <div class="form-group">
          <label class="form-label">Flag Emoji</label>
          <input type="text" id="newLangFlag" class="form-control" placeholder="🇮🇹" maxlength="10" />
        </div>
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:.5rem;cursor:pointer;font-size:.85rem;">
            <input type="checkbox" id="newLangDefault" style="width:16px;height:16px;accent-color:var(--accent);" />
            <span>Set as default language</span>
          </label>
        </div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-primary" onclick="TransAdmin.saveLanguage()" data-i18n="common.save">Save</button>
        <button class="btn btn-secondary" onclick="TransAdmin.closeAddLangModal()" data-i18n="common.cancel">Cancel</button>
      </div>
    </div>
  </div>

  <!-- Import Modal -->
  <div id="importOverlay" class="modal-overlay d-none" role="dialog" aria-modal="true">
    <div class="modal" style="max-width:480px;">
      <div class="modal-header">
        <span class="modal-title">Import Translations</span>
        <button class="btn btn-ghost btn-icon" onclick="document.getElementById('importOverlay').classList.add('d-none')"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label class="form-label">Import Format</label>
          <div id="importFormatLabel" class="fw-600 text-accent">JSON</div>
        </div>
        <div class="form-group" id="jsonImportGroup">
          <label class="form-label">Paste JSON</label>
          <textarea id="importJsonText" class="form-control font-mono" rows="10"
                    placeholder='[{"key":"login.title","module":"Auth","translations":{"en":"Sign In","ar":"دخول"}}]'></textarea>
        </div>
        <div id="importResult" class="alert alert-success d-none"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-primary" onclick="TransAdmin.runImport()"><i class="bi bi-upload"></i> Import</button>
        <button class="btn btn-secondary" onclick="document.getElementById('importOverlay').classList.add('d-none')" data-i18n="common.cancel">Cancel</button>
      </div>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const TransAdmin = (() => {
  let _currentLang = 'en';
  let _allRows = [];
  let _filteredRows = [];
  let _page = 1;
  const PAGE_SIZE = 50;
  let _searchQuery = '';
  let _moduleFilter = '';

  // ── Init ─────────────────────────────────────────────────────────────────────
  async function init() {
    i18n.translatePage();
    await loadLanguages();
  }

  // ── Language tabs ─────────────────────────────────────────────────────────────
  async function loadLanguages() {
    const res = await App.apiGet('/Handlers/Translations.ashx?action=languages');
    const langs = res?.data?.languages || [
      { code:'en',name:'English',direction:'ltr',flagIcon:'🇬🇧' },
      { code:'so',name:'Somali',direction:'ltr',flagIcon:'🇸🇴' },
      { code:'ar',name:'Arabic',direction:'rtl',flagIcon:'🇸🇦' },
      { code:'fr',name:'French',direction:'ltr',flagIcon:'🇫🇷' },
    ];

    const tabs = document.getElementById('langTabs');
    tabs.innerHTML = langs.map(l =>
      `<button class="tab-item${l.code===_currentLang?' active':''}" onclick="TransAdmin.switchLang('${l.code}')"
               style="padding:.4rem .875rem;border-bottom:2px solid ${l.code===_currentLang?'var(--accent)':'transparent'};">
        ${l.flagIcon || ''} ${escHtml(l.name)}
        ${l.direction==='rtl' ? '<span class="badge badge-muted ms-1" style="font-size:.6rem;">RTL</span>' : ''}
      </button>`
    ).join('');

    await loadTranslations(_currentLang);
  }

  async function switchLang(code) {
    _currentLang = code;
    _page = 1;
    document.getElementById('transValueHeader').textContent = `Value (${code})`;
    await loadLanguages(); // re-render tabs with new active
  }

  // ── Load translations ─────────────────────────────────────────────────────────
  async function loadTranslations(lang) {
    document.getElementById('translationsBody').innerHTML =
      '<tr><td colspan="4" style="text-align:center;padding:2rem;"><div class="skeleton skeleton-text"></div></td></tr>';

    const res = await App.apiGet(`/Handlers/Translations.ashx?lang=${lang}&all=1`);
    const bundle = res?.data?.translations || {};

    _allRows = Object.entries(bundle)
      .filter(([k]) => !k.startsWith('__'))
      .map(([key, text]) => ({ key, text, module: guessModule(key), edited: false }));

    applyFilters();
  }

  function applyFilters() {
    _filteredRows = _allRows.filter(r => {
      const matchSearch = !_searchQuery ||
        r.key.toLowerCase().includes(_searchQuery) ||
        r.text.toLowerCase().includes(_searchQuery);
      const matchModule = !_moduleFilter || r.module === _moduleFilter;
      return matchSearch && matchModule;
    });
    _page = 1;
    renderTable();
  }

  function renderTable() {
    const tbody = document.getElementById('translationsBody');
    const total = _filteredRows.length;
    const start = (_page - 1) * PAGE_SIZE;
    const rows  = _filteredRows.slice(start, start + PAGE_SIZE);

    document.getElementById('transPageInfo').textContent = `${start + 1}–${Math.min(start + rows.length, total)} of ${total}`;
    document.getElementById('translationCount').textContent = `${total} keys`;

    if (!rows.length) {
      tbody.innerHTML = `<tr><td colspan="4" style="text-align:center;padding:2rem;color:var(--text-muted);" data-i18n="common.no_data">No records found.</td></tr>`;
      renderPagination(0, 0);
      return;
    }

    tbody.innerHTML = rows.map((r, i) =>
      `<tr id="tr_${start+i}" data-key="${escAttr(r.key)}" data-module="${escAttr(r.module)}">
        <td class="font-mono" style="font-size:.75rem;color:var(--text-muted);">${escHtml(r.key)}</td>
        <td><span class="badge badge-muted">${escHtml(r.module)}</span></td>
        <td>
          <input type="text" class="form-control" style="padding:.35rem .625rem;font-size:.82rem;" 
                 value="${escAttr(r.text)}"
                 onchange="TransAdmin.markEdited(${start+i}, this.value)"
                 onblur="TransAdmin.autoSave(${start+i})" />
        </td>
        <td>
          <div class="d-flex gap-1">
            <button class="btn btn-ghost btn-icon btn-sm" onclick="TransAdmin.saveRow(${start+i})" title="Save" data-i18n-title="common.save">
              <i class="bi bi-check2" style="color:var(--success);"></i>
            </button>
            <button class="btn btn-ghost btn-icon btn-sm" onclick="TransAdmin.deleteRow(${start+i})" title="Delete" data-i18n-title="common.delete">
              <i class="bi bi-trash3" style="color:var(--danger);"></i>
            </button>
          </div>
        </td>
      </tr>`
    ).join('');

    renderPagination(total, PAGE_SIZE);
    i18n.translatePage();
  }

  function renderPagination(total, pageSize) {
    const pages = Math.ceil(total / pageSize) || 1;
    const pag = document.getElementById('transPagination');
    if (!pag) return;
    const btns = [];
    btns.push(`<button class="page-btn" ${_page===1?'disabled':''} onclick="TransAdmin.goPage(${_page-1})"><i class="bi bi-chevron-left"></i></button>`);
    for (let p = Math.max(1, _page-2); p <= Math.min(pages, _page+2); p++) {
      btns.push(`<button class="page-btn${p===_page?' active':''}" onclick="TransAdmin.goPage(${p})">${p}</button>`);
    }
    btns.push(`<button class="page-btn" ${_page===pages?'disabled':''} onclick="TransAdmin.goPage(${_page+1})"><i class="bi bi-chevron-right"></i></button>`);
    pag.innerHTML = btns.join('');
  }

  function goPage(p) { _page = p; renderTable(); }

  // ── CRUD ──────────────────────────────────────────────────────────────────────
  function markEdited(idx, value) {
    const globalIdx = (_page - 1) * PAGE_SIZE + idx - ((_page-1)*PAGE_SIZE);
    // find in _filteredRows
    const row = _filteredRows[(_page-1)*PAGE_SIZE + (idx % PAGE_SIZE)];
    if (row) { row.text = value; row.edited = true; }
  }

  function getRowFromIdx(idx) {
    return _filteredRows[(_page-1)*PAGE_SIZE + (idx % PAGE_SIZE)];
  }

  async function saveRow(idx) {
    const row = getRowFromIdx(idx);
    if (!row) return;
    const res = await App.apiPost('/Handlers/Translations.ashx?action=upsert', {
      languageCode: _currentLang,
      key: row.key,
      text: row.text,
      module: row.module,
    });
    if (res?.success) {
      App.showToast(i18n.t('admin.translations.saved'), 'success', 2000);
      row.edited = false;
    } else {
      App.showToast(i18n.t('common.error'), 'error');
    }
  }

  async function autoSave(idx) {
    const row = getRowFromIdx(idx);
    if (row?.edited) await saveRow(idx);
  }

  async function deleteRow(idx) {
    if (!confirm(i18n.t('common.confirm_delete'))) return;
    const row = getRowFromIdx(idx);
    if (!row) return;
    // Remove from arrays
    const fi = _filteredRows.indexOf(row);
    if (fi > -1) _filteredRows.splice(fi, 1);
    const ai = _allRows.indexOf(row);
    if (ai > -1) _allRows.splice(ai, 1);
    renderTable();
    // Also delete on server
    App.apiPost('/Handlers/Translations.ashx?action=delete', { languageCode: _currentLang, key: row.key });
  }

  function addRow() {
    const key = prompt('Translation Key (e.g. module.feature.label):');
    if (!key) return;
    const mod = prompt('Module (e.g. Sales, Common):', 'General');
    _allRows.unshift({ key, text: '', module: mod || 'General', edited: false });
    applyFilters();
  }

  // ── Search / filter ───────────────────────────────────────────────────────────
  let _searchTimer;
  function search(q) {
    clearTimeout(_searchTimer);
    _searchTimer = setTimeout(() => { _searchQuery = q.toLowerCase(); applyFilters(); }, 300);
  }

  function filterModule(mod) { _moduleFilter = mod; applyFilters(); }

  // ── Add Language Modal ────────────────────────────────────────────────────────
  function showAddLangModal() { document.getElementById('addLangOverlay').classList.remove('d-none'); }
  function closeAddLangModal() { document.getElementById('addLangOverlay').classList.add('d-none'); }

  async function saveLanguage() {
    const lang = {
      id: crypto.randomUUID ? crypto.randomUUID() : (Date.now().toString(36)),
      code: document.getElementById('newLangCode').value.trim().toLowerCase(),
      name: document.getElementById('newLangName').value.trim(),
      nativeName: document.getElementById('newLangNative').value.trim(),
      direction: document.getElementById('newLangDir').value,
      flagIcon: document.getElementById('newLangFlag').value.trim(),
      isDefault: document.getElementById('newLangDefault').checked,
      isActive: true,
      sortOrder: 100,
    };
    if (!lang.code || !lang.name) { App.showToast('Code and Name are required', 'error'); return; }

    const res = await App.apiPost('/Handlers/Translations.ashx?action=addlanguage', lang);
    if (res?.success) {
      closeAddLangModal();
      App.showToast('Language added successfully', 'success');
      await loadLanguages();
    } else {
      App.showToast(res?.message || i18n.t('common.error'), 'error');
    }
  }

  // ── Import / Export ───────────────────────────────────────────────────────────
  async function exportJson() {
    const res = await App.apiGet(`/Handlers/Translations.ashx?lang=${_currentLang}&format=json`);
    if (!res) return;
    const blob = new Blob([JSON.stringify(res.data?.translations || {}, null, 2)], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `translations_${_currentLang}.json`;
    a.click();
  }

  function showImportModal(format) {
    App.toggleDropdown('importDropdown');
    document.getElementById('importFormatLabel').textContent = format.toUpperCase();
    document.getElementById('importJsonText').value = '';
    document.getElementById('importResult').classList.add('d-none');
    document.getElementById('importOverlay').classList.remove('d-none');
  }

  async function runImport() {
    const text = document.getElementById('importJsonText').value.trim();
    if (!text) { App.showToast('Paste JSON data to import', 'error'); return; }
    let rows;
    try { rows = JSON.parse(text); }
    catch { App.showToast('Invalid JSON', 'error'); return; }

    const res = await App.apiPost('/Handlers/Translations.ashx?action=import', rows);
    const resultEl = document.getElementById('importResult');
    if (res?.success) {
      resultEl.textContent = i18n.t('admin.translations.import_success', res.count);
      resultEl.className = 'alert alert-success';
      resultEl.classList.remove('d-none');
      await loadTranslations(_currentLang);
    } else {
      resultEl.textContent = i18n.t('common.error');
      resultEl.className = 'alert alert-danger';
      resultEl.classList.remove('d-none');
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  function guessModule(key) {
    const parts = key.split('.');
    if (!parts.length) return 'General';
    const first = parts[0].toLowerCase();
    const map = { login:'Auth', logout:'Auth', nav:'Navigation', common:'Common', admin:'Admin', sync:'Sync', accounting:'Accounting', sales:'Sales', inventory:'Inventory', reports:'Reports', dashboard:'Dashboard', hr:'HR', crm:'CRM' };
    return map[first] || 'General';
  }
  function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }
  function escAttr(s) { return String(s||'').replace(/"/g,'&quot;').replace(/'/g,'&#39;'); }

  document.addEventListener('DOMContentLoaded', init);
  return { init, switchLang, search, filterModule, goPage, markEdited, autoSave, saveRow, deleteRow, addRow, showAddLangModal, closeAddLangModal, saveLanguage, exportJson, showImportModal, runImport };
})();
</script>
</asp:Content>
