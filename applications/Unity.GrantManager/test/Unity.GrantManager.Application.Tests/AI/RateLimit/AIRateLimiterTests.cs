using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
                ["AI:RateLimit:CooldownSeconds"] = "60"
            }).Build();
    }

    private AIRateLimiter NewLimiter() => new(_cache, _currentUser, _configuration);

    [Fact]
    public async Task GetStateAsync_Returns_Zero_When_NoCooldown()
    {
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
    }

    [Fact]
    public async Task EnsureAndStampAsync_FirstCall_Stamps_AndAllowsThrough()
    {
        await NewLimiter().EnsureAndStampAsync();
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBeInRange(1, 60);
    }

    [Fact]
    public async Task EnsureAndStampAsync_SecondCall_Throws_WithRemainingSecondsMessage()
    {
        var limiter = NewLimiter();
        await limiter.EnsureAndStampAsync();

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => limiter.EnsureAndStampAsync());
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
    public async Task EnsureAndStampAsync_IsNoOp_For_AnonymousUser()
    {
        _currentUser.Id.Returns((Guid?)null);
        await NewLimiter().EnsureAndStampAsync(); // Should not throw.
        var state = await NewLimiter().GetStateAsync();
        state.RetryAfterSeconds.ShouldBe(0);
    }

    [Fact]
    public async Task DifferentUsers_Have_IndependentCooldowns()
    {
        await NewLimiter().EnsureAndStampAsync();

        _currentUser.Id.Returns(Guid.NewGuid());
        await NewLimiter().EnsureAndStampAsync(); // Should not throw.
    }

    [Fact]
    public async Task ExpiredCooldown_AllowsNewStamp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:RateLimit:CooldownSeconds"] = "1"
            }).Build();
        var limiter = new AIRateLimiter(_cache, _currentUser, config);

        await limiter.EnsureAndStampAsync();
        await Task.Delay(TimeSpan.FromSeconds(1.2), CancellationToken.None);
        await limiter.EnsureAndStampAsync(); // Should not throw.
    }
}
