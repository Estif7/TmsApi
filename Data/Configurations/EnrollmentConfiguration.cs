using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        // A course can have many enrollments; deleting a course while
        // enrollments still reference it should fail loudly (Restrict)
        // rather than silently wiping historical enrollment/grade records.
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Same reasoning for students: a student with enrollment history
        // must not be deletable without deliberately handling their records first.
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Matches Student's soft-delete filter — an enrollment whose student
        // is soft-deleted should not surface in normal queries either.
        builder.HasQueryFilter(e => !e.Student.IsDeleted);
    }
}