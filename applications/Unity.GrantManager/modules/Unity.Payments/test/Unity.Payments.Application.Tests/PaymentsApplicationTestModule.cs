using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Unity.Payments.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Unity.Modules.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.Payments;

[DependsOn(
    typeof(PaymentsApplicationModule),
    typeof(PaymentsTestBaseModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
    )]
public class PaymentsApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();
  
        context.Services.AddAutofac(builder =>
        {
            builder.RegisterType<ResilientHttpRequest>().As<IResilientHttpRequest>();
        });

        context.Services.AddTransient<IResilientHttpRequest, ResilientHttpRequest>();

        var sqliteConnection = CreateDatabaseAndGetConnection();

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(abpDbContextConfigurationContext =>
            {
                abpDbContextConfigurationContext.DbContextOptions.UseSqlite(sqliteConnection);
            });
        });
    }

    private static SqliteConnection CreateDatabaseAndGetConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        new PaymentsDbContext(
            new DbContextOptionsBuilder<PaymentsDbContext>().UseSqlite(connection).Options
        ).GetService<IRelationalDatabaseCreator>().CreateTables();

        return connection;
    }
}
