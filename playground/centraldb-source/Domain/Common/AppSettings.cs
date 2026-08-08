namespace Domain.Common
{
    public class AppSettings
    {
        public string UAInternalToken { get; set; } = string.Empty;
        public int FailedLoginLitmit { get; set; } = 5;
        public required AppDatabaseSettings DatabaseSettings { get; set; }
        public required JwtSettings JwtSettings { get; set; }
        public required BaseUrl BaseUrls { get; set; }
        public required SwaggerSetting SwaggerSettings { get; set; }
        public required CacheSettings CacheSettings { get; set; }
        public required ObservabilityOptions ObservabilityOptions { get; set; }
        public HangfireSettings Hangfire { get; set; } = new();
        public ReportingSettings ReportingSettings { get; set; } = new();
    }

    public class SwaggerSetting
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class BaseUrl
    {
        public string DomainName { get; set; } = string.Empty;
        public string DomainUrl { get; set; } = string.Empty;
        public required string WebHubEndpointUrl { get; set; }
    }

    public class AppDatabaseSettings
    {
        public required string DefaultConnection { get; set; }
        public required string AzureRedisCacheUrl { get; set; }
        public string ErpReplicateConnection { get; set; } = string.Empty;
        public string ReportingConnection { get; set; } = string.Empty;
        public string CentralDbConnection { get; set; } = string.Empty;
    }

    public class JwtSettings
    {
        public string ValidAudience { get; set; } = "";
        public string ValidIssuer { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public int ExpireMinutes { get; set; } = 15;
        public int RefreshTokenExpiryTime { get; set; } = 25;
    }

    public class CacheSettings
    {
        public required string RedisCacheUrl { get; set; }
        public double SlidingExpiration { get; set; }
        public double AbsoluteExpirationRelativeToNow { get; set; }
    }

    public class ObservabilityOptions
    {
        public string ServiceName { get; set; } = default!;
        public string CollectorUrl { get; set; } = @"http://localhost:4317";

        public bool EnabledTracing { get; set; } = false;
        public bool EnabledMetrics { get; set; } = false;

        public Uri CollectorUri => new(this.CollectorUrl);
        public string OtlpLogsCollectorUrl => $"{this.CollectorUrl}/v1/logs";
    }

    // dormant: superseded by IFeatureAvailability presence detection
    public class HangfireSettings
    {
        public bool Enabled { get; set; } = false;
    }
    
    public class ReportingSettings
    {
        public string StorageRootPath { get; set; } = string.Empty;
        public ReportStorageSettings Storage { get; set; } = new();
        public ReportRealtimeSettings Realtime { get; set; } = new();
    }

    public class ReportStorageSettings
    {
        public string Provider { get; set; } = "Local";
        public string PathPrefix { get; set; } = "reports";
        public AzureBlobStorageSettings AzureBlob { get; set; } = new();
    }

    public class AzureBlobStorageSettings
    {
        public bool Enabled { get; set; } = false;
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "report-files";
        public bool CreateContainerIfNotExists { get; set; } = true;
        public bool DisableAzureDiagnostics { get; set; } = false;
    }
    
    // dormant: superseded by IFeatureAvailability presence detection
    public class ReportRealtimeSettings
    {
    }
}
