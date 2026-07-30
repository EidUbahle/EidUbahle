<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Reports.aspx.cs" Inherits="Pages_Accounting_Reports" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Financial Reports – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.reports">Reports</span>
    <span class="breadcrumb-item active">Financial Reports</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Financial Reports</h1>
      <p class="page-subtitle">Trial Balance · Balance Sheet · Income Statement</p>
    </div>
    <div class="page-actions d-flex gap-2">
      <button class="btn btn-ghost btn-sm" onclick="window.print()" title="Print">
        <i class="bi bi-printer"></i>
      </button>
      <button class="btn btn-primary btn-sm" onclick="ReportsPage.run()">
        <i class="bi bi-play-fill"></i> Generate Report
      </button>
    </div>
  </div>

  <!-- Filters -->
  <div class="card mb-4">
    <div class="card-body">
      <div class="row g-3 align-items-end">
        <div class="col-md-3">
          <label class="form-label">Report Type <span class="text-danger">*</span></label>
          <select id="reportType" class="form-select">
            <option value="trialbalance">Trial Balance</option>
            <option value="balancesheet">Balance Sheet</option>
            <option value="incomestatement">Income Statement</option>
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">Company <span class="text-danger">*</span></label>
          <select id="companySelect" class="form-select" onchange="ReportsPage.onCompanyChange()">
            <option value="">— Select Company —</option>
          </select>
        </div>
        <div class="col-md-2">
          <label class="form-label">Fiscal Year <span class="text-danger">*</span></label>
          <select id="fiscalYear" class="form-select">
            <option value="">— Select Year —</option>
          </select>
        </div>
        <div class="col-md-2">
          <label class="form-label">From Period</label>
          <select id="fromPeriod" class="form-select">
            <option value="1">Period 1 (Jan)</option>
            <option value="2">Period 2 (Feb)</option>
            <option value="3">Period 3 (Mar)</option>
            <option value="4">Period 4 (Apr)</option>
            <option value="5">Period 5 (May)</option>
            <option value="6">Period 6 (Jun)</option>
            <option value="7">Period 7 (Jul)</option>
            <option value="8">Period 8 (Aug)</option>
            <option value="9">Period 9 (Sep)</option>
            <option value="10">Period 10 (Oct)</option>
            <option value="11">Period 11 (Nov)</option>
            <option value="12">Period 12 (Dec)</option>
          </select>
        </div>
        <div class="col-md-2">
          <label class="form-label">To Period</label>
          <select id="toPeriod" class="form-select">
            <option value="1">Period 1 (Jan)</option>
            <option value="2">Period 2 (Feb)</option>
            <option value="3">Period 3 (Mar)</option>
            <option value="4">Period 4 (Apr)</option>
            <option value="5">Period 5 (May)</option>
            <option value="6">Period 6 (Jun)</option>
            <option value="7">Period 7 (Jul)</option>
            <option value="8">Period 8 (Aug)</option>
            <option value="9">Period 9 (Sep)</option>
            <option value="10">Period 10 (Oct)</option>
            <option value="11">Period 11 (Nov)</option>
            <option value="12" selected>Period 12 (Dec)</option>
          </select>
        </div>
        <div class="col-md-1 d-flex align-items-end">
          <div class="form-check">
            <input type="checkbox" id="includeZero" class="form-check-input" />
            <label for="includeZero" class="form-check-label" style="font-size:.8rem;">Show Zeros</label>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- Report output -->
  <div id="reportLoading" class="text-center py-5" style="display:none;"><span class="spinner"></span></div>
  <div id="reportOutput"></div>

  <script>
  var ReportsPage = (function() {
    function init() {
      loadCompanies();
    }

    function loadCompanies() {
      App.api('GET', '/Handlers/Tenant.ashx?action=companies', null, function(res) {
        if (!res.success) return;
        var sel = document.getElementById('companySelect');
        (res.data.items || res.data || []).forEach(function(c) {
          var o = document.createElement('option'); o.value = c.id; o.textContent = c.name; sel.appendChild(o);
        });
        var activeId = App.getActiveCompanyId && App.getActiveCompanyId();
        if (activeId) { sel.value = activeId; onCompanyChange(); }
      });
    }

    function onCompanyChange() {
      var companyId = document.getElementById('companySelect').value;
      if (!companyId) return;
      App.api('GET', '/Handlers/FiscalPeriods.ashx?companyId=' + companyId, null, function(res) {
        if (!res.success) return;
        var sel = document.getElementById('fiscalYear');
        sel.innerHTML = '<option value="">— Select Year —</option>';
        (res.data || []).forEach(function(fy) {
          var o = document.createElement('option'); o.value = fy.id;
          o.textContent = 'FY ' + fy.year + ' (' + fy.status + ')';
          sel.appendChild(o);
        });
        if (sel.options.length > 1) sel.selectedIndex = 1;
      });
    }

    function run() {
      var report    = document.getElementById('reportType').value;
      var companyId = document.getElementById('companySelect').value;
      var fyId      = document.getElementById('fiscalYear').value;
      var fromP     = document.getElementById('fromPeriod').value;
      var toP       = document.getElementById('toPeriod').value;
      var zero      = document.getElementById('includeZero').checked;

      if (!companyId || !fyId) { App.toast('Please select Company and Fiscal Year', 'warning'); return; }

      var url = '/Handlers/Reports.ashx?report=' + report +
        '&companyId=' + companyId + '&fiscalYearId=' + fyId +
        '&startPeriod=' + fromP + '&endPeriod=' + toP +
        '&includeZero=' + zero;

      document.getElementById('reportLoading').style.display = '';
      document.getElementById('reportOutput').innerHTML = '';

      App.api('GET', url, null, function(res) {
        document.getElementById('reportLoading').style.display = 'none';
        if (!res.success) { App.toast(res.message, 'error'); return; }
        renderReport(report, res.data);
      });
    }

    function renderReport(type, data) {
      var html = '';
      switch (type) {
        case 'trialbalance':  html = renderTrialBalance(data);   break;
        case 'balancesheet':  html = renderBalanceSheet(data);   break;
        case 'incomestatement': html = renderIncomeStatement(data); break;
      }
      document.getElementById('reportOutput').innerHTML = html;
    }

    function renderTrialBalance(tb) {
      var badge = tb.isBalanced
        ? '<span class="badge badge-success ms-2">Balanced ✓</span>'
        : '<span class="badge badge-danger ms-2">NOT BALANCED ✗</span>';
      var rows = tb.lines.map(function(l) {
        var indent = (l.level - 1) * 20;
        return '<tr>' +
          '<td style="padding-left:' + (indent + 12) + 'px;font-family:monospace;">' + escHtml(l.code) + '</td>' +
          '<td>' + escHtml(l.name) + '</td>' +
          '<td><span class="badge badge-xs" style="background:var(--color-primary-10);color:var(--color-primary)">' + escHtml(l.accountType) + '</span></td>' +
          '<td class="text-end">' + (l.openingDebit  ? fmt(l.openingDebit)  : '—') + '</td>' +
          '<td class="text-end">' + (l.openingCredit ? fmt(l.openingCredit) : '—') + '</td>' +
          '<td class="text-end">' + (l.periodDebit   ? fmt(l.periodDebit)   : '—') + '</td>' +
          '<td class="text-end">' + (l.periodCredit  ? fmt(l.periodCredit)  : '—') + '</td>' +
          '<td class="text-end fw-600">' + (l.closingDebit  ? fmt(l.closingDebit)  : '—') + '</td>' +
          '<td class="text-end fw-600">' + (l.closingCredit ? fmt(l.closingCredit) : '—') + '</td>' +
          '</tr>';
      }).join('');
      return '<div class="card">' +
        '<div class="card-header d-flex align-items-center justify-content-between">' +
        '<div><h4 class="mb-0">Trial Balance' + badge + '</h4>' +
        '<small class="text-muted">' + escHtml(tb.companyName) + ' · ' + escHtml(tb.fiscalYearName) + ' · ' + escHtml(tb.periodRange) + '</small></div></div>' +
        '<div class="card-body p-0">' +
        '<table class="table table-sm table-hover mb-0"><thead><tr>' +
        '<th>Code</th><th>Name</th><th>Type</th>' +
        '<th class="text-end">Open Dr</th><th class="text-end">Open Cr</th>' +
        '<th class="text-end">Period Dr</th><th class="text-end">Period Cr</th>' +
        '<th class="text-end">Close Dr</th><th class="text-end">Close Cr</th>' +
        '</tr></thead><tbody>' + rows + '</tbody>' +
        '<tfoot class="fw-bold"><tr><td colspan="7" class="text-end">TOTALS</td>' +
        '<td class="text-end">' + fmt(tb.totalDebit) + '</td>' +
        '<td class="text-end">' + fmt(tb.totalCredit) + '</td></tr></tfoot>' +
        '</table></div></div>';
    }

    function renderBalanceSheet(bs) {
      function renderSection(section) {
        var rows = '';
        section.groups.forEach(function(g) {
          rows += '<tr class="table-light"><td colspan="2" class="fw-600 text-muted" style="font-size:.8rem;text-transform:uppercase;letter-spacing:.05em;">' + escHtml(g.subType) + '</td></tr>';
          g.lines.forEach(function(l) {
            rows += '<tr><td style="padding-left:24px;">' + escHtml(l.name) + '</td><td class="text-end">' + fmt(l.balance) + '</td></tr>';
          });
          rows += '<tr class="fw-600 border-top"><td style="padding-left:12px;">Total ' + escHtml(g.subType) + '</td><td class="text-end">' + fmt(g.total) + '</td></tr>';
        });
        return '<tr class="table-secondary"><td colspan="2" class="fw-bold">' + escHtml(section.title) + '</td></tr>' + rows +
          '<tr class="fw-bold table-primary"><td>Total ' + escHtml(section.title) + '</td><td class="text-end">' + fmt(section.total) + '</td></tr>';
      }
      var balanced = bs.isBalanced
        ? '<span class="badge badge-success ms-2">Balanced ✓</span>'
        : '<span class="badge badge-danger ms-2">NOT BALANCED ✗</span>';
      return '<div class="card">' +
        '<div class="card-header"><h4 class="mb-0">Balance Sheet' + balanced + '</h4>' +
        '<small class="text-muted">' + escHtml(bs.companyName) + ' · ' + escHtml(bs.fiscalYearName) + ' · As of ' + new Date().toLocaleDateString() + '</small></div>' +
        '<div class="card-body p-0"><div class="row g-0">' +
        '<div class="col-md-6 border-end">' +
        '<table class="table table-sm mb-0"><thead><tr><th colspan="2">Assets</th></tr></thead><tbody>' + renderSection(bs.assets) + '</tbody></table></div>' +
        '<div class="col-md-6">' +
        '<table class="table table-sm mb-0"><thead><tr><th colspan="2">Liabilities &amp; Equity</th></tr></thead><tbody>' +
        renderSection(bs.liabilities) + renderSection(bs.equity) +
        '<tr class="fw-bold table-success"><td>Total Liabilities &amp; Equity</td><td class="text-end">' + fmt(bs.totalLiabilitiesAndEquity) + '</td></tr>' +
        '</tbody></table></div></div></div></div>';
    }

    function renderIncomeStatement(is_) {
      function renderSection(s) {
        if (!s.lines || !s.lines.length) return '';
        var rows = s.lines.map(function(l) {
          return '<tr><td style="padding-left:24px;">' + escHtml(l.name) + '</td><td class="text-end">' + fmt(l.balance) + '</td></tr>';
        }).join('');
        return '<tr class="table-secondary"><td colspan="2" class="fw-bold">' + escHtml(s.title) + '</td></tr>' + rows +
          '<tr class="fw-600 border-top"><td style="padding-left:12px;">Total ' + escHtml(s.title) + '</td><td class="text-end">' + fmt(s.total) + '</td></tr>';
      }
      return '<div class="card">' +
        '<div class="card-header"><h4 class="mb-0">Income Statement</h4>' +
        '<small class="text-muted">' + escHtml(is_.companyName) + ' · ' + escHtml(is_.fiscalYearName) + ' · ' + escHtml(is_.periodRange) + '</small></div>' +
        '<div class="card-body p-0"><table class="table table-sm mb-0"><tbody>' +
        renderSection(is_.revenue) + renderSection(is_.costOfSales) +
        '<tr class="fw-bold table-info"><td>Gross Profit</td><td class="text-end">' + fmt(is_.grossProfit) + '</td></tr>' +
        renderSection(is_.operatingExpenses) + renderSection(is_.otherIncome) + renderSection(is_.otherExpenses) +
        '<tr class="fw-bold table-success"><td>Net Income</td><td class="text-end">' + fmt(is_.netIncome) + '</td></tr>' +
        '</tbody></table></div></div>';
    }

    function fmt(n) { return (parseFloat(n)||0).toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2}); }
    function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

    return { init:init, onCompanyChange:onCompanyChange, run:run };
  })();

  document.addEventListener('DOMContentLoaded', function() { ReportsPage.init(); });
  </script>

</asp:Content>
