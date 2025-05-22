namespace AnimalClassifier.Extensions
{
    using AnimalClassifier.Core.Configurations;
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Helpers;
    using AnimalClassifier.Core.Services;
    using AnimalClassifier.Infrastructure.Data;
    using AnimalClassifier.Infrastructure.Data.Common;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.ML;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.ML;
    using System;
    using System.Text;
    using static Core.Constants.ConfigConstants;
    using static Constants.MessageConstants;

    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(DefaultConnection)
                ?? throw new InvalidOperationException(MissingConnectionString);

            services.AddDbContext<AnimalClassifierDbContext>(options =>
                options.UseSqlServer(connectionString));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IRepository, Repository>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRecognitionService, RecognitionService>();
            services.AddScoped<IFileValidator, FileValidator>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IAnimalService, AnimalService>();
            services.AddSingleton<MLContext>();

            services.Configure<UploadSettings>(configuration.GetSection(FileUploadSettings));
            services.Configure<MLModelSettings>(configuration.GetSection(MLModel));
            services.Configure<JwtSettings>(configuration.GetSection(Jwt));

            var mlModelSettings = configuration.GetSection(MLModel).Get<MLModelSettings>();
            if (string.IsNullOrWhiteSpace(mlModelSettings?.Path))
            {
                throw new InvalidOperationException(MissingMLModelPath);
            }

            services.AddPredictionEnginePool<ImageData, ImagePrediction>()
                .FromFile(mlModelSettings.Path);

            return services;
        }

        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AnimalClassifierDbContext>();


            var jwtSettings = configuration.GetSection(Jwt).Get<JwtSettings>();

            if (string.IsNullOrEmpty(jwtSettings?.SecretKey))
            {
                throw new InvalidOperationException(MissingJwtSecurityKey);
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                    };
                });

            return services;
        }
    }
}
