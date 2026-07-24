using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Unity.AI.RateLimit;
using Volo.Abp;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.AI.RateLimit;

public class AIRateLimiterTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IDistributedCache _cache = new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IConfiguration _configuration;

    public AIRateLimiterTests()
    {
        _currentUser.Id.Returns(_userId);
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Generation:CooldownSeconds"] = "60"
            }).Build();
    }

    private AIRateLimiter NewLimiter(params IAIGenerationActivityProvider[] activityProviders) =>
        new(_cache, _currentUser, _configuration, new TestDistributedLockProvider(), activityProviders);

    [Fact]
    public async Task GetStateAsync_Returns_Zero_When_NoCooldown()
    {
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
        state.IsGenerating.ShouldBeFalse();
    }

    [Fact]
    public async Task GetStateAsync_Returns_Generating_When_ActivityProvider_HasActiveGeneration()
    {
        var activityProvider = Substitute.For<IAIGenerationActivityProvider>();
        activityProvider.HasActiveGenerationAsync().Returns(true);

        var state = await NewLimiter(activityProvider).GetStateAsync();

        state.RetryAfterSeconds.ShouldBe(0);
        state.IsGenerating.ShouldBeTrue();
    }

    [Fact]
    public async Task GetStateAsync_Does_Not_Acquire_Cooldown_Lock()
    {
        var lockProvider = new CountingDistributedLockProvider();
        var limiter = new AIRateLimiter(_cache, _currentUser, _configuration, lockProvider, []);

        await limiter.GetStateAsync();

        lockProvider.CreatedLockCount.ShouldBe(1);
    }

    [Fact]
    public async Task EnsureAsync_AllowsThrough_When_NoCooldown()
    {
        await NewLimiter().EnsureAsync();
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
    }

    [Fact]
    public async Task StampAsync_Starts_Cooldown()
    {
        var limiter = NewLimiter();
        await limiter.StampAsync();
        var state = await limiter.GetStateAsync();
        state.RetryAfterSeconds.ShouldBeInRange(1, 60);
    }

    [Fact]
    public async Task EnsureAsync_Throws_When_Cooldown_Exists()
    {
        var limiter = NewLimiter();
        await limiter.StampAsync();

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => limiter.EnsureAsync());
        ex.Message.ShouldContain("rate limited");
        ex.Message.ShouldMatch(@"\d+ second");
    }

    [Fact]
    public async Task GetStateAsync_Returns_Zero_For_AnonymousUser()
    {
        _currentUser.Id.Returns((Guid?)null);
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
    }

    [Fact]
    public async Task EnsureAsync_IsNoOp_For_AnonymousUser()
    {
        _currentUser.Id.Returns((Guid?)null);
        await NewLimiter().EnsureAsync(); // Should not throw.
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
    }

    [Fact]
    public async Task DifferentUsers_Have_IndependentCooldowns()
    {
        await NewLimiter().StampAsync();

        _currentUser.Id.Returns(Guid.NewGuid());
        await NewLimiter().EnsureAsync(); // Should not throw.
    }

    [Fact]
    public async Task StampAsync_ForSuppliedUser_Starts_Cooldown_ForThatUser()
    {
        var otherUserId = Guid.NewGuid();

        await NewLimiter().StampAsync(otherUserId);

        await NewLimiter().EnsureAsync(); // Current user should not be blocked.

        _currentUser.Id.Returns(otherUserId);
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => NewLimiter().EnsureAsync());
        ex.Message.ShouldContain("rate limited");
    }

    [Fact]
    public async Task ExpiredCooldown_AllowsNewStamp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Generation:CooldownSeconds"] = "1"
            }).Build();
        var limiter = new AIRateLimiter(_cache, _currentUser, config, new TestDistributedLockProvider(), []);

        await limiter.StampAsync();
        await Task.Delay(TimeSpan.FromSeconds(1.2), CancellationToken.None);
        await limiter.EnsureAsync(); // Should not throw.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task StampAsync_Throws_When_Cooldown_Config_Is_Missing_Or_Invalid(string? configuredCooldownSeconds)
    {
        var values = new Dictionary<string, string?>();
        if (configuredCooldownSeconds != null)
        {
            values["Azure:Generation:CooldownSeconds"] = configuredCooldownSeconds;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var limiter = new AIRateLimiter(_cache, _currentUser, config, new TestDistributedLockProvider(), []);

        var ex = await Should.ThrowAsync<AbpException>(() => limiter.StampAsync());
        ex.Message.ShouldContain("Azure:Generation:CooldownSeconds");
    }

    private sealed class TestDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new TestDistributedLock(name);
    }

    private sealed class CountingDistributedLockProvider : IDistributedLockProvider
    {
        public int CreatedLockCount { get; private set; }

        public IDistributedLock CreateLock(string name)
        {
            CreatedLockCount++;
            return new TestDistributedLock(name);
        }
    }

    private sealed class TestDistributedLock(string name) : IDistributedLock
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);

        public string Name => name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            Gate.Wait(cancellationToken);
            return new TestDistributedSynchronizationHandle(Gate);
        }

        public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken);
            return new TestDistributedSynchronizationHandle(Gate);
        }

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            Gate.Wait(timeout, cancellationToken) ? new TestDistributedSynchronizationHandle(Gate) : null;

        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            await Gate.WaitAsync(timeout, cancellationToken)
                ? new TestDistributedSynchronizationHandle(Gate)
                : null;
    }

    private sealed class TestDistributedSynchronizationHandle(SemaphoreSlim gate) : IDistributedSynchronizationHandle
    {
        public CancellationToken HandleLostToken => CancellationToken.None;

        public void Dispose() => gate.Release();

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
