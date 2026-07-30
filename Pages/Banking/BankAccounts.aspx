<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="BankAccounts.aspx.cs" Inherits="Pages_Banking_BankAccounts" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Bank Accounts – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.section.finance">Finance</span>
    <span class="breadcrumb-item active" data-i18n="nav.banking">Banking</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Bank Accounts</h1>
      <p class="page-subtitle" id="bankSubtitle">Loading…</p>
    </div>
    <div class="page-actions d-flex gap-2 flex-wrap">
      <button class="btn btn-ghost btn-sm" onclick="BankPage.load()">
        <i class="bi bi-arrow-clockwise"></i>
      </button>
      <button class="btn btn-primary btn-sm" onclick="BankPage.openModal(null)">
        <i class="bi bi-plus-lg"></i> Add Bank Account
      </button>
    </div>
  </div>

  <!-- Company selector -->
  <div class="card mb-3">
    <div class="card-body" style="padding:.75rem 1rem;">
      <div class="d-flex gap-3 align-items-center flex-wrap">
        <label class="fw-600 text-muted" style="font-size:.85rem;">Company</label>
        <select class="form-select" id="companySelect" onchange="BankPage.onCompanyChange()" style="max-width:280px;">
          <option value="">— Select Company —</option>
        </select>
      </div>
    </div>
  </div>

  <!-- Bank accounts grid -->
  <div id="bankLoading" class="text-center py-5"><span class="spinner"></span></div>
  <div id="bankEmpty" class="text-center py-5 text-muted" style="display:none;">
    <i class="bi bi-bank fs-1 d-block mb-2 opacity-25"></i>
    No bank accounts found. <a href="#" onclick="BankPage.openModal(null)">Add your first bank account.</a>
  </div>
  <div id="bankGrid" class="row g-3" style="display:none;"></div>

  <!-- ── Bank Account Modal ────────────────────────────────────────────────── -->
  <div class="modal-overlay" id="bankModal" style="display:none;" onclick="if(event.target===this)BankPage.closeModal()">
    <div class="modal-dialog modal-lg" onclick="event.stopPropagation()">
      <div class="modal-header">
        <h3 class="modal-title" id="bankModalTitle">Add Bank Account</h3>
        <button class="btn btn-ghost btn-icon" onclick="BankPage.closeModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="modal-body">
        <div class="row g-3">
          <div class="col-md-6">
            <label class="form-label">Bank Name <span class="text-danger">*</span></label>
            <input type="text" id="bBankName" class="form-input" maxlength="200" />
          </div>
          <div class="col-md-6">
            <label class="form-label">Account Name <span class="text-danger">*</span></label>
            <input type="text" id="bAccountName" class="form-input" maxlength="200" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Account Number <span class="text-danger">*</span></label>
            <input type="text" id="bAccountNumber" class="form-input" maxlength="50" />
          </div>
          <div class="col-md-4">
            <label class="form-label">IBAN</label>
            <input type="text" id="bIBAN" class="form-input" maxlength="50" />
          </div>
          <div class="col-md-4">
            <label class="form-label">SWIFT / BIC</label>
            <input type="text" id="bSwift" class="form-input" maxlength="20" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Currency <span class="text-danger">*</span></label>
            <select id="bCurrency" class="form-select"></select>
          </div>
          <div class="col-md-4">
            <label class="form-label">Opening Balance</label>
            <input type="number" id="bOpeningBalance" class="form-input" value="0" step="0.01" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Opening Date</label>
            <input type="date" id="bOpeningDate" class="form-input" />
          </div>
          <div class="col-md-6">
            <label class="form-label">GL Account (Linked)</label>
            <select id="bGlAccount" class="form-select">
              <option value="">— Not linked —</option>
            </select>
          </div>
          <div class="col-md-6">
            <label class="form-label">Branch</label>
            <select id="bBranch" class="form-select">
              <option value="">— All Branches —</option>
            </select>
          </div>
          <div class="col-12">
            <label class="form-label">Notes</label>
            <textarea id="bNotes" class="form-input" rows="2" maxlength="500"></textarea>
          </div>
          <div class="col-12" id="bIsActiveWrap" style="display:none;">
            <div class="form-check">
              <input type="checkbox" id="bIsActive" class="form-check-input" checked />
              <label for="bIsActive" class="form-check-label">Active</label>
            </div>
          </div>
        </div>
        <div id="bankFormError" class="alert alert-danger mt-3" style="display:none;"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" onclick="BankPage.closeModal()">Cancel</button>
        <button class="btn btn-primary" onclick="BankPage.save()" id="btnBankSave">
          <i class="bi bi-check-lg"></i> Save
        </button>
      </div>
    </div>
  </div>

  <script>
  var BankPage = (function() {
    var companyId = null, editId = null, currencies = [], accounts = [];

    function init() {
      loadCompanies();
      loadCurrencies();
    }

    function loadCompanies() {
      App.api('GET', '/Handlers/Tenant.ashx?action=companies', null, function(res) {
        if (!res.success) return;
        var sel = document.getElementById('companySelect');
        (res.data.items || res.data || []).forEach(function(c) {
          var o = document.createElement('option'); o.value = c.id; o.textContent = c.name; sel.appendChild(o);
        });
        var activeId = App.getActiveCompanyId && App.getActiveCompanyId();
        if (activeId) sel.value = activeId;
        if (sel.value) { companyId = sel.value; load(); loadAccounts(); }
      });
    }

    function loadCurrencies() {
      App.api('GET', '/Handlers/Banks.ashx?action=currencies', null, function(res) {
        if (!res.success) return;
        currencies = res.data || [];
        var sel = document.getElementById('bCurrency');
        currencies.forEach(function(c) {
          var o = document.createElement('option'); o.value = c.code; o.textContent = c.code + ' – ' + c.name; sel.appendChild(o);
        });
      });
    }

    function loadAccounts() {
      if (!companyId) return;
      App.api('GET', '/Handlers/Accounts.ashx?companyId=' + companyId + '&activeOnly=true&accountType=Asset', null, function(res) {
        if (!res.success) return;
        accounts = res.data || [];
        var sel = document.getElementById('bGlAccount');
        sel.innerHTML = '<option value="">— Not linked —</option>';
        accounts.forEach(function(a) {
          var o = document.createElement('option'); o.value = a.id; o.textContent = a.code + ' – ' + a.name; sel.appendChild(o);
        });
      });
    }

    function onCompanyChange() {
      companyId = document.getElementById('companySelect').value;
      loadAccounts();
      load();
    }

    function load() {
      if (!companyId) return;
      document.getElementById('bankLoading').style.display = '';
      document.getElementById('bankGrid').style.display = 'none';
      document.getElementById('bankEmpty').style.display = 'none';

      App.api('GET', '/Handlers/Banks.ashx?companyId=' + companyId, null, function(res) {
        document.getElementById('bankLoading').style.display = 'none';
        if (!res.success) { App.toast(res.message, 'error'); return; }
        var items = res.data || [];
        document.getElementById('bankSubtitle').textContent = items.length + ' accounts';
        if (!items.length) { document.getElementById('bankEmpty').style.display = ''; return; }
        renderGrid(items);
      });
    }

    function renderGrid(items) {
      var grid = document.getElementById('bankGrid');
      grid.innerHTML = '';
      grid.style.display = '';
      items.forEach(function(ba) {
        var col = document.createElement('div');
        col.className = 'col-md-6 col-lg-4';
        col.innerHTML = '<div class="card h-100">' +
          '<div class="card-body">' +
          '<div class="d-flex align-items-start justify-content-between mb-2">' +
          '<div><i class="bi bi-bank fs-4 text-primary"></i></div>' +
          '<span class="badge ' + (ba.isActive ? 'badge-success' : 'badge-danger') + '">' + (ba.isActive ? 'Active' : 'Inactive') + '</span>' +
          '</div>' +
          '<h5 class="mb-1">' + escHtml(ba.bankName) + '</h5>' +
          '<p class="text-muted mb-2" style="font-size:.85rem;">' + escHtml(ba.accountName) + '</p>' +
          '<div class="d-flex flex-column gap-1" style="font-size:.82rem;">' +
          '<div><span class="text-muted">Account #:</span> <span class="fw-600">' + escHtml(ba.accountNumber) + '</span></div>' +
          (ba.iban ? '<div><span class="text-muted">IBAN:</span> <span>' + escHtml(ba.iban) + '</span></div>' : '') +
          '<div><span class="text-muted">Currency:</span> <span class="badge badge-default">' + escHtml(ba.currencyCode) + '</span></div>' +
          '<div><span class="text-muted">Balance:</span> <span class="fw-600 text-success">' + fmt(ba.currentBalance) + '</span></div>' +
          (ba.glAccountName ? '<div><span class="text-muted">GL:</span> ' + escHtml(ba.glAccountCode + ' – ' + ba.glAccountName) + '</div>' : '') +
          '</div>' +
          '</div>' +
          '<div class="card-footer d-flex gap-2 justify-content-end">' +
          '<button class="btn btn-ghost btn-sm" onclick="BankPage.openModal(\'' + ba.id + '\')" title="Edit"><i class="bi bi-pencil"></i></button>' +
          '<button class="btn btn-ghost btn-sm text-danger" onclick="BankPage.confirmDelete(\'' + ba.id + '\',\'' + escAttr(ba.bankName) + '\')" title="Delete"><i class="bi bi-trash"></i></button>' +
          '</div></div>';
        grid.appendChild(col);
      });
    }

    function openModal(id) {
      editId = id || null;
      document.getElementById('bankFormError').style.display = 'none';
      document.getElementById('bankModalTitle').textContent = id ? 'Edit Bank Account' : 'Add Bank Account';
      document.getElementById('bIsActiveWrap').style.display = id ? '' : 'none';
      if (!id) {
        ['bBankName','bAccountName','bAccountNumber','bIBAN','bSwift','bNotes'].forEach(function(i) { document.getElementById(i).value = ''; });
        document.getElementById('bCurrency').selectedIndex = 0;
        document.getElementById('bOpeningBalance').value = '0';
        document.getElementById('bOpeningDate').value = '';
        document.getElementById('bGlAccount').value = '';
        document.getElementById('bBranch').value = '';
      } else {
        App.api('GET', '/Handlers/Banks.ashx?id=' + id, null, function(res) {
          if (!res.success) return;
          var ba = res.data;
          document.getElementById('bBankName').value = ba.bankName || '';
          document.getElementById('bAccountName').value = ba.accountName || '';
          document.getElementById('bAccountNumber').value = ba.accountNumber || '';
          document.getElementById('bIBAN').value = ba.iban || '';
          document.getElementById('bSwift').value = ba.swiftCode || '';
          document.getElementById('bCurrency').value = ba.currencyCode || '';
          document.getElementById('bOpeningBalance').value = ba.openingBalance || '0';
          document.getElementById('bOpeningDate').value = ba.openingDate ? ba.openingDate.substr(0,10) : '';
          document.getElementById('bGlAccount').value = ba.glAccountId || '';
          document.getElementById('bNotes').value = ba.notes || '';
          document.getElementById('bIsActive').checked = !!ba.isActive;
        });
      }
      document.getElementById('bankModal').style.display = '';
    }

    function closeModal() { document.getElementById('bankModal').style.display = 'none'; }

    function save() {
      var err = document.getElementById('bankFormError');
      err.style.display = 'none';
      var dto = {
        companyId: companyId,
        bankName: document.getElementById('bBankName').value.trim(),
        accountName: document.getElementById('bAccountName').value.trim(),
        accountNumber: document.getElementById('bAccountNumber').value.trim(),
        iban: document.getElementById('bIBAN').value.trim() || null,
        swiftCode: document.getElementById('bSwift').value.trim() || null,
        currencyCode: document.getElementById('bCurrency').value,
        openingBalance: parseFloat(document.getElementById('bOpeningBalance').value) || 0,
        openingDate: document.getElementById('bOpeningDate').value || null,
        glAccountId: document.getElementById('bGlAccount').value || null,
        notes: document.getElementById('bNotes').value.trim() || null
      };
      var btn = document.getElementById('btnBankSave');
      btn.disabled = true;
      if (editId) {
        dto.id = editId; dto.isActive = document.getElementById('bIsActive').checked;
        App.api('PUT', '/Handlers/Banks.ashx', dto, function(res) {
          btn.disabled = false;
          if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
          App.toast('Bank account updated', 'success'); closeModal(); load();
        });
      } else {
        App.api('POST', '/Handlers/Banks.ashx', dto, function(res) {
          btn.disabled = false;
          if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
          App.toast('Bank account created', 'success'); closeModal(); load();
        });
      }
    }

    function confirmDelete(id, name) {
      if (!confirm('Delete bank account "' + name + '"?')) return;
      App.api('DELETE', '/Handlers/Banks.ashx?id=' + id, null, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Deleted', 'success'); load();
      });
    }

    function fmt(n) { return (parseFloat(n)||0).toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2}); }
    function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
    function escAttr(s) { return String(s||'').replace(/'/g,"\\'"); }

    return { init:init, load:load, onCompanyChange:onCompanyChange, openModal:openModal, closeModal:closeModal, save:save, confirmDelete:confirmDelete };
  })();

  document.addEventListener('DOMContentLoaded', function() { BankPage.init(); });
  </script>

</asp:Content>
