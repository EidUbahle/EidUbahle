<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Companies.aspx.cs" Inherits="Admin_Companies" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Companies &amp; Branches – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <span class="breadcrumb-item">Admin</span>
    <span class="breadcrumb-item active">Companies &amp; Branches</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Companies &amp; Branches</h1>
      <p class="page-subtitle" id="compSubtitle">Manage your legal entities and locations</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-primary btn-sm" onclick="CompaniesPage.openCreateCompany()">
        <i class="bi bi-building-add"></i> New Company
      </button>
    </div>
  </div>

  <div class="page-body">
    <div id="companiesAccordion"></div>
  </div>

  <!-- ── Company Modal ─────────────────────────────────────────────── -->
  <div class="modal-overlay" id="companyModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog modal-lg">
      <div class="modal-header">
        <h2 class="modal-title" id="companyModalTitle">New Company</h2>
        <button class="modal-close" onclick="CompaniesPage.closeCompanyModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <form id="companyForm" onsubmit="CompaniesPage.submitCompany(event)">
        <div class="modal-body">
          <input type="hidden" id="companyId" />
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;">
            <div class="form-group">
              <label class="form-label required">Company Name</label>
              <input type="text" id="coName" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label">Legal Name</label>
              <input type="text" id="coLegal" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Registration No.</label>
              <input type="text" id="coReg" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Tax Number (VAT/EIN)</label>
              <input type="text" id="coTax" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Country</label>
              <input type="text" id="coCountry" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">City</label>
              <input type="text" id="coCity" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Email</label>
              <input type="email" id="coEmail" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Phone</label>
              <input type="tel" id="coPhone" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Website</label>
              <input type="url" id="coWebsite" class="form-input" placeholder="https://" />
            </div>
            <div class="form-group">
              <label class="form-label">Default Currency</label>
              <select id="coCurrency" class="form-select">
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
            <div class="form-group">
              <label class="form-label">Fiscal Year Start</label>
              <select id="coFY" class="form-select">
                <option value="01-01">January 1</option>
                <option value="04-01">April 1</option>
                <option value="07-01">July 1</option>
                <option value="10-01">October 1</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Accounting Basis</label>
              <select id="coBasis" class="form-select">
                <option value="Accrual">Accrual</option>
                <option value="Cash">Cash</option>
              </select>
            </div>
          </div>
          <div class="form-group mt-2">
            <label class="form-label">Address</label>
            <textarea id="coAddress" class="form-input" rows="2"></textarea>
          </div>
          <div class="form-group" id="coActiveGroup" style="display:none;">
            <label class="form-check">
              <input type="checkbox" id="coActive" class="form-check-input" checked />
              <span class="form-check-label">Active</span>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="CompaniesPage.closeCompanyModal()">Cancel</button>
          <button type="submit" class="btn btn-primary" id="compSubmitBtn">Create Company</button>
        </div>
      </form>
    </div>
  </div>

  <!-- ── Branch Modal ───────────────────────────────────────────────── -->
  <div class="modal-overlay" id="branchModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog">
      <div class="modal-header">
        <h2 class="modal-title" id="branchModalTitle">New Branch</h2>
        <button class="modal-close" onclick="CompaniesPage.closeBranchModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <form id="branchForm" onsubmit="CompaniesPage.submitBranch(event)">
        <div class="modal-body">
          <input type="hidden" id="branchId" />
          <input type="hidden" id="branchCompanyId" />
          <div style="display:grid;grid-template-columns:2fr 1fr;gap:1rem;">
            <div class="form-group">
              <label class="form-label required">Branch Name</label>
              <input type="text" id="brName" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label">Branch Code</label>
              <input type="text" id="brCode" class="form-input" maxlength="10" placeholder="e.g. HQ01" />
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Address</label>
            <input type="text" id="brAddress" class="form-input" />
          </div>
          <div class="form-group">
            <label class="form-label">Phone</label>
            <input type="tel" id="brPhone" class="form-input" />
          </div>
          <div class="form-group">
            <label class="form-check">
              <input type="checkbox" id="brIsHO" class="form-check-input" />
              <span class="form-check-label">Head Office</span>
            </label>
            <p class="form-hint">Only one branch can be the head office per company.</p>
          </div>
          <div class="form-group" id="brActiveGroup" style="display:none;">
            <label class="form-check">
              <input type="checkbox" id="brActive" class="form-check-input" checked />
              <span class="form-check-label">Active</span>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="CompaniesPage.closeBranchModal()">Cancel</button>
          <button type="submit" class="btn btn-primary" id="brSubmitBtn">Create Branch</button>
        </div>
      </form>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const CompaniesPage = (() => {
  let _companies = [], _branches = [], _editCompany = false, _editBranch = false;

  async function init() { await load(); }

  async function load() {
    const [cr, br] = await Promise.all([
      App.apiGet('/Handlers/Tenant.ashx?resource=company&includeInactive=true'),
      App.apiGet('/Handlers/Tenant.ashx?resource=branch&includeInactive=true')
    ]);
    _companies = cr?.data || [];
    _branches = br?.data || [];
    document.getElementById('compSubtitle').textContent =
      `${_companies.length} compan${_companies.length !== 1 ? 'ies' : 'y'} · ${_branches.length} branch${_branches.length !== 1 ? 'es' : ''}`;
    renderAccordion();
  }

  function renderAccordion() {
    const el = document.getElementById('companiesAccordion');
    if (!_companies.length) {
      el.innerHTML = `<div class="card">
        <div class="card-body text-center p-5 text-muted">
          <i class="bi bi-building" style="font-size:3rem;opacity:.3;"></i>
          <p class="mt-3">No companies yet. <a href="#" onclick="CompaniesPage.openCreateCompany();return false;">Add your first company.</a></p>
        </div></div>`;
      return;
    }
    el.innerHTML = _companies.map(co => {
      const coBranches = _branches.filter(b => b.companyId === co.id);
      return `
      <div class="card mb-3">
        <div class="card-header" style="cursor:pointer;" onclick="CompaniesPage.toggleAccordion('co_${co.id}')">
          <div class="d-flex align-items-center gap-3">
            <i class="bi bi-building text-accent" style="font-size:1.25rem;"></i>
            <div style="flex:1;">
              <div class="fw-600">${esc(co.name)}</div>
              <div class="text-muted small">${esc(co.legalName||'')}${co.country?` · ${esc(co.country)}`:''} · ${co.defaultCurrencyCode}</div>
            </div>
            <div class="d-flex gap-2 align-items-center">
              <span class="badge badge-${co.isActive?'success':'muted'}">${co.isActive?'Active':'Inactive'}</span>
              <span class="text-muted small">${coBranches.length} branch${coBranches.length!==1?'es':''}</span>
              <button class="btn btn-ghost btn-xs" title="Edit" onclick="event.stopPropagation();CompaniesPage.openEditCompany(${JSON.stringify(co.id)})"><i class="bi bi-pencil"></i></button>
              <button class="btn btn-ghost btn-xs text-danger" title="Delete" onclick="event.stopPropagation();CompaniesPage.deleteCompany(${JSON.stringify(co.id)},${JSON.stringify(co.name)})"><i class="bi bi-trash3"></i></button>
              <button class="btn btn-primary btn-xs" title="Add Branch" onclick="event.stopPropagation();CompaniesPage.openCreateBranch(${JSON.stringify(co.id)})">
                <i class="bi bi-plus"></i> Branch
              </button>
            </div>
          </div>
        </div>
        <div id="co_${co.id}" style="display:${coBranches.length?'':'none'}">
          ${coBranches.length ? `
          <table class="data-table" style="margin:0;">
            <thead><tr>
              <th>Branch Name</th><th>Code</th><th>Address</th><th>Users</th><th>Status</th><th>Actions</th>
            </tr></thead>
            <tbody>${coBranches.map(b => `
              <tr>
                <td class="fw-600">${esc(b.name)}${b.isHeadOffice?' <span class="badge badge-accent">HQ</span>':''}</td>
                <td><code>${esc(b.code||'—')}</code></td>
                <td>${esc(b.address||'—')}</td>
                <td>${b.userCount}</td>
                <td><span class="badge badge-${b.isActive?'success':'muted'}">${b.isActive?'Active':'Inactive'}</span></td>
                <td>
                  <div class="d-flex gap-1">
                    <button class="btn btn-ghost btn-xs" onclick="CompaniesPage.openEditBranch(${JSON.stringify(b.id)})"><i class="bi bi-pencil"></i></button>
                    <button class="btn btn-ghost btn-xs text-danger" onclick="CompaniesPage.deleteBranch(${JSON.stringify(b.id)},${JSON.stringify(b.name)})"><i class="bi bi-trash3"></i></button>
                  </div>
                </td>
              </tr>`).join('')}
            </tbody>
          </table>` : `<p class="text-muted p-3">No branches yet.</p>`}
        </div>
      </div>`;
    }).join('');
  }

  function toggleAccordion(id) {
    const el = document.getElementById(id);
    if (el) el.style.display = el.style.display === 'none' ? '' : 'none';
  }

  // ── Company modal ─────────────────────────────────────────────────

  function openCreateCompany() {
    _editCompany = false;
    document.getElementById('companyModalTitle').textContent = 'New Company';
    document.getElementById('compSubmitBtn').textContent = 'Create Company';
    document.getElementById('companyForm').reset();
    document.getElementById('companyId').value = '';
    document.getElementById('coActiveGroup').style.display = 'none';
    showModal('companyModal');
  }

  function openEditCompany(companyId) {
    _editCompany = true;
    const co = _companies.find(c => c.id === companyId);
    if (!co) return;
    document.getElementById('companyModalTitle').textContent = 'Edit Company';
    document.getElementById('compSubmitBtn').textContent = 'Save Changes';
    document.getElementById('companyId').value = co.id;
    setVal('coName', co.name); setVal('coLegal', co.legalName);
    setVal('coReg', co.registrationNumber); setVal('coTax', co.taxNumber);
    setVal('coCountry', co.country); setVal('coCity', co.city);
    setVal('coEmail', co.email); setVal('coPhone', co.phone);
    setVal('coWebsite', co.website); setVal('coAddress', co.address);
    document.getElementById('coCurrency').value = co.defaultCurrencyCode || 'USD';
    document.getElementById('coFY').value = co.fiscalYearStart || '01-01';
    document.getElementById('coBasis').value = co.accountingBasis || 'Accrual';
    document.getElementById('coActive').checked = co.isActive;
    document.getElementById('coActiveGroup').style.display = '';
    showModal('companyModal');
  }

  async function submitCompany(e) {
    e.preventDefault();
    const dto = {
      id: val('companyId'), name: val('coName'), legalName: val('coLegal'),
      registrationNumber: val('coReg'), taxNumber: val('coTax'),
      country: val('coCountry'), city: val('coCity'), email: val('coEmail'),
      phone: val('coPhone'), website: val('coWebsite'), address: val('coAddress'),
      defaultCurrencyCode: document.getElementById('coCurrency').value,
      fiscalYearStart: document.getElementById('coFY').value,
      accountingBasis: document.getElementById('coBasis').value,
      isActive: document.getElementById('coActive').checked
    };
    const res = _editCompany
      ? await App.apiPut('/Handlers/Tenant.ashx?resource=company', dto)
      : await App.apiPost('/Handlers/Tenant.ashx?resource=company', dto);
    if (res?.success) { App.showToast(res.message||'Saved','success'); closeCompanyModal(); await load(); }
    else App.showToast(res?.message||'Error','error');
  }

  async function deleteCompany(id, name) {
    if (!confirm(`Delete company "${name}"? All branches will also be deleted.`)) return;
    const res = await App.apiDelete(`/Handlers/Tenant.ashx?resource=company&id=${id}`);
    if (res?.success) { App.showToast('Company deleted','success'); await load(); }
    else App.showToast(res?.message||'Error','error');
  }

  function closeCompanyModal() { hideModal('companyModal'); }

  // ── Branch modal ──────────────────────────────────────────────────

  function openCreateBranch(companyId) {
    _editBranch = false;
    document.getElementById('branchModalTitle').textContent = 'New Branch';
    document.getElementById('brSubmitBtn').textContent = 'Create Branch';
    document.getElementById('branchForm').reset();
    document.getElementById('branchId').value = '';
    document.getElementById('branchCompanyId').value = companyId;
    document.getElementById('brActiveGroup').style.display = 'none';
    showModal('branchModal');
  }

  function openEditBranch(branchId) {
    _editBranch = true;
    const b = _branches.find(x => x.id === branchId);
    if (!b) return;
    document.getElementById('branchModalTitle').textContent = 'Edit Branch';
    document.getElementById('brSubmitBtn').textContent = 'Save Changes';
    document.getElementById('branchId').value = b.id;
    document.getElementById('branchCompanyId').value = b.companyId;
    setVal('brName', b.name); setVal('brCode', b.code);
    setVal('brAddress', b.address); setVal('brPhone', b.phone);
    document.getElementById('brIsHO').checked = b.isHeadOffice;
    document.getElementById('brActive').checked = b.isActive;
    document.getElementById('brActiveGroup').style.display = '';
    showModal('branchModal');
  }

  async function submitBranch(e) {
    e.preventDefault();
    const dto = {
      id: val('branchId'), companyId: val('branchCompanyId'),
      name: val('brName'), code: val('brCode'),
      address: val('brAddress'), phone: val('brPhone'),
      isHeadOffice: document.getElementById('brIsHO').checked,
      isActive: document.getElementById('brActive').checked
    };
    const res = _editBranch
      ? await App.apiPut('/Handlers/Tenant.ashx?resource=branch', dto)
      : await App.apiPost('/Handlers/Tenant.ashx?resource=branch', dto);
    if (res?.success) { App.showToast(res.message||'Saved','success'); closeBranchModal(); await load(); }
    else App.showToast(res?.message||'Error','error');
  }

  async function deleteBranch(id, name) {
    if (!confirm(`Delete branch "${name}"?`)) return;
    const res = await App.apiDelete(`/Handlers/Tenant.ashx?resource=branch&id=${id}`);
    if (res?.success) { App.showToast('Branch deleted','success'); await load(); }
    else App.showToast(res?.message||'Error','error');
  }

  function closeBranchModal() { hideModal('branchModal'); }

  // ── Helpers ───────────────────────────────────────────────────────

  function showModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'flex'; }
  function hideModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'none'; }
  const val = id => (document.getElementById(id)?.value || '').trim();
  const setVal = (id, v) => { const el = document.getElementById(id); if (el) el.value = v || ''; };
  const esc = s => (!s ? '' : String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'));

  document.addEventListener('DOMContentLoaded', init);
  return { load, openCreateCompany, openEditCompany, closeCompanyModal, submitCompany, deleteCompany,
           openCreateBranch, openEditBranch, closeBranchModal, submitBranch, deleteBranch, toggleAccordion };
})();
</script>
</asp:Content>
