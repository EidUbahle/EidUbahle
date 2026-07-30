<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/Classic.master"
         CodeFile="Users.aspx.cs" Inherits="Admin_Users" %>

<asp:Content ContentPlaceHolderID="TitleContent" runat="server">Users – EidUbahle ERP</asp:Content>

<asp:Content ContentPlaceHolderID="BreadcrumbContent" runat="server">
  <nav aria-label="breadcrumb" class="breadcrumb">
    <a class="breadcrumb-item" href="/Pages/Dashboard.aspx" data-i18n="nav.dashboard">Dashboard</a>
    <span class="breadcrumb-item" data-i18n="nav.admin">Admin</span>
    <span class="breadcrumb-item active" data-i18n="admin.users">Users</span>
  </nav>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

  <div class="page-header d-flex align-items-center justify-content-between flex-wrap gap-3">
    <div>
      <h1 class="page-title" data-i18n="admin.users">Users</h1>
      <p class="page-subtitle" id="userCountLabel">Loading...</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-secondary btn-sm" onclick="UsersPage.openInviteModal()">
        <i class="bi bi-envelope-plus"></i>
        <span data-i18n="admin.users.invite">Invite User</span>
      </button>
      <button class="btn btn-primary btn-sm" onclick="UsersPage.openCreateModal()">
        <i class="bi bi-person-plus"></i>
        <span data-i18n="admin.users.create">Add User</span>
      </button>
    </div>
  </div>

  <div class="page-body">

    <!-- Filters -->
    <div class="card mb-3">
      <div class="card-body" style="padding:.75rem 1rem;">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <div style="flex:1;min-width:220px;">
            <input type="text" id="searchInput" class="form-input" placeholder="Search name, username or email…"
                   oninput="UsersPage.onSearch(this.value)" />
          </div>
          <select class="form-select" id="statusFilter" onchange="UsersPage.onFilterChange()" style="width:140px;">
            <option value="">All Status</option>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>
          <button class="btn btn-ghost btn-sm" onclick="UsersPage.load()">
            <i class="bi bi-arrow-clockwise"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Tabs: Users / Invitations -->
    <div class="card">
      <div class="card-header" style="padding:0;border-bottom:1px solid var(--border);">
        <nav class="d-flex gap-0" style="overflow-x:auto;">
          <button class="tab-btn active" id="tabUsers" onclick="UsersPage.switchTab('users')">
            <i class="bi bi-people"></i> <span data-i18n="admin.users">Users</span>
          </button>
          <button class="tab-btn" id="tabInvitations" onclick="UsersPage.switchTab('invitations')">
            <i class="bi bi-envelope"></i> <span data-i18n="admin.users.invitations">Invitations</span>
            <span class="badge badge-accent ms-1" id="pendingInvBadge" style="display:none;">0</span>
          </button>
        </nav>
      </div>

      <!-- Users table -->
      <div id="panelUsers">
        <div id="usersTableWrap">
          <table class="data-table">
            <thead>
              <tr>
                <th style="width:40px;"></th>
                <th data-i18n="common.name">Name</th>
                <th data-i18n="common.username">Username</th>
                <th data-i18n="common.email">Email</th>
                <th data-i18n="common.roles">Roles</th>
                <th data-i18n="common.branches">Branches</th>
                <th data-i18n="common.status">Status</th>
                <th data-i18n="common.last_login">Last Login</th>
                <th data-i18n="common.actions">Actions</th>
              </tr>
            </thead>
            <tbody id="usersBody">
              <tr><td colspan="9" class="text-center p-4">
                <div class="skeleton skeleton-text" style="width:60%;margin:auto;"></div>
              </td></tr>
            </tbody>
          </table>
        </div>
        <!-- Pagination -->
        <div class="d-flex align-items-center justify-content-between px-3 py-2 border-top">
          <span id="paginationInfo" class="text-muted small"></span>
          <div class="d-flex gap-2" id="paginationControls"></div>
        </div>
      </div>

      <!-- Invitations table -->
      <div id="panelInvitations" style="display:none;">
        <table class="data-table">
          <thead>
            <tr>
              <th data-i18n="common.email">Email</th>
              <th data-i18n="common.name">Name</th>
              <th data-i18n="common.status">Status</th>
              <th data-i18n="admin.users.invited_by">Invited By</th>
              <th data-i18n="common.expires">Expires</th>
              <th data-i18n="common.created">Created</th>
            </tr>
          </thead>
          <tbody id="invitationsBody">
            <tr><td colspan="6" class="text-center p-4">
              <div class="skeleton skeleton-text" style="width:50%;margin:auto;"></div>
            </td></tr>
          </tbody>
        </table>
      </div>
    </div>

  </div><!-- /page-body -->

  <!-- ── Create/Edit User Modal ─────────────────────────────────────── -->
  <div class="modal-overlay" id="userModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog modal-lg">
      <div class="modal-header">
        <h2 class="modal-title" id="userModalTitle">Add User</h2>
        <button class="modal-close" onclick="UsersPage.closeModal()" aria-label="Close">
          <i class="bi bi-x-lg"></i>
        </button>
      </div>
      <form id="userForm" onsubmit="UsersPage.submitUserForm(event)">
        <div class="modal-body">
          <input type="hidden" id="userId" />

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;" class="form-grid-2">
            <div class="form-group">
              <label class="form-label required" for="uFullName">Full Name</label>
              <input type="text" id="uFullName" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label required" for="uUsername">Username</label>
              <input type="text" id="uUsername" class="form-input" required autocomplete="off" />
            </div>
            <div class="form-group">
              <label class="form-label" for="uEmail">Email</label>
              <input type="email" id="uEmail" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label" for="uPhone">Phone</label>
              <input type="tel" id="uPhone" class="form-input" />
            </div>
            <div class="form-group" id="passwordGroup">
              <label class="form-label required" for="uPassword">Password</label>
              <div class="input-group">
                <input type="password" id="uPassword" class="form-input" minlength="8" autocomplete="new-password" />
                <button type="button" class="btn btn-ghost" onclick="UsersPage.togglePassword()" tabindex="-1">
                  <i class="bi bi-eye" id="pwEyeIcon"></i>
                </button>
              </div>
            </div>
            <div class="form-group">
              <label class="form-label" for="uLanguage">Language</label>
              <select id="uLanguage" class="form-select">
                <option value="en">English</option>
                <option value="so">Somali</option>
                <option value="ar">Arabic</option>
                <option value="fr">French</option>
              </select>
            </div>
          </div>

          <div class="form-group mt-2">
            <label class="form-check">
              <input type="checkbox" id="uIsAdmin" class="form-check-input" />
              <span class="form-check-label">Tenant Administrator</span>
            </label>
            <p class="form-hint">Admins can manage users, roles, and company settings.</p>
          </div>

          <!-- Roles -->
          <div class="form-group mt-3">
            <label class="form-label">Roles</label>
            <div id="rolesCheckList" class="d-flex flex-wrap gap-2 mt-1">
              <div class="skeleton skeleton-text" style="width:100px;"></div>
            </div>
          </div>

          <!-- Branch assignments -->
          <div class="form-group mt-3">
            <label class="form-label">Branch Access</label>
            <div id="branchAssignList" class="mt-1" style="max-height:200px;overflow-y:auto;border:1px solid var(--border);border-radius:var(--radius-sm);padding:.5rem;">
              <div class="skeleton skeleton-text"></div>
            </div>
          </div>

        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="UsersPage.closeModal()">Cancel</button>
          <button type="submit" class="btn btn-primary" id="userSubmitBtn">Save User</button>
        </div>
      </form>
    </div>
  </div>

  <!-- ── Invite Modal ───────────────────────────────────────────────── -->
  <div class="modal-overlay" id="inviteModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog">
      <div class="modal-header">
        <h2 class="modal-title">Invite User</h2>
        <button class="modal-close" onclick="UsersPage.closeInviteModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <form id="inviteForm" onsubmit="UsersPage.submitInvite(event)">
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label required">Email</label>
            <input type="email" id="invEmail" class="form-input" required />
          </div>
          <div class="form-group">
            <label class="form-label">Full Name</label>
            <input type="text" id="invFullName" class="form-input" />
          </div>
          <div id="invRolesCheckList" class="form-group">
            <label class="form-label">Roles</label>
            <div class="d-flex flex-wrap gap-2 mt-1" id="invRolesList">
              <div class="skeleton skeleton-text" style="width:100px;"></div>
            </div>
          </div>
          <div class="alert alert-info mt-3">
            <i class="bi bi-info-circle"></i>
            The user will receive an invitation link valid for 7 days.
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="UsersPage.closeInviteModal()">Cancel</button>
          <button type="submit" class="btn btn-primary">Send Invitation</button>
        </div>
      </form>
    </div>
  </div>

  <!-- ── Reset Password Modal ──────────────────────────────────────── -->
  <div class="modal-overlay" id="resetPwModal" style="display:none;" role="dialog" aria-modal="true">
    <div class="modal-dialog">
      <div class="modal-header">
        <h2 class="modal-title">Reset Password</h2>
        <button class="modal-close" onclick="UsersPage.closeResetModal()"><i class="bi bi-x-lg"></i></button>
      </div>
      <form id="resetPwForm" onsubmit="UsersPage.submitResetPassword(event)">
        <div class="modal-body">
          <input type="hidden" id="resetUserId" />
          <div class="form-group">
            <label class="form-label required">New Password</label>
            <input type="password" id="resetPw" class="form-input" required minlength="8" autocomplete="new-password" />
          </div>
          <div class="form-group">
            <label class="form-label required">Confirm Password</label>
            <input type="password" id="resetPwConfirm" class="form-input" required minlength="8" />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" onclick="UsersPage.closeResetModal()">Cancel</button>
          <button type="submit" class="btn btn-danger">Reset Password</button>
        </div>
      </form>
    </div>
  </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptContent" runat="server">
<script>
'use strict';
const UsersPage = (() => {
  let _page = 1, _pageSize = 20, _search = '', _isActive = '', _searchTimer = null;
  let _roles = [], _companies = [], _branches = [], _editMode = false;

  async function init() {
    await loadRoles();
    await loadBranches();
    await load();
    await loadInvitations();
  }

  // ── Data Loading ───────────────────────────────────────────────────

  async function load() {
    const tbody = document.getElementById('usersBody');
    tbody.innerHTML = '<tr><td colspan="9" class="text-center p-4"><div class="spinner-border spinner-sm"></div></td></tr>';

    const params = new URLSearchParams({ page: _page, pageSize: _pageSize });
    if (_search) params.set('search', _search);
    if (_isActive !== '') params.set('isActive', _isActive);

    const res = await App.apiGet(`/Handlers/Users.ashx?${params}`);
    const data = res?.data;
    if (!data) { tbody.innerHTML = '<tr><td colspan="9" class="text-center p-4 text-muted">Failed to load</td></tr>'; return; }

    const el = document.getElementById('userCountLabel');
    if (el) el.textContent = `${data.totalCount} user${data.totalCount !== 1 ? 's' : ''}`;

    if (!data.items?.length) {
      tbody.innerHTML = '<tr><td colspan="9" class="text-center p-4 text-muted">No users found</td></tr>';
      renderPagination(data);
      return;
    }
    tbody.innerHTML = data.items.map(u => `
      <tr>
        <td><div class="avatar-sm" style="background:hsl(${strHue(u.username)},60%,45%);color:#fff;border-radius:50%;width:32px;height:32px;display:flex;align-items:center;justify-content:center;font-size:.75rem;font-weight:600;">
          ${esc((u.fullName||u.username||'?')[0].toUpperCase())}
        </div></td>
        <td><div class="fw-600">${esc(u.fullName||'—')}</div></td>
        <td><code>${esc(u.username)}</code></td>
        <td>${esc(u.email||'—')}</td>
        <td>${(u.roleNames||[]).map(r=>`<span class="badge badge-muted">${esc(r)}</span>`).join(' ')||'—'}</td>
        <td>${(u.branchNames||[]).slice(0,2).map(b=>`<span class="badge badge-accent">${esc(b)}</span>`).join(' ')+(u.branchNames?.length>2?`<span class="badge">+${u.branchNames.length-2}</span>`:'')||'—'}</td>
        <td><span class="badge badge-${u.isActive?'success':'muted'}">${u.isActive?'Active':'Inactive'}</span>
          ${u.isTenantAdmin?'<span class="badge badge-warning ms-1">Admin</span>':''}</td>
        <td class="text-muted small">${u.lastLoginAt?fmtDate(u.lastLoginAt):'Never'}</td>
        <td>
          <div class="d-flex gap-1">
            <button class="btn btn-ghost btn-xs" title="Edit" onclick="UsersPage.openEditModal(${JSON.stringify(u.id)})"><i class="bi bi-pencil"></i></button>
            <button class="btn btn-ghost btn-xs" title="Reset password" onclick="UsersPage.openResetModal(${JSON.stringify(u.id)})"><i class="bi bi-key"></i></button>
            ${u.lockedUntil?`<button class="btn btn-ghost btn-xs text-warning" title="Unlock" onclick="UsersPage.unlock(${JSON.stringify(u.id)})"><i class="bi bi-lock"></i></button>`:''}
            <button class="btn btn-ghost btn-xs text-danger" title="Delete" onclick="UsersPage.deleteUser(${JSON.stringify(u.id)},${JSON.stringify(u.fullName||u.username)})"><i class="bi bi-trash3"></i></button>
          </div>
        </td>
      </tr>`).join('');
    renderPagination(data);
  }

  async function loadInvitations() {
    const res = await App.apiGet('/Handlers/Users.ashx?action=invitations');
    const invs = res?.data || [];
    const pending = invs.filter(i => i.status === 'Pending').length;
    const badge = document.getElementById('pendingInvBadge');
    if (badge) { badge.textContent = pending; badge.style.display = pending > 0 ? '' : 'none'; }
    const tbody = document.getElementById('invitationsBody');
    if (!invs.length) { tbody.innerHTML = '<tr><td colspan="6" class="text-center p-4 text-muted">No invitations</td></tr>'; return; }
    tbody.innerHTML = invs.map(i => `
      <tr>
        <td>${esc(i.email)}</td>
        <td>${esc(i.fullName||'—')}</td>
        <td><span class="badge badge-${statusBadge(i.status)}">${esc(i.status)}</span></td>
        <td>${esc(i.invitedByName||'—')}</td>
        <td class="${i.status==='Pending'&&new Date(i.expiresAt)<new Date()?'text-danger':''}">${fmtDate(i.expiresAt)}</td>
        <td>${fmtDate(i.createdAt)}</td>
      </tr>`).join('');
  }

  async function loadRoles() {
    const res = await App.apiGet('/Handlers/Roles.ashx');
    _roles = res?.data?.items || [];
    renderRoleCheckboxes('rolesCheckList', []);
    renderRoleCheckboxes('invRolesList', []);
  }

  async function loadBranches() {
    const res = await App.apiGet('/Handlers/Tenant.ashx?resource=branch&includeInactive=false');
    _branches = res?.data || [];
    const res2 = await App.apiGet('/Handlers/Tenant.ashx?resource=company');
    _companies = res2?.data || [];
  }

  // ── Tabs ───────────────────────────────────────────────────────────

  function switchTab(tab) {
    document.getElementById('panelUsers').style.display = tab === 'users' ? '' : 'none';
    document.getElementById('panelInvitations').style.display = tab === 'invitations' ? '' : 'none';
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.getElementById(tab === 'users' ? 'tabUsers' : 'tabInvitations').classList.add('active');
  }

  // ── Create modal ───────────────────────────────────────────────────

  function openCreateModal() {
    _editMode = false;
    document.getElementById('userModalTitle').textContent = 'Add User';
    document.getElementById('userSubmitBtn').textContent = 'Create User';
    document.getElementById('userForm').reset();
    document.getElementById('userId').value = '';
    document.getElementById('uUsername').removeAttribute('readonly');
    document.getElementById('passwordGroup').style.display = '';
    document.getElementById('uPassword').required = true;
    renderRoleCheckboxes('rolesCheckList', []);
    renderBranchList([]);
    showModal('userModal');
  }

  async function openEditModal(userId) {
    _editMode = true;
    document.getElementById('userModalTitle').textContent = 'Edit User';
    document.getElementById('userSubmitBtn').textContent = 'Save Changes';
    document.getElementById('passwordGroup').style.display = 'none';
    document.getElementById('uPassword').required = false;
    document.getElementById('uUsername').setAttribute('readonly', 'readonly');

    const res = await App.apiGet(`/Handlers/Users.ashx?id=${userId}`);
    const u = res?.data;
    if (!u) { App.showToast('Failed to load user', 'error'); return; }

    document.getElementById('userId').value = u.id;
    document.getElementById('uFullName').value = u.fullName || '';
    document.getElementById('uUsername').value = u.username || '';
    document.getElementById('uEmail').value = u.email || '';
    document.getElementById('uPhone').value = u.phone || '';
    document.getElementById('uLanguage').value = u.languageCode || 'en';
    document.getElementById('uIsAdmin').checked = u.isTenantAdmin;

    const assignedRoles = (u.roles || []).map(r => r.roleId);
    renderRoleCheckboxes('rolesCheckList', assignedRoles);
    renderBranchList((u.branches || []).map(b => b.branchId));
    showModal('userModal');
  }

  // ── Invite modal ───────────────────────────────────────────────────

  function openInviteModal() {
    document.getElementById('inviteForm').reset();
    renderRoleCheckboxes('invRolesList', []);
    showModal('inviteModal');
  }
  function closeInviteModal() { hideModal('inviteModal'); }

  // ── Reset password modal ───────────────────────────────────────────

  function openResetModal(userId) {
    document.getElementById('resetUserId').value = userId;
    document.getElementById('resetPwForm').reset();
    showModal('resetPwModal');
  }
  function closeResetModal() { hideModal('resetPwModal'); }

  // ── Form submissions ───────────────────────────────────────────────

  async function submitUserForm(e) {
    e.preventDefault();
    const id = document.getElementById('userId').value;
    const roleIds = getCheckedValues('rolesCheckList');
    const branches = getCheckedBranches();

    let res;
    if (_editMode) {
      const dto = {
        id, fullName: val('uFullName'), email: val('uEmail'), phone: val('uPhone'),
        isTenantAdmin: document.getElementById('uIsAdmin').checked,
        isActive: true, languageCode: val('uLanguage'),
        roleIds, branches
      };
      res = await App.apiPut('/Handlers/Users.ashx', dto);
    } else {
      const dto = {
        username: val('uUsername'), fullName: val('uFullName'), email: val('uEmail'),
        phone: val('uPhone'), password: val('uPassword'),
        isTenantAdmin: document.getElementById('uIsAdmin').checked,
        languageCode: val('uLanguage'), roleIds, branches
      };
      res = await App.apiPost('/Handlers/Users.ashx?action=create', dto);
    }

    if (res?.success) {
      App.showToast(res.message || 'Saved', 'success');
      closeModal(); await load();
    } else {
      App.showToast(res?.message || 'Error saving user', 'error');
    }
  }

  async function submitInvite(e) {
    e.preventDefault();
    const dto = {
      email: val('invEmail'), fullName: val('invFullName'),
      roleIds: getCheckedValues('invRolesList'), branches: []
    };
    const res = await App.apiPost('/Handlers/Users.ashx?action=invite', dto);
    if (res?.success) {
      App.showToast('Invitation sent', 'success');
      closeInviteModal(); await loadInvitations();
    } else {
      App.showToast(res?.message || 'Failed to send invite', 'error');
    }
  }

  async function submitResetPassword(e) {
    e.preventDefault();
    const pw = val('resetPw'), confirm = val('resetPwConfirm');
    if (pw !== confirm) { App.showToast('Passwords do not match', 'error'); return; }
    const dto = { userId: val('resetUserId'), newPassword: pw };
    const res = await App.apiPost('/Handlers/Users.ashx?action=reset_password', dto);
    if (res?.success) { App.showToast('Password reset', 'success'); closeResetModal(); }
    else App.showToast(res?.message || 'Error', 'error');
  }

  async function deleteUser(userId, name) {
    if (!confirm(`Delete user "${name}"? This action cannot be undone.`)) return;
    const res = await App.apiDelete(`/Handlers/Users.ashx?id=${userId}`);
    if (res?.success) { App.showToast('User deleted', 'success'); await load(); }
    else App.showToast(res?.message || 'Error deleting user', 'error');
  }

  async function unlock(userId) {
    const res = await App.apiPost('/Handlers/Users.ashx?action=unlock', { userId });
    if (res?.success) { App.showToast('Account unlocked', 'success'); await load(); }
    else App.showToast(res?.message || 'Error', 'error');
  }

  // ── Helpers ────────────────────────────────────────────────────────

  function renderRoleCheckboxes(containerId, selected) {
    const el = document.getElementById(containerId);
    if (!el) return;
    el.innerHTML = _roles.length ? _roles.map(r => `
      <label class="form-check d-inline-flex gap-1 me-3">
        <input type="checkbox" class="form-check-input role-cb" value="${r.id}"
               ${selected.includes(r.id)?'checked':''} />
        <span>${esc(r.name)}</span>
      </label>`).join('') : '<span class="text-muted small">No roles available</span>';
  }

  function renderBranchList(selectedIds) {
    const el = document.getElementById('branchAssignList');
    if (!el) return;
    if (!_branches.length) { el.innerHTML = '<span class="text-muted small">No branches available</span>'; return; }
    const byCompany = {};
    _branches.forEach(b => { if (!byCompany[b.companyId]) byCompany[b.companyId] = []; byCompany[b.companyId].push(b); });
    el.innerHTML = Object.entries(byCompany).map(([cid, branches]) => {
      const co = _companies.find(c => c.id === cid);
      return `<div class="mb-2"><div class="fw-600 small text-muted mb-1">${esc(co?.name||'')}</div>
        ${branches.map(b => `<label class="form-check mb-1">
          <input type="checkbox" class="form-check-input branch-cb" value="${b.id}"
                 data-company-id="${b.companyId}" ${selectedIds.includes(b.id)?'checked':''} />
          <span>${esc(b.name)}${b.isHeadOffice?' <span class="badge badge-accent">HQ</span>':''}</span>
        </label>`).join('')}
      </div>`;
    }).join('');
  }

  function getCheckedValues(containerId) {
    return [...document.querySelectorAll(`#${containerId} .role-cb:checked`)].map(cb => cb.value);
  }

  function getCheckedBranches() {
    return [...document.querySelectorAll('#branchAssignList .branch-cb:checked')].map(cb => ({
      companyId: cb.dataset.companyId, branchId: cb.value
    }));
  }

  function renderPagination(data) {
    const info = document.getElementById('paginationInfo');
    const controls = document.getElementById('paginationControls');
    if (!info || !controls) return;
    const from = (data.page - 1) * data.pageSize + 1;
    const to = Math.min(data.page * data.pageSize, data.totalCount);
    info.textContent = data.totalCount ? `${from}–${to} of ${data.totalCount}` : '';
    controls.innerHTML = '';
    if (data.totalPages > 1) {
      if (data.hasPreviousPage) {
        const btn = document.createElement('button');
        btn.className = 'btn btn-ghost btn-xs';
        btn.innerHTML = '<i class="bi bi-chevron-left"></i>';
        btn.onclick = () => { _page--; load(); };
        controls.appendChild(btn);
      }
      const span = document.createElement('span');
      span.className = 'btn btn-ghost btn-xs disabled';
      span.textContent = `${data.page} / ${data.totalPages}`;
      controls.appendChild(span);
      if (data.hasNextPage) {
        const btn = document.createElement('button');
        btn.className = 'btn btn-ghost btn-xs';
        btn.innerHTML = '<i class="bi bi-chevron-right"></i>';
        btn.onclick = () => { _page++; load(); };
        controls.appendChild(btn);
      }
    }
  }

  function onSearch(v) {
    clearTimeout(_searchTimer);
    _searchTimer = setTimeout(() => { _search = v; _page = 1; load(); }, 400);
  }
  function onFilterChange() {
    _isActive = document.getElementById('statusFilter').value;
    _page = 1; load();
  }
  function closeModal() { hideModal('userModal'); }
  function togglePassword() {
    const i = document.getElementById('uPassword');
    const ic = document.getElementById('pwEyeIcon');
    const show = i.type === 'password';
    i.type = show ? 'text' : 'password';
    ic.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
  }
  function showModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'flex'; }
  function hideModal(id) { const m = document.getElementById(id); if (m) m.style.display = 'none'; }
  const val = id => (document.getElementById(id)?.value || '').trim();
  const esc = s => (!s ? '' : String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'));
  const fmtDate = d => d ? new Date(d).toLocaleDateString() : '—';
  const strHue = s => [...(s||'')].reduce((h,c)=>h*31+c.charCodeAt(0),0)%360;
  const statusBadge = s => ({Pending:'warning',Accepted:'success',Expired:'muted',Cancelled:'danger'}[s]||'muted');

  document.addEventListener('DOMContentLoaded', init);
  return { load, openCreateModal, openEditModal, openInviteModal, closeInviteModal,
           openResetModal, closeResetModal, submitUserForm, submitInvite, submitResetPassword,
           deleteUser, unlock, switchTab, onSearch, onFilterChange, closeModal, togglePassword };
})();
</script>
</asp:Content>
