<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="FiscalPeriods.aspx.cs" Inherits="Pages_Accounting_FiscalPeriods" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Fiscal Periods – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.section.finance">Finance</span>
    <span class="breadcrumb-item active">Fiscal Periods</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Fiscal Years &amp; Periods</h1>
      <p class="page-subtitle" id="fySubtitle">Loading…</p>
    </div>
    <div class="page-actions d-flex gap-2">
      <button class="btn btn-ghost btn-sm" onclick="FYPage.load()"><i class="bi bi-arrow-clockwise"></i></button>
      <button class="btn btn-primary btn-sm" onclick="FYPage.openNewYear()">
        <i class="bi bi-plus-lg"></i> New Fiscal Year
      </button>
    </div>
  </div>

  <!-- Company selector -->
  <div class="card mb-3">
    <div class="card-body" style="padding:.75rem 1rem;">
      <div class="d-flex gap-3 align-items-center flex-wrap">
        <label class="fw-600 text-muted" style="font-size:.85rem;">Company</label>
        <select class="form-select" id="companySelect" onchange="FYPage.onCompanyChange()" style="max-width:280px;">
          <option value="">— Select Company —</option>
        </select>
      </div>
    </div>
  </div>

  <div id="fyLoading" class="text-center py-5"><span class="spinner"></span></div>
  <div id="fyEmpty" class="text-center py-5 text-muted" style="display:none;">
    <i class="bi bi-calendar3 fs-1 d-block mb-2 opacity-25"></i>
    No fiscal years found. <a href="#" onclick="FYPage.openNewYear()">Create your first fiscal year.</a>
  </div>
  <div id="fyList"></div>

  <!-- New Fiscal Year Modal -->
  <div class="modal-overlay" id="fyModal" style="display:none;" onclick="if(event.target===this)FYPage.closeModal()">
    <div class="modal-dialog" onclick="event.stopPropagation()">
      <div class="modal-header">
        <h3 class="modal-title">New Fiscal Year</h3>
        <button class="btn btn-ghost btn-icon" onclick="FYPage.closeModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="modal-body">
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label">Year <span class="text-danger">*</span></label>
            <input type="number" id="fyYear" class="form-input" min="2000" max="2100" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Start Date <span class="text-danger">*</span></label>
            <input type="date" id="fyStart" class="form-input" />
          </div>
          <div class="col-md-4">
            <label class="form-label">End Date <span class="text-danger">*</span></label>
            <input type="date" id="fyEnd" class="form-input" />
          </div>
          <div class="col-12">
            <small class="text-muted">12 monthly periods will be automatically created.</small>
          </div>
        </div>
        <div id="fyModalError" class="alert alert-danger mt-3" style="display:none;"></div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" onclick="FYPage.closeModal()">Cancel</button>
        <button class="btn btn-primary" onclick="FYPage.createYear()" id="btnCreateYear">
          <i class="bi bi-check-lg"></i> Create Fiscal Year
        </button>
      </div>
    </div>
  </div>

  <script>
  var FYPage = (function() {
    var companyId = null;

    function init() { loadCompanies(); }

    function loadCompanies() {
      App.api('GET', '/Handlers/Tenant.ashx?action=companies', null, function(res) {
        if (!res.success) return;
        var sel = document.getElementById('companySelect');
        (res.data.items || res.data || []).forEach(function(c) {
          var o = document.createElement('option'); o.value = c.id; o.textContent = c.name; sel.appendChild(o);
        });
        var activeId = App.getActiveCompanyId && App.getActiveCompanyId();
        if (activeId) sel.value = activeId;
        if (sel.value) { companyId = sel.value; load(); }
      });
    }

    function onCompanyChange() { companyId = document.getElementById('companySelect').value; load(); }

    function load() {
      if (!companyId) return;
      document.getElementById('fyLoading').style.display = '';
      document.getElementById('fyList').innerHTML = '';
      document.getElementById('fyEmpty').style.display = 'none';

      App.api('GET', '/Handlers/FiscalPeriods.ashx?companyId=' + companyId, null, function(res) {
        document.getElementById('fyLoading').style.display = 'none';
        if (!res.success) { App.toast(res.message, 'error'); return; }
        var items = res.data || [];
        document.getElementById('fySubtitle').textContent = items.length + ' fiscal years';
        if (!items.length) { document.getElementById('fyEmpty').style.display = ''; return; }
        renderYears(items);
      });
    }

    function renderYears(years) {
      var container = document.getElementById('fyList');
      container.innerHTML = '';
      years.forEach(function(fy) {
        var statusBadge = '<span class="badge badge-' + (fy.status === 'Open' ? 'success' : (fy.status === 'Closed' ? 'danger' : 'warning')) + '">' + fy.status + '</span>';
        var periodsHtml = (fy.periods || []).map(function(p) {
          var pBadge = p.status === 'Open' ? 'badge-success' : 'badge-danger';
          return '<div class="d-flex align-items-center justify-content-between py-1 px-2 border-bottom" style="font-size:.82rem;">' +
            '<span>' + escHtml(p.name) + ' (' + p.startDate.substr(0,10) + ' – ' + p.endDate.substr(0,10) + ')</span>' +
            '<div class="d-flex align-items-center gap-2">' +
            '<span class="badge ' + pBadge + '">' + p.status + '</span>' +
            (p.status === 'Open' ?
              '<button class="btn btn-ghost btn-xs text-warning" onclick="FYPage.closePeriod(\'' + p.id + '\',\'' + escAttr(p.name) + '\')" title="Close Period"><i class="bi bi-lock"></i></button>'
              : '') +
            '</div></div>';
        }).join('');

        var card = document.createElement('div');
        card.className = 'card mb-3';
        card.innerHTML =
          '<div class="card-header d-flex align-items-center justify-content-between">' +
          '<div class="d-flex align-items-center gap-2">' +
          '<i class="bi bi-calendar3 text-primary"></i>' +
          '<span class="fw-600">FY ' + fy.year + '</span>' + statusBadge +
          '<span class="text-muted" style="font-size:.82rem;">' + fy.startDate.substr(0,10) + ' – ' + fy.endDate.substr(0,10) + '</span>' +
          '<span class="badge badge-default">' + fy.openPeriodCount + ' open / ' + fy.closedPeriodCount + ' closed</span>' +
          '</div>' +
          (fy.status === 'Open' ?
            '<button class="btn btn-outline-danger btn-sm" onclick="FYPage.closeYear(\'' + fy.id + '\',\'' + fy.year + '\')">' +
            '<i class="bi bi-lock"></i> Close Year</button>'
            : '<span class="text-muted" style="font-size:.8rem;">Closed ' + (fy.closedAt ? fy.closedAt.substr(0,10) : '') + '</span>') +
          '</div>' +
          '<div class="card-body p-0">' + periodsHtml + '</div>';
        container.appendChild(card);
      });
    }

    function openNewYear() {
      var year = new Date().getFullYear();
      document.getElementById('fyYear').value = year;
      document.getElementById('fyStart').value = year + '-01-01';
      document.getElementById('fyEnd').value = year + '-12-31';
      document.getElementById('fyModalError').style.display = 'none';
      document.getElementById('fyModal').style.display = '';
    }

    function closeModal() { document.getElementById('fyModal').style.display = 'none'; }

    function createYear() {
      var err = document.getElementById('fyModalError');
      err.style.display = 'none';
      var dto = {
        companyId: companyId,
        year: parseInt(document.getElementById('fyYear').value),
        startDate: document.getElementById('fyStart').value,
        endDate: document.getElementById('fyEnd').value
      };
      document.getElementById('btnCreateYear').disabled = true;
      App.api('POST', '/Handlers/FiscalPeriods.ashx?action=create', dto, function(res) {
        document.getElementById('btnCreateYear').disabled = false;
        if (!res.success) { err.textContent = res.message; err.style.display = ''; return; }
        App.toast(res.message || 'Fiscal year created', 'success'); closeModal(); load();
      });
    }

    function closePeriod(id, name) {
      if (!confirm('Close period "' + name + '"? Journal entries cannot be posted to closed periods.')) return;
      App.api('POST', '/Handlers/FiscalPeriods.ashx?action=close_period', { fiscalPeriodId: id }, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Period closed', 'success'); load();
      });
    }

    function closeYear(id, year) {
      if (!confirm('Close fiscal year ' + year + '? All open periods will be closed.')) return;
      App.api('POST', '/Handlers/FiscalPeriods.ashx?action=close_year', { fiscalYearId: id }, function(res) {
        if (!res.success) { App.toast(res.message, 'error'); return; }
        App.toast('Fiscal year closed', 'success'); load();
      });
    }

    function escHtml(s) { return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
    function escAttr(s) { return String(s||'').replace(/'/g,"\\'"); }

    return { init:init, load:load, onCompanyChange:onCompanyChange, openNewYear:openNewYear, closeModal:closeModal, createYear:createYear, closePeriod:closePeriod, closeYear:closeYear };
  })();

  document.addEventListener('DOMContentLoaded', function() { FYPage.init(); });
  </script>

</asp:Content>
