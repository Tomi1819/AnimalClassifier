namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Data.Queries;
    using AnimalClassifier.Core.Data.Repositories;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Queries;
    using AnimalClassifier.Infrastructure.Data.Repositories;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// The database, the repositories every area reads and writes it
    /// through, and the queries that count and search across it.
    /// </summary>
    public static class PersistenceServiceCollectionExtension
    {
        private const string MissingConnectionString =
            $"ConnectionStrings:{AnimalClassifierDbContext.ConnectionStringName} is not set.";

        public static IServiceCollection AddApplicationPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(AnimalClassifierDbContext.ConnectionStringName)
                ?? throw new InvalidOperationException(MissingConnectionString);

            services.AddDbContext<AnimalClassifierDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IRecognitionLogRepository, RecognitionLogRepository>();
            services.AddScoped<IRecognitionFeedbackRepository, RecognitionFeedbackRepository>();
            services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IRecognitionStatisticsQueries, RecognitionStatisticsQueries>();
            services.AddScoped<IRecognitionSearchQueries, RecognitionSearchQueries>();
            services.AddScoped<IFeedbackSummaryQueries, FeedbackSummaryQueries>();

            return services;
        }
    }
}
