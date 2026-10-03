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
        /// <summary>
        /// The controllers, and every way a failure is answered. Whatever the
        /// cause, the answer is a <c>{ message }</c> for the user:
        /// <list type="bullet">
        /// <item><description>
        /// a refusal from a service, by <see cref="DomainExceptionFilter"/>;
        /// </description></item>
        /// <item><description>
        /// a request the model binding refused, by
        /// <see cref="InvalidModelStateResponse"/>;
        /// </description></item>
        /// <item><description>
        /// anything else, by <see cref="UnexpectedExceptionHandler"/>.
        /// </description></item>
        /// </list>
        /// </summary>
        public static IServiceCollection AddApplicationApi(this IServiceCollection services)
        {
            services.AddControllers(options => options.Filters.Add<DomainExceptionFilter>())
                .ConfigureApiBehaviorOptions(options =>
                    options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);

            services.AddExceptionHandler<UnexpectedExceptionHandler>();

            // The exception handler middleware will not start without it,
            // though the handler above answers every exception first.
            services.AddProblemDetails();

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
