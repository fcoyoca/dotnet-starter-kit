using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillProcedureConfiguration : IEntityTypeConfiguration<SuperBillProcedure>
{
    public void Configure(EntityTypeBuilder<SuperBillProcedure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBillProcedures");
        builder.HasKey(x => x.Id);

        // Reached only through SuperBill.Procedures — without this EF marks adds as Modified.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SuperBillId).IsRequired();
        builder.Property(x => x.ProcedureCodeId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Charge).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.HasIndex(x => x.SuperBillId);

        builder.HasMany(x => x.Diagnostics)
            .WithOne()
            .HasForeignKey(d => d.SuperBillProcedureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
