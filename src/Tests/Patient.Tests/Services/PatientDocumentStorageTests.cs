using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Infrastructure;
using FSH.Modules.Patient.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PatientDocumentStorageTests : IDisposable
{
    private readonly string _root;
    private readonly PatientDocumentStorage _sut;

    public PatientDocumentStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"patient-docs-tests-{Guid.NewGuid():N}");

        var options = Substitute.For<IOptions<PatientOptions>>();
        options.Value.Returns(new PatientOptions { PhiHmacKey = "dGVzdA==", DocumentsRootPath = _root });

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_root);

        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("acme", "acme", string.Empty, "acme@test.com", "Acme")));

        _sut = new PatientDocumentStorage(options, environment, accessor);
    }

    [Fact]
    public async Task SaveAsync_Should_Store_Under_PerTenant_PatientDocuments_Folder()
    {
        string storedPath = await _sut.SaveAsync("MRI Results.PDF", [1, 2, 3]);

        storedPath.ShouldStartWith("c_acme/patientDocuments/doc_");
        storedPath.ShouldEndWith(".pdf");
        File.Exists(Path.Combine(_root, storedPath)).ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsync_Should_RoundTrip_SavedContent()
    {
        byte[] content = [10, 20, 30, 40];
        string storedPath = await _sut.SaveAsync("notes.pdf", content);

        byte[]? read = await _sut.ReadAsync(storedPath);

        read.ShouldBe(content);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnNull_When_FileMissing()
    {
        (await _sut.ReadAsync("c_acme/patientDocuments/doc_missing.pdf")).ShouldBeNull();
    }

    [Fact]
    public async Task ReadAsync_Should_Reject_PathTraversal()
    {
        await Should.ThrowAsync<UnauthorizedException>(
            () => _sut.ReadAsync("../../outside.txt"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
