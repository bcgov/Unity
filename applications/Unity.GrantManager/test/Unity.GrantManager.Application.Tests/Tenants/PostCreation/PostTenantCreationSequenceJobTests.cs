using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Tenants.PostCreation;

public class PostTenantCreationSequenceJobTests
{
    private sealed class FakeStep(int order, string name, bool continueOnError, Func<Guid, Task>? onExecute = null)
        : IPostTenantCreationStep
    {
        public int Order { get; } = order;
        public string StepName { get; } = name;
        public bool ContinueOnError { get; } = continueOnError;
        public bool Executed { get; private set; }

        public async Task ExecuteAsync(Guid tenantId)
        {
            Executed = true;
            if (onExecute != null)
            {
                await onExecute(tenantId);
            }
        }
    }

    private static (PostTenantCreationSequenceJob Job, List<PostTenantCreationStepArgs> Enqueued) CreateJob(
        IEnumerable<IPostTenantCreationStep> steps)
    {
        var enqueued = new List<PostTenantCreationStepArgs>();
        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        backgroundJobManager.EnqueueAsync(
                Arg.Any<PostTenantCreationStepArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var args = callInfo.Arg<PostTenantCreationStepArgs>();
                ArgumentNullException.ThrowIfNull(args);
                enqueued.Add(args);
                return Task.FromResult(string.Empty);
            });

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());

        var job = new PostTenantCreationSequenceJob(
            steps, backgroundJobManager, currentTenant, Substitute.For<ILogger<PostTenantCreationSequenceJob>>());

        return (job, enqueued);
    }

    [Fact]
    public async Task ExecuteAsync_RunsStepAtIndex_AndEnqueuesNextStepIndex()
    {
        var tenantId = Guid.NewGuid();
        var step0 = new FakeStep(0, "Step0", continueOnError: false);
        var step1 = new FakeStep(1, "Step1", continueOnError: false);
        var (job, enqueued) = CreateJob([step0, step1]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenantId, StepIndex = 0 });

        step0.Executed.ShouldBeTrue();
        step1.Executed.ShouldBeFalse();
        var next = enqueued.ShouldHaveSingleItem();
        next.TenantId.ShouldBe(tenantId);
        next.StepIndex.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_StepIndexPastEnd_DoesNothing()
    {
        var (job, enqueued) = CreateJob([new FakeStep(0, "Step0", continueOnError: false)]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = Guid.NewGuid(), StepIndex = 1 });

        enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_ContinueOnErrorTrue_StillEnqueuesNextStep()
    {
        var step = new FakeStep(0, "Flaky", continueOnError: true, onExecute: _ => throw new InvalidOperationException("boom"));
        var (job, enqueued) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = Guid.NewGuid(), StepIndex = 0 });

        enqueued.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_ContinueOnErrorFalse_StopsSequence()
    {
        var step = new FakeStep(0, "Fatal", continueOnError: false, onExecute: _ => throw new InvalidOperationException("boom"));
        var (job, enqueued) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = Guid.NewGuid(), StepIndex = 0 });

        enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_RunsSteps_InAscendingOrder_RegardlessOfRegistrationOrder()
    {
        var executed = new List<string>();
        var stepHigh = new FakeStep(2, "High", continueOnError: false,
            onExecute: _ => { executed.Add("High"); return Task.CompletedTask; });
        var stepLow = new FakeStep(1, "Low", continueOnError: false,
            onExecute: _ => { executed.Add("Low"); return Task.CompletedTask; });
        var (job, _) = CreateJob([stepHigh, stepLow]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = Guid.NewGuid(), StepIndex = 0 });

        executed.ShouldBe(["Low"]);
    }
}
