namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.EntityFrameworkCore;
    using static Constants.MessageConstants;

    /// <summary>
    /// The database, and the repositories every area reads and writes it
    /// through.
    /// </summary>
    public static class PersistenceServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(AnimalClassifierDbContext.ConnectionStringName)
                ?? throw new InvalidOperationException(MissingConnectionString);

            services.AddDbContext<AnimalClassifierDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IRecognitionLogRepository, RecognitionLogRepository>();
            services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
