using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.Services;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Users;
using Xunit;

namespace Unity.Payments.Domain.PaymentRequests;

[Category("Domain")]
public class PaymentRequestQueryManager_PaymentSummary_Tests
{
    #region GetApplicationPaymentSummaryAsync (Single Application)

    [Fact]
    public async Task Should_Return_Summary_For_Single_Application_With_NoChildren()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = appId, TotalPaid = 1500m, TotalPending = 2000m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummaryAsync(appId, []);

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationId.ShouldBe(appId);
        result.TotalPaid.ShouldBe(1500m);
        result.TotalPending.ShouldBe(2000m);

        // Verify repo was called with only the parent ID
        await repo.Received(1).GetPaymentSummariesByCorrelationIdsAsync(
            Arg.Is<List<Guid>>(ids => ids.Count == 1 && ids.Contains(appId)));
    }

    [Fact]
    public async Task Should_Aggregate_Summary_From_Parent_And_Children()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = parentId, TotalPaid = 1000m, TotalPending = 500m },
                new() { ApplicationId = child1Id, TotalPaid = 300m, TotalPending = 200m },
                new() { ApplicationId = child2Id, TotalPaid = 700m, TotalPending = 100m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummaryAsync(parentId, [child1Id, child2Id]);

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationId.ShouldBe(parentId);
        result.TotalPaid.ShouldBe(2000m);    // 1000 + 300 + 700
        result.TotalPending.ShouldBe(800m);  // 500 + 200 + 100

        // Verify all IDs were sent to the repository
        await repo.Received(1).GetPaymentSummariesByCorrelationIdsAsync(
            Arg.Is<List<Guid>>(ids => ids.Count == 3
                && ids.Contains(parentId) && ids.Contains(child1Id) && ids.Contains(child2Id)));
    }

    [Fact]
    public async Task Should_Return_Zeros_When_No_Payment_Data_Exists()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>());

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummaryAsync(appId, []);

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationId.ShouldBe(appId);
        result.TotalPaid.ShouldBe(0m);
        result.TotalPending.ShouldBe(0m);
    }

    [Fact]
    public async Task Should_Return_Zeros_When_Children_Have_No_Payments()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = parentId, TotalPaid = 500m, TotalPending = 0m }
                // childId has no payments - not returned by repository
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummaryAsync(parentId, [childId]);

        // Assert
        result.TotalPaid.ShouldBe(500m);  // Only parent amount
        result.TotalPending.ShouldBe(0m);
    }

    [Fact]
    public async Task Should_Handle_Single_Child_Application()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = parentId, TotalPaid = 1000m, TotalPending = 0m },
                new() { ApplicationId = childId, TotalPaid = 500m, TotalPending = 300m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummaryAsync(parentId, [childId]);

        // Assert
        result.ApplicationId.ShouldBe(parentId);
        result.TotalPaid.ShouldBe(1500m);    // 1000 + 500
        result.TotalPending.ShouldBe(300m);  // 0 + 300
    }

    #endregion

    #region GetApplicationPaymentSummariesAsync (Batch)

    [Fact]
    public async Task Should_Return_Batch_Summaries_For_Multiple_Applications_Without_Children()
    {
        // Arrange
        var app1Id = Guid.NewGuid();
        var app2Id = Guid.NewGuid();
        var app3Id = Guid.NewGuid();

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = app1Id, TotalPaid = 1000m, TotalPending = 200m },
                new() { ApplicationId = app2Id, TotalPaid = 500m, TotalPending = 100m },
                new() { ApplicationId = app3Id, TotalPaid = 0m, TotalPending = 3000m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [app1Id, app2Id, app3Id],
            new Dictionary<Guid, List<Guid>>());

        // Assert
        result.Count.ShouldBe(3);
        result[app1Id].TotalPaid.ShouldBe(1000m);
        result[app1Id].TotalPending.ShouldBe(200m);
        result[app2Id].TotalPaid.ShouldBe(500m);
        result[app2Id].TotalPending.ShouldBe(100m);
        result[app3Id].TotalPaid.ShouldBe(0m);
        result[app3Id].TotalPending.ShouldBe(3000m);
    }

    [Fact]
    public async Task Should_Aggregate_Child_Amounts_In_Batch_Summaries()
    {
        // Arrange
        var parentAId = Guid.NewGuid();
        var parentBId = Guid.NewGuid();
        var childA1Id = Guid.NewGuid();
        var childA2Id = Guid.NewGuid();
        var childB1Id = Guid.NewGuid();

        var childMap = new Dictionary<Guid, List<Guid>>
        {
            { parentAId, [childA1Id, childA2Id] },
            { parentBId, [childB1Id] }
        };

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = parentAId, TotalPaid = 1000m, TotalPending = 100m },
                new() { ApplicationId = childA1Id, TotalPaid = 200m, TotalPending = 50m },
                new() { ApplicationId = childA2Id, TotalPaid = 300m, TotalPending = 75m },
                new() { ApplicationId = parentBId, TotalPaid = 500m, TotalPending = 0m },
                new() { ApplicationId = childB1Id, TotalPaid = 400m, TotalPending = 200m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [parentAId, parentBId], childMap);

        // Assert
        result.Count.ShouldBe(2);

        // Parent A: 1000+200+300 paid, 100+50+75 pending
        result[parentAId].TotalPaid.ShouldBe(1500m);
        result[parentAId].TotalPending.ShouldBe(225m);
        result[parentAId].ApplicationId.ShouldBe(parentAId);

        // Parent B: 500+400 paid, 0+200 pending
        result[parentBId].TotalPaid.ShouldBe(900m);
        result[parentBId].TotalPending.ShouldBe(200m);
        result[parentBId].ApplicationId.ShouldBe(parentBId);
    }

    [Fact]
    public async Task Should_Handle_Application_With_No_Matching_Summary_In_Batch()
    {
        // Arrange
        var app1Id = Guid.NewGuid();
        var app2Id = Guid.NewGuid();

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                // Only app1 has payment data, app2 doesn't
                new() { ApplicationId = app1Id, TotalPaid = 1000m, TotalPending = 500m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [app1Id, app2Id],
            new Dictionary<Guid, List<Guid>>());

        // Assert
        result.Count.ShouldBe(2);
        result[app1Id].TotalPaid.ShouldBe(1000m);
        result[app1Id].TotalPending.ShouldBe(500m);

        // app2 gets zero amounts since no data was returned
        result[app2Id].TotalPaid.ShouldBe(0m);
        result[app2Id].TotalPending.ShouldBe(0m);
        result[app2Id].ApplicationId.ShouldBe(app2Id);
    }

    [Fact]
    public async Task Should_Deduplicate_CorrelationIds_In_Batch_Repository_Call()
    {
        // Arrange - A child is shared between two parents (edge case)
        var parentAId = Guid.NewGuid();
        var parentBId = Guid.NewGuid();
        var sharedChildId = Guid.NewGuid();

        var childMap = new Dictionary<Guid, List<Guid>>
        {
            { parentAId, [sharedChildId] },
            { parentBId, [sharedChildId] }
        };

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = parentAId, TotalPaid = 100m, TotalPending = 0m },
                new() { ApplicationId = parentBId, TotalPaid = 200m, TotalPending = 0m },
                new() { ApplicationId = sharedChildId, TotalPaid = 50m, TotalPending = 25m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [parentAId, parentBId], childMap);

        // Assert
        // Verify repository was called with deduplicated IDs (3 unique, not 4)
        await repo.Received(1).GetPaymentSummariesByCorrelationIdsAsync(
            Arg.Is<List<Guid>>(ids => ids.Distinct().Count() == 3));

        // Both parents should include the shared child's amounts
        result[parentAId].TotalPaid.ShouldBe(150m);  // 100 + 50
        result[parentAId].TotalPending.ShouldBe(25m); // 0 + 25
        result[parentBId].TotalPaid.ShouldBe(250m);  // 200 + 50
        result[parentBId].TotalPending.ShouldBe(25m); // 0 + 25
    }

    [Fact]
    public async Task Should_Make_Single_Repository_Call_For_Batch()
    {
        // Arrange
        var app1Id = Guid.NewGuid();
        var app2Id = Guid.NewGuid();
        var child1Id = Guid.NewGuid();

        var childMap = new Dictionary<Guid, List<Guid>>
        {
            { app1Id, [child1Id] }
        };

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>());

        var manager = CreateManager(repo);

        // Act
        await manager.GetApplicationPaymentSummariesAsync([app1Id, app2Id], childMap);

        // Assert - should only call repository once (batch optimization)
        await repo.Received(1).GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>());
    }

    [Fact]
    public async Task Should_Return_Empty_Dictionary_For_Empty_Application_List()
    {
        // Arrange
        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>());

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [], new Dictionary<Guid, List<Guid>>());

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Parent_Without_Children_In_Mixed_Batch()
    {
        // Arrange - app1 has children, app2 does not
        var app1Id = Guid.NewGuid();
        var app2Id = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var childMap = new Dictionary<Guid, List<Guid>>
        {
            { app1Id, [childId] }
            // app2 has no entry in childMap
        };

        var repo = Substitute.For<IPaymentRequestRepository>();
        repo.GetPaymentSummariesByCorrelationIdsAsync(Arg.Any<List<Guid>>())
            .Returns(new List<ApplicationPaymentSummaryDto>
            {
                new() { ApplicationId = app1Id, TotalPaid = 1000m, TotalPending = 0m },
                new() { ApplicationId = childId, TotalPaid = 500m, TotalPending = 100m },
                new() { ApplicationId = app2Id, TotalPaid = 300m, TotalPending = 50m }
            });

        var manager = CreateManager(repo);

        // Act
        var result = await manager.GetApplicationPaymentSummariesAsync(
            [app1Id, app2Id], childMap);

        // Assert
        result[app1Id].TotalPaid.ShouldBe(1500m);   // 1000 + 500
        result[app1Id].TotalPending.ShouldBe(100m);  // 0 + 100

        result[app2Id].TotalPaid.ShouldBe(300m);     // Only own amount
        result[app2Id].TotalPending.ShouldBe(50m);   // Only own amount
    }

    #endregion

    #region Helpers

    private static PaymentRequestQueryManager CreateManager(IPaymentRequestRepository repo)
    {
        return new PaymentRequestQueryManager(
            repo,
            Substitute.For<ISiteRepository>(),
            Substitute.For<IExternalUserLookupServiceProvider>(),
            null!, // CasPaymentRequestCoordinator - not used by summary methods
            null!  // IObjectMapper - not used by summary methods
        );
    }

    #endregion
}
