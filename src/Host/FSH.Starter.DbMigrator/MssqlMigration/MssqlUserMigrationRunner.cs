using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Migrates active staff/provider accounts from BackChart/BronstonChiro <c>dbo.Users</c> into the Identity
/// module for a target tenant.
///
/// <para><b>Passwords are NOT migrated</b> — the legacy <c>uPassword</c> hash is incompatible with ASP.NET
/// Identity. Each user is created with a unique, strong, <i>discarded</i> random password and a confirmed
/// email, so the only way in is the forgot-password flow (effectively a forced reset).</para>
/// <para><b>Roles:</b> <c>uSuperUser</c> → Admin; everyone gets Basic. (<c>uDoctor</c> has no built-in role.)</para>
/// <para>Created directly via <see cref="UserManager{TUser}"/> (not the register pipeline) to avoid the
/// confirmation-email / job-enqueue path, which is disabled in the migrator.</para>
/// <para>Idempotent: a user whose email already exists is skipped.</para>
/// </summary>
internal sealed class MssqlUserMigrationRunner(
    IServiceProvider services,
    ILogger logger)
{
    private static readonly JsonSerializerOptions ErrorFileOptions = new() { WriteIndented = true };

    public async Task<int> RunAsync(
        string sourceConnectionString,
        string tenantId,
        bool dryRun,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var tenant = await ResolveTenantAsync(tenantId).ConfigureAwait(false);
        var errors = new List<UserMigrationError>();
        int created = 0, skipped = 0, validated = 0;

        await using var conn = new SqlConnection(sourceConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-users] connected to source: {conn.Database}").ConfigureAwait(false);

        await using (var openKey = new SqlCommand(MssqlUserMapper.OpenKeyStatement, conn))
        {
            await openKey.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        await Console.Out.WriteLineAsync(
            "[mssql-users] symmetric key opened — decryption active").ConfigureAwait(false);

        try
        {
            var sourceUsers = await ReadAllAsync(conn, ct).ConfigureAwait(false);
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-users] read {sourceUsers.Count} active source user(s)")).ConfigureAwait(false);

            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();

            foreach (var src in sourceUsers)
            {
                if (string.IsNullOrWhiteSpace(src.Email))
                {
                    errors.Add(new UserMigrationError(src.LegacyUserId, null, "ValidationFailed", "Email is required"));
                    continue;
                }

                if (dryRun)
                {
                    validated++;
                    continue;
                }

                try
                {
                    var result = await CreateUserAsync(userManager, src).ConfigureAwait(false);
                    if (result == UpsertResult.Created) created++; else skipped++;
                }
#pragma warning disable CA1031 // One bad row must not abort the migration; record and continue.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    errors.Add(new UserMigrationError(src.LegacyUserId, src.Email, ex.GetType().Name, ex.Message));
                    logger.LogWarning(ex, "[mssql-users] uID={LegacyUserId} failed: {Message}",
                        src.LegacyUserId, ex.Message);
                }
            }
        }
        finally
        {
            await using var closeKey = new SqlCommand(MssqlUserMapper.CloseKeyStatement, conn);
            await closeKey.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-users] DRY-RUN complete — {validated} valid, {errors.Count} error(s)")).ConfigureAwait(false);
        }
        else
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-users] complete — created {created}, skipped {skipped} (already exist), errors {errors.Count}"))
                .ConfigureAwait(false);
        }

        if (errors.Count > 0)
        {
            await WriteErrorFileAsync(errors, dryRun, ct).ConfigureAwait(false);
        }

        return errors.Count == 0 ? 0 : 1;
    }

    private async Task<UpsertResult> CreateUserAsync(
        UserManager<FshUser> userManager, MssqlUserMapper.SourceUser src)
    {
        // Idempotent: if the user already exists, still (re)ensure roles, then skip creation.
        var existing = await userManager.FindByEmailAsync(src.Email!).ConfigureAwait(false);
        if (existing is not null)
        {
            await EnsureRolesAsync(userManager, existing, src.IsSuperUser).ConfigureAwait(false);
            return UpsertResult.Skipped;
        }

        var userName = await BuildUniqueUserNameAsync(userManager, src).ConfigureAwait(false);

        var user = new FshUser
        {
            Email = src.Email,
            UserName = userName,
            FirstName = src.FirstName,
            LastName = src.LastName,
            IsActive = true,
            EmailConfirmed = true,        // known existing staff — no confirmation email needed
            PhoneNumberConfirmed = false,
        };

        // Discarded random password: nobody knows it, so the user must use forgot-password (forced reset).
        var createResult = await userManager.CreateAsync(user, GenerateStrongPassword()).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "CreateAsync failed: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await EnsureRolesAsync(userManager, user, src.IsSuperUser).ConfigureAwait(false);

        return UpsertResult.Created;
    }

    private async Task EnsureRolesAsync(UserManager<FshUser> userManager, FshUser user, bool isSuperUser)
    {
        await EnsureRoleAsync(userManager, user, RoleConstants.Basic).ConfigureAwait(false);
        if (isSuperUser)
        {
            await EnsureRoleAsync(userManager, user, RoleConstants.Admin).ConfigureAwait(false);
        }
    }

    private async Task EnsureRoleAsync(UserManager<FshUser> userManager, FshUser user, string role)
    {
        try
        {
            if (await userManager.IsInRoleAsync(user, role).ConfigureAwait(false))
            {
                return;
            }
            var result = await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                logger.LogWarning("[mssql-users] could not add {Email} to role {Role}: {Errors}",
                    user.Email, role, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
#pragma warning disable CA1031 // A role hiccup (e.g. role not seeded) must not fail the user row.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(ex, "[mssql-users] role {Role} could not be assigned to {Email}: {Message}",
                role, user.Email, ex.Message);
        }
    }

    private static async Task<string> BuildUniqueUserNameAsync(
        UserManager<FshUser> userManager, MssqlUserMapper.SourceUser src)
    {
        var baseName = Sanitize(src.UserName) ?? Sanitize(src.Email!.Split('@')[0]) ?? $"user{src.LegacyUserId}";
        if (await userManager.FindByNameAsync(baseName).ConfigureAwait(false) is null)
        {
            return baseName;
        }
        // Collision — append the legacy id, then a guid fragment if still taken.
        var withId = $"{baseName}.{src.LegacyUserId}";
        if (await userManager.FindByNameAsync(withId).ConfigureAwait(false) is null)
        {
            return withId;
        }
        return $"{baseName}.{Guid.NewGuid():N}"[..Math.Min(baseName.Length + 9, 32)];
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var kept = value.Trim()
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-' or '@' or '+')
            .ToArray();
        return kept.Length == 0 ? null : new string(kept);
    }

    /// <summary>Generates a 20-char password guaranteed to include each required character class.</summary>
    private static string GenerateStrongPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*?-_";
        const string all = lower + upper + digits + symbols;

        var chars = new char[20];
        chars[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }
        // Fisher–Yates shuffle so the guaranteed classes aren't always in the first 4 positions.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static async Task<List<MssqlUserMapper.SourceUser>> ReadAllAsync(SqlConnection conn, CancellationToken ct)
    {
        var results = new List<MssqlUserMapper.SourceUser>();
#pragma warning disable CA2100 // Query is a fixed code-defined constant — no user input.
        await using var cmd = new SqlCommand(MssqlUserMapper.SelectQuery, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MssqlUserMapper.Map(reader));
        }
        return results;
    }

    private async Task<AppTenantInfo> ResolveTenantAsync(string tenantId)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        return await store.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Tenant '{tenantId}' not found in the tenant catalog. Run 'apply --seed' first.");
    }

    private static async Task WriteErrorFileAsync(
        List<UserMigrationError> errors, bool dryRun, CancellationToken ct)
    {
        var suffix = dryRun ? "dry-run" : "live";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = $"user-migration-errors-{suffix}-{timestamp}.json";
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(errors, ErrorFileOptions), ct)
            .ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-users] {errors.Count} error(s) written to {path}").ConfigureAwait(false);
    }

    private enum UpsertResult { Created, Skipped }

    private sealed record UserMigrationError(int LegacyUserId, string? Email, string ErrorType, string Message);
}
