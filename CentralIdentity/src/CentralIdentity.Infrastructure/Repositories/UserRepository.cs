using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<long> CreateAsync(IdentityUser user, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityUsers]
                ([Username],[Email],[Phone],[PasswordHash],[FirstName],[LastName],
                 [IsActive],[EmailVerified],[PhoneVerified],[TwoFactorEnabled],
                 [FailedLoginAttempts],[SecurityStamp],[PasswordChangedAtUtc],[CreatedAtUtc])
            VALUES
                (@Username,@Email,@Phone,@PasswordHash,@FirstName,@LastName,
                 @IsActive,@EmailVerified,@PhoneVerified,@TwoFactorEnabled,
                 @FailedLoginAttempts,@SecurityStamp,@PasswordChangedAtUtc,@CreatedAtUtc);
            SELECT SCOPE_IDENTITY();";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
        cmd.Parameters.AddWithValue("@LastName", user.LastName);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@EmailVerified", user.EmailVerified);
        cmd.Parameters.AddWithValue("@PhoneVerified", user.PhoneVerified);
        cmd.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
        cmd.Parameters.AddWithValue("@FailedLoginAttempts", user.FailedLoginAttempts);
        cmd.Parameters.AddWithValue("@SecurityStamp", user.SecurityStamp);
        cmd.Parameters.AddWithValue("@PasswordChangedAtUtc", (object?)user.PasswordChangedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", user.CreatedAtUtc);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task<IdentityUser?> GetByIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUsers] WHERE [UserId] = @UserId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<IdentityUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUsers] WHERE [Email] = @Email";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<IdentityUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUsers] WHERE [Username] = @Username";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username.ToLowerInvariant());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<IReadOnlyList<IdentityUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM [dbo].[IdentityUsers]
            ORDER BY [CreatedAtUtc] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var users = new List<IdentityUser>();
        while (await reader.ReadAsync(ct))
            users.Add(MapUser(reader));
        return users.AsReadOnly();
    }

    public async Task UpdateAsync(IdentityUser user, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE [dbo].[IdentityUsers]
            SET [Phone]=@Phone,[FirstName]=@FirstName,[LastName]=@LastName,
                [IsActive]=@IsActive,[EmailVerified]=@EmailVerified,
                [PhoneVerified]=@PhoneVerified,[TwoFactorEnabled]=@TwoFactorEnabled,
                [FailedLoginAttempts]=@FailedLoginAttempts,[LockoutEndUtc]=@LockoutEndUtc,
                [PasswordChangedAtUtc]=@PasswordChangedAtUtc,[LastLoginAtUtc]=@LastLoginAtUtc,
                [SecurityStamp]=@SecurityStamp,[PasswordHash]=@PasswordHash,
                [UpdatedAtUtc]=@UpdatedAtUtc
            WHERE [UserId]=@UserId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", user.UserId);
        cmd.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
        cmd.Parameters.AddWithValue("@LastName", user.LastName);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@EmailVerified", user.EmailVerified);
        cmd.Parameters.AddWithValue("@PhoneVerified", user.PhoneVerified);
        cmd.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
        cmd.Parameters.AddWithValue("@FailedLoginAttempts", user.FailedLoginAttempts);
        cmd.Parameters.AddWithValue("@LockoutEndUtc", (object?)user.LockoutEndUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PasswordChangedAtUtc", (object?)user.PasswordChangedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastLoginAtUtc", (object?)user.LastLoginAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SecurityStamp", user.SecurityStamp);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@UpdatedAtUtc", (object?)user.UpdatedAtUtc ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM [dbo].[IdentityUsers] WHERE [Email] = @Email";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());
        var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return count > 0;
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM [dbo].[IdentityUsers] WHERE [Username] = @Username";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username.ToLowerInvariant());
        var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return count > 0;
    }

    private static IdentityUser MapUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
        LastName = reader.GetString(reader.GetOrdinal("LastName")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        EmailVerified = reader.GetBoolean(reader.GetOrdinal("EmailVerified")),
        PhoneVerified = reader.GetBoolean(reader.GetOrdinal("PhoneVerified")),
        TwoFactorEnabled = reader.GetBoolean(reader.GetOrdinal("TwoFactorEnabled")),
        FailedLoginAttempts = reader.GetInt32(reader.GetOrdinal("FailedLoginAttempts")),
        LockoutEndUtc = reader.IsDBNull(reader.GetOrdinal("LockoutEndUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("LockoutEndUtc")),
        PasswordChangedAtUtc = reader.IsDBNull(reader.GetOrdinal("PasswordChangedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("PasswordChangedAtUtc")),
        LastLoginAtUtc = reader.IsDBNull(reader.GetOrdinal("LastLoginAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAtUtc")),
        SecurityStamp = reader.GetString(reader.GetOrdinal("SecurityStamp")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        UpdatedAtUtc = reader.IsDBNull(reader.GetOrdinal("UpdatedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))
    };
}
