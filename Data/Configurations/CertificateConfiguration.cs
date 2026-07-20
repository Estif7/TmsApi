using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SerialNumber)
            .IsRequired();

        builder.HasIndex(c => c.SerialNumber)
            .IsUnique();

        // Matches Student's soft-delete filter, same reasoning as EnrollmentConfiguration.
        builder.HasQueryFilter(c => !c.Student.IsDeleted);
    }
}
