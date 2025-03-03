namespace AnimalClassifier.Infrastructure.Data
{
    using AnimalClassifier.Infrastructure.Data.Configurations;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class AnimalClassifierDbContext : IdentityDbContext<ApplicationUser>
    {
        public AnimalClassifierDbContext(DbContextOptions<AnimalClassifierDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new AnimalRecognitionLogConfiguration());
        }

        public DbSet<AnimalImage> AnimalImages { get; set; } = null!;
        public DbSet<AnimalRecognitionLog> AnimalRecognitionLogs { get; set; } = null!;
    }
}
