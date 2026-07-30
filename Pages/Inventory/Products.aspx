<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Products.aspx.cs" Inherits="Pages_Inventory_Products" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Products – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.section.trading">Trading</span>
    <span class="breadcrumb-item active">Products</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Products</h1>
      <p class="page-subtitle" id="productSubtitle">Loading…</p>
    </div>
    <div class="page-actions d-flex gap-2 flex-wrap">
      <a href="/Pages/Inventory/Categories.aspx" class="btn btn-ghost btn-sm">
        <i class="bi bi-tags"></i> Categories &amp; Brands
      </a>
      <button class="btn btn-ghost btn-sm" onclick="ProductsPage.load()">
        <i class="bi bi-arrow-clockwise"></i>
      </button>
      <button class="btn btn-primary btn-sm" onclick="ProductsPage.openModal(null)" id="btnAddProduct">
        <i class="bi bi-plus-lg"></i> New Product
      </button>
    </div>
  </div>

  <div class="page-body">

    <!-- Filters -->
    <div class="card mb-3">
      <div class="card-body" style="padding:.75rem 1rem;">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <input type="text" id="searchInput" class="form-input" placeholder="Search SKU, name, barcode…"
                 oninput="ProductsPage.onSearch(this.value)" style="width:240px;" />
          <select class="form-select" id="filterCategory" onchange="ProductsPage.load()" style="width:180px;">
            <option value="">All Categories</option>
          </select>
          <select class="form-select" id="filterBrand" onchange="ProductsPage.load()" style="width:150px;">
            <option value="">All Brands</option>
          </select>
          <select class="form-select" id="filterType" onchange="ProductsPage.load()" style="width:150px;">
            <option value="">All Types</option>
            <option value="Storable">Storable</option>
            <option value="Service">Service</option>
            <option value="Consumable">Consumable</option>
          </select>
          <a href="/Pages/Inventory/StockMovements.aspx" class="btn btn-ghost btn-sm ms-auto">
            <i class="bi bi-arrow-left-right"></i> Stock Movements
          </a>
          <button class="btn btn-ghost btn-sm" onclick="ProductsPage.openValuationModal()">
            <i class="bi bi-graph-up"></i> Valuation
          </button>
          <button class="btn btn-ghost btn-sm position-relative" onclick="ProductsPage.openAlertsModal()">
            <i class="bi bi-bell"></i>
            <span class="badge bg-danger badge-float" id="alertBadge" style="display:none;position:absolute;top:-4px;right:-4px;font-size:.65rem;"></span>
          </button>
        </div>
      </div>
    </div>

    <!-- Products table -->
    <div class="card">
      <div class="card-body p-0">
        <div id="loadingState" class="p-5 text-center">
          <div class="skeleton" style="height:40px;margin-bottom:8px;"></div>
          <div class="skeleton" style="height:40px;margin-bottom:8px;"></div>
          <div class="skeleton" style="height:40px;"></div>
        </div>
        <div id="emptyState" class="p-5 text-center d-none">
          <i class="bi bi-box-seam" style="font-size:3rem;color:var(--text-muted);"></i>
          <p class="mt-3 text-muted">No products found. <button class="btn btn-link p-0" onclick="ProductsPage.openModal(null)">Create your first product</button>.</p>
        </div>
        <div class="table-responsive" id="tableWrapper" style="display:none;">
          <table class="table table-hover mb-0" id="productsTable">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Name</th>
                <th>Category</th>
                <th>Brand</th>
                <th>Type</th>
                <th>Costing</th>
                <th class="text-end">Cost</th>
                <th class="text-end">Sale Price</th>
                <th class="text-end">On Hand</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody id="productsBody"></tbody>
          </table>
        </div>
        <!-- Pagination -->
        <div class="d-flex justify-content-between align-items-center p-3 border-top" id="paginationBar" style="display:none!important;">
          <span class="text-muted" id="paginationInfo" style="font-size:.85rem;"></span>
          <div class="d-flex gap-2">
            <button class="btn btn-ghost btn-sm" id="btnPrev" onclick="ProductsPage.prevPage()">
              <i class="bi bi-chevron-left"></i> Prev
            </button>
            <button class="btn btn-ghost btn-sm" id="btnNext" onclick="ProductsPage.nextPage()">
              Next <i class="bi bi-chevron-right"></i>
            </button>
          </div>
        </div>
      </div>
    </div>

  </div><!-- /page-body -->

  <!-- ── Product Modal ─────────────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="productModalBackdrop" onclick="ProductsPage.closeModal()"></div>
  <div class="modal-panel d-none" id="productModal" role="dialog" aria-labelledby="productModalTitle" aria-modal="true" style="max-width:680px;">
    <div class="modal-header">
      <h2 class="modal-title" id="productModalTitle">New Product</h2>
      <button class="btn btn-ghost btn-icon" onclick="ProductsPage.closeModal()" aria-label="Close">
        <i class="bi bi-x-lg"></i>
      </button>
    </div>
    <div class="modal-body" style="max-height:70vh;overflow-y:auto;">
      <input type="hidden" id="productId" />
      <div class="row g-3">
        <div class="col-md-4">
          <label class="form-label required">SKU</label>
          <input type="text" id="prodSKU" class="form-input" placeholder="e.g. PROD-001" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Barcode</label>
          <input type="text" id="prodBarcode" class="form-input" />
        </div>
        <div class="col-md-4">
          <label class="form-label required">Product Type</label>
          <select id="prodType" class="form-select">
            <option value="Storable">Storable</option>
            <option value="Service">Service</option>
            <option value="Consumable">Consumable</option>
          </select>
        </div>
        <div class="col-12">
          <label class="form-label required">Name</label>
          <input type="text" id="prodName" class="form-input" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Category</label>
          <select id="prodCategory" class="form-select"><option value="">— None —</option></select>
        </div>
        <div class="col-md-6">
          <label class="form-label">Brand</label>
          <select id="prodBrand" class="form-select"><option value="">— None —</option></select>
        </div>
        <div class="col-md-4">
          <label class="form-label">Costing Method</label>
          <select id="prodCosting" class="form-select">
            <option value="AVCO">AVCO (Average Cost)</option>
            <option value="FIFO">FIFO (First-In First-Out)</option>
          </select>
        </div>
        <div class="col-md-4">
          <label class="form-label">Tracking</label>
          <select id="prodTracking" class="form-select">
            <option value="None">None</option>
            <option value="Batch">Batch / Lot</option>
            <option value="Serial">Serial Number</option>
          </select>
        </div>
        <div class="col-md-4">
          <label class="form-label">Stock UOM</label>
          <select id="prodStockUom" class="form-select"><option value="">— Select —</option></select>
        </div>
        <div class="col-md-4">
          <label class="form-label">Standard Cost</label>
          <input type="number" id="prodStdCost" class="form-input" value="0" min="0" step="0.01" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Sale Price</label>
          <input type="number" id="prodSalePrice" class="form-input" value="0" min="0" step="0.01" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Tax Rate (%)</label>
          <input type="number" id="prodTax" class="form-input" value="0" min="0" max="100" step="0.01" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Reorder Point</label>
          <input type="number" id="prodReorderPt" class="form-input" value="0" min="0" step="1"
                 title="Alert when stock falls below this quantity" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Reorder Qty</label>
          <input type="number" id="prodReorderQty" class="form-input" value="0" min="0" step="1"
                 title="Suggested purchase quantity when reorder point is hit" />
        </div>
        <div class="col-12">
          <label class="form-label">Description</label>
          <textarea id="prodDesc" class="form-input" rows="2"></textarea>
        </div>
        <div class="col-md-6">
          <label class="form-label">Purchase UOM</label>
          <select id="prodPurchUom" class="form-select"><option value="">— Same as Stock —</option></select>
        </div>
        <div class="col-md-6">
          <label class="form-label">Sales UOM</label>
          <select id="prodSaleUom" class="form-select"><option value="">— Same as Stock —</option></select>
        </div>
      </div>
    </div>
    <div class="modal-footer">
      <div id="productModalError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="ProductsPage.closeModal()">Cancel</button>
      <button class="btn btn-primary" id="btnSaveProduct" onclick="ProductsPage.save()">
        <span id="btnSaveLabel">Save Product</span>
        <span id="btnSaveSpinner" class="spinner-border spinner-border-sm d-none" role="status"></span>
      </button>
    </div>
  </div>

  <!-- ── Valuation Modal ───────────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="valuationBackdrop" onclick="ProductsPage.closeValuation()"></div>
  <div class="modal-panel d-none" id="valuationModal" role="dialog" style="max-width:800px;">
    <div class="modal-header">
      <h2 class="modal-title">Stock Valuation</h2>
      <button class="btn btn-ghost btn-icon" onclick="ProductsPage.closeValuation()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body" style="max-height:70vh;overflow-y:auto;">
      <div class="d-flex gap-3 mb-3">
        <select id="valuationWarehouse" class="form-select" onchange="ProductsPage.loadValuation()" style="max-width:200px;">
          <option value="">All Warehouses</option>
        </select>
        <div class="ms-auto">
          <span class="fw-700" id="valuationTotal">Total: —</span>
        </div>
      </div>
      <div class="table-responsive">
        <table class="table table-sm mb-0">
          <thead><tr><th>SKU</th><th>Product</th><th>Warehouse</th><th>Method</th>
            <th class="text-end">Qty</th><th class="text-end">Unit Cost</th><th class="text-end">Total Value</th></tr></thead>
          <tbody id="valuationBody"></tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- ── Low Stock Alerts Modal ────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="alertsBackdrop" onclick="ProductsPage.closeAlerts()"></div>
  <div class="modal-panel d-none" id="alertsModal" role="dialog" style="max-width:750px;">
    <div class="modal-header">
      <h2 class="modal-title"><i class="bi bi-bell text-warning me-2"></i>Low Stock Alerts</h2>
      <button class="btn btn-ghost btn-icon" onclick="ProductsPage.closeAlerts()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body" style="max-height:65vh;overflow-y:auto;">
      <div class="table-responsive">
        <table class="table table-sm mb-0">
          <thead><tr><th>Product</th><th>Warehouse</th><th>Type</th>
            <th class="text-end">On Hand</th><th class="text-end">Available</th><th class="text-end">Reorder Pt</th>
            <th>Status</th><th></th></tr></thead>
          <tbody id="alertsBody"></tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- ── Stock Detail Modal ────────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="stockDetailBackdrop" onclick="ProductsPage.closeStockDetail()"></div>
  <div class="modal-panel d-none" id="stockDetailModal" role="dialog" style="max-width:600px;">
    <div class="modal-header">
      <h2 class="modal-title" id="stockDetailTitle">Stock Levels</h2>
      <button class="btn btn-ghost btn-icon" onclick="ProductsPage.closeStockDetail()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body" style="max-height:65vh;overflow-y:auto;">
      <div class="table-responsive">
        <table class="table table-sm mb-0">
          <thead><tr><th>Warehouse</th><th>Location</th>
            <th class="text-end">On Hand</th><th class="text-end">Reserved</th><th class="text-end">Available</th>
            <th class="text-end">Avg Cost</th><th class="text-end">Value</th></tr></thead>
          <tbody id="stockDetailBody"></tbody>
        </table>
      </div>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
const ProductsPage = (() => {
  let _state = { page: 1, pageSize: 50, total: 0, searchTimer: null };
  const fmt = (n) => (n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

  async function load() {
    const search   = document.getElementById('searchInput').value;
    const catId    = document.getElementById('filterCategory').value;
    const brandId  = document.getElementById('filterBrand').value;
    const type     = document.getElementById('filterType').value;
    const params   = new URLSearchParams({ entity:'product', page: _state.page, pageSize: _state.pageSize });
    if (search) params.append('search', search);
    if (catId) params.append('categoryId', catId);
    if (brandId) params.append('brandId', brandId);
    if (type) params.append('type', type);

    document.getElementById('loadingState').classList.remove('d-none');
    document.getElementById('tableWrapper').style.display = 'none';
    document.getElementById('emptyState').classList.add('d-none');

    try {
      const res = await App.api('/Handlers/Products.ashx?' + params);
      if (!res.success) { App.toast(res.message, 'danger'); return; }
      const { items, total } = res.data;
      _state.total = total;
      renderTable(items);
      document.getElementById('productSubtitle').textContent = `${total.toLocaleString()} products`;
      renderPagination(total);
    } catch (e) { App.toast('Failed to load products', 'danger'); }
  }

  function renderTable(items) {
    document.getElementById('loadingState').classList.add('d-none');
    if (!items || !items.length) {
      document.getElementById('emptyState').classList.remove('d-none');
      document.getElementById('tableWrapper').style.display = 'none';
      document.getElementById('paginationBar').style.display = 'none';
      return;
    }
    document.getElementById('tableWrapper').style.display = '';
    document.getElementById('paginationBar').style.display = '';
    const body = document.getElementById('productsBody');
    body.innerHTML = items.map(p => `
      <tr>
        <td><code style="font-size:.8rem;">${esc(p.SKU)}</code></td>
        <td>
          <div class="fw-600">${esc(p.Name)}</div>
          ${p.Barcode ? `<small class="text-muted">${esc(p.Barcode)}</small>` : ''}
        </td>
        <td>${esc(p.CategoryName || '—')}</td>
        <td>${esc(p.BrandName || '—')}</td>
        <td><span class="badge bg-${typeColor(p.ProductType)}">${esc(p.ProductType)}</span></td>
        <td><span class="badge bg-secondary">${esc(p.CostingMethod)}</span></td>
        <td class="text-end">${fmt(p.StandardCost)}</td>
        <td class="text-end">${fmt(p.SalePrice)}</td>
        <td class="text-end">
          <button class="btn btn-link p-0 fw-600" onclick="ProductsPage.showStockDetail('${p.Id}','${esc(p.Name)}')" style="font-size:.9rem;">
            ${fmt(p.TotalQtyOnHand)} ${esc(p.StockUomSymbol || '')}
          </button>
        </td>
        <td><span class="badge bg-${p.IsActive ? 'success' : 'secondary'}">${p.IsActive ? 'Active' : 'Inactive'}</span></td>
        <td class="text-end">
          <div class="d-flex gap-1 justify-content-end">
            <button class="btn btn-ghost btn-icon btn-sm" onclick="ProductsPage.openModal('${p.Id}')" title="Edit"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-ghost btn-icon btn-sm text-danger" onclick="ProductsPage.deleteProduct('${p.Id}','${esc(p.Name)}')" title="Delete"><i class="bi bi-trash"></i></button>
          </div>
        </td>
      </tr>`).join('');
  }

  function typeColor(t) { return t==='Service'?'info':t==='Consumable'?'warning':'primary'; }

  function renderPagination(total) {
    const totalPages = Math.ceil(total / _state.pageSize);
    const bar = document.getElementById('paginationBar');
    bar.style.display = total > _state.pageSize ? '' : 'none';
    const start = (_state.page - 1) * _state.pageSize + 1;
    const end   = Math.min(_state.page * _state.pageSize, total);
    document.getElementById('paginationInfo').textContent = `${start}–${end} of ${total}`;
    document.getElementById('btnPrev').disabled = _state.page <= 1;
    document.getElementById('btnNext').disabled = _state.page >= totalPages;
  }

  function prevPage() { if (_state.page > 1) { _state.page--; load(); } }
  function nextPage() { _state.page++; load(); }

  function onSearch(v) {
    clearTimeout(_state.searchTimer);
    _state.searchTimer = setTimeout(() => { _state.page = 1; load(); }, 350);
  }

  async function loadDropdowns() {
    const [catRes, brandRes, uomRes] = await Promise.all([
      App.api('/Handlers/Products.ashx?entity=category&flat=1'),
      App.api('/Handlers/Products.ashx?entity=brand'),
      App.api('/Handlers/Products.ashx?entity=uom'),
    ]);

    // Filter selects
    const catSel = document.getElementById('filterCategory');
    if (catRes.success) catRes.data.forEach(c => catSel.add(new Option(c.Name, c.Id)));

    const brandSel = document.getElementById('filterBrand');
    if (brandRes.success) brandRes.data.forEach(b => brandSel.add(new Option(b.Name, b.Id)));

    // Modal selects
    const modalCat = document.getElementById('prodCategory');
    if (catRes.success) catRes.data.forEach(c => modalCat.add(new Option('  '.repeat(c.Level-1) + c.Name, c.Id)));

    const modalBrand = document.getElementById('prodBrand');
    if (brandRes.success) brandRes.data.forEach(b => modalBrand.add(new Option(b.Name, b.Id)));

    const uomSels = ['prodStockUom','prodPurchUom','prodSaleUom'];
    uomSels.forEach(id => {
      const sel = document.getElementById(id);
      if (uomRes.success) uomRes.data.forEach(u => sel.add(new Option(`${u.Name} (${u.Symbol})`, u.Id)));
    });

    // Valuation warehouse dropdown
    const whRes = await App.api('/Handlers/Warehouses.ashx?entity=warehouse');
    const wSel = document.getElementById('valuationWarehouse');
    if (whRes.success) whRes.data.forEach(w => wSel.add(new Option(w.Name, w.Id)));
  }

  async function openModal(id) {
    document.getElementById('productId').value = id || '';
    document.getElementById('productModalTitle').textContent = id ? 'Edit Product' : 'New Product';
    document.getElementById('productModalError').textContent = '';
    document.getElementById('btnSaveLabel').textContent = id ? 'Save Changes' : 'Save Product';

    if (id) {
      const res = await App.api('/Handlers/Products.ashx?entity=product&id=' + id);
      if (!res.success) { App.toast(res.message, 'danger'); return; }
      const p = res.data;
      document.getElementById('prodSKU').value        = p.SKU || '';
      document.getElementById('prodBarcode').value    = p.Barcode || '';
      document.getElementById('prodName').value       = p.Name || '';
      document.getElementById('prodType').value       = p.ProductType || 'Storable';
      document.getElementById('prodCosting').value    = p.CostingMethod || 'AVCO';
      document.getElementById('prodTracking').value   = p.TrackingType || 'None';
      document.getElementById('prodCategory').value   = p.CategoryId || '';
      document.getElementById('prodBrand').value      = p.BrandId || '';
      document.getElementById('prodStdCost').value    = p.StandardCost || 0;
      document.getElementById('prodSalePrice').value  = p.SalePrice || 0;
      document.getElementById('prodTax').value        = p.TaxRate || 0;
      document.getElementById('prodReorderPt').value  = p.ReorderPoint || 0;
      document.getElementById('prodReorderQty').value = p.ReorderQty || 0;
      document.getElementById('prodDesc').value       = p.Description || '';
      document.getElementById('prodStockUom').value   = p.StockUomId || '';
      document.getElementById('prodPurchUom').value   = p.PurchaseUomId || '';
      document.getElementById('prodSaleUom').value    = p.SalesUomId || '';
    } else {
      ['prodSKU','prodBarcode','prodName','prodDesc'].forEach(id => document.getElementById(id).value = '');
      ['prodType','prodCosting','prodTracking','prodCategory','prodBrand','prodStockUom','prodPurchUom','prodSaleUom'].forEach(id => {
        const el = document.getElementById(id); if (el) el.selectedIndex = 0;
      });
      ['prodStdCost','prodSalePrice','prodTax','prodReorderPt','prodReorderQty'].forEach(id => document.getElementById(id).value = 0);
    }

    document.getElementById('productModal').classList.remove('d-none');
    document.getElementById('productModalBackdrop').classList.remove('d-none');
  }

  function closeModal() {
    document.getElementById('productModal').classList.add('d-none');
    document.getElementById('productModalBackdrop').classList.add('d-none');
  }

  async function save() {
    const id  = document.getElementById('productId').value;
    const dto = {
      SKU:           document.getElementById('prodSKU').value.trim(),
      Barcode:       document.getElementById('prodBarcode').value.trim() || null,
      Name:          document.getElementById('prodName').value.trim(),
      ProductType:   document.getElementById('prodType').value,
      CostingMethod: document.getElementById('prodCosting').value,
      TrackingType:  document.getElementById('prodTracking').value,
      CategoryId:    document.getElementById('prodCategory').value || null,
      BrandId:       document.getElementById('prodBrand').value || null,
      StockUomId:    document.getElementById('prodStockUom').value || null,
      PurchaseUomId: document.getElementById('prodPurchUom').value || null,
      SalesUomId:    document.getElementById('prodSaleUom').value || null,
      StandardCost:  parseFloat(document.getElementById('prodStdCost').value) || 0,
      SalePrice:     parseFloat(document.getElementById('prodSalePrice').value) || 0,
      TaxRate:       parseFloat(document.getElementById('prodTax').value) || 0,
      ReorderPoint:  parseFloat(document.getElementById('prodReorderPt').value) || 0,
      ReorderQty:    parseFloat(document.getElementById('prodReorderQty').value) || 0,
      Description:   document.getElementById('prodDesc').value.trim() || null,
      IsSaleable: true, IsPurchaseable: true, IsActive: true,
    };

    if (!dto.SKU || !dto.Name) {
      document.getElementById('productModalError').textContent = 'SKU and Name are required.';
      return;
    }

    const btn = document.getElementById('btnSaveProduct');
    btn.disabled = true;
    document.getElementById('btnSaveSpinner').classList.remove('d-none');
    document.getElementById('productModalError').textContent = '';

    try {
      const method = id ? 'PUT' : 'POST';
      const url    = id ? `/Handlers/Products.ashx?entity=product&id=${id}` : '/Handlers/Products.ashx?entity=product';
      const res    = await App.api(url, { method, body: JSON.stringify(dto) });
      if (res.success) {
        App.toast(res.message || 'Saved', 'success');
        closeModal();
        load();
      } else {
        document.getElementById('productModalError').textContent = res.message || 'Save failed';
      }
    } catch (e) { document.getElementById('productModalError').textContent = 'An error occurred'; }
    finally { btn.disabled = false; document.getElementById('btnSaveSpinner').classList.add('d-none'); }
  }

  async function deleteProduct(id, name) {
    if (!confirm(`Delete product "${name}"? This cannot be undone.`)) return;
    const res = await App.api(`/Handlers/Products.ashx?entity=product&id=${id}`, { method:'DELETE' });
    if (res.success) { App.toast('Product deleted', 'success'); load(); }
    else App.toast(res.message, 'danger');
  }

  async function showStockDetail(productId, name) {
    document.getElementById('stockDetailTitle').textContent = `Stock – ${name}`;
    document.getElementById('stockDetailBody').innerHTML = '<tr><td colspan="7" class="text-center py-3">Loading…</td></tr>';
    document.getElementById('stockDetailModal').classList.remove('d-none');
    document.getElementById('stockDetailBackdrop').classList.remove('d-none');

    const res = await App.api(`/Handlers/Warehouses.ashx?entity=stock&productId=${productId}`);
    if (!res.success) { document.getElementById('stockDetailBody').innerHTML = '<tr><td colspan="7" class="text-danger">Failed to load</td></tr>'; return; }
    const rows = res.data;
    document.getElementById('stockDetailBody').innerHTML = rows.length
      ? rows.map(r => `<tr>
          <td>${esc(r.WarehouseName)}</td>
          <td>${esc(r.LocationName || '—')}</td>
          <td class="text-end">${fmt(r.QtyOnHand)}</td>
          <td class="text-end">${fmt(r.QtyReserved)}</td>
          <td class="text-end fw-600">${fmt(r.QtyAvailable)}</td>
          <td class="text-end">${fmt(r.AvgCost)}</td>
          <td class="text-end">${fmt(r.TotalValue)}</td>
        </tr>`).join('')
      : '<tr><td colspan="7" class="text-center py-3 text-muted">No stock on hand</td></tr>';
  }

  function closeStockDetail() {
    document.getElementById('stockDetailModal').classList.add('d-none');
    document.getElementById('stockDetailBackdrop').classList.add('d-none');
  }

  async function openValuationModal() {
    document.getElementById('valuationModal').classList.remove('d-none');
    document.getElementById('valuationBackdrop').classList.remove('d-none');
    await loadValuation();
  }

  async function loadValuation() {
    const wId = document.getElementById('valuationWarehouse').value;
    const url = `/Handlers/Inventory.ashx?entity=valuation${wId ? '&warehouseId='+wId : ''}`;
    const res = await App.api(url);
    if (!res.success) { App.toast(res.message, 'danger'); return; }
    const { Lines, TotalValue } = res.data;
    document.getElementById('valuationTotal').textContent = `Total: ${fmt(TotalValue)}`;
    document.getElementById('valuationBody').innerHTML = Lines.length
      ? Lines.map(l => `<tr>
          <td><code>${esc(l.SKU)}</code></td>
          <td>${esc(l.ProductName)}</td>
          <td>${esc(l.WarehouseName)}</td>
          <td><span class="badge bg-secondary">${esc(l.Method)}</span></td>
          <td class="text-end">${fmt(l.QtyOnHand)}</td>
          <td class="text-end">${fmt(l.UnitCost)}</td>
          <td class="text-end fw-600">${fmt(l.TotalValue)}</td>
        </tr>`).join('')
      : '<tr><td colspan="7" class="text-center py-3 text-muted">No stock to value</td></tr>';
  }

  function closeValuation() {
    document.getElementById('valuationModal').classList.add('d-none');
    document.getElementById('valuationBackdrop').classList.add('d-none');
  }

  async function openAlertsModal() {
    document.getElementById('alertsModal').classList.remove('d-none');
    document.getElementById('alertsBackdrop').classList.remove('d-none');
    await loadAlerts();
  }

  async function loadAlerts() {
    const res = await App.api('/Handlers/Inventory.ashx?entity=alert&status=Active');
    if (!res.success) return;
    const alerts = res.data;
    // Update badge
    const badge = document.getElementById('alertBadge');
    if (alerts.length > 0) { badge.style.display = ''; badge.textContent = alerts.length; }
    else badge.style.display = 'none';

    document.getElementById('alertsBody').innerHTML = alerts.length
      ? alerts.map(a => `<tr>
          <td>
            <div class="fw-600">${esc(a.ProductName)}</div>
            <small class="text-muted">${esc(a.ProductSKU)}</small>
          </td>
          <td>${esc(a.WarehouseName)}</td>
          <td><span class="badge bg-${a.AlertType==='OutOfStock'?'danger':'warning'}">${esc(a.AlertType)}</span></td>
          <td class="text-end">${fmt(a.QtyOnHand)}</td>
          <td class="text-end">${fmt(a.QtyAvailable)}</td>
          <td class="text-end">${fmt(a.ReorderPoint)}</td>
          <td><span class="badge bg-secondary">${esc(a.Status)}</span></td>
          <td>
            <button class="btn btn-ghost btn-sm" onclick="ProductsPage.acknowledgeAlert('${a.Id}')">
              <i class="bi bi-check-lg"></i>
            </button>
          </td>
        </tr>`).join('')
      : '<tr><td colspan="8" class="text-center py-3 text-muted"><i class="bi bi-check-circle me-2"></i>No active alerts</td></tr>';
  }

  async function acknowledgeAlert(id) {
    const res = await App.api(`/Handlers/Inventory.ashx?entity=alert&action=acknowledge&id=${id}`, { method:'POST' });
    if (res.success) { App.toast('Alert acknowledged', 'success'); await loadAlerts(); }
    else App.toast(res.message, 'danger');
  }

  function closeAlerts() {
    document.getElementById('alertsModal').classList.add('d-none');
    document.getElementById('alertsBackdrop').classList.add('d-none');
  }

  function esc(s) {
    if (!s) return '';
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }

  // Init
  document.addEventListener('DOMContentLoaded', async () => {
    await loadDropdowns();
    await load();
    await loadAlerts();
  });

  return { load, onSearch, openModal, closeModal, save, deleteProduct,
           showStockDetail, closeStockDetail,
           openValuationModal, closeValuation, loadValuation,
           openAlertsModal, closeAlerts, acknowledgeAlert,
           prevPage, nextPage };
})();
</script>
</asp:Content>
