namespace AnimalClassifier.Infrastructure.Data.Configurations
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    public class AnimalRecognitionLogConfiguration : IEntityTypeConfiguration<AnimalRecognitionLog>
    {
        public void Configure(EntityTypeBuilder<AnimalRecognitionLog> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(a => a.ImagePath)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.DateRecognized)
                .IsRequired();

            // The statistics read the recognitions made since a date, which
            // would otherwise mean reading every one ever made to find them.
            builder.HasIndex(a => a.DateRecognized);

            builder.Property(a => a.IsDeleted)
                .HasDefaultValue(false);

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.ToTable("AnimalRecognitionLogs");
        }
    }
}
