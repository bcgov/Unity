using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
/// shape (WithFullDetailsAsync with typical date filters) shortly after startup so the cache
/// is warm before any user makes a request.
///
/// Warmup is split into two independent phases:
///   Phase 1 (model compilation) — always succeeds; no DB connection required.
///   Phase 2 (per-tenant DB round-trip) — iterates every tenant from the host database and
///     warms Npgsql's connection pool and PostgreSQL's query plan cache for each, ensuring no
///     tenant's first user pays the connection-establishment cost.
///
/// Note: EF Core 9 compiled models (dotnet ef dbcontext optimize) are not applicable here
/// because ABP's AbpDbContext applies dynamic query filters (ISoftDelete, IMultiTenant) that
/// cannot be baked into a design-time compiled model.
/// </summary>
public class GrantManagerDbWarmupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GrantManagerDbWarmupService> _logger;

    public GrantManagerDbWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<GrantManagerDbWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Allow ABP's OnApplicationInitialization and any module bootstrapping to fully complete before issuing queries. 
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

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

        _logger.LogInformation("[DbWarmup] Phase 2 — warming {TenantCount} tenant(s).", tenants.Count);

        var warmed = 0;
        foreach (var tenant in tenants)
        {
            if (stoppingToken.IsCancellationRequested) return;

            using var tenantScope = _scopeFactory.CreateScope();
            var currentTenant = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            var tenantUowManager = tenantScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using (currentTenant.Change(tenant.Id))
            {
                try
                {
                    using var uow = tenantUowManager.Begin(requiresNew: true, isTransactional: false);
                    var repository = tenantScope.ServiceProvider.GetRequiredService<IApplicationRepository>();

                    await repository.WithFullDetailsAsync(
                        skipCount: 0,
                        maxResultCount: 1,
                        sorting: null,
                        submittedFromDate: DateTime.UtcNow.AddMonths(-6),
                        submittedToDate: DateTime.UtcNow);

                    await uow.CompleteAsync();
                    warmed++;
                    _logger.LogDebug("[DbWarmup] Tenant '{TenantName}' ({TenantId}) warmed.", tenant.Name, tenant.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex,
                        "[DbWarmup] Tenant '{TenantName}' ({TenantId}) — DB round-trip skipped. " +
                        "Tenant database may not be accessible in this environment.",
                        tenant.Name, tenant.Id);
                }
            }
        }

        _logger.LogInformation("[DbWarmup] Phase 2 complete — {Warmed}/{Total} tenant(s) warmed.", warmed, tenants.Count);
    }
}
