using Hangfire.Common;
using Infrastructure.CentralDbSync;

namespace WebApi.Infrastructure.CentralDb;

public static class CentralDbSyncHangfireTypeResolver
{
    private const string JobTypeFullName = "Infrastructure.CentralDbSync.CentralDbSyncJobs";

    public static void Configure()
    {
        var defaultResolver = TypeHelper.CurrentTypeResolver;

        TypeHelper.CurrentTypeResolver = typeName =>
        {
            if (typeName != null && typeName.Contains(JobTypeFullName, StringComparison.Ordinal))
                return typeof(CentralDbSyncJobs);
            return defaultResolver(typeName);
        };
    }
}
