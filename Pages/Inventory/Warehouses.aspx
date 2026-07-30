<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Warehouses.aspx.cs" Inherits="Pages_Inventory_Warehouses" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Warehouses – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <a class="breadcrumb-item" href="/Pages/Inventory/Products.aspx">Inventory</a>
    <span class="breadcrumb-item active">Warehouses</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Warehouses</h1>
      <p class="page-subtitle" id="whSubtitle">Manage storage locations</p>
    </div>
    <div class="page-actions d-flex gap-2">
      <button class="btn btn-ghost btn-sm" onclick="WHPage.load()"><i class="bi bi-arrow-clockwise"></i></button>
      <button class="btn btn-primary btn-sm" onclick="WHPage.openModal(null)">
        <i class="bi bi-plus-lg"></i> New Warehouse
      </button>
    </div>
  </div>

  <div class="page-body">
    <div id="whLoading" class="p-5 text-center">
      <div class="skeleton" style="height:40px;margin-bottom:8px;"></div>
      <div class="skeleton" style="height:40px;"></div>
    </div>
    <div id="whEmpty" class="p-5 text-center d-none">
      <i class="bi bi-building" style="font-size:3rem;color:var(--text-muted);"></i>
      <p class="mt-3 text-muted">No warehouses configured.</p>
    </div>
    <div id="whCards" class="row g-4" style="display:none;"></div>
  </div>

  <!-- ── Warehouse Modal ─────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="whBackdrop" onclick="WHPage.closeModal()"></div>
  <div class="modal-panel d-none" id="whModal" role="dialog" style="max-width:560px;">
    <div class="modal-header">
      <h2 class="modal-title" id="whModalTitle">New Warehouse</h2>
      <button class="btn btn-ghost btn-icon" onclick="WHPage.closeModal()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body">
      <input type="hidden" id="whId" />
      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label required">Company</label>
          <select id="whCompany" class="form-select"><option value="">— Select —</option></select>
        </div>
        <div class="col-md-6">
          <label class="form-label">Branch</label>
          <select id="whBranch" class="form-select"><option value="">— All Branches —</option></select>
        </div>
        <div class="col-md-4">
          <label class="form-label required">Code</label>
          <input type="text" id="whCode" class="form-input" placeholder="WH-001" />
        </div>
        <div class="col-md-8">
          <label class="form-label required">Name</label>
          <input type="text" id="whName" class="form-input" />
        </div>
        <div class="col-12">
          <label class="form-label">Address</label>
          <textarea id="whAddr" class="form-input" rows="2"></textarea>
        </div>
        <div class="col-md-6">
          <label class="form-label">Contact Person</label>
          <input type="text" id="whContact" class="form-input" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Contact Phone</label>
          <input type="text" id="whPhone" class="form-input" />
        </div>
        <div class="col-12">
          <div class="form-check">
            <input type="checkbox" id="whIsDefault" class="form-check-input" />
            <label class="form-check-label" for="whIsDefault">Set as default warehouse for selected company</label>
          </div>
        </div>
      </div>
    </div>
    <div class="modal-footer">
      <div id="whError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="WHPage.closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="WHPage.save()">Save Warehouse</button>
    </div>
  </div>

  <!-- ── Location Modal ─────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="locBackdrop" onclick="WHPage.closeLocModal()"></div>
  <div class="modal-panel d-none" id="locModal" role="dialog" style="max-width:440px;">
    <div class="modal-header">
      <h2 class="modal-title">Add Location</h2>
      <button class="btn btn-ghost btn-icon" onclick="WHPage.closeLocModal()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body">
      <input type="hidden" id="locWarehouseId" />
      <div class="mb-3">
        <label class="form-label required">Code</label>
        <input type="text" id="locCode" class="form-input" placeholder="A1-01" />
      </div>
      <div class="mb-3">
        <label class="form-label required">Name</label>
        <input type="text" id="locName" class="form-input" />
      </div>
      <div class="mb-3">
        <label class="form-label">Type</label>
        <select id="locType" class="form-select">
          <option value="Storage">Storage</option>
          <option value="Receiving">Receiving</option>
          <option value="Shipping">Shipping</option>
          <option value="QC">Quality Control</option>
          <option value="Virtual">Virtual</option>
        </select>
      </div>
    </div>
    <div class="modal-footer">
      <div id="locError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="WHPage.closeLocModal()">Cancel</button>
      <button class="btn btn-primary" onclick="WHPage.saveLoc()">Add Location</button>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server"></asp:Content>
<asp:Content ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
const WHPage = (() => {
  let _warehouses = [];

  function esc(s) { if (!s) return ''; return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }

  async function load() {
    document.getElementById('whLoading').classList.remove('d-none');
    document.getElementById('whCards').style.display = 'none';
    document.getElementById('whEmpty').classList.add('d-none');

    const res = await App.api('/Handlers/Warehouses.ashx?entity=warehouse');
    document.getElementById('whLoading').classList.add('d-none');
    if (!res.success) { App.toast(res.message, 'danger'); return; }
    _warehouses = res.data || [];
    document.getElementById('whSubtitle').textContent = `${_warehouses.length} warehouse(s)`;

    if (!_warehouses.length) { document.getElementById('whEmpty').classList.remove('d-none'); return; }
    document.getElementById('whCards').style.display = '';

    document.getElementById('whCards').innerHTML = _warehouses.map(w => `
      <div class="col-md-6 col-lg-4">
        <div class="card h-100">
          <div class="card-body">
            <div class="d-flex align-items-start justify-content-between mb-2">
              <div>
                <div class="fw-700">${esc(w.Name)}</div>
                <code class="text-muted" style="font-size:.8rem;">${esc(w.Code)}</code>
              </div>
              <div class="d-flex gap-1">
                ${w.IsDefault ? '<span class="badge bg-primary">Default</span>' : ''}
                <span class="badge bg-${w.IsActive?'success':'secondary'}">${w.IsActive?'Active':'Inactive'}</span>
              </div>
            </div>
            <div class="text-muted" style="font-size:.85rem;">
              <div><i class="bi bi-building me-1"></i>${esc(w.CompanyName)}</div>
              ${w.BranchName ? `<div><i class="bi bi-diagram-3 me-1"></i>${esc(w.BranchName)}</div>` : ''}
              ${w.Address ? `<div><i class="bi bi-geo-alt me-1"></i>${esc(w.Address)}</div>` : ''}
              ${w.ContactPerson ? `<div><i class="bi bi-person me-1"></i>${esc(w.ContactPerson)} ${esc(w.ContactPhone||'')}</div>` : ''}
            </div>
            <div class="mt-3 pt-2 border-top d-flex align-items-center justify-content-between">
              <span class="text-muted" style="font-size:.8rem;">${w.LocationCount} location(s)</span>
              <div class="d-flex gap-1">
                <button class="btn btn-ghost btn-sm" onclick="WHPage.openLocModal('${w.Id}')" title="Add Location">
                  <i class="bi bi-plus-circle"></i>
                </button>
                <button class="btn btn-ghost btn-sm" onclick="WHPage.openModal('${w.Id}')" title="Edit">
                  <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-ghost btn-sm text-danger" onclick="WHPage.deleteWarehouse('${w.Id}','${esc(w.Name)}')" title="Delete">
                  <i class="bi bi-trash"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>`).join('');
  }

  async function loadDropdowns() {
    const res = await App.api('/Handlers/Tenant.ashx?action=companies');
    if (res.success && res.data) {
      const sel = document.getElementById('whCompany');
      while (sel.options.length > 1) sel.remove(1);
      res.data.forEach(c => sel.add(new Option(c.Name, c.Id)));
    }
  }

  function openModal(id) {
    const w = id ? _warehouses.find(x => x.Id === id) : null;
    document.getElementById('whModalTitle').textContent = id ? 'Edit Warehouse' : 'New Warehouse';
    document.getElementById('whId').value      = id || '';
    document.getElementById('whCode').value    = w ? w.Code : '';
    document.getElementById('whName').value    = w ? w.Name : '';
    document.getElementById('whAddr').value    = w ? (w.Address||'') : '';
    document.getElementById('whContact').value = w ? (w.ContactPerson||'') : '';
    document.getElementById('whPhone').value   = w ? (w.ContactPhone||'') : '';
    document.getElementById('whIsDefault').checked = w ? w.IsDefault : false;
    document.getElementById('whCompany').value = w ? w.CompanyId : '';
    document.getElementById('whError').textContent = '';
    document.getElementById('whModal').classList.remove('d-none');
    document.getElementById('whBackdrop').classList.remove('d-none');
  }

  function closeModal() {
    document.getElementById('whModal').classList.add('d-none');
    document.getElementById('whBackdrop').classList.add('d-none');
  }

  async function save() {
    const id = document.getElementById('whId').value;
    const dto = {
      CompanyId:     document.getElementById('whCompany').value,
      BranchId:      document.getElementById('whBranch').value || null,
      Code:          document.getElementById('whCode').value.trim(),
      Name:          document.getElementById('whName').value.trim(),
      Address:       document.getElementById('whAddr').value.trim() || null,
      ContactPerson: document.getElementById('whContact').value.trim() || null,
      ContactPhone:  document.getElementById('whPhone').value.trim() || null,
      IsDefault:     document.getElementById('whIsDefault').checked,
    };
    if (!dto.CompanyId) { document.getElementById('whError').textContent = 'Company is required'; return; }
    if (!dto.Code)      { document.getElementById('whError').textContent = 'Code is required'; return; }
    if (!dto.Name)      { document.getElementById('whError').textContent = 'Name is required'; return; }

    const method = id ? 'PUT' : 'POST';
    const url    = id ? `/Handlers/Warehouses.ashx?entity=warehouse&id=${id}` : '/Handlers/Warehouses.ashx?entity=warehouse';
    const res = await App.api(url, { method, body: JSON.stringify(dto) });
    if (res.success) { App.toast(res.message, 'success'); closeModal(); load(); }
    else document.getElementById('whError').textContent = res.message;
  }

  async function deleteWarehouse(id, name) {
    if (!confirm(`Delete warehouse "${name}"?`)) return;
    const res = await App.api(`/Handlers/Warehouses.ashx?entity=warehouse&id=${id}`, { method:'DELETE' });
    if (res.success) { App.toast('Warehouse deleted', 'success'); load(); }
    else App.toast(res.message, 'danger');
  }

  function openLocModal(warehouseId) {
    document.getElementById('locWarehouseId').value = warehouseId;
    document.getElementById('locCode').value = '';
    document.getElementById('locName').value = '';
    document.getElementById('locType').selectedIndex = 0;
    document.getElementById('locError').textContent = '';
    document.getElementById('locModal').classList.remove('d-none');
    document.getElementById('locBackdrop').classList.remove('d-none');
  }

  function closeLocModal() {
    document.getElementById('locModal').classList.add('d-none');
    document.getElementById('locBackdrop').classList.add('d-none');
  }

  async function saveLoc() {
    const dto = {
      WarehouseId:  document.getElementById('locWarehouseId').value,
      Code:         document.getElementById('locCode').value.trim(),
      Name:         document.getElementById('locName').value.trim(),
      LocationType: document.getElementById('locType').value,
    };
    if (!dto.Code || !dto.Name) { document.getElementById('locError').textContent = 'Code and Name required'; return; }
    const res = await App.api('/Handlers/Warehouses.ashx?entity=location', { method:'POST', body:JSON.stringify(dto) });
    if (res.success) { App.toast('Location added', 'success'); closeLocModal(); load(); }
    else document.getElementById('locError').textContent = res.message;
  }

  document.addEventListener('DOMContentLoaded', () => { loadDropdowns(); load(); });
  return { load, openModal, closeModal, save, deleteWarehouse, openLocModal, closeLocModal, saveLoc };
})();
</script>
</asp:Content>
