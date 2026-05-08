using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Unity.GrantManager.Applications;

using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.GrantManager.EntityFrameworkCore;

/// <summary>
/// Background service that pre-warms the EF Core query pipeline after application startup.
///
/// On first use, EF Core performs three expensive one-time operations:
///   1. Model snapshot compilation  — GrantTenantDbContext.OnModelCreating (30+ entity types)
///   2. LINQ→SQL expression tree translation — especially costly for multi-JOIN includes
///   3. Npgsql connection pool establishment + PostgreSQL query plan caching
///
/// These costs are normally deferred to the first HTTP request, causing 6-8 second cold-start
/// latency for the GrantApplications DataTable. This service fires the most expensive query
/// shape (GetApplicationListRecordsAsync with typical date filters) shortly after startup so the
/// cache is warm before any user makes a request.
///
/// Warmup is split into two independent phases:
///   Phase 1 (model compilation) — always succeeds; no DB connection required.
///   Phase 2 (per-tenant DB round-trip) — iterates tenants from the host database and warms
///     Npgsql's connection pool and PostgreSQL's query plan cache for each.
///
/// Phase 2 behaviour is configurable via <see cref="DbWarmupOptions"/> (appsettings "DbWarmup" section):
///   IsPhase2Enabled      — set false to skip Phase 2 entirely (default: true).
///   MaxTenants           — cap the number of tenants warmed; 0 = unlimited (default: 0).
///   Phase2TimeoutSeconds — abandon Phase 2 after N seconds; 0 = no timeout (default: 0).
///
/// </summary>
public class GrantManagerDbWarmupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GrantManagerDbWarmupService> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DbWarmupOptions _options;

    public GrantManagerDbWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<GrantManagerDbWarmupService> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<DbWarmupOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the host has fully started so ABP module initialization and startup hooks
        // are complete before issuing any warmup queries.
        if (!_hostApplicationLifetime.ApplicationStarted.IsCancellationRequested)
        {
            var applicationStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var applicationStartedRegistration = _hostApplicationLifetime.ApplicationStarted.Register(
                static state => ((TaskCompletionSource)state!).TrySetResult(),
                applicationStartedTcs);
            using var cancellationRegistration = stoppingToken.Register(
                static state => ((TaskCompletionSource)state!).TrySetCanceled(),
                applicationStartedTcs);

            await applicationStartedTcs.Task;
        }

        if (stoppingToken.IsCancellationRequested) return;

        _logger.LogInformation("[DbWarmup] Starting EF Core query pipeline warmup.");

        // Step 1: Model
        // Accessing dbContext.Model forces EF Core to run OnModelCreating synchronously.
        // This is a pure in-process operation; no DB connection is opened. 
        using (var phase1Scope = _scopeFactory.CreateScope())
        {
            var unitOfWorkManager = phase1Scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            try
            {
                using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                var dbContextProvider = phase1Scope.ServiceProvider
                    .GetRequiredService<IDbContextProvider<GrantTenantDbContext>>();
                var dbContext = await dbContextProvider.GetDbContextAsync();

                // Accessing Model triggers OnModelCreating if not yet compiled.
                // The result is cached for the lifetime of the application.
                _ = dbContext.Model;

                await uow.CompleteAsync();
                _logger.LogInformation("[DbWarmup] Phase 1 complete — EF Core model compiled.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DbWarmup] Phase 1 (model compilation) failed — this is unexpected.");
            }
        }

        // Step 2: Per-tenant DB connection + PostgreSQL query plan warmup
        // Enumerates all tenants (ITenantRepository -> GrantManagerDbContext -> accessible without an active tenant scope).
        // Foreach tenant, opens a new DI scope, activates the tenant via ICurrentTenant.Change, and issues a Take(1) query so that:
        //   - Opens and pools a connection to that tenant's database
        //   - PostgreSQL parses and caches the parameterised execution plan for the query shape
        //   - EFCore's compiled query cache is populated for this tenant
        // Each tenant is isolated in its own scope to prevent UoW state from leaking between tenants.
        // Uses GetApplicationListRecordsAsync — the same optimized projected query the DataTable endpoint calls.
        if (!_options.IsPhase2Enabled)
        {
            _logger.LogInformation("[DbWarmup] Phase 2 disabled via configuration — skipping per-tenant warmup.");
            return;
        }

        IReadOnlyList<Tenant> tenants;

        using (var tenantListScope = _scopeFactory.CreateScope())
        {
            var tenantUowManager = tenantListScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            try
            {
                using var uow = tenantUowManager.Begin(requiresNew: true, isTransactional: false);
                var tenantRepository = tenantListScope.ServiceProvider.GetRequiredService<ITenantRepository>();
                tenants = await tenantRepository.GetListAsync();
                await uow.CompleteAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DbWarmup] Phase 2 — could not retrieve tenant list from host database. Skipping per-tenant warmup.");
                return;
            }
        }

        if (tenants.Count == 0)
        {
            _logger.LogDebug("[DbWarmup] Phase 2 — no tenants found in host database. Skipping per-tenant DB warmup.");
            return;
        }

        // Apply MaxTenants cap
        var tenantsToWarm = _options.MaxTenants > 0
            ? tenants.Take(_options.MaxTenants).ToList()
            : (IReadOnlyList<Tenant>)tenants;

        if (_options.MaxTenants > 0 && tenants.Count > _options.MaxTenants)
        {
            _logger.LogInformation(
                "[DbWarmup] Phase 2 — capped at {MaxTenants} of {TotalTenants} tenant(s) (MaxTenants setting).",
                _options.MaxTenants, tenants.Count);
        }

        _logger.LogInformation("[DbWarmup] Phase 2 — warming {TenantCount} tenant(s).", tenantsToWarm.Count);

        // Apply Phase2TimeoutSeconds — link a deadline token with stoppingToken
        using var phase2Cts = _options.Phase2TimeoutSeconds > 0
            ? CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)
            : null;
        if (phase2Cts != null)
        {
            phase2Cts.CancelAfter(TimeSpan.FromSeconds(_options.Phase2TimeoutSeconds));
            _logger.LogDebug("[DbWarmup] Phase 2 — timeout set to {Seconds}s.", _options.Phase2TimeoutSeconds);
        }
        var phase2Token = phase2Cts?.Token ?? stoppingToken;

        var warmed = 0;
        foreach (var tenant in tenantsToWarm)
        {
            if (phase2Token.IsCancellationRequested)
            {
                // Distinguish between a Phase 2 timeout and a host shutdown
                if (!stoppingToken.IsCancellationRequested)
                    _logger.LogInformation(
                        "[DbWarmup] Phase 2 — timeout reached after {Warmed}/{Total} tenant(s).",
                        warmed, tenantsToWarm.Count);
                return;
            }

            using var tenantScope = _scopeFactory.CreateScope();
            var currentTenant = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            var tenantUowManager = tenantScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using (currentTenant.Change(tenant.Id))
            {
                try
                {
                    using var uow = tenantUowManager.Begin(requiresNew: true, isTransactional: false);
                    var repository = tenantScope.ServiceProvider.GetRequiredService<IApplicationRepository>();

                    await repository.GetApplicationListRecordsAsync(
                        skipCount: 0,
                        maxResultCount: 1,
                        sorting: null,
                        submittedFromDate: DateTime.UtcNow.AddMonths(-6),
                        submittedToDate: DateTime.UtcNow);

                    await uow.CompleteAsync();
                    warmed++;
                    _logger.LogDebug("[DbWarmup] Tenant '{TenantName}' ({TenantId}) warmed.", tenant.Name, tenant.Id);
                }
                catch (OperationCanceledException) when (phase2Token.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex,
                        "[DbWarmup] Tenant '{TenantName}' ({TenantId}) — DB round-trip skipped. " +
                        "Tenant database may not be accessible in this environment.",
                        tenant.Name, tenant.Id);
                }
            }
        }

        _logger.LogInformation("[DbWarmup] Phase 2 complete — {Warmed}/{Total} tenant(s) warmed.", warmed, tenantsToWarm.Count);
    }
}