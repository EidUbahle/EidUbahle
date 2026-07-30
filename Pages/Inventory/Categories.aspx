<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Categories.aspx.cs" Inherits="Pages_Inventory_Categories" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Categories &amp; Brands – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <a class="breadcrumb-item" href="/Pages/Inventory/Products.aspx">Inventory</a>
    <span class="breadcrumb-item active">Categories &amp; Brands</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Categories &amp; Brands</h1>
      <p class="page-subtitle">Manage product classification</p>
    </div>
    <div class="page-actions d-flex gap-2">
      <a href="/Pages/Inventory/Products.aspx" class="btn btn-ghost btn-sm">
        <i class="bi bi-arrow-left"></i> Products
      </a>
    </div>
  </div>

  <div class="page-body">
    <div class="row g-4">

      <!-- Categories -->
      <div class="col-lg-7">
        <div class="card h-100">
          <div class="card-header d-flex align-items-center justify-content-between">
            <h2 class="card-title mb-0">Categories</h2>
            <button class="btn btn-primary btn-sm" onclick="CatPage.openCatModal(null)">
              <i class="bi bi-plus-lg"></i> Add
            </button>
          </div>
          <div class="card-body p-0">
            <div id="catLoading" class="p-4 text-center text-muted">Loading…</div>
            <div id="catEmpty" class="p-4 text-center text-muted d-none">No categories yet.</div>
            <div class="table-responsive" id="catTable" style="display:none;">
              <table class="table table-hover mb-0">
                <thead><tr><th>Name</th><th>Code</th><th>Level</th><th class="text-end">Products</th><th></th></tr></thead>
                <tbody id="catBody"></tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Brands -->
      <div class="col-lg-5">
        <div class="card h-100">
          <div class="card-header d-flex align-items-center justify-content-between">
            <h2 class="card-title mb-0">Brands</h2>
            <button class="btn btn-primary btn-sm" onclick="CatPage.openBrandModal(null)">
              <i class="bi bi-plus-lg"></i> Add
            </button>
          </div>
          <div class="card-body p-0">
            <div id="brandLoading" class="p-4 text-center text-muted">Loading…</div>
            <div id="brandEmpty" class="p-4 text-center text-muted d-none">No brands yet.</div>
            <div class="table-responsive" id="brandTable" style="display:none;">
              <table class="table table-hover mb-0">
                <thead><tr><th>Name</th><th>Code</th><th class="text-end">Products</th><th></th></tr></thead>
                <tbody id="brandBody"></tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

    </div><!-- /row -->
  </div>

  <!-- ── Category Modal ──────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="catBackdrop" onclick="CatPage.closeCatModal()"></div>
  <div class="modal-panel d-none" id="catModal" role="dialog" style="max-width:500px;">
    <div class="modal-header">
      <h2 class="modal-title" id="catModalTitle">New Category</h2>
      <button class="btn btn-ghost btn-icon" onclick="CatPage.closeCatModal()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body">
      <input type="hidden" id="catId" />
      <div class="mb-3">
        <label class="form-label">Parent Category</label>
        <select id="catParent" class="form-select"><option value="">— Root Level —</option></select>
      </div>
      <div class="mb-3">
        <label class="form-label required">Name</label>
        <input type="text" id="catName" class="form-input" />
      </div>
      <div class="mb-3">
        <label class="form-label">Code</label>
        <input type="text" id="catCode" class="form-input" placeholder="Optional short code" />
      </div>
      <div class="mb-3">
        <label class="form-label">Description</label>
        <textarea id="catDesc" class="form-input" rows="2"></textarea>
      </div>
      <div class="mb-3">
        <label class="form-label">Sort Order</label>
        <input type="number" id="catSort" class="form-input" value="0" min="0" />
      </div>
    </div>
    <div class="modal-footer">
      <div id="catError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="CatPage.closeCatModal()">Cancel</button>
      <button class="btn btn-primary" onclick="CatPage.saveCat()">Save</button>
    </div>
  </div>

  <!-- ── Brand Modal ─────────────────────────────────────────────── -->
  <div class="modal-backdrop d-none" id="brandBackdrop" onclick="CatPage.closeBrandModal()"></div>
  <div class="modal-panel d-none" id="brandModal" role="dialog" style="max-width:460px;">
    <div class="modal-header">
      <h2 class="modal-title" id="brandModalTitle">New Brand</h2>
      <button class="btn btn-ghost btn-icon" onclick="CatPage.closeBrandModal()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="modal-body">
      <input type="hidden" id="brandId" />
      <div class="mb-3">
        <label class="form-label required">Name</label>
        <input type="text" id="brandName" class="form-input" />
      </div>
      <div class="mb-3">
        <label class="form-label">Code</label>
        <input type="text" id="brandCode" class="form-input" />
      </div>
      <div class="mb-3">
        <label class="form-label">Description</label>
        <textarea id="brandDesc" class="form-input" rows="2"></textarea>
      </div>
    </div>
    <div class="modal-footer">
      <div id="brandError" class="text-danger me-auto" style="font-size:.85rem;"></div>
      <button class="btn btn-ghost" onclick="CatPage.closeBrandModal()">Cancel</button>
      <button class="btn btn-primary" onclick="CatPage.saveBrand()">Save</button>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server"></asp:Content>
<asp:Content ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
const CatPage = (() => {
  let _cats = [], _brands = [];

  function esc(s) {
    if (!s) return '';
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }

  async function loadCategories() {
    document.getElementById('catLoading').classList.remove('d-none');
    document.getElementById('catTable').style.display = 'none';
    document.getElementById('catEmpty').classList.add('d-none');
    const res = await App.api('/Handlers/Products.ashx?entity=category&flat=1');
    document.getElementById('catLoading').classList.add('d-none');
    if (!res.success) return;
    _cats = res.data || [];
    if (!_cats.length) { document.getElementById('catEmpty').classList.remove('d-none'); return; }
    document.getElementById('catTable').style.display = '';
    document.getElementById('catBody').innerHTML = _cats.map(c => `
      <tr>
        <td style="padding-left:${(c.Level-1)*16+12}px;">
          ${'›'.repeat(Math.max(0,c.Level-1))} <strong>${esc(c.Name)}</strong>
          ${c.Description ? `<br><small class="text-muted">${esc(c.Description)}</small>` : ''}
        </td>
        <td><code>${esc(c.Code||'—')}</code></td>
        <td><span class="badge bg-secondary">L${c.Level}</span></td>
        <td class="text-end">${c.ProductCount}</td>
        <td class="text-end">
          <div class="d-flex gap-1 justify-content-end">
            <button class="btn btn-ghost btn-icon btn-sm" onclick="CatPage.openCatModal('${c.Id}')"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-ghost btn-icon btn-sm text-danger" onclick="CatPage.deleteCat('${c.Id}','${esc(c.Name)}')"><i class="bi bi-trash"></i></button>
          </div>
        </td>
      </tr>`).join('');
    // Populate parent dropdown in modal
    const sel = document.getElementById('catParent');
    while (sel.options.length > 1) sel.remove(1);
    _cats.forEach(c => sel.add(new Option('  '.repeat(c.Level-1) + c.Name, c.Id)));
  }

  async function loadBrands() {
    document.getElementById('brandLoading').classList.remove('d-none');
    const res = await App.api('/Handlers/Products.ashx?entity=brand');
    document.getElementById('brandLoading').classList.add('d-none');
    if (!res.success) return;
    _brands = res.data || [];
    if (!_brands.length) { document.getElementById('brandEmpty').classList.remove('d-none'); return; }
    document.getElementById('brandTable').style.display = '';
    document.getElementById('brandBody').innerHTML = _brands.map(b => `
      <tr>
        <td><strong>${esc(b.Name)}</strong></td>
        <td><code>${esc(b.Code||'—')}</code></td>
        <td class="text-end">${b.ProductCount}</td>
        <td class="text-end">
          <div class="d-flex gap-1 justify-content-end">
            <button class="btn btn-ghost btn-icon btn-sm" onclick="CatPage.openBrandModal('${b.Id}')"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-ghost btn-icon btn-sm text-danger" onclick="CatPage.deleteBrand('${b.Id}','${esc(b.Name)}')"><i class="bi bi-trash"></i></button>
          </div>
        </td>
      </tr>`).join('');
  }

  function openCatModal(id) {
    const cat = id ? _cats.find(c => c.Id === id) : null;
    document.getElementById('catModalTitle').textContent = id ? 'Edit Category' : 'New Category';
    document.getElementById('catId').value     = id || '';
    document.getElementById('catName').value   = cat ? cat.Name : '';
    document.getElementById('catCode').value   = cat ? (cat.Code || '') : '';
    document.getElementById('catDesc').value   = cat ? (cat.Description || '') : '';
    document.getElementById('catSort').value   = cat ? cat.SortOrder : 0;
    document.getElementById('catParent').value = cat ? (cat.ParentId || '') : '';
    document.getElementById('catError').textContent = '';
    document.getElementById('catModal').classList.remove('d-none');
    document.getElementById('catBackdrop').classList.remove('d-none');
  }

  function closeCatModal() {
    document.getElementById('catModal').classList.add('d-none');
    document.getElementById('catBackdrop').classList.add('d-none');
  }

  async function saveCat() {
    const id = document.getElementById('catId').value;
    const dto = {
      ParentId:    document.getElementById('catParent').value || null,
      Name:        document.getElementById('catName').value.trim(),
      Code:        document.getElementById('catCode').value.trim() || null,
      Description: document.getElementById('catDesc').value.trim() || null,
      SortOrder:   parseInt(document.getElementById('catSort').value) || 0,
    };
    if (!dto.Name) { document.getElementById('catError').textContent = 'Name is required'; return; }
    const method = id ? 'PUT' : 'POST';
    const url    = id ? `/Handlers/Products.ashx?entity=category&id=${id}` : '/Handlers/Products.ashx?entity=category';
    const res = await App.api(url, { method, body: JSON.stringify(dto) });
    if (res.success) { App.toast(res.message, 'success'); closeCatModal(); loadCategories(); }
    else document.getElementById('catError').textContent = res.message;
  }

  async function deleteCat(id, name) {
    if (!confirm(`Delete category "${name}"?`)) return;
    const res = await App.api(`/Handlers/Products.ashx?entity=category&id=${id}`, { method:'DELETE' });
    if (res.success) { App.toast('Deleted', 'success'); loadCategories(); }
    else App.toast(res.message, 'danger');
  }

  function openBrandModal(id) {
    const b = id ? _brands.find(b => b.Id === id) : null;
    document.getElementById('brandModalTitle').textContent = id ? 'Edit Brand' : 'New Brand';
    document.getElementById('brandId').value   = id || '';
    document.getElementById('brandName').value = b ? b.Name : '';
    document.getElementById('brandCode').value = b ? (b.Code || '') : '';
    document.getElementById('brandDesc').value = b ? (b.Description || '') : '';
    document.getElementById('brandError').textContent = '';
    document.getElementById('brandModal').classList.remove('d-none');
    document.getElementById('brandBackdrop').classList.remove('d-none');
  }

  function closeBrandModal() {
    document.getElementById('brandModal').classList.add('d-none');
    document.getElementById('brandBackdrop').classList.add('d-none');
  }

  async function saveBrand() {
    const id = document.getElementById('brandId').value;
    const dto = {
      Name:        document.getElementById('brandName').value.trim(),
      Code:        document.getElementById('brandCode').value.trim() || null,
      Description: document.getElementById('brandDesc').value.trim() || null,
    };
    if (!dto.Name) { document.getElementById('brandError').textContent = 'Name is required'; return; }
    const method = id ? 'PUT' : 'POST';
    const url    = id ? `/Handlers/Products.ashx?entity=brand&id=${id}` : '/Handlers/Products.ashx?entity=brand';
    const res = await App.api(url, { method, body: JSON.stringify(dto) });
    if (res.success) { App.toast(res.message, 'success'); closeBrandModal(); loadBrands(); }
    else document.getElementById('brandError').textContent = res.message;
  }

  async function deleteBrand(id, name) {
    if (!confirm(`Delete brand "${name}"?`)) return;
    const res = await App.api(`/Handlers/Products.ashx?entity=brand&id=${id}`, { method:'DELETE' });
    if (res.success) { App.toast('Deleted', 'success'); loadBrands(); }
    else App.toast(res.message, 'danger');
  }

  document.addEventListener('DOMContentLoaded', () => {
    loadCategories();
    loadBrands();
  });

  return { openCatModal, closeCatModal, saveCat, deleteCat,
           openBrandModal, closeBrandModal, saveBrand, deleteBrand };
})();
</script>
</asp:Content>
