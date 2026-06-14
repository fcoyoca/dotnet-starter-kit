using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Domain.Patient>
{
    private readonly IPhiEncryptor _phi;

    public PatientConfiguration(IPhiEncryptor phi)
    {
        ArgumentNullException.ThrowIfNull(phi);
        _phi = phi;
    }

    public void Configure(EntityTypeBuilder<Domain.Patient> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Patients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.PatientCode).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Ignore(x => x.DomainEvents);

        // Demographics (owned — same table)
        builder.OwnsOne(x => x.Demographics, d =>
        {
            d.Property(x => x.FirstName).IsRequired().HasMaxLength(100).HasColumnName("FirstName");
            d.Property(x => x.LastName).IsRequired().HasMaxLength(100).HasColumnName("LastName");
            d.Property(x => x.MiddleInitial).HasMaxLength(5).HasColumnName("MiddleInitial");
            d.Property(x => x.Gender).IsRequired().HasMaxLength(10).HasColumnName("Gender");
            d.Property(x => x.MaritalStatus).HasMaxLength(20).HasColumnName("MaritalStatus");
            d.Property(x => x.MedicalAlertNotes).HasMaxLength(8000).HasColumnName("MedicalAlertNotes");
            d.HasIndex(x => x.LastName);
        });

        // Contact (owned — same table)
        builder.OwnsOne(x => x.Contact, c =>
        {
            c.Property(x => x.Address1).HasMaxLength(200).HasColumnName("Address1");
            c.Property(x => x.Address2).HasMaxLength(200).HasColumnName("Address2");
            c.Property(x => x.City).HasMaxLength(100).HasColumnName("City");
            c.Property(x => x.State).HasMaxLength(2).HasColumnName("State");
            c.Property(x => x.ZipCode).HasMaxLength(10).HasColumnName("ZipCode");
            c.Property(x => x.Phone).HasMaxLength(20).HasColumnName("Phone");
            c.Property(x => x.PhoneExtension).HasMaxLength(10).HasColumnName("PhoneExtension");
            c.Property(x => x.CellPhone).HasMaxLength(20).HasColumnName("CellPhone");
            c.Property(x => x.Email).HasMaxLength(100).HasColumnName("Email");
        });

        // PHI (owned — same table, value converters encrypt/decrypt transparently)
        var encryptConverter = new ValueConverter<string?, string?>(
            v => _phi.Encrypt(v),
            v => _phi.Decrypt(v));

        builder.OwnsOne(x => x.PHI, phi =>
        {
            phi.Property(x => x.Ssn)
                .HasMaxLength(512)
                .HasColumnName("SsnEncrypted")
                .HasConversion(encryptConverter);
            phi.Property(x => x.SsnSearchHash)
                .HasMaxLength(64)
                .HasColumnName("SsnSearchHash")
                .IsFixedLength();
            phi.Property(x => x.GuardianSsn)
                .HasMaxLength(512)
                .HasColumnName("GuardianSsnEncrypted")
                .HasConversion(encryptConverter);
            phi.HasIndex(x => x.SsnSearchHash);
        });

        // Employment (owned — same table, nullable)
        builder.OwnsOne(x => x.Employment, e =>
        {
            e.Property(x => x.Occupation).HasMaxLength(100).HasColumnName("Occupation");
            e.Property(x => x.EmployerName).HasMaxLength(200).HasColumnName("EmployerName");
            e.Property(x => x.EmployerAddress1).HasMaxLength(200).HasColumnName("EmployerAddress1");
            e.Property(x => x.EmployerAddress2).HasMaxLength(200).HasColumnName("EmployerAddress2");
            e.Property(x => x.EmployerCity).HasMaxLength(100).HasColumnName("EmployerCity");
            e.Property(x => x.EmployerState).HasMaxLength(2).HasColumnName("EmployerState");
            e.Property(x => x.EmployerZipCode).HasMaxLength(10).HasColumnName("EmployerZipCode");
            e.Property(x => x.EmployerPhone).HasMaxLength(20).HasColumnName("EmployerPhone");
            e.Property(x => x.EmployerPhoneExtension).HasMaxLength(10).HasColumnName("EmployerPhoneExtension");
        });

        // Guardian (owned — same table, nullable, only set when IsMinor=true)
        builder.OwnsOne(x => x.Guardian, g =>
        {
            g.Property(x => x.FirstName).HasMaxLength(100).HasColumnName("GuardianFirstName");
            g.Property(x => x.LastName).HasMaxLength(100).HasColumnName("GuardianLastName");
            g.Property(x => x.MiddleInitial).HasMaxLength(5).HasColumnName("GuardianMiddleInitial");
            g.Property(x => x.Gender).HasMaxLength(10).HasColumnName("GuardianGender");
            g.Property(x => x.MaritalStatus).HasMaxLength(20).HasColumnName("GuardianMaritalStatus");
            g.Property(x => x.Address1).HasMaxLength(200).HasColumnName("GuardianAddress1");
            g.Property(x => x.Address2).HasMaxLength(200).HasColumnName("GuardianAddress2");
            g.Property(x => x.City).HasMaxLength(100).HasColumnName("GuardianCity");
            g.Property(x => x.State).HasMaxLength(2).HasColumnName("GuardianState");
            g.Property(x => x.ZipCode).HasMaxLength(10).HasColumnName("GuardianZipCode");
            g.Property(x => x.Phone).HasMaxLength(20).HasColumnName("GuardianPhone");
            g.Property(x => x.CellPhone).HasMaxLength(20).HasColumnName("GuardianCellPhone");
            g.Property(x => x.EmployerName).HasMaxLength(200).HasColumnName("GuardianEmployerName");
            g.Property(x => x.EmployerAddress1).HasMaxLength(200).HasColumnName("GuardianEmployerAddress1");
            g.Property(x => x.EmployerAddress2).HasMaxLength(200).HasColumnName("GuardianEmployerAddress2");
            g.Property(x => x.EmployerCity).HasMaxLength(100).HasColumnName("GuardianEmployerCity");
            g.Property(x => x.EmployerState).HasMaxLength(2).HasColumnName("GuardianEmployerState");
            g.Property(x => x.EmployerZipCode).HasMaxLength(10).HasColumnName("GuardianEmployerZipCode");
        });

        // Next of Kin (owned — same table, nullable)
        builder.OwnsOne(x => x.NextOfKin, n =>
        {
            n.Property(x => x.FirstName).HasMaxLength(100).HasColumnName("NextOfKinFirstName");
            n.Property(x => x.LastName).HasMaxLength(100).HasColumnName("NextOfKinLastName");
            n.Property(x => x.Phone).HasMaxLength(20).HasColumnName("NextOfKinPhone");
            n.Property(x => x.Relation).HasMaxLength(50).HasColumnName("NextOfKinRelation");
            n.Property(x => x.RelationRoleCode).HasMaxLength(20).HasColumnName("NextOfKinRelationRoleCode");
        });

        // Insurance (owned — same table, nullable)
        builder.OwnsOne(x => x.Insurance, i =>
        {
            i.Property(x => x.InsuredFullName).HasMaxLength(200).HasColumnName("InsuredFullName");
            i.Property(x => x.InsuredEmployerName).HasMaxLength(200).HasColumnName("InsuredEmployerName");
        });
    }
}
