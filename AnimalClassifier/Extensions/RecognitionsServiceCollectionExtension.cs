namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Core.Services.Helpers;
    using Microsoft.Extensions.ML;
    using Microsoft.ML;

    /// <summary>
    /// Recognising animals in what users upload, and everything read back from
    /// those recognitions: a user's history, the search and the statistics.
    /// </summary>
    public static class RecognitionsServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationRecognitions(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IRecognitionService, RecognitionService>();
            services.AddScoped<IFileValidator, FileValidator>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalService, AnimalService>();
            services.AddSingleton<MLContext>();

            // The pool is built from the model's path, so the settings are read
            // here rather than once the app starts.
            var modelSettings = configuration.GetValidatedSettings<MLModelSettings>();

            // Relative to the content root, as the uploads are, rather than to
            // whichever folder the app happened to be started from.
            services.AddPredictionEnginePool<ImageData, ImagePrediction>()
                .FromFile(Path.GetFullPath(modelSettings.Path, environment.ContentRootPath));

            return services;
        }
    }
}
