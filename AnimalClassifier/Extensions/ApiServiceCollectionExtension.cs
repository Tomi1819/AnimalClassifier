namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Filters;
    using static Core.Constants.ConfigConstants;

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

        public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection(CorsAllowedOrigins).Get<string[]>()
                ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            return services;
        }
    }
}
