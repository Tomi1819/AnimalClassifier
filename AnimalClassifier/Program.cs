using AnimalClassifier.Extensions;
using static AnimalClassifier.Core.Constants.ConfigConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDbContext(builder.Configuration);

builder.Services.AddApplicationIdentity(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

builder.Services.AddApplicationEmail(builder.Configuration, builder.Environment);

builder.Services.AddApplicationRateLimiting(builder.Configuration);

builder.Services.AddApplicationCors(builder.Configuration);

var app = builder.Build();

await app.SeedRolesAsync();

app.UseHttpsRedirection();

app.UseApplicationUploads();

app.UseCors(CorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
