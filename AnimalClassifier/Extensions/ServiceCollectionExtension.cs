namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationDbContex(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<AnimalClassifierDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }
    }
}
