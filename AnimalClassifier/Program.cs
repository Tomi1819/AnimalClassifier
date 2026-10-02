using AnimalClassifier.Extensions;
using AnimalClassifier.Filters;
using static AnimalClassifier.Core.Constants.ConfigConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDbContext(builder.Configuration);

builder.Services.AddApplicationIdentity(builder.Configuration);

builder.Services.AddControllers(options => options.Filters.Add<DomainExceptionFilter>());

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

builder.Services.AddApplicationPasskeys(builder.Configuration);

builder.Services.AddApplicationEmail(builder.Configuration, builder.Environment);

builder.Services.AddApplicationRateLimiting(builder.Configuration);

builder.Services.AddApplicationCors(builder.Configuration);

var app = builder.Build();

await app.SeedRolesAsync();

app.UseHttpsRedirection();

app.UseApplicationUploads();

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// After authorization, so that a limit kept per account knows whose request it
// is, and a request refused for want of a session is not counted.
app.UseRateLimiter();

app.MapControllers();

app.Run();
