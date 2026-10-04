using AnimalClassifier.Cors;
using AnimalClassifier.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationHosting(builder.Environment);

builder.Services.AddApplicationPersistence(builder.Configuration);

builder.Services.AddApplicationEmail(builder.Configuration, builder.Environment);

builder.Services.AddApplicationStorage(builder.Environment);

builder.Services.AddApplicationFrontend();

builder.Services.AddApplicationIdentity();

builder.Services.AddApplicationAuthentication();

builder.Services.AddApplicationPasskeys();

builder.Services.AddApplicationRecognitions(builder.Configuration, builder.Environment);

builder.Services.AddApplicationAdmin();

builder.Services.AddApplicationApi();

builder.Services.AddApplicationRateLimiting();

builder.Services.AddApplicationCors();

var app = builder.Build();

await app.SeedRolesAsync();

// First, so that everything after it sees the caller's own address and scheme
// rather than a reverse proxy's.
app.UseForwardedHeaders();

// Next, so that a failure anywhere after it is answered in the same shape.
app.UseExceptionHandler();

app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

// After authorization, so that a limit kept per account knows whose request it
// is, and a request refused for want of a session is not counted.
app.UseRateLimiter();

app.MapControllers();

app.Run();
