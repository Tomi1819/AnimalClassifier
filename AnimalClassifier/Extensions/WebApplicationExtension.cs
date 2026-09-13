namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;
    using static Core.Constants.ConfigConstants;
    using static Core.Constants.RoleConstants;

    public static class WebApplicationExtension
    {
        public static WebApplication UseApplicationUploads(this WebApplication app)
        {
            var uploadSettings = app.Services.GetRequiredService<IOptions<UploadSettings>>().Value;

            Directory.CreateDirectory(uploadSettings.UploadPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadSettings.UploadPath),
                RequestPath = uploadSettings.RequestPath
            });

            return app;
        }

        /// <summary>
        /// Creates the roles, and makes the account configured as Admin:Email an
        /// administrator so that a new installation has someone to manage it. The
        /// account has to be registered first. Removing the setting later demotes
        /// no one.
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

            var adminEmail = app.Configuration[AdminEmail];
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return;
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                app.Logger.LogWarning("No account is registered with the administrator email {AdminEmail}.", adminEmail);
                return;
            }

            if (!await userManager.IsInRoleAsync(admin, Admin))
            {
                await userManager.AddToRoleAsync(admin, Admin);
            }
        }
    }
}
