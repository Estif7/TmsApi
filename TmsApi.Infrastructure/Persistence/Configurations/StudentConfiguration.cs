using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Shadow audit property — not a C# property on Student, so it never
        // clutters DTOs, but EF tracks and persists it like any other column.
        builder.Property<DateTime>("LastUpdated");

        // Concurrency token — Npgsql maps IsRowVersion() to PostgreSQL's
        // system xmin column automatically; no separate column is created.
        builder.Property(s => s.Version)
            .IsRowVersion();

        // Soft-delete filter — applies to every query against Student
        // unless explicitly bypassed with IgnoreQueryFilters().
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
