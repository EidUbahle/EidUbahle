<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="StockMovements.aspx.cs" Inherits="Pages_Inventory_StockMovements" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Stock Movements – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <a class="breadcrumb-item" href="/Pages/Inventory/Products.aspx">Inventory</a>
    <span class="breadcrumb-item active">Stock Movements</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Stock Movements</h1>
      <p class="page-subtitle">Opening stock, adjustments, and warehouse transfers</p>
    </div>
    <div class="page-actions d-flex gap-2 flex-wrap">
      <button class="btn btn-ghost btn-sm" onclick="MovPage.load()"><i class="bi bi-arrow-clockwise"></i></button>
      <div class="dropdown">
        <button class="btn btn-primary btn-sm dropdown-toggle" data-bs-toggle="dropdown">
          <i class="bi bi-plus-lg"></i> New Movement
        </button>
        <ul class="dropdown-menu">
          <li><button class="dropdown-item" onclick="MovPage.openModal('Opening')"><i class="bi bi-box-arrow-in-down me-2"></i>Opening Stock</button></li>
          <li><button class="dropdown-item" onclick="MovPage.openModal('Adjustment')"><i class="bi bi-sliders me-2"></i>Adjustment</button></li>
          <li><hr class="dropdown-divider"></li>
          <li><button class="dropdown-item" onclick="MovPage.openModal('TransferOut')"><i class="bi bi-arrow-left-right me-2"></i>Transfer</button></li>
          <li><button class="dropdown-item" onclick="MovPage.openModal('Receipt')"><i class="bi bi-download me-2"></i>Receipt</button></li>
          <li><button class="dropdown-item" onclick="MovPage.openModal('Issue')"><i class="bi bi-upload me-2"></i>Issue</button></li>
        </ul>
      </div>
    </div>
  </div>

  <div class="page-body">

    <!-- Filters -->
    <div class="card mb-3">
      <div class="card-body" style="padding:.75rem 1rem;">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <select class="form-select" id="filterType" onchange="MovPage.load()" style="width:160px;">
            <option value="">All Types</option>
            <option value="Opening">Opening</option>
            <option value="Adjustment">Adjustment</option>
            <option value="TransferOut">Transfer Out</option>
            <option value="TransferIn">Transfer In</option>
            <option value="Receipt">Receipt</option>
            <option value="Issue">Issue</option>
          </select>
          <select class="form-select" id="filterStatus" onchange="MovPage.load()" style="width:130px;">
            <option value="">All Status</option>
            <option value="Draft">Draft</option>
            <option value="Posted">Posted</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          <select class="form-select" id="filterCompany" onchange="MovPage.load()" style="width:180px;">
            <option value="">All Companies</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Movements table -->
    <div class="card">
      <div class="card-body p-0">
        <div id="movLoading" class="p-5 text-center">
          <div class="skeleton" style="height:40px;margin-bottom:8px;"></div>
          <div class="skeleton" style="height:40px;"></div>
        </div>
        <div id="movEmpty" class="p-5 text-center d-none">
          <i class="bi bi-arrow-left-right" style="font-size:3rem;color:var(--text-muted);"></i>
          <p class="mt-3 text-muted">No stock movements found.</p>
        </div>
        <div class="table-responsive" id="movTable" style="display:none;">
          <table class="table table-hover mb-0">
            <thead><tr>
              <th>Document #</th><th>Type</th><th>Date</th><th>Company</th>
              <th>From</th><th>To</th><th class="text-end">Lines</th>
              <th class="text-end">Value</th><th>Status</th><th></th>
            </tr></thead>
            <tbody id="movBody"></tbody>
          </table>
        </div>
      </div>
    </div>

  </div>

  <!-- ── Movement Create Modal ───────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="movBackdrop" onclick="MovPage.closeModal()"></div>
  <div class="modal-panel d-none" id="movModal" role="dialog" style="max-width:800px;">
    <div class="modal-header">
      <h2 class="modal-title" id="movModalTitle">New Stock Movement</h2>
      <button class="btn btn-ghost btn-icon" onclick="MovPage.closeModal()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body" style="max-height:75vh;overflow-y:auto;">
      <div class="row g-3 mb-3">
        <div class="col-md-4">
          <label class="form-label required">Company</label>
          <select id="movCompany" class="form-select"><option value="">— Select —</option></select>
        </div>
        <div class="col-md-4">
          <label class="form-label required">Date</label>
          <input type="date" id="movDate" class="form-input" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Reference</label>
          <input type="text" id="movRef" class="form-input" placeholder="Optional ref" />
        </div>
        <div class="col-md-6" id="fromWhRow">
          <label class="form-label" id="fromWhLabel">Warehouse</label>
          <select id="movFromWH" class="form-select"><option value="">— Select —</option></select>
        </div>
        <div class="col-md-6" id="toWhRow" style="display:none;">
          <label class="form-label">Destination Warehouse</label>
          <select id="movToWH" class="form-select"><option value="">— Select —</option></select>
        </div>
        <div class="col-12">
          <label class="form-label">Notes</label>
          <textarea id="movNotes" class="form-input" rows="2"></textarea>
        </div>
      </div>

      <!-- Lines -->
      <div class="d-flex align-items-center justify-content-between mb-2">
        <h3 class="fw-600 mb-0" style="font-size:1rem;">Line Items</h3>
        <button class="btn btn-ghost btn-sm" onclick="MovPage.addLine()">
          <i class="bi bi-plus-lg"></i> Add Line
        </button>
      </div>
      <div class="table-responsive">
        <table class="table table-sm mb-0">
          <thead><tr>
            <th style="width:30%">Product</th>
            <th style="width:12%">Qty</th>
            <th style="width:14%">UOM</th>
            <th style="width:15%">Unit Cost</th>
            <th style="width:20%">Reason</th>
            <th style="width:9%"></th>
          </tr></thead>
          <tbody id="movLines"></tbody>
        </table>
      </div>
    </div>
    <div class="modal-footer">
      <div id="movError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="MovPage.closeModal()">Cancel</button>
      <button class="btn btn-secondary" onclick="MovPage.saveDraft()">Save Draft</button>
      <button class="btn btn-primary" onclick="MovPage.saveAndPost()">Save &amp; Post</button>
    </div>
  </div>

  <!-- ── Movement Detail Modal ───────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="movDetailBackdrop" onclick="MovPage.closeDetail()"></div>
  <div class="modal-panel d-none" id="movDetailModal" role="dialog" style="max-width:750px;">
    <div class="modal-header">
      <h2 class="modal-title" id="movDetailTitle">Movement Detail</h2>
      <button class="btn btn-ghost btn-icon" onclick="MovPage.closeDetail()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body" style="max-height:70vh;overflow-y:auto;" id="movDetailBody">Loading…</div>
    <div class="modal-footer" id="movDetailFooter"></div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server"></asp:Content>
<asp:Content ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
const MovPage = (() => {
  let _type = 'Adjustment', _products = [], _uoms = [], _warehouses = [];
  const fmt = n => (n||0).toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2});
  const esc = s => s ? String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;') : '';

  async function load() {
    document.getElementById('movLoading').classList.remove('d-none');
    document.getElementById('movTable').style.display = 'none';
    document.getElementById('movEmpty').classList.add('d-none');

    const type   = document.getElementById('filterType').value;
    const status = document.getElementById('filterStatus').value;
    const cId    = document.getElementById('filterCompany').value;
    const params = new URLSearchParams({ entity:'movement' });
    if (type)   params.append('type', type);
    if (status) params.append('status', status);
    if (cId)    params.append('companyId', cId);

    const res = await App.api('/Handlers/StockMovements.ashx?' + params);
    document.getElementById('movLoading').classList.add('d-none');
    if (!res.success) { App.toast(res.message, 'danger'); return; }
    const items = res.data?.items || [];
    if (!items.length) { document.getElementById('movEmpty').classList.remove('d-none'); return; }

    document.getElementById('movTable').style.display = '';
    const typeColors = { Opening:'primary', Adjustment:'warning', TransferOut:'info', TransferIn:'info', Receipt:'success', Issue:'danger' };
    const statusColors = { Draft:'secondary', Posted:'success', Cancelled:'danger' };
    document.getElementById('movBody').innerHTML = items.map(m => `
      <tr>
        <td><button class="btn btn-link p-0 fw-600" onclick="MovPage.showDetail('${m.Id}')">${esc(m.MovementNumber)}</button></td>
        <td><span class="badge bg-${typeColors[m.MovementType]||'secondary'}">${esc(m.MovementType)}</span></td>
        <td>${new Date(m.MovementDate).toLocaleDateString()}</td>
        <td>${esc(m.CompanyName)}</td>
        <td>${esc(m.FromWarehouseName||'—')}</td>
        <td>${esc(m.ToWarehouseName||'—')}</td>
        <td class="text-end">${m.TotalLines}</td>
        <td class="text-end">${fmt(m.TotalValue)}</td>
        <td><span class="badge bg-${statusColors[m.Status]||'secondary'}">${esc(m.Status)}</span></td>
        <td class="text-end">
          ${m.Status==='Draft' ? `
            <button class="btn btn-ghost btn-icon btn-sm text-success" onclick="MovPage.postMovement('${m.Id}')" title="Post"><i class="bi bi-check-circle"></i></button>
            <button class="btn btn-ghost btn-icon btn-sm text-danger" onclick="MovPage.deleteMovement('${m.Id}')" title="Delete"><i class="bi bi-trash"></i></button>
          ` : ''}
        </td>
      </tr>`).join('');
  }

  async function loadDropdowns() {
    const [compRes, whRes, prodRes, uomRes] = await Promise.all([
      App.api('/Handlers/Tenant.ashx?action=companies'),
      App.api('/Handlers/Warehouses.ashx?entity=warehouse'),
      App.api('/Handlers/Products.ashx?entity=product&pageSize=200'),
      App.api('/Handlers/Products.ashx?entity=uom'),
    ]);

    if (compRes.success) {
      [document.getElementById('filterCompany'), document.getElementById('movCompany')].forEach(sel => {
        while (sel.options.length > 1) sel.remove(1);
        (compRes.data || []).forEach(c => sel.add(new Option(c.Name, c.Id)));
      });
    }

    _warehouses = whRes.success ? (whRes.data || []) : [];
    [document.getElementById('movFromWH'), document.getElementById('movToWH')].forEach(sel => {
      while (sel.options.length > 1) sel.remove(1);
      _warehouses.forEach(w => sel.add(new Option(w.Name, w.Id)));
    });

    _products = prodRes.success ? (prodRes.data?.items || []) : [];
    _uoms     = uomRes.success  ? (uomRes.data || []) : [];
  }

  function openModal(type) {
    _type = type;
    document.getElementById('movModalTitle').textContent = {
      Opening:'Opening Stock', Adjustment:'Stock Adjustment', TransferOut:'Warehouse Transfer',
      Receipt:'Goods Receipt', Issue:'Stock Issue'
    }[type] || 'Stock Movement';
    document.getElementById('movDate').value = new Date().toISOString().slice(0,10);
    document.getElementById('movRef').value   = '';
    document.getElementById('movNotes').value = '';
    document.getElementById('movLines').innerHTML = '';
    document.getElementById('movError').textContent = '';
    document.getElementById('movCompany').selectedIndex = 0;
    document.getElementById('movFromWH').selectedIndex  = 0;
    document.getElementById('movToWH').selectedIndex    = 0;
    document.getElementById('fromWhLabel').textContent = type === 'TransferOut' ? 'From Warehouse' : 'Warehouse';
    document.getElementById('toWhRow').style.display = type === 'TransferOut' ? '' : 'none';
    addLine();
    document.getElementById('movModal').classList.remove('d-none');
    document.getElementById('movBackdrop').classList.remove('d-none');
  }

  function closeModal() {
    document.getElementById('movModal').classList.add('d-none');
    document.getElementById('movBackdrop').classList.add('d-none');
  }

  let _lineIdx = 0;
  function addLine() {
    const i = _lineIdx++;
    const prodOpts = _products.map(p => `<option value="${p.Id}">${esc(p.SKU)} - ${esc(p.Name)}</option>`).join('');
    const uomOpts  = _uoms.map(u => `<option value="${u.Id}">${esc(u.Symbol)}</option>`).join('');
    const row = document.createElement('tr');
    row.id = `line-${i}`;
    row.innerHTML = `
      <td><select class="form-select form-select-sm"><option value="">— Select Product —</option>${prodOpts}</select></td>
      <td><input type="number" class="form-input form-input-sm" value="1" min="0.001" step="0.001" /></td>
      <td><select class="form-select form-select-sm"><option value="">—</option>${uomOpts}</select></td>
      <td><input type="number" class="form-input form-input-sm" value="0" min="0" step="0.01" placeholder="Cost" /></td>
      <td><input type="text" class="form-input form-input-sm" placeholder="Reason" /></td>
      <td><button class="btn btn-ghost btn-icon btn-sm text-danger" onclick="document.getElementById('line-${i}').remove()"><i class="bi bi-x"></i></button></td>
    `;
    document.getElementById('movLines').appendChild(row);
  }

  function collectLines() {
    const lines = [];
    document.querySelectorAll('#movLines tr').forEach(row => {
      const sels = row.querySelectorAll('select');
      const inps = row.querySelectorAll('input');
      const productId = sels[0]?.value;
      if (!productId) return;
      lines.push({
        ProductId: productId,
        Qty:       parseFloat(inps[0]?.value) || 1,
        UomId:     sels[1]?.value || null,
        UnitCost:  parseFloat(inps[1]?.value) || 0,
        Reason:    inps[2]?.value.trim() || null,
      });
    });
    return lines;
  }

  async function saveMovement(autoPost) {
    const lines = collectLines();
    if (!document.getElementById('movCompany').value) { document.getElementById('movError').textContent = 'Company required'; return null; }
    if (!document.getElementById('movFromWH').value)  { document.getElementById('movError').textContent = 'Warehouse required'; return null; }
    if (!lines.length) { document.getElementById('movError').textContent = 'Add at least one line'; return null; }
    const dto = {
      CompanyId:     document.getElementById('movCompany').value,
      MovementType:  _type,
      MovementDate:  document.getElementById('movDate').value,
      Reference:     document.getElementById('movRef').value.trim() || null,
      Notes:         document.getElementById('movNotes').value.trim() || null,
      FromWarehouseId: document.getElementById('movFromWH').value || null,
      ToWarehouseId:   _type === 'TransferOut' ? (document.getElementById('movToWH').value || null) : null,
      Lines: lines,
    };
    const res = await App.api('/Handlers/StockMovements.ashx?entity=movement', { method:'POST', body:JSON.stringify(dto) });
    if (!res.success) { document.getElementById('movError').textContent = res.message; return null; }
    if (autoPost) {
      const postRes = await App.api(`/Handlers/StockMovements.ashx?entity=movement&action=post&id=${res.data}`, { method:'POST' });
      if (!postRes.success) { App.toast('Saved as Draft (post failed: ' + postRes.message + ')', 'warning'); }
      else App.toast('Movement posted successfully', 'success');
    } else {
      App.toast('Saved as draft', 'success');
    }
    closeModal();
    load();
    return res.data;
  }

  function saveDraft()    { saveMovement(false); }
  function saveAndPost()  { saveMovement(true); }

  async function postMovement(id) {
    if (!confirm('Post this movement? Stock levels will be updated.')) return;
    const res = await App.api(`/Handlers/StockMovements.ashx?entity=movement&action=post&id=${id}`, { method:'POST' });
    if (res.success) { App.toast('Movement posted', 'success'); load(); }
    else App.toast(res.message, 'danger');
  }

  async function deleteMovement(id) {
    if (!confirm('Delete this draft movement?')) return;
    const res = await App.api(`/Handlers/StockMovements.ashx?entity=movement&id=${id}`, { method:'DELETE' });
    if (res.success) { App.toast('Deleted', 'success'); load(); }
    else App.toast(res.message, 'danger');
  }

  async function showDetail(id) {
    document.getElementById('movDetailBody').textContent = 'Loading…';
    document.getElementById('movDetailModal').classList.remove('d-none');
    document.getElementById('movDetailBackdrop').classList.remove('d-none');

    const res = await App.api(`/Handlers/StockMovements.ashx?entity=movement&id=${id}`);
    if (!res.success) { document.getElementById('movDetailBody').textContent = 'Failed to load'; return; }
    const m = res.data;
    document.getElementById('movDetailTitle').textContent = m.MovementNumber;
    document.getElementById('movDetailBody').innerHTML = `
      <div class="row g-3 mb-4">
        <div class="col-md-4"><label class="form-label text-muted">Type</label><div class="fw-600">${esc(m.MovementType)}</div></div>
        <div class="col-md-4"><label class="form-label text-muted">Date</label><div>${new Date(m.MovementDate).toLocaleDateString()}</div></div>
        <div class="col-md-4"><label class="form-label text-muted">Status</label><div><span class="badge bg-${m.Status==='Posted'?'success':'secondary'}">${esc(m.Status)}</span></div></div>
        <div class="col-md-4"><label class="form-label text-muted">Company</label><div>${esc(m.CompanyName)}</div></div>
        ${m.FromWarehouseName ? `<div class="col-md-4"><label class="form-label text-muted">From</label><div>${esc(m.FromWarehouseName)}</div></div>` : ''}
        ${m.ToWarehouseName ? `<div class="col-md-4"><label class="form-label text-muted">To</label><div>${esc(m.ToWarehouseName)}</div></div>` : ''}
        ${m.Reference ? `<div class="col-12"><label class="form-label text-muted">Reference</label><div>${esc(m.Reference)}</div></div>` : ''}
        ${m.Notes ? `<div class="col-12"><label class="form-label text-muted">Notes</label><div>${esc(m.Notes)}</div></div>` : ''}
      </div>
      <table class="table table-sm">
        <thead><tr><th>Product</th><th>Batch/Serial</th><th class="text-end">Qty</th><th>UOM</th><th class="text-end">Unit Cost</th><th class="text-end">Total</th></tr></thead>
        <tbody>${(m.Lines||[]).map(l => `<tr>
          <td><div class="fw-600">${esc(l.ProductName)}</div><small class="text-muted">${esc(l.ProductSKU)}</small></td>
          <td>${esc(l.BatchNumber||l.SerialNumber||'—')}</td>
          <td class="text-end">${fmt(l.Qty)}</td>
          <td>${esc(l.UomSymbol||'')}</td>
          <td class="text-end">${fmt(l.UnitCost)}</td>
          <td class="text-end fw-600">${fmt(l.TotalCost)}</td>
        </tr>`).join('')}</tbody>
        <tfoot><tr><th colspan="5" class="text-end">Total Value</th><th class="text-end">${fmt(m.TotalValue)}</th></tr></tfoot>
      </table>`;

    document.getElementById('movDetailFooter').innerHTML = m.Status === 'Draft' ? `
      <button class="btn btn-ghost" onclick="MovPage.closeDetail()">Close</button>
      <button class="btn btn-success" onclick="MovPage.postMovement('${m.Id}'); MovPage.closeDetail();">
        <i class="bi bi-check-circle me-1"></i> Post Movement
      </button>` : `<button class="btn btn-ghost" onclick="MovPage.closeDetail()">Close</button>`;
  }

  function closeDetail() {
    document.getElementById('movDetailModal').classList.add('d-none');
    document.getElementById('movDetailBackdrop').classList.add('d-none');
  }

  document.addEventListener('DOMContentLoaded', () => { loadDropdowns(); load(); });
  return { load, openModal, closeModal, saveDraft, saveAndPost, addLine,
           postMovement, deleteMovement, showDetail, closeDetail };
})();
</script>
</asp:Content>
