namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Cors;
    using AnimalClassifier.ErrorHandling;
    using Microsoft.AspNetCore.Cors.Infrastructure;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// How the API itself answers: its controllers, how a failure is put to
    /// the caller, and which origins a browser may call it from.
    /// </summary>
    public static class ApiServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationApi(this IServiceCollection services)
        {
            services.AddControllers(options => options.Filters.Add<DomainExceptionFilter>());

            return services;
        }

        public static IServiceCollection AddApplicationCors(this IServiceCollection services)
        {
            services.AddSettings<CorsSettings>();

            services.AddCors();

            services.AddOptions<CorsOptions>()
                .Configure<IOptions<CorsSettings>>((options, settings) =>
                    options.AddPolicy(CorsSettings.PolicyName, policy => policy
                        .WithOrigins(settings.Value.AllowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()));

            return services;
        }
    }
}
