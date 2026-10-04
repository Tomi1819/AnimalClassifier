namespace AnimalClassifier.Tests.Admin
{
    using AnimalClassifier.Core.Admin;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Infrastructure.Data.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net.Http.Json;
    using static AnimalClassifier.Core.Identity.RoleConstants;

    /// <summary>
    /// Admin:Email, which names the first administrator as the app starts.
    /// What it does depends on whether there is an administrator already, so
    /// each test has an app and a database of its own.
    /// </summary>
    public class AdminBootstrapTests
    {
        private const string RegisterPath = "/api/auth/register";
        private const string Password = "secret-one";

        [Fact]
        public async Task AConfirmedAccount_IsMadeAnAdministrator_WhileThereIsNone()
        {
            await using var factory = new ApiFactory();
            var email = await RegisterAsync(factory, emailConfirmed: true);

            StartWithAdminEmail(factory, email);

            Assert.True(await IsAdministratorAsync(factory, email));
        }

        // Otherwise whoever registered the address before its owner would be
        // given the role.
        [Fact]
        public async Task AnAccountWhoseEmailIsNotConfirmed_IsNot()
        {
            await using var factory = new ApiFactory();
            var email = await RegisterAsync(factory, emailConfirmed: false);

            StartWithAdminEmail(factory, email);

            Assert.False(await IsAdministratorAsync(factory, email));
        }

        // Otherwise an administrator whose role was revoked would get it back
        // on the next start.
        [Fact]
        public async Task NoOne_IsMadeAnAdministrator_OnceThereIsOne()
        {
            await using var factory = new ApiFactory();
            await MakeAdministratorAsync(factory, await RegisterAsync(factory, emailConfirmed: true));
            var email = await RegisterAsync(factory, emailConfirmed: true);

            StartWithAdminEmail(factory, email);

            Assert.False(await IsAdministratorAsync(factory, email));
        }

        private static async Task<string> RegisterAsync(ApiFactory factory, bool emailConfirmed)
        {
            var email = $"{Guid.NewGuid():N}@example.test";

            (await factory.CreateClient().PostAsJsonAsync(RegisterPath, new RegisterRequest
            {
                Email = email,
                Password = Password
            })).EnsureSuccessStatusCode();

            if (emailConfirmed)
            {
                await ChangeUserAsync(factory, email, async (userManager, user) =>
                    await userManager.ConfirmEmailAsync(user, await userManager.GenerateEmailConfirmationTokenAsync(user)));
            }

            return email;
        }

        private static Task MakeAdministratorAsync(ApiFactory factory, string email) =>
            ChangeUserAsync(factory, email, (userManager, user) => userManager.AddToRoleAsync(user, Admin));

        private static async Task ChangeUserAsync(ApiFactory factory, string email,
            Func<UserManager<ApplicationUser>, ApplicationUser, Task<IdentityResult>> change)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            Assert.True((await change(userManager, (await userManager.FindByEmailAsync(email))!)).Succeeded);
        }

        // The setting is read as the app starts, which creating a client does.
        private static void StartWithAdminEmail(ApiFactory factory, string email)
        {
            using var app = factory.WithWebHostBuilder(builder =>
                builder.UseSetting(ApiFactory.Key<AdminSettings>(nameof(AdminSettings.Email)), email));

            using var client = app.CreateClient();
        }

        private static async Task<bool> IsAdministratorAsync(ApiFactory factory, string email)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            return await userManager.IsInRoleAsync((await userManager.FindByEmailAsync(email))!, Admin);
        }
    }
}
