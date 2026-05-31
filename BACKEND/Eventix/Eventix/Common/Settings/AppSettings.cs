namespace Eventix.Common.Settings
{
    /// <summary>
    /// JWT configuration settings
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpireMinutes { get; set; } = 60;
        public int ResetTokenExpireMinutes { get; set; } = 5;
    }

    /// <summary>
    /// Database configuration settings
    /// </summary>
    public class DatabaseSettings
    {
        public const string SectionName = "ConnectionStrings";

        public string DB { get; set; } = string.Empty;
    }

    /// <summary>
    /// Application-wide settings
    /// </summary>
    public class AppSettings
    {
        public const string SectionName = "AppSettings";

        public string ApplicationName { get; set; } = "Eventix";
        public string Version { get; set; } = "1.0.0";
        public string Environment { get; set; } = "Development";
        public bool EnableSwagger { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
    }

    /// <summary>
    /// Security settings
    /// </summary>
    public class SecuritySettings
    {
        public const string SectionName = "Security";

        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
        public int PasswordMinLength { get; set; } = 6;
        public bool RequireEmailConfirmation { get; set; } = false;
        public bool EnableTwoFactorAuth { get; set; } = false;
    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "Email";

        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// File upload settings
    /// </summary>
    public class FileUploadSettings
    {
        public const string SectionName = "FileUpload";

        public string UploadPath { get; set; } = "uploads";
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
        public string[] AllowedFileTypes { get; set; } = { ".jpg", ".jpeg", ".png", ".gif" };
        public string[] AllowedMimeTypes { get; set; } = { "image/jpeg", "image/png", "image/gif" };
    }

    /// <summary>
    /// Pagination settings
    /// </summary>
    public class PaginationSettings
    {
        public const string SectionName = "Pagination";

        public int DefaultPageSize { get; set; } = 10;
        public int MaxPageSize { get; set; } = 100;
        public int DefaultPage { get; set; } = 1;
    }

    /// <summary>
    /// Cache settings
    /// </summary>
    public class CacheSettings
    {
        public const string SectionName = "Cache";

        public bool EnableCaching { get; set; } = true;
        public int DefaultCacheDurationMinutes { get; set; } = 30;
        public int EventsCacheDurationMinutes { get; set; } = 15;
        public int CategoriesCacheDurationMinutes { get; set; } = 60;
    }

    /// <summary>
    /// API settings
    /// </summary>
    public class ApiSettings
    {
        public const string SectionName = "Api";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v1";
        public bool EnableRateLimiting { get; set; } = true;
        public int RateLimitRequests { get; set; } = 100;
        public int RateLimitWindowMinutes { get; set; } = 1;
    }
}