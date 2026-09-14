using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;
using Xunit;

namespace Unity.GrantManager.Tenants.PostCreation;

public class PostTenantCreationSequenceJobTests
{
    private sealed class FakeStep(int order, string name, bool continueOnError, Func<Guid, Task>? onExecute = null, bool canExecute = true)
        : IPostTenantCreationStep
    {
        public int Order { get; } = order;
        public string Key { get; } = name;
        public string StepName { get; } = name;
        public bool ContinueOnError { get; } = continueOnError;
        public bool Executed { get; private set; }

        public Task<bool> CanExecuteAsync(Guid tenantId) => Task.FromResult(canExecute);

        public async Task ExecuteAsync(Guid tenantId)
        {
            Executed = true;
            if (onExecute != null)
            {
                await onExecute(tenantId);
            }
        }
    }

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising its instance state in a
    // plain, DB-less unit test (see MetabaseTenantRegistrationStepTests for the same pattern).
    private static Tenant CreateTenant(Guid id)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        return (Tenant)ctor.Invoke([id, "test-tenant", "TEST-TENANT"]);
    }

    private static (PostTenantCreationSequenceJob Job, List<PostTenantCreationStepArgs> Enqueued, Tenant Tenant, ICurrentTenant CurrentTenant) CreateJob(
        IEnumerable<IPostTenantCreationStep> steps, Guid? tenantId = null)
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

        var tenant = CreateTenant(tenantId ?? Guid.NewGuid());
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>()).Returns(tenant);

        var clock = Substitute.For<IClock>();
        clock.Now.Returns(DateTime.UtcNow);

        var job = new PostTenantCreationSequenceJob(
            steps, backgroundJobManager, tenantRepository, currentTenant, clock,
            Substitute.For<ILogger<PostTenantCreationSequenceJob>>());

        return (job, enqueued, tenant, currentTenant);
    }

    [Fact]
    public async Task ExecuteAsync_RunsStepAtIndex_AndEnqueuesNextStepIndex()
    {
        var step0 = new FakeStep(0, "Step0", continueOnError: false);
        var step1 = new FakeStep(1, "Step1", continueOnError: false);
        var (job, enqueued, tenant, _) = CreateJob([step0, step1]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        step0.Executed.ShouldBeTrue();
        step1.Executed.ShouldBeFalse();
        var next = enqueued.ShouldHaveSingleItem();
        next.TenantId.ShouldBe(tenant.Id);
        next.StepIndex.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_StepIndexPastEnd_DoesNothing()
    {
        var (job, enqueued, tenant, _) = CreateJob([new FakeStep(0, "Step0", continueOnError: false)]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 1 });

        enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_ContinueOnErrorTrue_StillEnqueuesNextStep()
    {
        var step = new FakeStep(0, "Flaky", continueOnError: true, onExecute: _ => throw new InvalidOperationException("boom"));
        var (job, enqueued, tenant, _) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        enqueued.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_ContinueOnErrorFalse_StopsSequence()
    {
        var step = new FakeStep(0, "Fatal", continueOnError: false, onExecute: _ => throw new InvalidOperationException("boom"));
        var (job, enqueued, tenant, _) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        enqueued.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_CanExecuteAsyncReturnsFalse_SkipsExecuteButStillEnqueuesNextStep()
    {
        var step0 = new FakeStep(0, "Step0", continueOnError: false, canExecute: false);
        var step1 = new FakeStep(1, "Step1", continueOnError: false);
        var (job, enqueued, tenant, _) = CreateJob([step0, step1]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        step0.Executed.ShouldBeFalse();
        var next = enqueued.ShouldHaveSingleItem();
        next.StepIndex.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_RunsSteps_InAscendingOrder_RegardlessOfRegistrationOrder()
    {
        var executed = new List<string>();
        var stepHigh = new FakeStep(2, "High", continueOnError: false,
            onExecute: _ => { executed.Add("High"); return Task.CompletedTask; });
        var stepLow = new FakeStep(1, "Low", continueOnError: false,
            onExecute: _ => { executed.Add("Low"); return Task.CompletedTask; });
        var (job, _, tenant, _) = CreateJob([stepHigh, stepLow]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        executed.ShouldBe(["Low"]);
    }

    [Fact]
    public async Task ExecuteAsync_StepSucceeds_RecordsSuccessStatusOnTenant()
    {
        var step = new FakeStep(0, "Step0", continueOnError: false);
        var (job, _, tenant, _) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        var section = tenant.GetPostTenantCreationSections().ShouldHaveSingleItem();
        section.Key.ShouldBe("Step0");
        section.Status.ShouldBe(PostTenantCreationStepStatus.Success);
        section.Message.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_StepSucceeds_RecordsStatusUnderHostTenantContext()
    {
        // Tenant is host-side data. The success path calls UpdateStepStatusAsync from inside the
        // currentTenant.Change(args.TenantId) block, so it must switch back to the host (null)
        // context itself before touching ITenantRepository, rather than relying on the ambient
        // per-tenant context still being active.
        var step = new FakeStep(0, "Step0", continueOnError: false);
        var (job, _, tenant, currentTenant) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        currentTenant.Received().Change(null);
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_RecordsErrorStatusWithMessageOnTenant()
    {
        var step = new FakeStep(0, "Flaky", continueOnError: true, onExecute: _ => throw new InvalidOperationException("boom"));
        var (job, _, tenant, _) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        var section = tenant.GetPostTenantCreationSections().ShouldHaveSingleItem();
        section.Status.ShouldBe(PostTenantCreationStepStatus.Error);
        section.Message.ShouldBe("boom");
    }

    [Fact]
    public async Task ExecuteAsync_CanExecuteAsyncReturnsFalse_LeavesStepStatusUnrecorded()
    {
        var step = new FakeStep(0, "Step0", continueOnError: false, canExecute: false);
        var (job, _, tenant, _) = CreateJob([step]);

        await job.ExecuteAsync(new PostTenantCreationStepArgs { TenantId = tenant.Id, StepIndex = 0 });

        tenant.GetPostTenantCreationSections().ShouldBeEmpty();
    }
}
