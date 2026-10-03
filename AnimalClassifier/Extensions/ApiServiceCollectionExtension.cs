namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Cors;
    using AnimalClassifier.Filters;

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
            var allowedOrigins = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins
                ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(CorsSettings.PolicyName, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            return services;
        }
    }
}
