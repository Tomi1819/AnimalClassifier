using AnimalClassifier.Cors;
using AnimalClassifier.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationPersistence(builder.Configuration);

builder.Services.AddApplicationEmail(builder.Configuration, builder.Environment);

builder.Services.AddApplicationStorage(builder.Configuration, builder.Environment);

builder.Services.AddApplicationFrontend(builder.Configuration);

builder.Services.AddApplicationIdentity();

builder.Services.AddApplicationAuthentication(builder.Configuration);

builder.Services.AddApplicationPasskeys(builder.Configuration);

builder.Services.AddApplicationRecognitions(builder.Configuration);

builder.Services.AddApplicationAdmin();

builder.Services.AddApplicationApi();

builder.Services.AddApplicationRateLimiting(builder.Configuration);

builder.Services.AddApplicationCors(builder.Configuration);

var app = builder.Build();

await app.SeedRolesAsync();

app.UseHttpsRedirection();

app.UseApplicationUploads();

app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

// After authorization, so that a limit kept per account knows whose request it
// is, and a request refused for want of a session is not counted.
app.UseRateLimiter();

app.MapControllers();

app.Run();
