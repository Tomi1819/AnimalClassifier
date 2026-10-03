namespace AnimalClassifier.Infrastructure.Data
{
    using AnimalClassifier.Infrastructure.Data.Converters;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class AnimalClassifierDbContext : IdentityDbContext<ApplicationUser>
    {
        /// <summary>
        /// The connection string, under <c>ConnectionStrings</c>, that the
        /// database is reached by.
        /// </summary>
        public const string ConnectionStringName = "DefaultConnection";

        public AnimalClassifierDbContext(DbContextOptions<AnimalClassifierDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Every IEntityTypeConfiguration in this assembly, so that a new
            // table's configuration takes effect by being written.
            builder.ApplyConfigurationsFromAssembly(typeof(AnimalClassifierDbContext).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        }

        public DbSet<AnimalRecognitionLog> AnimalRecognitionLogs { get; set; } = null!;
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;
    }
}
