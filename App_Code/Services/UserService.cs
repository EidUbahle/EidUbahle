using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Infrastructure.Security;
using EidUbahle.Repositories;

namespace EidUbahle.Services
{
    /// <summary>
    /// Business logic for user management: create, update, invite, delete, password operations.
    /// Enforces tenant limits (MaxUsers) and uniqueness rules.
    /// </summary>
    public class UserService
    {
        private readonly UserRepository _repo;
        private readonly TenantRepository _tenantRepo;
        private readonly IAppCache _cache;

        private const int MinPasswordLength = 8;

        public UserService(string connectionString, IAppCache cache)
        {
            _repo = new UserRepository(connectionString);
            _tenantRepo = new TenantRepository(connectionString);
            _cache = cache;
        }

        // ── List ─────────────────────────────────────────────────────────

        public ApiResponseDto<PagedResultDto<UserListItemDto>> GetUsers(
            Guid tenantId, string search = null, bool? isActive = null, int page = 1, int pageSize = 20)
        {
            var data = _repo.GetUsers(tenantId, search, isActive, page, pageSize);
            return ApiResponseDto<PagedResultDto<UserListItemDto>>.Ok(data);
        }

        // ── Detail ────────────────────────────────────────────────────────

        public ApiResponseDto<UserDetailDto> GetById(Guid tenantId, Guid userId)
        {
            var user = _repo.GetById(tenantId, userId);
            if (user == null)
                return ApiResponseDto<UserDetailDto>.Fail("User not found", "ERR_NOT_FOUND");
            return ApiResponseDto<UserDetailDto>.Ok(user);
        }

        // ── Create ────────────────────────────────────────────────────────

        public ApiResponseDto<Guid> CreateUser(Guid tenantId, Guid requestingUserId, CreateUserDto dto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Username))
                return ApiResponseDto<Guid>.Fail("Username is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Password))
                return ApiResponseDto<Guid>.Fail("Password is required", "ERR_VALIDATION");
            if (dto.Password.Length < MinPasswordLength)
                return ApiResponseDto<Guid>.Fail($"Password must be at least {MinPasswordLength} characters", "ERR_VALIDATION");

            // Tenant limit check
            var settings = _tenantRepo.GetSettings(tenantId);
            if (settings != null && _repo.CountActiveUsers(tenantId) >= settings.MaxUsers)
                return ApiResponseDto<Guid>.Fail("User limit reached for your plan", "ERR_LIMIT");

            // Uniqueness
            if (_repo.UsernameExists(tenantId, dto.Username))
                return ApiResponseDto<Guid>.Fail("Username already exists", "ERR_DUPLICATE");
            if (!string.IsNullOrWhiteSpace(dto.Email) && _repo.EmailExists(tenantId, dto.Email))
                return ApiResponseDto<Guid>.Fail("Email already exists", "ERR_DUPLICATE");

            var (hash, salt) = PasswordService.HashPassword(dto.Password);
            var id = _repo.Create(tenantId, dto, hash, salt);

            _cache.Remove($"perms:{id}");
            return ApiResponseDto<Guid>.Ok(id, "User created successfully");
        }

        // ── Update ────────────────────────────────────────────────────────

        public ApiResponseDto<bool> UpdateUser(Guid tenantId, Guid requestingUserId, UpdateUserDto dto)
        {
            var existing = _repo.GetById(tenantId, dto.Id);
            if (existing == null)
                return ApiResponseDto<bool>.Fail("User not found", "ERR_NOT_FOUND");

            if (!string.IsNullOrWhiteSpace(dto.Email) && _repo.EmailExists(tenantId, dto.Email, dto.Id))
                return ApiResponseDto<bool>.Fail("Email already in use by another user", "ERR_DUPLICATE");

            _repo.Update(tenantId, dto);
            _cache.Remove($"perms:{dto.Id}");
            return ApiResponseDto<bool>.Ok(true, "User updated successfully");
        }

        // ── Change password (self) ────────────────────────────────────────

        public ApiResponseDto<bool> ChangePassword(Guid tenantId, ChangePasswordDto dto)
        {
            var user = _repo.GetAppUserById(dto.UserId);
            if (user == null || user.TenantId != tenantId)
                return ApiResponseDto<bool>.Fail("User not found", "ERR_NOT_FOUND");

            if (!PasswordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                return ApiResponseDto<bool>.Fail("Current password is incorrect", "ERR_INVALID_PASSWORD");

            if (dto.NewPassword.Length < MinPasswordLength)
                return ApiResponseDto<bool>.Fail($"Password must be at least {MinPasswordLength} characters", "ERR_VALIDATION");

            var (hash, salt) = PasswordService.HashPassword(dto.NewPassword);
            _repo.UpdatePassword(dto.UserId, hash, salt);
            return ApiResponseDto<bool>.Ok(true, "Password changed successfully");
        }

        // ── Reset password (admin) ────────────────────────────────────────

        public ApiResponseDto<bool> ResetPassword(Guid tenantId, ResetPasswordDto dto)
        {
            var user = _repo.GetAppUserById(dto.UserId);
            if (user == null || user.TenantId != tenantId)
                return ApiResponseDto<bool>.Fail("User not found", "ERR_NOT_FOUND");

            if (dto.NewPassword.Length < MinPasswordLength)
                return ApiResponseDto<bool>.Fail($"Password must be at least {MinPasswordLength} characters", "ERR_VALIDATION");

            var (hash, salt) = PasswordService.HashPassword(dto.NewPassword);
            _repo.UpdatePassword(dto.UserId, hash, salt);
            return ApiResponseDto<bool>.Ok(true, "Password reset successfully");
        }

        // ── Delete ────────────────────────────────────────────────────────

        public ApiResponseDto<bool> DeleteUser(Guid tenantId, Guid requestingUserId, Guid userId)
        {
            if (userId == requestingUserId)
                return ApiResponseDto<bool>.Fail("You cannot delete your own account", "ERR_SELF_DELETE");

            var user = _repo.GetById(tenantId, userId);
            if (user == null)
                return ApiResponseDto<bool>.Fail("User not found", "ERR_NOT_FOUND");

            _repo.Delete(tenantId, userId);
            _cache.Remove($"perms:{userId}");
            return ApiResponseDto<bool>.Ok(true, "User deleted successfully");
        }

        // ── Unlock ────────────────────────────────────────────────────────

        public ApiResponseDto<bool> UnlockUser(Guid tenantId, Guid userId)
        {
            _repo.Unlock(tenantId, userId);
            return ApiResponseDto<bool>.Ok(true, "Account unlocked");
        }

        // ── Invite ────────────────────────────────────────────────────────

        public ApiResponseDto<InvitationDto> InviteUser(Guid tenantId, Guid invitedBy, InviteUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return ApiResponseDto<InvitationDto>.Fail("Email is required", "ERR_VALIDATION");

            // Tenant limit check
            var settings = _tenantRepo.GetSettings(tenantId);
            if (settings != null && _repo.CountActiveUsers(tenantId) >= settings.MaxUsers)
                return ApiResponseDto<InvitationDto>.Fail("User limit reached for your plan", "ERR_LIMIT");

            if (_repo.EmailExists(tenantId, dto.Email))
                return ApiResponseDto<InvitationDto>.Fail("A user with this email already exists", "ERR_DUPLICATE");

            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            _repo.CreateInvitation(tenantId, invitedBy, dto.Email, dto.FullName, token, expiresAt, dto.RoleIds, dto.Branches);

            return ApiResponseDto<InvitationDto>.Ok(new InvitationDto
            {
                Email = dto.Email,
                FullName = dto.FullName,
                Status = "Pending",
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            }, "Invitation created. Share the invite link with the user.");
        }

        // ── Accept invitation ─────────────────────────────────────────────

        public ApiResponseDto<Guid> AcceptInvitation(Guid tenantId, AcceptInviteDto dto)
        {
            var inv = _repo.GetInvitationByToken(dto.Token);
            if (inv == null)
                return ApiResponseDto<Guid>.Fail("Invalid invitation token", "ERR_INVALID_TOKEN");
            if (inv.Status != "Pending")
                return ApiResponseDto<Guid>.Fail("This invitation has already been used", "ERR_USED");
            if (inv.ExpiresAt < DateTime.UtcNow)
                return ApiResponseDto<Guid>.Fail("This invitation has expired", "ERR_EXPIRED");

            if (string.IsNullOrWhiteSpace(dto.Username))
                return ApiResponseDto<Guid>.Fail("Username is required", "ERR_VALIDATION");
            if (dto.Password?.Length < MinPasswordLength)
                return ApiResponseDto<Guid>.Fail($"Password must be at least {MinPasswordLength} characters", "ERR_VALIDATION");

            if (_repo.UsernameExists(tenantId, dto.Username))
                return ApiResponseDto<Guid>.Fail("Username already exists", "ERR_DUPLICATE");

            var (hash, salt) = PasswordService.HashPassword(dto.Password);
            var createDto = new CreateUserDto
            {
                Username = dto.Username,
                FullName = dto.FullName ?? inv.FullName,
                Email = inv.Email,
                Phone = dto.Phone,
                Password = dto.Password,
                LanguageCode = "en"
            };
            var userId = _repo.Create(tenantId, createDto, hash, salt);
            _repo.AcceptInvitation(inv.Id);
            return ApiResponseDto<Guid>.Ok(userId, "Account created successfully");
        }

        // ── Invitations list ──────────────────────────────────────────────

        public ApiResponseDto<List<InvitationDto>> GetInvitations(Guid tenantId)
        {
            return ApiResponseDto<List<InvitationDto>>.Ok(_repo.GetInvitations(tenantId));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
