// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Tests.Client;
using AzureMcp.Tests.Client.Helpers;
using Xunit;

namespace AzureMcp.Tests.Areas.Support.LiveTests;

[Trait("Area", "Support")]
[Trait("Category", "Live")]
public class SupportCommandTests(LiveTestFixture liveTestFixture, ITestOutputHelper output)
    : CommandTestsBase(liveTestFixture, output), IClassFixture<LiveTestFixture>
{
    [Fact]
    public async Task Should_List_Support_Tickets()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "top", "10" }
            });

        Assert.NotNull(result);
    }

    [Fact] 
    public async Task Should_Handle_Valid_OData_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "Status eq 'Open'" },
                { "top", "5" }
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Return_400_For_Invalid_Subscription()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", "invalid-subscription-id" }
            });

        Assert.NotNull(result);
        
        // Verify it's an error response
        var errorMessage = result.Value.GetProperty("message").GetString();
        Assert.Contains("Could not find subscription", errorMessage);
        Assert.Contains("invalid-subscription-id", errorMessage);
        
        var errorType = result.Value.GetProperty("type").GetString();
        Assert.Equal("InvalidOperationException", errorType);
    }

    [Fact]
    public async Task Should_Return_400_For_Missing_Subscription()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new());

        // For validation errors (missing required parameters), the result may be null
        // as the MCP client throws exceptions that are converted to null results
        Assert.Null(result);
    }

    [Theory]
    [InlineData("subscription")]
    public async Task Should_Support_Subscription_Name_Resolution(string subscriptionParam)
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { subscriptionParam, Settings.SubscriptionName },
                { "top", "5" }
            });

        // Should succeed or return a reasonable error (403 if no permission, etc.)
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Return_Error_For_Unsupported_ServiceName_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "serviceName eq 'Billing'" },
                { "top", "5" }
            });

        Assert.NotNull(result);
        
        // Should return an error for unsupported property
        var errorMessage = result.Value.GetProperty("message").GetString();
        Assert.Contains("serviceName", errorMessage);
        Assert.Contains("not supported for OData filtering", errorMessage);
        Assert.Contains("Supported properties: CreatedDate, Status, ProblemClassificationId, ServiceId", errorMessage);
    }

    [Fact]
    public async Task Should_Return_Error_For_Unsupported_ProblemClassificationName_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "problemClassificationName eq 'pricing'" },
                { "top", "5" }
            });

        Assert.NotNull(result);
        
        // Should return an error for unsupported property
        var errorMessage = result.Value.GetProperty("message").GetString();
        Assert.Contains("problemClassificationName", errorMessage);
        Assert.Contains("not supported for OData filtering", errorMessage);
    }

    [Fact]
    public async Task Should_Handle_Valid_CreatedDate_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "CreatedDate ge 2024-01-01T00:00:00Z" },
                { "top", "5" }
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Handle_Valid_Combined_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "Status eq 'Open' and CreatedDate ge 2024-01-01T00:00:00Z" },
                { "top", "5" }
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Return_Error_For_Unsupported_ServiceDisplayName_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "serviceDisplayName eq 'Storage'" },
                { "top", "5" }
            });

        Assert.NotNull(result);
        
        // Should return an error for unsupported property
        var errorMessage = result.Value.GetProperty("message").GetString();
        Assert.Contains("serviceDisplayName", errorMessage);
        Assert.Contains("not supported for OData filtering", errorMessage);
    }

    [Fact]
    public async Task Should_Return_Error_For_Unsupported_Title_Filter()
    {
        var result = await CallToolAsync(
            "azmcp-support-ticket-list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "filter", "title eq 'My Support Ticket'" },
                { "top", "5" }
            });

        Assert.NotNull(result);
        
        // Should return an error for unsupported property
        var errorMessage = result.Value.GetProperty("message").GetString();
        Assert.Contains("title", errorMessage);
        Assert.Contains("not supported for OData filtering", errorMessage);
    }
}
