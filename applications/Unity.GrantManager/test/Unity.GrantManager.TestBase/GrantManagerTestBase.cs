using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Volo.Abp.Testing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Unity.GrantManager;

/* All test classes are derived from this class, directly or indirectly.
 */
public abstract class GrantManagerTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{    
    protected GrantManagerTestBase()
    {
    }

    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    public override void Dispose()
    {
        // ABP builds IConfiguration with reloadOnChange and SetBasePath, and nothing disposes the
        // resulting PhysicalFileProvider, so each test host leaks an appsettings.json file watcher
        // (an inotify instance on Linux, capped at 128 on CI runners). Disposing the root alone
        // does not release it because the source does not own the builder-created file provider.
        var configuration = ServiceProvider.GetService<IConfiguration>() as IConfigurationRoot;
        base.Dispose();
        if (configuration != null)
        {
            var fileProviders = configuration.Providers
                .OfType<FileConfigurationProvider>()
                .Select(p => p.Source.FileProvider)
                .OfType<IDisposable>()
                .Distinct()
                .ToList();
            (configuration as IDisposable)?.Dispose();
            fileProviders.ForEach(fp => fp.Dispose());
        }
        GC.SuppressFinalize(this);
    }

    protected virtual Task WithUnitOfWorkAsync(Func<Task> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task WithUnitOfWorkAsync(AbpUnitOfWorkOptions options, Func<Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(options);
        await action();

        await uow.CompleteAsync();
    }

    protected virtual Task<TResult> WithUnitOfWorkAsync<TResult>(Func<Task<TResult>> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task<TResult> WithUnitOfWorkAsync<TResult>(AbpUnitOfWorkOptions options, Func<Task<TResult>> func)
    {
        using var scope = ServiceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(options);
        var result = await func();
        await uow.CompleteAsync();
        return result;
    }

    protected virtual async Task<IdentityUser?> GetUserOrNullAsync(Guid id)
    {
        var userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        return await WithUnitOfWorkAsync(
            () => userRepository.FindAsync(id)
        );
    }

    protected virtual async Task<IdentityUser> GetUserAsync(Guid id)
    {
        var user = await GetUserOrNullAsync(id);
        return user ?? throw new EntityNotFoundException(typeof(IdentityUser), id);
    }
}
