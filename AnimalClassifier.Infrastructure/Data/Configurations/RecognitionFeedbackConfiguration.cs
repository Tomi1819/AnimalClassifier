namespace AnimalClassifier.Infrastructure.Data.Configurations
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class RecognitionFeedbackConfiguration : IEntityTypeConfiguration<RecognitionFeedback>
    {
        public void Configure(EntityTypeBuilder<RecognitionFeedback> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Verdict)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(f => f.ReviewStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(f => f.ActualAnimal)
                .HasMaxLength(RecognitionFeedback.MaxActualAnimalLength);

            builder.Property(f => f.Comment)
                .HasMaxLength(RecognitionFeedback.MaxCommentLength);

            // The review reads the feedback in one state at a time, newest
            // first.
            builder.HasIndex(f => new { f.ReviewStatus, f.DateSubmitted });

            // Removed with its recognition by the database itself, so that
            // removing a user's recognitions in bulk takes their feedback too.
            builder.HasOne(f => f.Recognition)
                .WithOne(r => r.Feedback)
                .HasForeignKey<RecognitionFeedback>(f => f.RecognitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("RecognitionFeedback");
        }
    }
}
