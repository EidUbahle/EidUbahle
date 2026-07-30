<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="ChartOfAccounts.aspx.cs" Inherits="Pages_Accounting_ChartOfAccounts" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Chart of Accounts – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.section.finance">Finance</span>
    <span class="breadcrumb-item active" data-i18n="nav.accounting">Chart of Accounts</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title" data-i18n="nav.accounting">Chart of Accounts</h1>
      <p class="page-subtitle" id="coaSubtitle">Loading…</p>
    </div>
    <div class="page-actions d-flex gap-2 flex-wrap">
      <select class="form-select" id="filterType" onchange="COAPage.load()" style="width:160px;">
        <option value="">All Types</option>
        <option value="Asset">Asset</option>
        <option value="Liability">Liability</option>
        <option value="Equity">Equity</option>
        <option value="Revenue">Revenue</option>
        <option value="Expense">Expense</option>
      </select>
      <button class="btn btn-ghost btn-sm" onclick="COAPage.load()">
        <i class="bi bi-arrow-clockwise"></i>
      </button>
      <button class="btn btn-primary btn-sm" onclick="COAPage.openModal(null)" id="btnAddAccount">
        <i class="bi bi-plus-lg"></i>
        <span>Add Account</span>
      </button>
    </div>
  </div>

  <div class="page-body">

    <!-- Company selector -->
    <div class="card mb-3">
      <div class="card-body" style="padding:.75rem 1rem;">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <label class="fw-600 text-muted" style="font-size:.85rem;">Company</label>
          <select class="form-select" id="companySelect" onchange="COAPage.onCompanyChange()" style="max-width:280px;">
            <option value="">— Select Company —</option>
          </select>
          <div class="ms-auto">
            <input type="text" id="searchInput" class="form-input" placeholder="Search code or name…"
                   oninput="COAPage.onSearch(this.value)" style="width:220px;" />
          </div>
        </div>
      </div>
    </div>

    <!-- Accounts table -->
    <div class="card">
      <div class="card-body p-0">
        <div id="coaLoading" class="text-center py-5"><span class="spinner"></span></div>
        <div id="coaEmpty" class="text-center py-5 text-muted" style="display:none;">
          <i class="bi bi-diagram-3 fs-1 d-block mb-2 opacity-25"></i>
          No accounts found. <a href="#" onclick="COAPage.openModal(null)">Add your first account.</a>
        </div>
        <div id="coaTableWrap" style="display:none;">
          <table class="table table-hover table-sm" id="coaTable">
            <thead>
              <tr>
                <th style="width:120px;">Code</th>
                <th>Name</th>
                <th style="width:120px;">Type</th>
                <th style="width:100px;">Sub-Type</th>
                <th style="width:80px;">Normal</th>
                <th style="width:70px;">Currency</th>
                <th style="width:70px;">Posting</th>
                <th style="width:70px;">Status</th>
                <th style="width:90px;" class="text-end">Actions</th>
              </tr>
            </thead>
            <tbody id="coaTbody"></tbody>
          </table>
        </div>
      </div>
    </div>

  </div>

  <!-- ── Account Modal ─────────────────────────────────────────────────────── -->
  <div class="modal-overlay" id="accountModal" style="display:none;" onclick="if(event.target===this)COAPage.closeModal()">
    <div class="modal-dialog modal-lg" onclick="event.stopPropagation()">
      <div class="modal-header">
        <h3 class="modal-title" id="modalTitle">Add Account</h3>
        <button class="btn btn-ghost btn-icon" onclick="COAPage.closeModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="modal-body">
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label">Account Code <span class="text-danger">*</span></label>
            <input type="text" id="fCode" class="form-input" placeholder="e.g. 1100" maxlength="30" />
          </div>
          <div class="col-md-8">
            <label class="form-label">Account Name <span class="text-danger">*</span></label>
            <input type="text" id="fName" class="form-input" placeholder="e.g. Cash and Cash Equivalents" maxlength="200" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Account Type <span class="text-danger">*</span></label>
            <select id="fType" class="form-select" onchange="COAPage.onTypeChange()">
              <option value="">— Select —</option>
              <option value="Asset">Asset</option>
              <option value="Liability">Liability</option>
              <option value="Equity">Equity</option>
              <option value="Revenue">Revenue</option>
              <option value="Expense">Expense</option>
            </select>
          </div>
          <div class="col-md-4">
            <label class="form-label">Sub-Type</label>
            <select id="fSubType" class="form-select">
              <option value="">— None —</option>
            </select>
          </div>
          <div class="col-md-4">
            <label class="form-label">Parent Account</label>
            <select id="fParent" class="form-select">
              <option value="">— None (Root) —</option>
            </select>
          </div>
          <div class="col-md-4">
            <label class="form-label">Currency</label>
            <select id="fCurrency" class="form-select">
              <option value="">— Default —</option>
            </select>
          </div>
          <div class="col-md-4">
            <label class="form-label">Sort Order</label>
            <input type="number" id="fSortOrder" class="form-input" value="0" min="0" />
          </div>
          <div class="col-md-4 d-flex gap-4 align-items-end">
            <div class="form-check">
              <input type="checkbox" id="fIsPosting" class="form-check-input" checked />
              <label for="fIsPosting" class="form-check-label">Posting Account</label>
            </div>
            <div class="form-check" id="fIsActiveWrap" style="display:none;">
              <input type="checkbox" id="fIsActive" class="form-check-input" checked />
              <label for="fIsActive" class="form-check-label">Active</label>
            </div>
          </div>
          <div class="col-12">
            <label class="form-label">Description</label>
            <textarea id="fDescription" class="form-input" rows="2" placeholder="Optional description…" maxlength="500"></textarea>
          </div>
        </div>
        <div id="formError" class="alert alert-danger mt-3" style="display:none;"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" onclick="COAPage.closeModal()">Cancel</button>
        <button class="btn btn-primary" onclick="COAPage.save()" id="btnSave">
          <i class="bi bi-check-lg"></i> Save Account
        </button>
      </div>
    </div>
  </div>

  <script>
  var COAPage = (function () {
    var accounts = [], currencies = [], editId = null;
    var companyId = null, searchTimeout = null;

    function init() {
      loadCompanies();
      loadCurrencies();
    }

    function loadCompanies() {
      App.api('GET', '/Handlers/Tenant.ashx?action=companies', null, function(res) {
        if (!res.success) return;
        var sel = document.getElementById('companySelect');
        (res.data.items || res.data || []).forEach(function(c) {
          var o = document.createElement('option');
          o.value = c.id; o.textContent = c.name;
          sel.appendChild(o);
        });
        // Auto-select first company
        var activeId = App.getActiveCompanyId && App.getActiveCompanyId();
        if (activeId) sel.value = activeId;
        if (sel.value) { companyId = sel.value; load(); }
      });
    }

    function loadCurrencies() {
      App.api('GET', '/Handlers/Banks.ashx?action=currencies', null, function(res) {
        if (!res.success) return;
        currencies = res.data || [];
        var sel = document.getElementById('fCurrency');
        currencies.forEach(function(c) {
          var o = document.createElement('option');
          o.value = c.code; o.textContent = c.code + ' – ' + c.name;
          sel.appendChild(o);
        });
      });
    }

    function onCompanyChange() {
      companyId = document.getElementById('companySelect').value;
      load();
    }

    function load() {
      if (!companyId) return;
      document.getElementById('coaLoading').style.display = '';
      document.getElementById('coaTableWrap').style.display = 'none';
      document.getElementById('coaEmpty').style.display = 'none';

      var type = document.getElementById('filterType').value;
      var url = '/Handlers/Accounts.ashx?companyId=' + companyId;
      if (type) url += '&accountType=' + encodeURIComponent(type);

      App.api('GET', url, null, function(res) {
        document.getElementById('coaLoading').style.display = 'none';
        if (!res.success) { App.toast(res.message || 'Failed to load', 'error'); return; }
        accounts = res.data || [];
        renderTable(accounts);
      });
    }

    function onSearch(val) {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(function() { renderTable(filter(accounts, val)); }, 200);
    }

    function filter(list, q) {
      if (!q) return list;
      q = q.toLowerCase();
      return list.filter(function(a) {
        return a.code.toLowerCase().indexOf(q) >= 0 || a.name.toLowerCase().indexOf(q) >= 0;
      });
    }

    function renderTable(list) {
      var tbody = document.getElementById('coaTbody');
      tbody.innerHTML = '';
      if (!list.length) {
        document.getElementById('coaEmpty').style.display = '';
        document.getElementById('coaTableWrap').style.display = 'none';
        document.getElementById('coaSubtitle').textContent = '0 accounts';
        return;
      }
      document.getElementById('coaEmpty').style.display = 'none';
      document.getElementById('coaTableWrap').style.display = '';
      document.getElementById('coaSubtitle').textContent = list.length + ' accounts';

      var typeColors = { Asset:'primary', Liability:'danger', Equity:'warning', Revenue:'success', Expense:'secondary' };
      list.forEach(function(a) {
        var indent = (a.level - 1) * 20;
        var badgeClass = 'badge-' + (typeColors[a.accountType] || 'default');
        var tr = document.createElement('tr');
        tr.style.opacity = a.isActive ? '1' : '0.5';
        tr.innerHTML = '<td style="font-family:monospace;font-size:.85rem;">' + escHtml(a.code) + '</td>' +
          '<td><span style="padding-left:' + indent + 'px;">' +
            (!a.isPosting ? '<i class="bi bi-folder2 text-muted me-1"></i>' : '<i class="bi bi-dot text-muted me-1"></i>') +
            '<strong>' + escHtml(a.name) + '</strong></span></td>' +
          '<td><span class="badge ' + badgeClass + '">' + escHtml(a.accountType) + '</span></td>' +
          '<td class="text-muted">' + escHtml(a.accountSubType || '') + '</td>' +
          '<td class="text-muted">' + escHtml(a.normalBalance) + '</td>' +
          '<td class="text-muted">' + escHtml(a.currencyCode || '—') + '</td>' +
          '<td><span class="badge ' + (a.isPosting ? 'badge-success' : 'badge-default') + '">' + (a.isPosting ? 'Yes' : 'Header') + '</span></td>' +
          '<td><span class="badge ' + (a.isActive ? 'badge-success' : 'badge-danger') + '">' + (a.isActive ? 'Active' : 'Inactive') + '</span></td>' +
          '<td class="text-end">' +
            '<button class="btn btn-ghost btn-icon btn-xs" onclick="COAPage.openModal(\'' + a.id + '\')" title="Edit"><i class="bi bi-pencil"></i></button>' +
            '<button class="btn btn-ghost btn-icon btn-xs text-danger" onclick="COAPage.confirmDelete(\'' + a.id + '\',\'' + escAttr(a.name) + '\')" title="Delete"><i class="bi bi-trash"></i></button>' +
          '</td>';
        tbody.appendChild(tr);
      });
    }

    var subTypeMap = {
      Asset:     ['CurrentAsset','FixedAsset','OtherAsset','BankAndCash'],
      Liability: ['CurrentLiability','LongTermLiability','OtherLiability'],
      Equity:    ['Capital','RetainedEarnings','OtherEquity'],
      Revenue:   ['Revenue','OtherIncome'],
      Expense:   ['CostOfSales','OperatingExpense','OtherExpense']
    };

    function onTypeChange() {
      var type = document.getElementById('fType').value;
      var sel = document.getElementById('fSubType');
      sel.innerHTML = '<option value="">— None —</option>';
      (subTypeMap[type] || []).forEach(function(st) {
        var o = document.createElement('option');
        o.value = st; o.textContent = st.replace(/([A-Z])/g,' $1').trim();
        sel.appendChild(o);
      });
      // Populate parent dropdown filtered by type
      populateParents(type);
    }

    function populateParents(type) {
      var sel = document.getElementById('fParent');
      sel.innerHTML = '<option value="">— None (Root) —</option>';
      accounts.filter(function(a) { return !type || a.accountType === type; })
        .forEach(function(a) {
          var o = document.createElement('option');
          o.value = a.id; o.textContent = a.code + ' – ' + a.name;
          sel.appendChild(o);
        });
    }

    function openModal(id) {
      editId = id || null;
      document.getElementById('formError').style.display = 'none';
      document.getElementById('modalTitle').textContent = id ? 'Edit Account' : 'Add Account';
      document.getElementById('fIsActiveWrap').style.display = id ? '' : 'none';
      resetForm();
      if (id) {
        App.api('GET', '/Handlers/Accounts.ashx?id=' + id, null, function(res) {
          if (!res.success) { App.toast(res.message, 'error'); return; }
          var a = res.data;
          document.getElementById('fCode').value = a.code || '';
          document.getElementById('fName').value = a.name || '';
          document.getElementById('fType').value = a.accountType || '';
          onTypeChange();
          document.getElementById('fSubType').value = a.accountSubType || '';
          document.getElementById('fParent').value = a.parentId || '';
          document.getElementById('fCurrency').value = a.currencyCode || '';
          document.getElementById('fSortOrder').value = a.sortOrder || 0;
          document.getElementById('fIsPosting').checked = !!a.isPosting;
          document.getElementById('fIsActive').checked = !!a.isActive;
          document.getElementById('fDescription').value = a.description || '';
        });
      } else {
        onTypeChange();
      }
      document.getElementById('accountModal').style.display = '';
    }

    function closeModal() {
      document.getElementById('accountModal').style.display = 'none';
    }

    function resetForm() {
      ['fCode','fName','fDescription'].forEach(function(id) { document.getElementById(id).value = ''; });
      document.getElementById('fType').value = '';
      document.getElementById('fSubType').innerHTML = '<option value="">— None —</option>';
      document.getElementById('fParent').innerHTML = '<option value="">— None —</option>';
      document.getElementById('fCurrency').selectedIndex = 0;
      document.getElementById('fSortOrder').value = '0';
      document.getElementById('fIsPosting').checked = true;
      document.getElementById('fIsActive').checked = true;
    }

    function save() {
      var err = document.getElementById('formError');
      err.style.display = 'none';
      var code = document.getElementById('fCode').value.trim();
      var name = document.getElementById('fName').value.trim();
      var type = document.getElementById('fType').value;
      if (!code || !name || !type) {
        err.textContent = 'Code, Name and Type are required.'; err.style.display = ''; return;
      }

      var dto = {
        companyId: companyId,
        code: code, name: name, accountType: type,
        accountSubType: document.getElementById('fSubType').value || null,
        parentId: document.getElementById('fParent').value || null,
        currencyCode: document.getElementById('fCurrency').value || null,
        sortOrder: parseInt(document.getElementById('fSortOrder').value) || 0,
        isPosting: document.getElementById('fIsPosting').checked,
        description: document.getElementById('fDescription').value.trim() || null
      };

      var btn = document.getElementById('btnSave');
      btn.disabled = true;
      if (editId) {
        dto.id = editId;
        dto.isActive = document.getElementById('fIsActive').checked;
        App.api('PUT', '/Handlers/Accounts.ashx', dto, function(res) {
          btn.disabled = false;
          if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
          App.toast('Account updated', 'success'); closeModal(); load();
        });
      } else {
        App.api('POST', '/Handlers/Accounts.ashx', dto, function(res) {
          btn.disabled = false;
          if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
          App.toast('Account created', 'success'); closeModal(); load();
        });
      }
    }

    function confirmDelete(id, name) {
      if (!confirm('Delete account "' + name + '"?\nThis cannot be undone if it has no transactions.')) return;
      App.api('DELETE', '/Handlers/Accounts.ashx?id=' + id, null, function(res) {
        if (!res.success) { App.toast(res.message || 'Cannot delete', 'error'); return; }
        App.toast('Account deleted', 'success'); load();
      });
    }

    function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
    function escAttr(s) { return String(s||'').replace(/'/g,"\\'"); }

    return { init:init, load:load, onCompanyChange:onCompanyChange, onSearch:onSearch,
             openModal:openModal, closeModal:closeModal, save:save, confirmDelete:confirmDelete,
             onTypeChange:onTypeChange };
  })();

  document.addEventListener('DOMContentLoaded', function() { COAPage.init(); });
  </script>

</asp:Content>
