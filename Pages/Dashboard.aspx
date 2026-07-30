<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Dashboard.aspx.cs" Inherits="Pages_Dashboard" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Dashboard – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <span class="breadcrumb-item active" data-i18n="nav.dashboard">Dashboard</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title" data-i18n="nav.dashboard">Dashboard</h1>
      <p class="page-subtitle" id="dashWelcome">Welcome back</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-secondary btn-sm" onclick="Dashboard.refresh()">
        <i class="bi bi-arrow-repeat"></i>
        <span data-i18n="common.sync_now">Sync Now</span>
      </button>
      <div class="dropdown">
        <button class="btn btn-secondary btn-sm" onclick="App.toggleDropdown('periodDropdown')">
          <i class="bi bi-calendar3"></i>
          <span id="periodLabel">This Month</span>
          <i class="bi bi-chevron-down" style="font-size:.65rem;"></i>
        </button>
        <div class="dropdown-menu" id="periodDropdown">
          <a class="dropdown-item" href="#" onclick="Dashboard.setPeriod('today');return false;">Today</a>
          <a class="dropdown-item" href="#" onclick="Dashboard.setPeriod('week');return false;">This Week</a>
          <a class="dropdown-item active" href="#" onclick="Dashboard.setPeriod('month');return false;">This Month</a>
          <a class="dropdown-item" href="#" onclick="Dashboard.setPeriod('quarter');return false;">This Quarter</a>
          <a class="dropdown-item" href="#" onclick="Dashboard.setPeriod('year');return false;">This Year</a>
        </div>
      </div>
    </div>
  </div>

  <div class="page-body">

    <!-- KPI Cards skeleton → filled by JS -->
    <div id="kpiGrid" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:1rem;margin-bottom:1.5rem;">
      <!-- Skeleton loading state -->
      <div class="kpi-card"><div class="skeleton skeleton-title"></div><div class="skeleton skeleton-text" style="width:60%;height:32px;margin-bottom:.5rem;"></div><div class="skeleton skeleton-text" style="width:80%;"></div></div>
      <div class="kpi-card"><div class="skeleton skeleton-title"></div><div class="skeleton skeleton-text" style="width:60%;height:32px;margin-bottom:.5rem;"></div><div class="skeleton skeleton-text" style="width:80%;"></div></div>
      <div class="kpi-card"><div class="skeleton skeleton-title"></div><div class="skeleton skeleton-text" style="width:60%;height:32px;margin-bottom:.5rem;"></div><div class="skeleton skeleton-text" style="width:80%;"></div></div>
      <div class="kpi-card"><div class="skeleton skeleton-title"></div><div class="skeleton skeleton-text" style="width:60%;height:32px;margin-bottom:.5rem;"></div><div class="skeleton skeleton-text" style="width:80%;"></div></div>
    </div>

    <!-- Charts row -->
    <div style="display:grid;grid-template-columns:2fr 1fr;gap:1rem;margin-bottom:1.5rem;" class="dash-charts-row">
      <!-- Revenue trend -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">Revenue vs Expenses</span>
          <div class="d-flex gap-2">
            <span class="badge badge-accent"><i class="bi bi-circle-fill" style="font-size:.5rem;color:var(--accent);"></i> Revenue</span>
            <span class="badge badge-danger"><i class="bi bi-circle-fill" style="font-size:.5rem;color:var(--danger);"></i> Expenses</span>
          </div>
        </div>
        <div class="card-body" style="padding:1rem;">
          <div id="revenueChart" style="height:220px;display:flex;align-items:center;justify-content:center;">
            <div class="skeleton" style="width:100%;height:100%;border-radius:var(--radius-md);"></div>
          </div>
        </div>
      </div>
      <!-- Cash flow donut -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">Cash Flow</span>
        </div>
        <div class="card-body" style="padding:1rem;">
          <div id="cashFlowChart" style="height:220px;display:flex;align-items:center;justify-content:center;">
            <div class="skeleton" style="width:100%;height:100%;border-radius:var(--radius-md);"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Bottom row: Recent transactions + Quick actions -->
    <div style="display:grid;grid-template-columns:2fr 1fr;gap:1rem;" class="dash-bottom-row">

      <!-- Recent Transactions -->
      <div class="card">
        <div class="card-header">
          <span class="card-title" data-i18n="dashboard.recent_transactions">Recent Transactions</span>
          <a href="/Pages/Accounting/JournalEntry.aspx" class="btn btn-ghost btn-sm" data-i18n="common.view_all">View All</a>
        </div>
        <div id="recentTransactions">
          <table class="data-table" style="margin:0;">
            <thead><tr>
              <th data-i18n="common.date">Date</th>
              <th data-i18n="common.description">Description</th>
              <th data-i18n="common.amount">Amount</th>
              <th data-i18n="common.status">Status</th>
            </tr></thead>
            <tbody id="recentTransBody">
              <tr><td colspan="4" style="text-align:center;padding:2rem;"><div class="skeleton skeleton-text"></div></td></tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">Quick Actions</span>
        </div>
        <div class="card-body" style="display:flex;flex-direction:column;gap:.625rem;">
          <a href="/Pages/Sales/Invoices.aspx?action=new" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-plus-circle"></i> New Invoice
          </a>
          <a href="/Pages/Accounting/JournalEntry.aspx?action=new" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-journal-plus"></i> Journal Entry
          </a>
          <a href="/Pages/Inventory/Products.aspx?action=new" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-box-seam"></i> Add Product
          </a>
          <a href="/Pages/Purchases/PurchaseOrders.aspx?action=new" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-cart-plus"></i> Purchase Order
          </a>
          <a href="/Pages/Banking/BankAccounts.aspx" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-bank"></i> Bank Reconciliation
          </a>
          <a href="/Pages/Reports/Index.aspx" class="btn btn-outline d-flex justify-content-start gap-2">
            <i class="bi bi-bar-chart-line"></i> Run Report
          </a>
        </div>
      </div>

    </div><!-- /bottom row -->

  </div><!-- /page-body -->
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const Dashboard = (() => {
  let _period = 'month';

  async function init() {
    // Welcome message
    const session = await EidDB.getCurrentSession();
    if (session) {
      const hour = new Date().getHours();
      const greeting = hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening';
      const el = document.getElementById('dashWelcome');
      if (el) el.textContent = `${greeting}, ${session.fullName || session.username}`;
    }
    await loadKPIs();
    await loadRecentTransactions();
  }

  async function loadKPIs() {
    const res = await App.apiGet(`/Handlers/Dashboard.ashx?period=${_period}`);
    const grid = document.getElementById('kpiGrid');
    if (!grid) return;

    const kpis = res?.data?.kpis || getDefaultKPIs();
    grid.innerHTML = kpis.map(k => `
      <div class="kpi-card anim-count-up">
        <div class="kpi-icon"><i class="bi bi-${k.icon}"></i></div>
        <div class="kpi-label">${escHtml(k.label)}</div>
        <div class="kpi-value">${escHtml(k.value)}</div>
        ${k.trend !== undefined ? `
          <div class="kpi-trend ${k.trend >= 0 ? 'up' : 'down'}">
            <i class="bi bi-arrow-${k.trend >= 0 ? 'up' : 'down'}-short"></i>
            ${Math.abs(k.trend).toFixed(1)}% vs last period
          </div>` : ''}
      </div>`).join('');
  }

  function getDefaultKPIs() {
    return [
      { label: 'Total Revenue', value: '—', icon: 'cash-coin', trend: null },
      { label: 'Total Expenses', value: '—', icon: 'credit-card', trend: null },
      { label: 'Net Profit', value: '—', icon: 'graph-up-arrow', trend: null },
      { label: 'Outstanding AR', value: '—', icon: 'people', trend: null },
      { label: 'Outstanding AP', value: '—', icon: 'cart3', trend: null },
      { label: 'Cash Balance', value: '—', icon: 'bank', trend: null },
    ];
  }

  async function loadRecentTransactions() {
    const tbody = document.getElementById('recentTransBody');
    if (!tbody) return;
    const res = await App.apiGet('/Handlers/Dashboard.ashx?action=recent_transactions&limit=10');
    const items = res?.data?.transactions || [];
    if (!items.length) {
      tbody.innerHTML = `<tr><td colspan="4" style="text-align:center;padding:2rem;color:var(--text-muted);" data-i18n="common.no_data">No records found.</td></tr>`;
      return;
    }
    tbody.innerHTML = items.map(t => `
      <tr>
        <td data-label="Date">${escHtml(t.date)}</td>
        <td data-label="Description" class="truncate" style="max-width:200px;">${escHtml(t.description)}</td>
        <td data-label="Amount" class="font-mono ${t.amount < 0 ? 'text-danger' : 'text-success'}">${formatCurrency(t.amount, t.currency)}</td>
        <td data-label="Status"><span class="badge badge-${statusClass(t.status)}">${escHtml(t.status)}</span></td>
      </tr>`).join('');
  }

  async function setPeriod(period) {
    _period = period;
    const labels = { today:'Today', week:'This Week', month:'This Month', quarter:'This Quarter', year:'This Year' };
    const el = document.getElementById('periodLabel');
    if (el) el.textContent = labels[period];
    App.toggleDropdown('periodDropdown');
    await loadKPIs();
  }

  async function refresh() {
    await SyncEngine.sync(true);
    await loadKPIs();
    await loadRecentTransactions();
    App.showToast('Dashboard refreshed', 'success');
  }

  function escHtml(s) { return App.apiPost ? s : s; } // use window.escHtml if available
  function formatCurrency(v, c) {
    try { return new Intl.NumberFormat('en-US', { style:'currency', currency: c||'USD', notation:'compact' }).format(v); }
    catch { return v; }
  }
  function statusClass(s) {
    const m = { Posted:'success', Draft:'muted', Pending:'warning', Rejected:'danger', Cancelled:'danger' };
    return m[s] || 'muted';
  }

  document.addEventListener('DOMContentLoaded', init);
  return { init, loadKPIs, loadRecentTransactions, setPeriod, refresh };
})();
</script>
</asp:Content>
