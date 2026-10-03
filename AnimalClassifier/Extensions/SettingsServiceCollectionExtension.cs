namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Common.Settings;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// How every area reads its settings. Each settings class names its own
    /// section and sets its rules with attributes, such as <c>[Required]</c>,
    /// so that what a setting must be is written beside the setting.
    /// </summary>
    public static class SettingsServiceCollectionExtension
    {
        /// <summary>
        /// Binds the settings to their section, and refuses to start the app
        /// while they break one of their rules. A missing setting is reported
        /// once, at startup, rather than by whichever request first needs it.
        /// </summary>
        public static OptionsBuilder<TSettings> AddSettings<TSettings>(this IServiceCollection services)
            where TSettings : class, ISettings =>
            services.AddOptions<TSettings>()
                    .BindConfiguration(TSettings.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

        /// <summary>
        /// The settings as configured, for a service that cannot even be
        /// registered without them. They are checked against their rules here,
        /// since nothing checks them before they are used.
        /// </summary>
        /// <exception cref="OptionsValidationException">
        /// When the settings break one of their rules.
        /// </exception>
        public static TSettings GetValidatedSettings<TSettings>(this IConfiguration configuration)
            where TSettings : class, ISettings, new()
        {
            var settings = configuration.GetSection(TSettings.SectionName).Get<TSettings>() ?? new TSettings();
            var result = new DataAnnotationValidateOptions<TSettings>(name: null).Validate(Options.DefaultName, settings);

            if (result.Failed)
            {
                throw new OptionsValidationException(Options.DefaultName, typeof(TSettings), result.Failures);
            }

            return settings;
        }
    }
}
