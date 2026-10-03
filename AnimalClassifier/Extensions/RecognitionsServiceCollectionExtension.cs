namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Core.Services.Helpers;
    using Microsoft.Extensions.ML;
    using Microsoft.ML;
    using static Constants.MessageConstants;
    using static Core.Constants.ConfigConstants;

    /// <summary>
    /// Recognising animals in what users upload, and everything read back from
    /// those recognitions: a user's history, the search and the statistics.
    /// </summary>
    public static class RecognitionsServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationRecognitions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IRecognitionService, RecognitionService>();
            services.AddScoped<IFileValidator, FileValidator>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalService, AnimalService>();
            services.AddSingleton<MLContext>();

            services.Configure<MLModelSettings>(configuration.GetSection(MLModel));

            var mlModelSettings = configuration.GetSection(MLModel).Get<MLModelSettings>();
            if (string.IsNullOrWhiteSpace(mlModelSettings?.Path))
            {
                throw new InvalidOperationException(MissingMLModelPath);
            }

            services.AddPredictionEnginePool<ImageData, ImagePrediction>()
                .FromFile(mlModelSettings.Path);

            return services;
        }
    }
}
