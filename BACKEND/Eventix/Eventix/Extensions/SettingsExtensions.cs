using Eventix.Common.Settings;

namespace Eventix.Extensions
{
    /// <summary>
    /// Extension methods for configuring application settings
    /// </summary>
    public static class SettingsExtensions
    {
        /// <summary>
        /// Configure all application settings from configuration
        /// </summary>
        public static IServiceCollection ConfigureAppSettings(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure JWT settings
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            // Configure Database settings
            services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

            // Configure App settings
            services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

            // Configure Security settings
            services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));

            // Configure Email settings
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

            // Configure File Upload settings
            services.Configure<FileUploadSettings>(configuration.GetSection(FileUploadSettings.SectionName));

            // Configure Pagination settings
            services.Configure<PaginationSettings>(configuration.GetSection(PaginationSettings.SectionName));

            // Configure Cache settings
            services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

            // Configure API settings
            services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));

            return services;
        }

        /// <summary>
        /// Validate required configuration settings
        /// </summary>
        public static IServiceCollection ValidateSettings(this IServiceCollection services)
        {
            using var serviceProvider = services.BuildServiceProvider();

            // Validate JWT settings
            var jwtSettings = serviceProvider.GetService<IConfiguration>()?.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key))
            {
                throw new InvalidOperationException("JWT:Key is required in configuration");
            }
            if (string.IsNullOrEmpty(jwtSettings.Issuer))
            {
                throw new InvalidOperationException("JWT:Issuer is required in configuration");
            }
            if (string.IsNullOrEmpty(jwtSettings.Audience))
            {
                throw new InvalidOperationException("JWT:Audience is required in configuration");
            }

            // Validate Database settings
            var dbSettings = serviceProvider.GetService<IConfiguration>()?.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>();
            if (dbSettings == null || string.IsNullOrEmpty(dbSettings.DB))
            {
                throw new InvalidOperationException("ConnectionStrings:DB is required in configuration");
            }

            return services;
        }
    }
}