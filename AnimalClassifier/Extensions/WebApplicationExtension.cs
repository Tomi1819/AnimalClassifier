namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;

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
    }
}
