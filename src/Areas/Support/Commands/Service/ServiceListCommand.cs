// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.Parsing;
using AzureMcp.Areas.Support.Commands;
using AzureMcp.Areas.Support.Models;
using AzureMcp.Areas.Support.Options.Service;
using AzureMcp.Areas.Support.Services;
using AzureMcp.Commands;
using AzureMcp.Models;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Support.Commands.Service;

public sealed class ServiceListCommand(ILogger<ServiceListCommand> logger)
    : BaseSupportCommand<ServiceListOptions>(logger)
{
    private const string CommandTitle = "List Azure Support Services";

    public override string Name => "list";

    public override string Description =>
        """
        Lists all Azure services available for support ticket creation.
        Returns a list of Azure services with their IDs, names, display names, and resource types.
        This information is useful for creating support tickets or filtering tickets by service.
        """;

    public override string Title => CommandTitle;

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
        Title = CommandTitle)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        try
        {
            // Required validation step
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            // Add subscription information for telemetry
            context.Activity?.WithSubscriptionTag(options);

            // Get the service from DI
            var service = context.GetService<ISupportService>();

            // Call service operation
            var results = await service.ListAzureServicesAsync(
                options.Tenant,
                options.RetryPolicy);

            // Set results if any were returned
            context.Response.Results = results?.Count > 0 ?
                ResponseResult.Create(
                    new ServiceListCommandResult(results),
                    SupportJsonContext.Default.ServiceListCommandResult) :
                null;
        }
        catch (Exception ex)
        {
            // Log error with all relevant context
            _logger.LogError(ex,
                "Error in {Operation}. Options: {@Options}",
                Name, options);
            HandleException(context, ex);
        }

        return context.Response;
    }

    // Implementation-specific error handling
    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx when reqEx.Status == 404 =>
            "Azure services not found. Verify you have access to support services.",
        Azure.RequestFailedException reqEx when reqEx.Status == 403 =>
            $"Authorization failed accessing support services. Details: {reqEx.Message}",
        Azure.RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx => reqEx.Status,
        _ => base.GetStatusCode(ex)
    };

    // Strongly-typed result record
    internal record ServiceListCommandResult(List<AzureServiceInfo> Services);
}
