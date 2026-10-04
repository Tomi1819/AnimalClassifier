namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Classification.Models;
    using AnimalClassifier.Core.Recognitions.History;
    using AnimalClassifier.Core.Recognitions.Media;
    using AnimalClassifier.Core.Recognitions.Search;
    using AnimalClassifier.Core.Recognitions.Statistics;
    using AnimalClassifier.Core.Recognitions.Uploads;
    using Microsoft.Extensions.ML;

    /// <summary>
    /// Recognising animals in what users upload, and everything read back from
    /// those recognitions: a user's history, the search and the statistics.
    /// </summary>
    public static class RecognitionsServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationRecognitions(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddScoped<IMediaLinkService, MediaLinkService>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IRecognitionHistoryService, RecognitionHistoryService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalSearchService, AnimalSearchService>();

            // Both are safe to share: the classifier takes an engine from the
            // pool for each image, and the sampler keeps nothing between videos.
            services.AddSingleton<IImageClassifier, MLImageClassifier>();
            services.AddSingleton<IVideoFrameSampler, VideoFrameSampler>();

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
