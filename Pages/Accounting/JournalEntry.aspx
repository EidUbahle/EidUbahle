<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="JournalEntry.aspx.cs" Inherits="Pages_Accounting_JournalEntry" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Journal Entries – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.section.finance">Finance</span>
    <span class="breadcrumb-item active" data-i18n="nav.journals">Journal Entries</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Journal Entries</h1>
      <p class="page-subtitle" id="jeSubtitle">Loading…</p>
    </div>
    <div class="page-actions d-flex gap-2 flex-wrap">
      <button class="btn btn-ghost btn-sm" onclick="JEPage.load()">
        <i class="bi bi-arrow-clockwise"></i>
      </button>
      <button class="btn btn-primary btn-sm" onclick="JEPage.openNew()" id="btnNewJE">
        <i class="bi bi-plus-lg"></i> New Journal Entry
      </button>
    </div>
  </div>

  <!-- List view -->
  <div id="listView">
    <!-- Filters -->
    <div class="card mb-3">
      <div class="card-body" style="padding:.75rem 1rem;">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <select class="form-select" id="companySelect" onchange="JEPage.onCompanyChange()" style="max-width:240px;">
            <option value="">— Select Company —</option>
          </select>
          <input type="text" id="searchInput" class="form-input" placeholder="Search number, description…"
                 oninput="JEPage.onSearch(this.value)" style="flex:1;min-width:180px;" />
          <select class="form-select" id="statusFilter" onchange="JEPage.load()" style="width:140px;">
            <option value="">All Status</option>
            <option value="Draft">Draft</option>
            <option value="Posted">Posted</option>
            <option value="Reversed">Reversed</option>
            <option value="Void">Void</option>
          </select>
          <input type="date" id="startDate" class="form-input" onchange="JEPage.load()" style="width:140px;" />
          <input type="date" id="endDate"   class="form-input" onchange="JEPage.load()" style="width:140px;" />
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body p-0">
        <div id="jeLoading" class="text-center py-5"><span class="spinner"></span></div>
        <div id="jeEmpty" class="text-center py-5 text-muted" style="display:none;">
          <i class="bi bi-journal-text fs-1 d-block mb-2 opacity-25"></i>
          No journal entries found.
        </div>
        <div id="jeTableWrap" style="display:none;">
          <table class="table table-hover table-sm">
            <thead>
              <tr>
                <th>Entry #</th>
                <th>Date</th>
                <th>Description</th>
                <th>Reference</th>
                <th>Currency</th>
                <th class="text-end">Debit</th>
                <th class="text-end">Credit</th>
                <th>Status</th>
                <th>Created By</th>
                <th class="text-end">Actions</th>
              </tr>
            </thead>
            <tbody id="jeTbody"></tbody>
          </table>
          <!-- Pagination -->
          <div class="d-flex align-items-center justify-content-between p-3 border-top" id="jePagination">
            <span id="jePagInfo" class="text-muted" style="font-size:.85rem;"></span>
            <div class="d-flex gap-2">
              <button class="btn btn-ghost btn-sm" id="jePrevBtn" onclick="JEPage.prevPage()"><i class="bi bi-chevron-left"></i></button>
              <button class="btn btn-ghost btn-sm" id="jeNextBtn" onclick="JEPage.nextPage()"><i class="bi bi-chevron-right"></i></button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- New/Edit Entry Form -->
  <div id="formView" style="display:none;">
    <div class="d-flex align-items-center gap-3 mb-3">
      <button class="btn btn-ghost btn-sm" onclick="JEPage.showList()">
        <i class="bi bi-arrow-left"></i> Back
      </button>
      <h2 class="mb-0" id="formTitle">New Journal Entry</h2>
    </div>

    <div class="card mb-3">
      <div class="card-header"><span class="fw-600">Entry Details</span></div>
      <div class="card-body">
        <div class="row g-3">
          <div class="col-md-3">
            <label class="form-label">Entry Date <span class="text-danger">*</span></label>
            <input type="date" id="fDate" class="form-input" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Currency</label>
            <select id="fCurrency" class="form-select"></select>
          </div>
          <div class="col-md-3">
            <label class="form-label">Exchange Rate</label>
            <input type="number" id="fExchangeRate" class="form-input" value="1" step="0.0001" min="0.0001" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Entry Type</label>
            <select id="fEntryType" class="form-select">
              <option value="Normal">Normal</option>
              <option value="Adjustment">Adjustment</option>
              <option value="Opening">Opening</option>
              <option value="Closing">Closing</option>
            </select>
          </div>
          <div class="col-md-6">
            <label class="form-label">Description <span class="text-danger">*</span></label>
            <input type="text" id="fDescription" class="form-input" placeholder="Enter description…" maxlength="500" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Reference</label>
            <input type="text" id="fReference" class="form-input" placeholder="Optional reference…" maxlength="100" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Branch</label>
            <select id="fBranch" class="form-select">
              <option value="">— All Branches —</option>
            </select>
          </div>
          <div class="col-12">
            <label class="form-label">Notes</label>
            <textarea id="fNotes" class="form-input" rows="2" maxlength="1000" placeholder="Internal notes…"></textarea>
          </div>
        </div>
      </div>
    </div>

    <!-- Lines -->
    <div class="card mb-3">
      <div class="card-header d-flex justify-content-between align-items-center">
        <span class="fw-600">Journal Lines</span>
        <button class="btn btn-ghost btn-sm" onclick="JEPage.addLine()">
          <i class="bi bi-plus-lg"></i> Add Line
        </button>
      </div>
      <div class="card-body p-0">
        <table class="table table-sm mb-0" id="linesTable">
          <thead>
            <tr>
              <th>#</th>
              <th>Account</th>
              <th>Description</th>
              <th class="text-end" style="width:130px;">Debit</th>
              <th class="text-end" style="width:130px;">Credit</th>
              <th style="width:40px;"></th>
            </tr>
          </thead>
          <tbody id="linesTbody"></tbody>
          <tfoot>
            <tr class="fw-bold">
              <td colspan="3" class="text-end">Totals:</td>
              <td class="text-end" id="totalDebit">0.00</td>
              <td class="text-end" id="totalCredit">0.00</td>
              <td></td>
            </tr>
            <tr id="balanceRow">
              <td colspan="6" class="text-center">
                <span id="balanceMsg" class="badge badge-success">Balanced ✓</span>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <div id="formError" class="alert alert-danger" style="display:none;"></div>

    <div class="d-flex gap-2 justify-content-end">
      <button class="btn btn-ghost" onclick="JEPage.showList()">Cancel</button>
      <button class="btn btn-secondary" id="btnSaveDraft" onclick="JEPage.save(false)">
        <i class="bi bi-floppy"></i> Save Draft
      </button>
      <button class="btn btn-primary" id="btnSavePost" onclick="JEPage.save(true)">
        <i class="bi bi-send-check"></i> Save & Post
      </button>
    </div>
  </div>

  <script>
  var JEPage = (function() {
    var currentPage = 1, totalPages = 0, companyId = null, accounts = [], currencies = [];
    var searchTimeout = null;

    function init() {
      loadCompanies();
      loadCurrencies();
      var today = new Date().toISOString().substr(0,10);
      document.getElementById('fDate').value = today;
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
        var sel = document.getElementById('fCurrency');
        currencies.forEach(function(c) {
          var o = document.createElement('option'); o.value = c.code;
          o.textContent = c.code + (c.symbol ? ' (' + c.symbol + ')' : '');
          sel.appendChild(o);
        });
      });
    }

    function loadAccounts() {
      if (!companyId) return;
      App.api('GET', '/Handlers/Accounts.ashx?companyId=' + companyId + '&activeOnly=true', null, function(res) {
        if (res.success) accounts = (res.data || []).filter(function(a) { return a.isPosting; });
      });
    }

    function onCompanyChange() {
      companyId = document.getElementById('companySelect').value;
      currentPage = 1;
      loadAccounts();
      load();
    }

    function onSearch(val) {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(function() { currentPage = 1; load(); }, 300);
    }

    function load() {
      if (!companyId) return;
      document.getElementById('jeLoading').style.display = '';
      document.getElementById('jeTableWrap').style.display = 'none';
      document.getElementById('jeEmpty').style.display = 'none';

      var search = document.getElementById('searchInput').value;
      var status = document.getElementById('statusFilter').value;
      var startDate = document.getElementById('startDate').value;
      var endDate   = document.getElementById('endDate').value;
      var url = '/Handlers/Journals.ashx?companyId=' + companyId +
        '&page=' + currentPage + '&pageSize=20' +
        (search ? '&search=' + encodeURIComponent(search) : '') +
        (status ? '&status=' + status : '') +
        (startDate ? '&startDate=' + startDate : '') +
        (endDate   ? '&endDate='   + endDate   : '');

      App.api('GET', url, null, function(res) {
        document.getElementById('jeLoading').style.display = 'none';
        if (!res.success) { App.toast(res.message, 'error'); return; }
        var d = res.data;
        totalPages = d.totalPages;
        renderTable(d.items || []);
        document.getElementById('jeSubtitle').textContent = d.totalCount + ' entries';
        document.getElementById('jePagInfo').textContent =
          'Page ' + d.page + ' of ' + d.totalPages + ' (' + d.totalCount + ' total)';
        document.getElementById('jePrevBtn').disabled = !d.hasPreviousPage;
        document.getElementById('jeNextBtn').disabled = !d.hasNextPage;
        document.getElementById('jePagination').style.display = d.totalCount ? '' : 'none';
      });
    }

    function renderTable(items) {
      var tbody = document.getElementById('jeTbody');
      tbody.innerHTML = '';
      if (!items.length) {
        document.getElementById('jeEmpty').style.display = '';
        document.getElementById('jeTableWrap').style.display = 'none';
        return;
      }
      document.getElementById('jeEmpty').style.display = 'none';
      document.getElementById('jeTableWrap').style.display = '';

      var statusColors = { Draft:'default', Posted:'success', Reversed:'warning', Void:'danger' };
      items.forEach(function(je) {
        var tr = document.createElement('tr');
        tr.innerHTML =
          '<td><a href="#" onclick="JEPage.viewEntry(\'' + je.id + '\');return false;" class="fw-600 text-primary">' + escHtml(je.entryNumber) + '</a></td>' +
          '<td>' + (je.entryDate ? je.entryDate.substr(0,10) : '') + '</td>' +
          '<td class="truncate" style="max-width:200px;" title="' + escAttr(je.description) + '">' + escHtml(je.description) + '</td>' +
          '<td class="text-muted">' + escHtml(je.reference || '') + '</td>' +
          '<td>' + escHtml(je.currencyCode) + '</td>' +
          '<td class="text-end">' + fmt(je.totalDebit) + '</td>' +
          '<td class="text-end">' + fmt(je.totalCredit) + '</td>' +
          '<td><span class="badge badge-' + (statusColors[je.status] || 'default') + '">' + je.status + '</span></td>' +
          '<td class="text-muted">' + escHtml(je.createdByName || '') + '</td>' +
          '<td class="text-end d-flex gap-1 justify-content-end">' +
            '<button class="btn btn-ghost btn-icon btn-xs" onclick="JEPage.viewEntry(\'' + je.id + '\')" title="View"><i class="bi bi-eye"></i></button>' +
            (je.status === 'Draft' ? '<button class="btn btn-ghost btn-icon btn-xs text-success" onclick="JEPage.postEntry(\'' + je.id + '\')" title="Post"><i class="bi bi-send-check"></i></button>' : '') +
            (je.status === 'Posted' ? '<button class="btn btn-ghost btn-icon btn-xs text-warning" onclick="JEPage.reverseEntry(\'' + je.id + '\')" title="Reverse"><i class="bi bi-arrow-counterclockwise"></i></button>' : '') +
            (je.status === 'Draft' ? '<button class="btn btn-ghost btn-icon btn-xs text-danger" onclick="JEPage.deleteEntry(\'' + je.id + '\')" title="Delete"><i class="bi bi-trash"></i></button>' : '') +
          '</td>';
        tbody.appendChild(tr);
      });
    }

    var lineCount = 0;
    function openNew() {
      document.getElementById('listView').style.display = 'none';
      document.getElementById('formView').style.display = '';
      document.getElementById('formTitle').textContent = 'New Journal Entry';
      document.getElementById('linesTbody').innerHTML = '';
      document.getElementById('formError').style.display = 'none';
      lineCount = 0;
      addLine(); addLine();
      updateTotals();
    }

    function showList() {
      document.getElementById('listView').style.display = '';
      document.getElementById('formView').style.display = 'none';
    }

    function addLine() {
      lineCount++;
      var n = lineCount;
      var tbody = document.getElementById('linesTbody');
      var tr = document.createElement('tr');
      tr.id = 'line_' + n;
      var opts = accounts.map(function(a) { return '<option value="' + a.id + '">' + escHtml(a.code + ' – ' + a.name) + '</option>'; }).join('');
      tr.innerHTML =
        '<td style="width:30px;">' + n + '</td>' +
        '<td><select class="form-select form-select-sm" id="lacct_' + n + '"><option value="">— Select Account —</option>' + opts + '</select></td>' +
        '<td><input type="text" class="form-input form-input-sm" id="ldesc_' + n + '" placeholder="Description…" /></td>' +
        '<td><input type="number" class="form-input form-input-sm text-end" id="ldebit_' + n + '" value="" min="0" step="0.01" oninput="JEPage.onAmountChange(' + n + ',\'debit\')" /></td>' +
        '<td><input type="number" class="form-input form-input-sm text-end" id="lcredit_' + n + '" value="" min="0" step="0.01" oninput="JEPage.onAmountChange(' + n + ',\'credit\')" /></td>' +
        '<td><button class="btn btn-ghost btn-icon btn-xs text-danger" onclick="JEPage.removeLine(' + n + ')"><i class="bi bi-x"></i></button></td>';
      tbody.appendChild(tr);
    }

    function onAmountChange(n, side) {
      // Clear opposite side when value entered
      var debit = document.getElementById('ldebit_' + n);
      var credit = document.getElementById('lcredit_' + n);
      if (side === 'debit' && parseFloat(debit.value) > 0) credit.value = '';
      if (side === 'credit' && parseFloat(credit.value) > 0) debit.value = '';
      updateTotals();
    }

    function removeLine(n) {
      var el = document.getElementById('line_' + n);
      if (el) el.remove();
      updateTotals();
    }

    function updateTotals() {
      var totalD = 0, totalC = 0;
      for (var i = 1; i <= lineCount; i++) {
        var d = document.getElementById('ldebit_' + i);
        var c = document.getElementById('lcredit_' + i);
        if (!d) continue;
        totalD += parseFloat(d.value) || 0;
        totalC += parseFloat(c.value) || 0;
      }
      document.getElementById('totalDebit').textContent  = totalD.toFixed(2);
      document.getElementById('totalCredit').textContent = totalC.toFixed(2);
      var balanced = Math.abs(totalD - totalC) < 0.005;
      var msg = document.getElementById('balanceMsg');
      msg.textContent = balanced ? 'Balanced ✓' : 'Not Balanced — Diff: ' + Math.abs(totalD - totalC).toFixed(2);
      msg.className = 'badge ' + (balanced ? 'badge-success' : 'badge-danger');
    }

    function getLines() {
      var lines = [];
      for (var i = 1; i <= lineCount; i++) {
        var acct = document.getElementById('lacct_' + i);
        if (!acct || !acct.value) continue;
        var debit  = parseFloat(document.getElementById('ldebit_' + i).value)  || 0;
        var credit = parseFloat(document.getElementById('lcredit_' + i).value) || 0;
        if (debit === 0 && credit === 0) continue;
        lines.push({ lineNumber: lines.length + 1, accountId: acct.value,
                     description: document.getElementById('ldesc_' + i).value,
                     debitAmount: debit, creditAmount: credit, exchangeRate: 1 });
      }
      return lines;
    }

    function save(postImmediately) {
      var err = document.getElementById('formError');
      err.style.display = 'none';
      var lines = getLines();
      var dto = {
        companyId: companyId,
        entryDate: document.getElementById('fDate').value,
        description: document.getElementById('fDescription').value.trim(),
        reference: document.getElementById('fReference').value.trim() || null,
        entryType: document.getElementById('fEntryType').value,
        currencyCode: document.getElementById('fCurrency').value || 'USD',
        exchangeRate: parseFloat(document.getElementById('fExchangeRate').value) || 1,
        notes: document.getElementById('fNotes').value.trim() || null,
        lines: lines,
        postImmediately: postImmediately
      };
      document.getElementById('btnSaveDraft').disabled = true;
      document.getElementById('btnSavePost').disabled = true;
      App.api('POST', '/Handlers/Journals.ashx', dto, function(res) {
        document.getElementById('btnSaveDraft').disabled = false;
        document.getElementById('btnSavePost').disabled = false;
        if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
        App.toast(res.message || 'Saved', 'success');
        showList(); load();
      });
    }

    function viewEntry(id) {
      App.api('GET', '/Handlers/Journals.ashx?id=' + id, null, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        var je = res.data;
        showEntryDetail(je);
      });
    }

    function showEntryDetail(je) {
      // Simple detail view – navigate to list and show a modal or expanded row
      // For brevity, show alert with key info; production would show a modal or detail panel
      var info = 'Entry: ' + je.entryNumber + '\nDate: ' + (je.entryDate||'').substr(0,10) +
        '\nStatus: ' + je.status + '\nDescription: ' + je.description +
        '\nDebit: ' + fmt(je.totalDebit) + '  Credit: ' + fmt(je.totalCredit) +
        '\nLines: ' + (je.lines||[]).length;
      alert(info);
    }

    function postEntry(id) {
      if (!confirm('Post this journal entry? This cannot be undone.')) return;
      App.api('POST', '/Handlers/Journals.ashx?action=post', { journalEntryId: id }, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Journal entry posted', 'success'); load();
      });
    }

    function reverseEntry(id) {
      var date = prompt('Reversal date (YYYY-MM-DD):', new Date().toISOString().substr(0,10));
      if (!date) return;
      App.api('POST', '/Handlers/Journals.ashx?action=reverse', {
        journalEntryId: id, reverseDate: date, description: ''
      }, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Reversal entry created and posted', 'success'); load();
      });
    }

    function deleteEntry(id) {
      if (!confirm('Delete this draft journal entry?')) return;
      App.api('DELETE', '/Handlers/Journals.ashx?id=' + id, null, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Journal entry deleted', 'success'); load();
      });
    }

    function prevPage() { if (currentPage > 1) { currentPage--; load(); } }
    function nextPage() { if (currentPage < totalPages) { currentPage++; load(); } }
    function fmt(n) { return (parseFloat(n)||0).toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2}); }
    function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
    function escAttr(s) { return String(s||'').replace(/'/g,"\\'"); }

    return { init:init, load:load, onCompanyChange:onCompanyChange, onSearch:onSearch,
             openNew:openNew, showList:showList, addLine:addLine, removeLine:removeLine,
             onAmountChange:onAmountChange, save:save, viewEntry:viewEntry,
             postEntry:postEntry, reverseEntry:reverseEntry, deleteEntry:deleteEntry,
             prevPage:prevPage, nextPage:nextPage };
  })();

  document.addEventListener('DOMContentLoaded', function() { JEPage.init(); });
  </script>

</asp:Content>
