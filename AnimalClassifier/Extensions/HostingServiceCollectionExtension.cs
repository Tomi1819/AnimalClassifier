namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Hosting;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.KeyManagement;
    using Microsoft.AspNetCore.DataProtection.Repositories;
    using Microsoft.AspNetCore.DataProtection.XmlEncryption;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Options;
    using System.Net;

    /// <summary>
    /// What running anywhere but a developer's machine needs: keys that
    /// outlast a restart, the caller's own address from behind a proxy, and
    /// browsers held to HTTPS.
    /// </summary>
    public static class HostingServiceCollectionExtension
    {
        // Rather than the folder the app is deployed to, which the keys would
        // otherwise be tied to, so that a deployment elsewhere can read them.
        private const string ApplicationName = "AnimalClassifier";

        private const string MissingKeysPath =
            "DataProtection:KeysPath has to be set outside development, to a folder outside the app's own.";

        private const string UnknownProxyAddress = "ForwardedHeaders:KnownProxies has to hold IP addresses.";

        // Long enough that a browser seldom asks over HTTP at all.
        private static readonly TimeSpan HstsMaxAge = TimeSpan.FromDays(365);

        public static IServiceCollection AddApplicationHosting(this IServiceCollection services, IHostEnvironment environment)
        {
            services.AddApplicationDataProtection(environment);

            services.AddSettings<ForwardedHeadersSettings>()
                .Validate(settings => settings.KnownProxies.All(proxy => IPAddress.TryParse(proxy, out _)), UnknownProxyAddress);

            // A proxy on this machine is known without being named.
            services.AddOptions<ForwardedHeadersOptions>()
                .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                    foreach (var proxy in settings.Value.KnownProxies)
                    {
                        options.KnownProxies.Add(IPAddress.Parse(proxy));
                    }
                });

            services.AddHsts(options => options.MaxAge = HstsMaxAge);

            return services;
        }

        // Kept in a folder of their own, and encrypted there with the
        // machine's own key, so that a copy of the folder is of no use
        // elsewhere. Development leaves them where it always has.
        private static void AddApplicationDataProtection(this IServiceCollection services, IHostEnvironment environment)
        {
            services.AddDataProtection().SetApplicationName(ApplicationName);

            services.AddSettings<DataProtectionSettings>()
                .PostConfigure(settings =>
                {
                    if (!string.IsNullOrWhiteSpace(settings.KeysPath))
                    {
                        settings.KeysPath = Path.GetFullPath(settings.KeysPath, environment.ContentRootPath);
                    }
                })
                .Validate(settings => environment.IsDevelopment()
                    || (!string.IsNullOrWhiteSpace(settings.KeysPath) && environment.IsOutsideContentRoot(settings.KeysPath)),
                    MissingKeysPath);

            services.AddOptions<KeyManagementOptions>()
                .Configure<IOptions<DataProtectionSettings>, ILoggerFactory>((options, settings, loggerFactory) =>
                {
                    if (string.IsNullOrWhiteSpace(settings.Value.KeysPath))
                    {
                        return;
                    }

                    options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(settings.Value.KeysPath), loggerFactory);

                    if (OperatingSystem.IsWindows())
                    {
                        options.XmlEncryptor = new DpapiXmlEncryptor(protectToLocalMachine: true, loggerFactory);
                    }
                });
        }
    }
}
