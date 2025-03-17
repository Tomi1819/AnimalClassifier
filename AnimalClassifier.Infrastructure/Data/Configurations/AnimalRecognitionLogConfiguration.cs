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

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.ToTable("AnimalRecognitionLogs");
        }
    }
}
