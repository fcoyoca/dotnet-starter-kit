namespace FSH.Starter.DbMigrator;

/// <summary>
/// Lightweight command-line parser. Avoids dragging in System.CommandLine for
/// a handful of flags — keep this honest and minimal.
///
/// Verbs:   apply | seed | seed-demo | list-pending | migrate-from-mssql  (default: apply)
/// Flags:   --tenant &lt;id&gt;              scope to one tenant id
///          --catalog-only             skip per-tenant migrations
///          --seed                     after apply, also run SeedAsync per tenant
///          --source-connection &lt;cs&gt;  (migrate-from-mssql) BackChart MSSQL connection string
///          --dry-run                  (migrate-from-mssql) validate only, zero DB writes
///          --batch-size &lt;n&gt;          (migrate-from-mssql) rows per SQL page (default 100)
///          --help / -h                print help text
/// </summary>
internal sealed record MigratorCommand(
    string Command,
    string? Tenant,
    bool CatalogOnly,
    bool SeedAfter,
    bool Help,
    string? SourceConnectionString = null,
    bool DryRun = false,
    int BatchSize = 100)
{
    private static readonly string[] KnownVerbs =
        ["apply", "seed", "seed-demo", "list-pending", "migrate-from-mssql", "migrate-lookups-from-mssql", "migrate-users-from-mssql", "migrate-drug-catalog-from-mssql"];

    public static MigratorCommand Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var rawVerb = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "apply";
        // Canonicalise to a known verb via OrdinalIgnoreCase match (CA1308 forbids
        // ToLowerInvariant for security-sensitive normalisation).
        var verb = KnownVerbs.FirstOrDefault(v => string.Equals(v, rawVerb, StringComparison.OrdinalIgnoreCase))
            ?? rawVerb;

        var tenant = ExtractValue(args, "--tenant");
        var catalogOnly = args.Any(a => string.Equals(a, "--catalog-only", StringComparison.OrdinalIgnoreCase));
        var seedAfter = args.Any(a => string.Equals(a, "--seed", StringComparison.OrdinalIgnoreCase));
        var help = args.Any(a => a is "-h" or "--help");
        var sourceConn = ExtractValue(args, "--source-connection");
        var dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
        var batchSizeRaw = ExtractValue(args, "--batch-size");
        var batchSize = int.TryParse(batchSizeRaw, out var bs) && bs > 0 ? bs : 100;

        return new MigratorCommand(verb, tenant, catalogOnly, seedAfter, help,
            sourceConn, dryRun, batchSize);
    }

    private static string? ExtractValue(string[] args, string flag)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
            // Also accept --flag=value form.
            if (args[i].StartsWith($"{flag}=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i][(flag.Length + 1)..];
            }
        }
        return null;
    }

    public const string HelpText = """
        FSH DbMigrator — apply EF Core migrations across the tenant catalog
        and every tenant's per-module databases.

        Usage:
          dotnet run --project src/Host/FSH.Starter.DbMigrator -- [verb] [options]

        Verbs:
          apply               Apply pending migrations (default). Use --seed to also run SeedAsync.
          seed                Run only the SeedAsync step per tenant.
          seed-demo           Provision the demo tenants (acme, globex) with users, catalog,
                              tickets, and chat. Dev-only — refuses to run unless
                              ASPNETCORE_ENVIRONMENT=Development.
          list-pending        Print pending migrations without applying anything.
          migrate-from-mssql  Read patients from a BackChart MSSQL database and upsert them
                              into a target FSH tenant. Requires --source-connection and --tenant.
          migrate-lookups-from-mssql
                              Migrate the reference lookup tables (races, ethnicities, languages,
                              smoking statuses, contact methods, referral types) from a BackChart
                              MSSQL database into the Administration module, preserving original IDs.
                              Run BEFORE migrate-from-mssql. Requires --source-connection and --tenant.
          migrate-users-from-mssql
                              Migrate active staff/provider accounts from a BackChart MSSQL database
                              into the Identity module for a tenant. Passwords are NOT migrated (each
                              user gets a discarded random password + confirmed email, so they sign in
                              via forgot-password). uSuperUser → Admin, all → Basic. Requires
                              --source-connection and --tenant.
          migrate-drug-catalog-from-mssql
                              Copy RXNCONSO → Drugs (COPY), SnomedAssociation → AllergyReactions,
                              MedicationUnitTypes → MedicationDoseUnits. Run before migrate-from-mssql.
                              Requires --source-connection and --tenant. Supports --dry-run.
          apply           Apply pending migrations (default). Use --seed to also run SeedAsync.
          seed            Run only the SeedAsync step per tenant.
          seed-demo       Provision the demo tenants (acme, globex) with users, catalog,
                          tickets, and chat. Dev-only — refuses to run unless
                          DOTNET_ENVIRONMENT=Development.
          list-pending    Print pending migrations without applying anything.

        Options:
          --tenant <id>              Restrict to a single tenant id (default: all tenants).
          --catalog-only             Skip the per-tenant pass; only the tenant catalog is migrated.
          --seed                     After apply, also call ITenantService.SeedTenantAsync.
          --source-connection <cs>   (migrate-from-mssql) MSSQL connection string for the
                                     BackChart source database.
          --dry-run                  (migrate-from-mssql) Validate all rows but write nothing.
                                     Errors are logged to migration-errors-dry-run-{ts}.json.
          --batch-size <n>           (migrate-from-mssql) Rows per SQL page (default: 100).
          -h, --help                 Print this help text.

        Examples:
          dotnet run ... -- migrate-from-mssql \
            --source-connection "Server=...;Database=BackChart;..." \
            --tenant acme \
            --dry-run

          dotnet run ... -- migrate-from-mssql \
            --source-connection "Server=...;Database=BackChart;..." \
            --tenant acme \
            --batch-size 200

        Exit codes:
          0 — success (or dry-run with zero validation errors)
          1 — failure (see logged exception or migration-errors-*.json)
        """;
}
