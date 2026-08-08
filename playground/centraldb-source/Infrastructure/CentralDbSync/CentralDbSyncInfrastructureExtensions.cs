namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules.Acc;
using Application.Features.CentralDbSync.Config.Rules.Artwork;
using Application.Features.CentralDbSync.Config.Rules.Configs.CMP;
using Application.Features.CentralDbSync.Config.Rules.Configs.FabricRating;
using Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;
using Application.Features.CentralDbSync.Config.Rules.Configs;
using Application.Features.CentralDbSync.Config.Rules.Configs.Printing;
using Application.Features.CentralDbSync.Config.Rules.Configs.Sizes;
using Application.Features.CentralDbSync.Config.Rules.Configs.Trims;
using Application.Features.CentralDbSync.Config.Rules.Costing;
using Application.Features.CentralDbSync.Config.Rules.CRM;
using Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;
using Application.Features.CentralDbSync.Config.Rules.System;
using Application.Features.CentralDbSync.Config.Rules.IPO.Fabric;
using Application.Features.CentralDbSync.Config.Rules.IPO.Trim;
using Application.Features.CentralDbSync.Config.Rules.WH;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Services;
using Domain.Common;
using Infrastructure.CentralDbSync.Sql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class CentralDbSyncInfrastructureExtensions
{
    private static readonly string CentralDbPlaceholder =
        "Host=Placeholder_FeatureDisabled;Database=Placeholder;Username=placeholder;Password=placeholder;";

    public static IServiceCollection AddCentralDbSync(
        this IServiceCollection services,
        AppSettings appSettings,
        bool enabled)
    {
        var centralDbConnection = enabled
            ? appSettings.DatabaseSettings.CentralDbConnection
            : CentralDbPlaceholder;
        var erpConnection = appSettings.DatabaseSettings.ErpReplicateConnection;

        services.AddSingleton<IValueTransformerRegistry, NoOpValueTransformerRegistry>();
        services.AddSingleton<MappingRuleValidator>();
        services.AddSingleton<ITableMappingRuleCatalog, CrmPartnerMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, CmpOperationsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, UnitsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimGroupsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimTypesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, UnitConversionsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimKindsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TreatmentTypesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricKindsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WorkTypesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, StyleCategoriesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricTypesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, GarmentKindsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, SeasonsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, DropsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, ColoursMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, MachineAreasMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, MachinesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, ExchangeRatesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, CurrenciesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, ArtworkPositionsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricWeaveStructuresMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricWeaveParametersMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricYarnsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricYarnCompositionsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricColorsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricsSupplierRequestMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricPriceMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricOtherFeeTypesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoStylesFabricDevelopmentMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoStyleFabricsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoStylesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoStylesTrimsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PoTrimRatingMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimRatingMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimsCompositionsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimsMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, TrimPriceMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, CmpTimingMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, CmpGateMatrixMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, FabricConsumptionMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, ConsumptionMatrixMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WastageMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WastageMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, RateConfigMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, PrintAreaBandsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, SizesMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WashHeaderMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WashMatrixMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, GlobalConfigsToConfigMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, CompaniesConfigsToConfigMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, UsersMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WarehouseLocationsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, WarehousePalletsMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterGroupMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterGroupDetailMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterGroupReceivingMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterGroupInspectionRequestLotMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoFabricMasterGroupInspectionRequestAppearanceTestingMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoTrimMasterMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoTrimMasterGroupMappingRuleCatalog>();
        services.AddSingleton<ITableMappingRuleCatalog, IpoTrimMasterGroupDetailMappingRuleCatalog>();
        services.AddSingleton<IMappingRuleProvider, TableMappingRegistry>();
        services.AddSingleton<PredicateSqlBuilder>();
        services.AddSingleton<SqlServerSqlBuilder>();
        services.AddSingleton<UpsertSqlBuilder>();
        services.AddSingleton<IBootstrapConcurrencyManager, BootstrapConcurrencyManager>();
        services.AddSingleton<ICentralDbConnectionFactory>(
            _ => new NpgsqlConnectionFactory(centralDbConnection));

        // Register infrastructure implementations as their abstractions
        services.AddScoped<IBootstrapSnapshotReader>(sp =>
            new SqlServerGenericReader(
                erpConnection,
                sp.GetRequiredService<IMappingRuleProvider>(),
                sp.GetRequiredService<SqlServerSqlBuilder>(),
                sp.GetRequiredService<ILogger<SqlServerGenericReader>>()));
        services.AddScoped<IChangeTrackingReader>(sp =>
            new SqlServerGenericReader(
                erpConnection,
                sp.GetRequiredService<IMappingRuleProvider>(),
                sp.GetRequiredService<SqlServerSqlBuilder>(),
                sp.GetRequiredService<ILogger<SqlServerGenericReader>>()));

        services.AddScoped<ISyncBatchApplier>(sp =>
            new PostgresGenericApplier(
                centralDbConnection,
                sp.GetRequiredService<IMappingRuleProvider>(),
                sp.GetRequiredService<IValueTransformerRegistry>(),
                sp.GetRequiredService<UpsertSqlBuilder>(),
                sp.GetRequiredService<ILogger<PostgresGenericApplier>>()));
        services.AddScoped<ISyncCheckpointStore>(sp =>
            new PostgresSyncCheckpointStore(centralDbConnection));
        services.AddScoped<ITableSyncLock>(sp =>
            new PostgresTableSyncLock(centralDbConnection));
        services.AddScoped<ISyncRunLog>(sp =>
            new PostgresSyncRunLog(centralDbConnection));

        // Register request store, job scheduler, and Hangfire job-liveness probe.
        services.AddScoped<IBootstrapRequestStore>(sp =>
            new PostgresBootstrapRequestStore(centralDbConnection));
        services.AddScoped<IBootstrapJobScheduler, HangfireBootstrapJobScheduler>();
        services.AddScoped<IBootstrapJobStateChecker, HangfireBootstrapJobStateChecker>();

        // Register scalable bootstrap stores
        services.AddScoped<IBootstrapParentStore>(sp =>
            new PostgresBootstrapParentStore(centralDbConnection));
        services.AddScoped<IBootstrapChildStore>(sp =>
            new PostgresBootstrapChildStore(centralDbConnection));
        services.AddScoped<IBootstrapCtDispatchStore>(sp =>
            new PostgresBootstrapCtDispatchStore(centralDbConnection));

        // Register scalable bootstrap source reader (SQL Server)
        services.AddScoped<IStagedBootstrapSourceReader>(sp =>
            new SqlServerStagedBootstrapReader(
                erpConnection,
                sp.GetRequiredService<SqlServerSqlBuilder>(),
                sp.GetRequiredService<ILogger<SqlServerStagedBootstrapReader>>()));

        // Register typed staging store (PostgreSQL)
        services.AddScoped<ITypedBootstrapStagingStore>(sp =>
            new PostgresTypedBootstrapStagingStore(
                centralDbConnection,
                sp.GetRequiredService<IMappingRuleProvider>(),
                sp.GetRequiredService<ILogger<PostgresTypedBootstrapStagingStore>>()));

        // Register CT catch-up and final publisher
        services.AddScoped<IBootstrapCtCatchUpService, BootstrapCtCatchUpService>();
        services.AddScoped<IBootstrapFinalPublisher>(sp =>
            new PostgresBootstrapFinalPublisher(
                centralDbConnection,
                sp.GetRequiredService<ITypedBootstrapStagingStore>(),
                sp.GetRequiredService<ISyncConfigStore>(),
                sp.GetRequiredService<IBootstrapCtDispatchStore>(),
                sp.GetRequiredService<ILogger<PostgresBootstrapFinalPublisher>>()));

        // Register CT dispatch service
        services.AddScoped<IBootstrapCtDispatchService, BootstrapCtDispatchService>();

        // Register scalable application services
        services.AddScoped<BootstrapFailureService>();
        services.AddScoped<ScalableBootstrapCoordinator>();
        services.AddScoped<BootstrapChildService>();

        // Register monitor services
        services.AddScoped<IBootstrapDiagnosticEventStore>(sp =>
            new PostgresBootstrapDiagnosticEventStore(centralDbConnection));
        services.AddScoped<IBootstrapMonitorQueryService>(sp =>
            new PostgresBootstrapMonitorQueryService(
                centralDbConnection,
                sp.GetRequiredService<IBootstrapJobStateChecker>(),
                sp.GetRequiredService<IBootstrapDiagnosticEventStore>()));
        services.AddScoped<IBootstrapMonitorActionService, BootstrapMonitorActionService>();

        // Register application services
        services.AddSingleton<IBootstrapReconciliationPolicy, BootstrapReconciliationPolicy>();
        services.AddScoped<BootstrapSyncService>();
        services.AddScoped<BootstrapRequestService>();
        services.AddScoped<ChangeTrackingSyncService>();
        services.AddScoped<SyncOrchestrator>();

        // Register config store (runtime toggle)
        services.AddScoped<ISyncConfigStore>(sp =>
            new PostgresSyncConfigStore(centralDbConnection));

        services.AddScoped<ICentralDbSyncQueryService>(sp =>
            new PostgresCentralDbSyncQueryService(
                centralDbConnection,
                sp.GetRequiredService<IMappingRuleProvider>()));

        // CT health check — queries SQL Server sys.change_tracking_tables
        services.AddScoped<ISqlServerCtHealthCheck>(sp =>
            new SqlServerCtHealthCheck(
                erpConnection,
                sp.GetRequiredService<IMappingRuleProvider>()));

        return services;
    }
}
