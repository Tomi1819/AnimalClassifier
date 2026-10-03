namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Admin;

    /// <summary>
    /// What administrators do to other users, and the record of it.
    /// </summary>
    public static class AdminServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationAdmin(this IServiceCollection services)
        {
            services.AddSettings<AdminSettings>();

            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
