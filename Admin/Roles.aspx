<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Roles.aspx.cs" Inherits="Admin_Roles" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Roles & Permissions – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx">Dashboard</a>
    <span class="breadcrumb-item">Admin</span>
    <span class="breadcrumb-item active">Roles &amp; Permissions</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
<style>
  /* Permission matrix table */
  .perm-matrix th.action-head { font-size:.7rem; text-transform:uppercase; letter-spacing:.04em; min-width:70px; text-align:center; }
  .perm-matrix td.action-cell { text-align:center; }
  .perm-matrix tr.feature-row td:first-child { padding-left:2rem; color:var(--text-muted); font-size:.85rem; }
  .perm-matrix tr.module-row td { background:var(--surface-2); font-weight:600; }
  .perm-toggle { width:18px; height:18px; cursor:pointer; accent-color:var(--accent); }
  .role-card { border:2px solid var(--border); border-radius:var(--radius-md); cursor:pointer; transition:border-color .15s; }
  .role-card:hover, .role-card.selected { border-color:var(--accent); }
  .role-card.selected { background:color-mix(in srgb,var(--accent) 8%,transparent); }
  .role-card.system-role { opacity:.8; }
</style>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title">Roles &amp; Permissions</h1>
      <p class="page-subtitle">Define what each role can do across the system</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-primary btn-sm" onclick="RolesPage.openCreateModal()">
        <i class="bi bi-shield-plus"></i> New Role
      </button>
    </div>
  </div>

  <div class="page-body">
    <div style="display:grid;grid-template-columns:280px 1fr;gap:1rem;align-items:start;" class="roles-layout">

      <!-- Role list -->
      <div>
        <div class="card">
          <div class="card-header">
            <span class="card-title">Roles</span>
            <span id="roleCount" class="badge badge-muted">—</span>
          </div>
          <div id="roleList" style="max-height:600px;overflow-y:auto;padding:.5rem;">
            <div class="skeleton skeleton-text m-2"></div>
            <div class="skeleton skeleton-text m-2"></div>
            <div class="skeleton skeleton-text m-2"></div>
          </div>
        </div>
      </div>

      <!-- Permission matrix -->
      <div>
        <div class="card" id="permMatrixCard" style="display:none;">
          <div class="card-header">
            <div>
              <span class="card-title" id="permMatrixTitle">Permissions</span>
              <p class="text-muted small mt-1" id="permMatrixSubtitle">Select a role to view permissions</p>
            </div>
            <div class="d-flex gap-2">
              <button class="btn btn-ghost btn-sm" id="selectAllBtn" onclick="RolesPage.selectAll()">Select All</button>
              <button class="btn btn-ghost btn-sm" id="clearAllBtn" onclick="RolesPage.clearAll()">Clear All</button>
              <button class="btn btn-primary btn-sm" id="savePermsBtn" onclick="RolesPage.savePermissions()" style="display:none;">
                <i class="bi bi-floppy"></i> Save
              </button>
            </div>
          </div>
          <div style="overflow-x:auto;">
            <table class="data-table perm-matrix" id="permMatrix" style="margin:0;">
              <thead id="permMatrixHead"></thead>
              <tbody id="permMatrixBody"></tbody>
            </table>
          </div>
        </div>
        <div class="card" id="noRoleSelected">
          <div class="card-body text-center p-5 text-muted">
            <i class="bi bi-shield" style="font-size:3rem;opacity:.3;"></i>
            <p class="mt-3">Select a role to view and edit its permissions</p>
          </div>
        </div>
      </div>

    </div>
  </div>

  <!-- ── Create/Edit Role Modal ─────────────────────────────────────── -->
  <div class="modal-overlay" id="roleModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog">
      <div class="modal-header">
        <h2 class="modal-title" id="roleModalTitle">New Role</h2>
        <button class="modal-close" onclick="RolesPage.closeModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <form id="roleForm" onsubmit="RolesPage.submitRole(event)">
        <div class="modal-body">
          <input type="hidden" id="roleId" />
          <div class="form-group">
            <label class="form-label required">Role Name</label>
            <input type="text" id="roleName" class="form-input" required maxlength="100" />
          </div>
          <div class="form-group">
            <label class="form-label">Description</label>
            <textarea id="roleDesc" class="form-input" rows="3" style="resize:vertical;"></textarea>
          </div>
          <div class="form-group" id="activeGroup" style="display:none;">
            <label class="form-check">
              <input type="checkbox" id="roleActive" class="form-check-input" checked />
              <span class="form-check-label">Active</span>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="RolesPage.closeModal()">Cancel</button>
          <button type="submit" class="btn btn-primary" id="roleSubmitBtn">Create Role</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Delete confirm handled via native confirm() -->

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const RolesPage = (() => {
  let _roles = [], _matrix = null, _selectedRole = null, _editMode = false, _isDirty = false;

  async function init() {
    await loadRoles();
    await loadMatrix();
  }

  // ── Data ──────────────────────────────────────────────────────────

  async function loadRoles() {
    const res = await App.apiGet('/Handlers/Roles.ashx?pageSize=100');
    _roles = res?.data?.items || [];
    renderRoleList();
  }

  async function loadMatrix() {
    const res = await App.apiGet('/Handlers/Roles.ashx?action=permission_matrix');
    _matrix = res?.data;
    if (_selectedRole) renderMatrix(_selectedRole);
  }

  function renderRoleList() {
    const el = document.getElementById('roleList');
    const badge = document.getElementById('roleCount');
    if (badge) badge.textContent = _roles.length;
    if (!_roles.length) { el.innerHTML = '<p class="text-muted small p-3">No roles found</p>'; return; }
    el.innerHTML = _roles.map(r => `
      <div class="role-card p-3 mb-2 d-flex align-items-start gap-2 ${r.isSystem?'system-role':''} ${_selectedRole?.id===r.id?'selected':''}"
           id="roleCard_${r.id}" onclick="RolesPage.selectRole(${JSON.stringify(r.id)})">
        <i class="bi bi-shield${r.isSystem?'-lock':''} mt-1" style="color:var(--accent);"></i>
        <div style="flex:1;min-width:0;">
          <div class="fw-600 truncate">${esc(r.name)}</div>
          <div class="text-muted small">${r.permissionCount} perm${r.permissionCount!==1?'s':''} · ${r.userCount} user${r.userCount!==1?'s':''}</div>
          ${r.isSystem?'<span class="badge badge-muted" style="font-size:.65rem;">System</span>':''}
          ${!r.isActive?'<span class="badge badge-danger" style="font-size:.65rem;">Inactive</span>':''}
        </div>
        ${!r.isSystem ? `
          <div class="d-flex flex-column gap-1">
            <button class="btn btn-ghost btn-xs" title="Edit" onclick="event.stopPropagation();RolesPage.openEditModal(${JSON.stringify(r.id)})"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-ghost btn-xs text-danger" title="Delete" onclick="event.stopPropagation();RolesPage.deleteRole(${JSON.stringify(r.id)},${JSON.stringify(r.name)})"><i class="bi bi-trash3"></i></button>
          </div>` : ''}
      </div>`).join('');
  }

  async function selectRole(roleId) {
    if (_isDirty && !confirm('You have unsaved changes. Discard?')) return;
    _isDirty = false;

    const res = await App.apiGet(`/Handlers/Roles.ashx?id=${roleId}`);
    _selectedRole = res?.data;
    if (!_selectedRole) return;

    document.querySelectorAll('.role-card').forEach(c => c.classList.remove('selected'));
    const card = document.getElementById(`roleCard_${roleId}`);
    if (card) card.classList.add('selected');

    document.getElementById('noRoleSelected').style.display = 'none';
    document.getElementById('permMatrixCard').style.display = '';
    document.getElementById('permMatrixTitle').textContent = _selectedRole.name;
    document.getElementById('permMatrixSubtitle').textContent =
      `${_selectedRole.permissionCount} permissions · ${_selectedRole.userCount} users`;
    document.getElementById('savePermsBtn').style.display = _selectedRole.isSystem ? 'none' : '';

    renderMatrix(_selectedRole);
  }

  function renderMatrix(role) {
    if (!_matrix) return;
    const grantedIds = new Set((role.permissions || []).filter(p => p.isGranted).map(p => p.permissionId));
    const allActions = [...new Set(_matrix.groups.flatMap(g => g.features.flatMap(f => f.actions.map(a => a.action))))].sort();

    const thead = document.getElementById('permMatrixHead');
    thead.innerHTML = `<tr>
      <th style="min-width:180px;">Module / Feature</th>
      ${allActions.map(a => `<th class="action-head">${esc(a)}</th>`).join('')}
    </tr>`;

    const tbody = document.getElementById('permMatrixBody');
    const rows = [];
    for (const group of _matrix.groups) {
      rows.push(`<tr class="module-row"><td colspan="${allActions.length+1}"><i class="bi bi-grid me-2"></i>${esc(group.module)}</td></tr>`);
      for (const feature of group.features) {
        const cells = allActions.map(act => {
          const perm = feature.actions.find(a => a.action === act);
          if (!perm) return '<td class="action-cell"><span style="opacity:.2;">—</span></td>';
          const checked = grantedIds.has(perm.id);
          const disabled = role.isSystem ? 'disabled' : '';
          return `<td class="action-cell">
            <input type="checkbox" class="perm-toggle" title="${esc(perm.permissionKey)}"
                   data-perm-id="${perm.id}" ${checked?'checked':''} ${disabled}
                   onchange="RolesPage.onPermChange()" />
          </td>`;
        });
        rows.push(`<tr class="feature-row"><td>${esc(feature.feature)}</td>${cells.join('')}</tr>`);
      }
    }
    tbody.innerHTML = rows.join('');
  }

  function onPermChange() {
    _isDirty = true;
  }

  function selectAll() {
    document.querySelectorAll('.perm-toggle:not(:disabled)').forEach(cb => cb.checked = true);
    _isDirty = true;
  }
  function clearAll() {
    document.querySelectorAll('.perm-toggle:not(:disabled)').forEach(cb => cb.checked = false);
    _isDirty = true;
  }

  async function savePermissions() {
    if (!_selectedRole) return;
    const permIds = [...document.querySelectorAll('.perm-toggle:checked')].map(cb => cb.dataset.permId);
    const dto = { id: _selectedRole.id, name: _selectedRole.name, description: _selectedRole.description,
                  isActive: _selectedRole.isActive, permissionIds: permIds };
    const res = await App.apiPut('/Handlers/Roles.ashx', dto);
    if (res?.success) {
      App.showToast('Permissions saved', 'success');
      _isDirty = false;
      await loadRoles();
    } else {
      App.showToast(res?.message || 'Error saving', 'error');
    }
  }

  // ── Modal ─────────────────────────────────────────────────────────

  function openCreateModal() {
    _editMode = false;
    document.getElementById('roleModalTitle').textContent = 'New Role';
    document.getElementById('roleSubmitBtn').textContent = 'Create Role';
    document.getElementById('roleForm').reset();
    document.getElementById('roleId').value = '';
    document.getElementById('activeGroup').style.display = 'none';
    showModal('roleModal');
  }

  function openEditModal(roleId) {
    _editMode = true;
    const role = _roles.find(r => r.id === roleId);
    if (!role) return;
    document.getElementById('roleModalTitle').textContent = 'Edit Role';
    document.getElementById('roleSubmitBtn').textContent = 'Save';
    document.getElementById('roleId').value = role.id;
    document.getElementById('roleName').value = role.name;
    document.getElementById('roleDesc').value = role.description || '';
    document.getElementById('roleActive').checked = role.isActive;
    document.getElementById('activeGroup').style.display = '';
    showModal('roleModal');
  }

  async function submitRole(e) {
    e.preventDefault();
    let res;
    if (_editMode) {
      const dto = { id: val('roleId'), name: val('roleName'), description: val('roleDesc'),
                    isActive: document.getElementById('roleActive').checked, permissionIds: [] };
      res = await App.apiPut('/Handlers/Roles.ashx', dto);
    } else {
      const dto = { name: val('roleName'), description: val('roleDesc'), permissionIds: [] };
      res = await App.apiPost('/Handlers/Roles.ashx', dto);
    }
    if (res?.success) {
      App.showToast(res.message || 'Saved', 'success');
      closeModal(); await loadRoles();
    } else {
      App.showToast(res?.message || 'Error', 'error');
    }
  }

  async function deleteRole(roleId, name) {
    if (!confirm(`Delete role "${name}"? Users with this role will lose it.`)) return;
    const res = await App.apiDelete(`/Handlers/Roles.ashx?id=${roleId}`);
    if (res?.success) {
      App.showToast('Role deleted', 'success');
      if (_selectedRole?.id === roleId) {
        _selectedRole = null;
        document.getElementById('permMatrixCard').style.display = 'none';
        document.getElementById('noRoleSelected').style.display = '';
      }
      await loadRoles();
    } else {
      App.showToast(res?.message || 'Error', 'error');
    }
  }

  function closeModal() { hideModal('roleModal'); }
  function showModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'flex'; }
  function hideModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'none'; }
  const val = id => (document.getElementById(id)?.value || '').trim();
  const esc = s => (!s ? '' : String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'));

  window.addEventListener('beforeunload', e => {
    if (_isDirty) { e.preventDefault(); e.returnValue = ''; }
  });

  document.addEventListener('DOMContentLoaded', init);
  return { init, selectRole, openCreateModal, openEditModal, closeModal, submitRole, deleteRole,
           savePermissions, selectAll, clearAll, onPermChange };
})();
</script>
</asp:Content>
