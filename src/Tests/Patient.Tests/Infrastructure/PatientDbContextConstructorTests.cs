using System.Reflection;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Patient.Tests.Infrastructure;

public sealed class PatientDbContextConstructorTests
{
    [Fact]
    public void Has_The_Canonical_Four_Arg_BaseDbContext_Constructor()
    {
        var ctor = typeof(PatientDbContext).GetConstructor(
        [
            typeof(IMultiTenantContextAccessor<AppTenantInfo>),
            typeof(DbContextOptions<PatientDbContext>),
            typeof(IOptions<DatabaseOptions>),
            typeof(IHostEnvironment),
        ]);

        ctor.ShouldNotBeNull(
            "Architecture.Tests reflects for this exact signature to construct the context.");
    }

    [Fact]
    public void Di_Resolves_The_Context_With_The_Real_Phi_Encryptor()
    {
        IPhiEncryptor realEncryptor = Substitute.For<IPhiEncryptor>();

        var services = new ServiceCollection();
        services.AddSingleton<IMultiTenantContextAccessor<AppTenantInfo>>(
            Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>());
        services.AddSingleton(Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        }));
        services.AddSingleton<IHostEnvironment>(Substitute.For<IHostEnvironment>());
        services.AddSingleton(realEncryptor);
        services.AddDbContext<PatientDbContext>(o =>
            o.UseNpgsql("Host=arch;Database=arch;Username=arch;Password=arch"));

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        PatientDbContext ctx = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        // Reflect the private _phi field: must be the registered real encryptor,
        // proving DI selected the 5-arg ctor (not the no-op 4-arg test ctor).
        FieldInfo? phiField = typeof(PatientDbContext).GetField("_phi", BindingFlags.Instance | BindingFlags.NonPublic);
        phiField.ShouldNotBeNull();
        phiField.GetValue(ctx).ShouldBeSameAs(realEncryptor);
    }
}
