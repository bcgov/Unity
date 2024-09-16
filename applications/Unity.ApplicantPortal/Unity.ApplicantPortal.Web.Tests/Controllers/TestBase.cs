using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.ApplicantPortal.Data;

namespace Unity.ApplicantPortal.Web.Controllers.Tests;

public class TestBase
{
    #region Initialization
    private readonly DbContextOptions<AppDbContext> _contextOptions;
    protected readonly AppDbContext _dbContext;

    protected TestBase()
    {
        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AppTests")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new AppDbContext(_contextOptions);
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();
    }
    #endregion

    #region Common Methods
    protected static ILogger<T> InitializeLogger<T>()
    {
        var serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddDebug()).BuildServiceProvider();
        var factory = serviceProvider.GetService<ILoggerFactory>();
        return factory!.CreateLogger<T>();
    }
    #endregion
}
