namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json;
    using static AnimalClassifier.Core.Constants.ConfigConstants;
    using static AnimalClassifier.Core.Constants.MessageConstants;
    using static AnimalClassifier.Core.Constants.RoleConstants;
    using static AnimalClassifier.Core.Services.Helpers.SecurityAlertEmail;

    public class AccountControllerTests : IClassFixture<ApiFactory>
    {
        private const string Password = "secret1";
        private const string NewPassword = "secret2";
        private const string WrongPassword = "not-the-password";
        private const string AccountPath = "/api/account";
        private const string ChangeNamePath = "/api/account/name";
        private const string ChangePasswordPath = "/api/account/change-password";
        private const string SignOutOtherSessionsPath = "/api/account/sign-out-other-sessions";
        private const string ExportPath = "/api/account/export";
        private const string HistoryPath = "/api/upload/history";

        // The archive's layout, which is what a user reading their copy relies on.
        private const string AccountEntry = "account.json";
        private const string RecognitionsEntry = "recognitions.json";
        private const string UploadsFolder = "uploads/";
        private const string UploadedFileEntry = "uploads/cat.jpg";

        private readonly ApiFactory factory;

        public AccountControllerTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task GetProfile_WithoutSigningIn_IsRefused()
        {
            var response = await factory.CreateClient().GetAsync(AccountPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_ReturnsTheAccountsDetails()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var profile = await client.GetFromJsonAsync<AccountProfile>(AccountPath);

            Assert.Equal(account.FullName, profile!.FullName);
            Assert.Equal(account.Email, profile.Email);
        }

        // Without its "Z", a browser would read the date as local time.
        [Fact]
        public async Task GetProfile_SendsTheRegistrationDateAsUtc()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var profile = await client.GetFromJsonAsync<AccountProfile>(AccountPath);

            Assert.Equal(DateTimeKind.Utc, profile!.DateRegistered.Kind);
        }

        [Fact]
        public async Task ChangeName_WithoutSigningIn_IsRefused()
        {
            var response = await ChangeNameAsync(factory.CreateClient(), "Jane Goodall");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // The letters stay as typed, which registration would have turned into
        // "Mcdonald"; only the spacing is tidied.
        [Fact]
        public async Task ChangeName_KeepsTheLettersAsTyped()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangeNameAsync(client, "  Jane   McDonald ");
            var profile = await response.Content.ReadFromJsonAsync<AccountProfile>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Jane McDonald", profile!.FullName);
            Assert.Equal("Jane McDonald", (await client.GetFromJsonAsync<AccountProfile>(AccountPath))!.FullName);
        }

        [Fact]
        public async Task ChangeName_KeepsTheSessionGoing()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await ChangeNameAsync(client, "Jane Goodall")).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ChangeName_ToABlankName_IsABadRequest(string blankName)
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangeNameAsync(client, blankName);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(EmptyFullName, await response.Content.ReadAsStringAsync());
            Assert.Equal(account.FullName, (await client.GetFromJsonAsync<AccountProfile>(AccountPath))!.FullName);
        }

        [Fact]
        public async Task ChangeName_ToATooLongName_IsABadRequest()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangeNameAsync(client, new string('a', 101));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(account.FullName, (await client.GetFromJsonAsync<AccountProfile>(AccountPath))!.FullName);
        }

        [Fact]
        public async Task ChangePassword_WithoutSigningIn_IsRefused()
        {
            var response = await ChangePasswordAsync(factory.CreateClient(), Password, NewPassword);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithTheCurrentPassword_ChangesIt()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, NewPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_EndsTheOtherSessions()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await ChangePasswordAsync(client, Password, NewPassword)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_AnswersWithATokenThatKeepsTheCallerSignedIn()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, NewPassword);
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.Equal(HttpStatusCode.OK, (await WithToken(login!.Token).GetAsync(HistoryPath)).StatusCode);
        }

        // Anything but a bad request would be taken by the frontend as the
        // session itself having ended.
        [Fact]
        public async Task ChangePassword_WithAWrongCurrentPassword_IsABadRequest()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, WrongPassword, NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(IncorrectCurrentPassword, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPasswords_LocksTheAccount()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            await GuessUntilLockedOutAsync(client);

            var response = await LogInAsync(account.Email, Password);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        // Otherwise the lockout would stop nothing: guessing would carry on
        // here until the right password got through.
        [Fact]
        public async Task ChangePassword_WhileLocked_RefusesEvenTheRightPassword()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            await GuessUntilLockedOutAsync(client);

            var response = await ChangePasswordAsync(client, Password, NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(LockedOutAccount, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ChangePassword_EmailsASecurityAlert()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await ChangePasswordAsync(client, Password, NewPassword)).EnsureSuccessStatusCode();

            Assert.Contains(PasswordChangedSubject, factory.Emails.SubjectsSentTo(account.Email));
        }

        [Fact]
        public async Task ChangePassword_ThatIsRefused_EmailsNothing()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            await ChangePasswordAsync(client, WrongPassword, NewPassword);

            Assert.False(factory.Emails.AnySentTo(account.Email));
        }

        [Fact]
        public async Task ChangePassword_WithATooShortPassword_IsRefused()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await ChangePasswordAsync(client, Password, "abc");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_WithoutSigningIn_IsRefused()
        {
            var response = await SignOutOtherSessionsAsync(factory.CreateClient());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_EndsTheOtherSessions()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
        }

        // The caller's old token goes with the rest, which is why the answer
        // carries a new one.
        [Fact]
        public async Task SignOutOtherSessions_EndsTheCallersOldToken()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_AnswersWithATokenThatKeepsTheCallerSignedIn()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await SignOutOtherSessionsAsync(client);
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.Equal(HttpStatusCode.OK, (await WithToken(login!.Token).GetAsync(HistoryPath)).StatusCode);
        }

        [Fact]
        public async Task SignOutOtherSessions_EmailsASecurityAlert()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Contains(OtherSessionsSignedOutSubject, factory.Emails.SubjectsSentTo(account.Email));
        }

        [Fact]
        public async Task SignOutOtherSessions_LeavesThePasswordAsItWas()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            (await SignOutOtherSessionsAsync(client)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task ExportData_WithoutSigningIn_IsRefused()
        {
            var response = await factory.CreateClient().GetAsync(ExportPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ExportData_AnswersWithAZipToDownload()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await client.GetAsync(ExportPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        }

        [Fact]
        public async Task ExportData_ContainsTheAccount()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            using var archive = await ExportDataAsync(client);

            var exported = await ReadJsonEntryAsync<ExportedAccount>(archive, AccountEntry);
            Assert.Equal(account.FullName, exported.FullName);
            Assert.Equal(account.Email, exported.Email);
        }

        // Cleared recognitions are still kept, and counted by the statistics,
        // so the copy has to include them to be complete.
        [Fact]
        public async Task ExportData_ContainsClearedRecognitionsToo()
        {
            var account = await RegisterAsync();
            await AddRecognitionAsync(account.UserId, isDeleted: false);
            await AddRecognitionAsync(account.UserId, isDeleted: true);
            var client = await SignInAsync(account.Email, Password);

            using var archive = await ExportDataAsync(client);

            var recognitions = await ReadJsonEntryAsync<List<ExportedRecognition>>(archive, RecognitionsEntry);
            Assert.Equal(2, recognitions.Count);
            Assert.Single(recognitions, r => r.IsCleared);
        }

        [Fact]
        public async Task ExportData_ContainsTheUploadedFiles()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            using var archive = await ExportDataAsync(client);

            var entry = archive.GetEntry(UploadedFileEntry);
            Assert.NotNull(entry);
            await using var exported = new MemoryStream();
            await using (var contents = await entry.OpenAsync())
            {
                await contents.CopyToAsync(exported);
            }
            Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(uploadDirectory, "cat.jpg")), exported.ToArray());
        }

        [Fact]
        public async Task ExportData_PointsEachRecognitionAtItsFile()
        {
            var account = await RegisterAsync();
            await AddRecognitionAsync(account.UserId, isDeleted: false);
            await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            using var archive = await ExportDataAsync(client);

            var recognition = Assert.Single(await ReadJsonEntryAsync<List<ExportedRecognition>>(archive, RecognitionsEntry));
            Assert.Equal(UploadedFileEntry, recognition.File);
            Assert.NotNull(archive.GetEntry(recognition.File));
        }

        [Fact]
        public async Task ExportData_LeavesOutOtherAccounts()
        {
            var other = await RegisterAsync();
            await AddRecognitionAsync(other.UserId, isDeleted: false);
            await AddUploadAsync(other.UserId);
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            using var archive = await ExportDataAsync(client);

            Assert.Empty(await ReadJsonEntryAsync<List<ExportedRecognition>>(archive, RecognitionsEntry));
            Assert.DoesNotContain(archive.Entries, e => e.FullName.StartsWith(UploadsFolder));
        }

        [Fact]
        public async Task ExportData_BeyondTheLimit_IsRefused()
        {
            using var limited = WithDataExportLimit(1);
            var account = await RegisterAsync();
            var client = await SignInAsync(limited, account.Email);

            var allowed = await client.GetAsync(ExportPath);
            var refused = await client.GetAsync(ExportPath);

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        // Kept per account, so that people behind one address, such as a
        // household or an office, do not use up each other's exports.
        [Fact]
        public async Task ExportData_BeyondAnotherAccountsLimit_IsAllowed()
        {
            using var limited = WithDataExportLimit(1);
            var other = await SignInAsync(limited, (await RegisterAsync()).Email);
            var client = await SignInAsync(limited, (await RegisterAsync()).Email);

            (await other.GetAsync(ExportPath)).EnsureSuccessStatusCode();
            var response = await client.GetAsync(ExportPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_WithoutSigningIn_IsRefused()
        {
            var response = await DeleteAccountAsync(factory.CreateClient(), Password);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_WithThePassword_DeletesIt()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, Password);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_EndsEverySession()
        {
            var account = await RegisterAsync();
            var elsewhere = await SignInAsync(account.Email, Password);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await elsewhere.GetAsync(HistoryPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(HistoryPath)).StatusCode);
        }

        // As with changing the password, anything but a bad request would be
        // taken by the frontend as the session itself having ended.
        [Fact]
        public async Task DeleteAccount_WithAWrongPassword_IsABadRequest()
        {
            var account = await RegisterAsync();
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, WrongPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(IncorrectCurrentPassword, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_ForAnAdministrator_IsRefused()
        {
            var account = await RegisterAsync();
            await MakeAdministratorAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            var response = await DeleteAccountAsync(client, Password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(AdministratorAccountDeletion, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        // Cleared recognitions only leave the owner's history, so they have to
        // go as well for the account to leave nothing behind.
        [Fact]
        public async Task DeleteAccount_RemovesTheRecognitions()
        {
            var account = await RegisterAsync();
            await AddRecognitionAsync(account.UserId, isDeleted: false);
            await AddRecognitionAsync(account.UserId, isDeleted: true);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();
            Assert.False(await context.AnimalRecognitionLogs.AnyAsync(l => l.UserId == account.UserId));
        }

        [Fact]
        public async Task DeleteAccount_RemovesTheUploadedFiles()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            Assert.False(Directory.Exists(uploadDirectory));
        }

        // The account is gone by the time its files are removed, so one that
        // is still open elsewhere must not be reported as a failed deletion.
        [Fact]
        public async Task DeleteAccount_WithAFileThatCannotBeRemoved_StillDeletesIt()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            HttpResponseMessage response;
            await using (File.Open(Path.Combine(uploadDirectory, "cat.jpg"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                response = await DeleteAccountAsync(client, Password);
            }

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync(account.Email, Password)).StatusCode);
        }

        // Files cannot be brought back the way the database rows can, so a
        // refused request must not touch them.
        [Fact]
        public async Task DeleteAccount_WithAWrongPassword_KeepsTheUploadedFiles()
        {
            var account = await RegisterAsync();
            var uploadDirectory = await AddUploadAsync(account.UserId);
            var client = await SignInAsync(account.Email, Password);

            await DeleteAccountAsync(client, WrongPassword);

            Assert.True(Directory.Exists(uploadDirectory));
        }

        [Fact]
        public async Task DeleteAccount_KeepsTheAuditLogEntriesAboutIt()
        {
            var admin = await RegisterAsync();
            await MakeAdministratorAsync(admin.UserId);
            var adminClient = await SignInAsync(admin.Email, Password);
            var account = await RegisterAsync();
            (await adminClient.PostAsync($"/api/admin/users/{account.UserId}/lock", null)).EnsureSuccessStatusCode();
            (await adminClient.PostAsync($"/api/admin/users/{account.UserId}/unlock", null)).EnsureSuccessStatusCode();
            var client = await SignInAsync(account.Email, Password);

            (await DeleteAccountAsync(client, Password)).EnsureSuccessStatusCode();

            var log = await adminClient.GetFromJsonAsync<PagedResult<AdminAuditLogItem>>("/api/admin/audit");
            var entries = log!.Items.Where(e => e.AdminEmail == admin.Email).ToList();
            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.Equal(DeletedUser, e.UserEmail));
        }

        private async Task GuessUntilLockedOutAsync(HttpClient client)
        {
            var maxFailedAccessAttempts = factory.Services
                .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

            for (var attempt = 0; attempt < maxFailedAccessAttempts; attempt++)
            {
                await ChangePasswordAsync(client, WrongPassword, NewPassword);
            }
        }

        private static Task<HttpResponseMessage> ChangeNameAsync(HttpClient client, string fullName) =>
            client.PutAsJsonAsync(ChangeNamePath, new ChangeNameRequest { FullName = fullName });

        private static Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string currentPassword, string newPassword) =>
            client.PostAsJsonAsync(ChangePasswordPath, new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

        private static Task<HttpResponseMessage> SignOutOtherSessionsAsync(HttpClient client) =>
            client.PostAsync(SignOutOtherSessionsPath, content: null);

        // HttpClient has no DeleteAsJsonAsync, and the password goes in the body.
        private static Task<HttpResponseMessage> DeleteAccountAsync(HttpClient client, string password) =>
            client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, AccountPath)
            {
                Content = JsonContent.Create(new DeleteAccountRequest { Password = password })
            });

        private static async Task<ZipArchive> ExportDataAsync(HttpClient client)
        {
            var response = await client.GetAsync(ExportPath);
            response.EnsureSuccessStatusCode();

            return new ZipArchive(await response.Content.ReadAsStreamAsync());
        }

        private static async Task<T> ReadJsonEntryAsync<T>(ZipArchive archive, string entryName)
        {
            await using var entry = await archive.GetEntry(entryName)!.OpenAsync();

            return (await JsonSerializer.DeserializeAsync<T>(entry, JsonSerializerOptions.Web))!;
        }

        // Lowered, so that a test reaches the limit in one export rather than
        // several.
        private WebApplicationFactory<Program> WithDataExportLimit(int permitLimit) =>
            factory.WithWebHostBuilder(builder =>
                builder.UseSetting($"{RateLimiting}:DataExportPermitLimit", permitLimit.ToString()));

        // The copy of the app shares the database and the signing key, so the
        // token the usual one issues is good for it as well.
        private async Task<HttpClient> SignInAsync(WebApplicationFactory<Program> app, string email)
        {
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                (await SignInAsync(email, Password)).DefaultRequestHeaders.Authorization;

            return client;
        }

        private async Task MakeAdministratorAsync(string userId)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await userManager.AddToRoleAsync((await userManager.FindByIdAsync(userId))!, Admin);
        }

        // Added directly, since uploading would need the recognition model.
        private async Task AddRecognitionAsync(string userId, bool isDeleted)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalClassifierDbContext>();

            context.AnimalRecognitionLogs.Add(new AnimalRecognitionLog
            {
                AnimalName = "Cat",
                ImagePath = $"/uploads/{userId}/cat.jpg",
                DateRecognized = DateTime.UtcNow,
                UserId = userId,
                IsDeleted = isDeleted
            });
            await context.SaveChangesAsync();
        }

        /// <returns>The directory the user's uploads are kept in.</returns>
        private async Task<string> AddUploadAsync(string userId)
        {
            var uploadPath = factory.Services.GetRequiredService<IOptions<UploadSettings>>().Value.UploadPath;
            var userDirectory = Path.Combine(uploadPath, userId);

            Directory.CreateDirectory(userDirectory);
            await File.WriteAllBytesAsync(Path.Combine(userDirectory, "cat.jpg"), [0xFF, 0xD8]);

            return userDirectory;
        }

        private Task<HttpResponseMessage> LogInAsync(string email, string password) =>
            factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LogInRequest
            {
                Email = email,
                Password = password
            });

        private async Task<HttpClient> SignInAsync(string email, string password)
        {
            var response = await LogInAsync(email, password);
            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            return WithToken(login!.Token);
        }

        private HttpClient WithToken(string token)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private async Task<RegisterResponse> RegisterAsync()
        {
            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
            {
                FullName = "Test User",
                Email = UniqueEmail(),
                Password = Password
            });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        }

        private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.test";
    }
}
