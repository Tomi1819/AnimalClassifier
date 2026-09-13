namespace AnimalClassifier.Infrastructure.Data.Configurations
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
    {
        public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasOne(a => a.Admin)
                .WithMany()
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.ToTable("AdminAuditLogs");
        }
    }
}
