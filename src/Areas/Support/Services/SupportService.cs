// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.ResourceManager.Support;
using AzureMcp.Areas.Support.Models;
using AzureMcp.Options;
using AzureMcp.Services.Azure;
using AzureMcp.Services.Azure.Subscription;
using AzureMcp.Services.Azure.Tenant;

namespace AzureMcp.Areas.Support.Services;

public class SupportService(ISubscriptionService subscriptionService, ITenantService tenantService)
    : BaseAzureService(tenantService), ISupportService
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));

    public async Task<List<SupportTicket>> ListSupportTickets(
        string subscription,
        string? filter = null,
        int? top = null,
        string? tenantId = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);
        
        // Validate filter for basic OData properties only
        if (!string.IsNullOrEmpty(filter))
        {
            ValidateBasicODataFilter(filter);
        }

        try
        {
            var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenantId, retryPolicy);
            var supportTickets = subscriptionResource.GetSubscriptionSupportTickets();
            
            var tickets = new List<SupportTicket>();
            
            // Use native Azure SDK filtering capabilities with the raw filter
            var asyncEnumerable = supportTickets.GetAllAsync(top: top, filter: filter);

            await foreach (var ticketResource in asyncEnumerable)
            {
                var ticket = new SupportTicket
                {
                    Id = ticketResource.Id?.ToString(),
                    Name = ticketResource.Data?.Name,
                    Title = ticketResource.Data?.Title,
                    Description = ticketResource.Data?.Description,
                    Status = ticketResource.Data?.Status?.ToString(),
                    Severity = ticketResource.Data?.Severity.ToString(),
                    ServiceName = ticketResource.Data?.ServiceDisplayName,
                    ProblemClassification = ticketResource.Data?.ProblemClassificationDisplayName,
                    CreatedDate = ticketResource.Data?.CreatedOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ModifiedDate = ticketResource.Data?.ModifiedOn?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                // Add contact information if available
                if (ticketResource.Data?.ContactDetails != null)
                {
                    ticket.ContactDetails = new ContactInformation
                    {
                        FirstName = ticketResource.Data.ContactDetails.FirstName,
                        LastName = ticketResource.Data.ContactDetails.LastName,
                        PreferredContactMethod = ticketResource.Data.ContactDetails.PreferredContactMethod.ToString(),
                        PrimaryEmailAddress = ticketResource.Data.ContactDetails.PrimaryEmailAddress,
                        PhoneNumber = ticketResource.Data.ContactDetails.PhoneNumber,
                        PreferredTimeZone = ticketResource.Data.ContactDetails.PreferredTimeZone,
                        Country = ticketResource.Data.ContactDetails.Country,
                        PreferredSupportLanguage = ticketResource.Data.ContactDetails.PreferredSupportLanguage
                    };
                }

                tickets.Add(ticket);
            }

            return tickets;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list support tickets: {ex.Message}", ex);
        }
    }

    private static void ValidateBasicODataFilter(string filter)
    {
        // Basic validation for unsupported properties that were previously processed by SupportFilterProcessor
        var unsupportedProperties = new[] { "serviceName", "serviceDisplayName", "problemClassificationName", "title", "description", "severity", "contactDetails" };
        
        foreach (var property in unsupportedProperties)
        {
            if (filter.Contains(property, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Property '{property}' is not supported for OData filtering. " +
                    "Supported properties: CreatedDate, Status, ProblemClassificationId, ServiceId. " +
                    "For service and classification filtering, use the dedicated service and classification commands to discover the appropriate IDs.");
            }
        }
    }

    public async Task<List<ProblemClassificationInfo>> GetProblemClassificationsAsync(
        string? serviceName = null,
        string? tenantId = null)
    {
        try
        {
            var armClient = await CreateArmClientAsync(tenantId);
            var tenantResource = armClient.GetTenants().First();
            
            var supportServices = tenantResource.GetSupportAzureServices();
            
            var problemClassifications = new List<ProblemClassificationInfo>();

            if (!string.IsNullOrEmpty(serviceName))
            {
                // Get problem classifications for a specific service
                var serviceResource = await supportServices.GetAsync(serviceName);
                var classifications = serviceResource.Value.GetProblemClassifications();
                
                await foreach (var classification in classifications.GetAllAsync())
                {
                    problemClassifications.Add(new ProblemClassificationInfo(
                        Id: classification.Id?.ToString() ?? string.Empty,
                        Name: classification.Data?.Name ?? string.Empty,
                        DisplayName: classification.Data?.DisplayName ?? string.Empty,
                        ServiceName: serviceName,
                        ServiceDisplayName: serviceResource.Value.Data?.DisplayName ?? serviceName
                    ));
                }
            }
            else
            {
                // Get problem classifications for all services
                await foreach (var service in supportServices.GetAllAsync())
                {
                    var classifications = service.GetProblemClassifications();
                    
                    await foreach (var classification in classifications.GetAllAsync())
                    {
                        problemClassifications.Add(new ProblemClassificationInfo(
                            Id: classification.Id?.ToString() ?? string.Empty,
                            Name: classification.Data?.Name ?? string.Empty,
                            DisplayName: classification.Data?.DisplayName ?? string.Empty,
                            ServiceName: service.Data?.Name ?? string.Empty,
                            ServiceDisplayName: service.Data?.DisplayName ?? string.Empty
                        ));
                    }
                }
            }

            return problemClassifications;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get problem classifications: {ex.Message}", ex);
        }
    }

    public async Task<List<AzureServiceInfo>> ListAzureServicesAsync(
        string? tenantId = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        try
        {
            var armClient = await CreateArmClientAsync(tenantId, retryPolicy);
            var tenantResource = armClient.GetTenants().First();
            
            var supportServices = tenantResource.GetSupportAzureServices();
            var azureServices = new List<AzureServiceInfo>();

            await foreach (var service in supportServices.GetAllAsync())
            {
                azureServices.Add(new AzureServiceInfo(
                    Id: service.Id?.ToString() ?? string.Empty,
                    Name: service.Data?.Name ?? string.Empty,
                    DisplayName: service.Data?.DisplayName ?? string.Empty,
                    ResourceType: service.Data?.ResourceType ?? string.Empty
                ));
            }

            return azureServices;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list Azure services: {ex.Message}", ex);
        }
    }
}
