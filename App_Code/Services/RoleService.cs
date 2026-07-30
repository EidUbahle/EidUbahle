using System;
using System.Collections.Generic;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Repositories;

namespace EidUbahle.Services
{
    /// <summary>
    /// Business logic for Role and Permission management (RBAC).
    /// Enforces rules: system roles cannot be edited/deleted; unique names per tenant.
    /// </summary>
    public class RoleService
    {
        private readonly RoleRepository _repo;
        private readonly IAppCache _cache;

        public RoleService(string connectionString, IAppCache cache)
        {
            _repo = new RoleRepository(connectionString);
            _cache = cache;
        }

        // ── Permissions ───────────────────────────────────────────────────

        public ApiResponseDto<PermissionMatrixDto> GetPermissionMatrix()
        {
            var cacheKey = "perm_matrix";
            var matrix = _cache.GetOrAdd(cacheKey, () => _repo.GetPermissionMatrix(), TimeSpan.FromHours(1));
            return ApiResponseDto<PermissionMatrixDto>.Ok(matrix);
        }

        public ApiResponseDto<List<PermissionDto>> GetAllPermissions()
        {
            var list = _repo.GetAllPermissions();
            return ApiResponseDto<List<PermissionDto>>.Ok(list);
        }

        // ── Roles ─────────────────────────────────────────────────────────

        public ApiResponseDto<PagedResultDto<RoleListItemDto>> GetRoles(
            Guid tenantId, string search = null, int page = 1, int pageSize = 50)
        {
            var data = _repo.GetRoles(tenantId, search, page, pageSize);
            return ApiResponseDto<PagedResultDto<RoleListItemDto>>.Ok(data);
        }

        public ApiResponseDto<RoleDetailDto> GetById(Guid tenantId, Guid roleId)
        {
            var role = _repo.GetById(tenantId, roleId);
            if (role == null)
                return ApiResponseDto<RoleDetailDto>.Fail("Role not found", "ERR_NOT_FOUND");
            return ApiResponseDto<RoleDetailDto>.Ok(role);
        }

        // ── Create ────────────────────────────────────────────────────────

        public ApiResponseDto<Guid> CreateRole(Guid tenantId, CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Role name is required", "ERR_VALIDATION");

            if (_repo.NameExists(tenantId, dto.Name))
                return ApiResponseDto<Guid>.Fail("A role with this name already exists", "ERR_DUPLICATE");

            var id = _repo.Create(tenantId, dto);
            _cache.Remove("perm_matrix");
            return ApiResponseDto<Guid>.Ok(id, "Role created successfully");
        }

        // ── Update ────────────────────────────────────────────────────────

        public ApiResponseDto<bool> UpdateRole(Guid tenantId, UpdateRoleDto dto)
        {
            var existing = _repo.GetById(tenantId, dto.Id);
            if (existing == null)
                return ApiResponseDto<bool>.Fail("Role not found", "ERR_NOT_FOUND");

            if (existing.IsSystem)
                return ApiResponseDto<bool>.Fail("System roles cannot be modified", "ERR_SYSTEM_ROLE");

            if (_repo.NameExists(tenantId, dto.Name, dto.Id))
                return ApiResponseDto<bool>.Fail("A role with this name already exists", "ERR_DUPLICATE");

            _repo.Update(tenantId, dto);

            // Invalidate cached permissions for all users in this role
            _cache.Remove("perm_matrix");
            return ApiResponseDto<bool>.Ok(true, "Role updated successfully");
        }

        // ── Delete ────────────────────────────────────────────────────────

        public ApiResponseDto<bool> DeleteRole(Guid tenantId, Guid roleId)
        {
            var existing = _repo.GetById(tenantId, roleId);
            if (existing == null)
                return ApiResponseDto<bool>.Fail("Role not found", "ERR_NOT_FOUND");

            if (existing.IsSystem)
                return ApiResponseDto<bool>.Fail("System roles cannot be deleted", "ERR_SYSTEM_ROLE");

            if (existing.UserCount > 0)
                return ApiResponseDto<bool>.Fail(
                    $"Cannot delete role with {existing.UserCount} assigned user(s). Remove assignments first.",
                    "ERR_IN_USE");

            _repo.Delete(tenantId, roleId);
            _cache.Remove("perm_matrix");
            return ApiResponseDto<bool>.Ok(true, "Role deleted successfully");
        }
    }
}
