namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Admin;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    public static class WebApplicationExtension
    {
        // The API answers with JSON, images and videos, never a page, so
        // nothing it sends may run a script, be read as another type, or be
        // shown inside another site's frame.
        private const string NoSniff = "nosniff";
        private const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

        /// <summary>
        /// Adds the headers every answer carries, the error ones included,
        /// whose headers are cleared on the way.
        /// </summary>
        public static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.Use((context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.XContentTypeOptions = NoSniff;
                    context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;

                    return Task.CompletedTask;
                });

                return next(context);
            });

            return app;
        }

        /// <summary>
        /// Creates the roles, and makes the account configured as Admin:Email an
        /// administrator while there is none, so that a new installation has
        /// someone to manage it. The account has to be registered and its email
        /// confirmed first, or whoever registered the address before its owner
        /// would be given the role. Once there is an administrator the setting
        /// does nothing, so one whose role was revoked does not get it back on
        /// the next start; removing it demotes no one.
        /// </summary>
        public static async Task SeedRolesAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in new[] { Admin, User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = scope.ServiceProvider.GetRequiredService<IOptions<AdminSettings>>().Value.Email;
            if (string.IsNullOrWhiteSpace(adminEmail) || (await userManager.GetUsersInRoleAsync(Admin)).Count > 0)
            {
                return;
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                app.Logger.LogWarning("No account is registered with the administrator email {AdminEmail}.", adminEmail);
                return;
            }

            if (!admin.EmailConfirmed)
            {
                app.Logger.LogWarning("The administrator email {AdminEmail} has not been confirmed yet.", adminEmail);
                return;
            }

            await userManager.AddToRoleAsync(admin, Admin);
        }
    }
}
