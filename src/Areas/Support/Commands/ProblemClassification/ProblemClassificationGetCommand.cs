// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Support.Models;
using AzureMcp.Areas.Support.Options.ProblemClassification;
using AzureMcp.Areas.Support.Services;
using AzureMcp.Commands;
using AzureMcp.Extensions;
using AzureMcp.Models;
using AzureMcp.Models.Command;
using Microsoft.Extensions.Logging;
using System.CommandLine.Parsing;

namespace AzureMcp.Areas.Support.Commands.ProblemClassification;

public sealed class ProblemClassificationGetCommand(
    ILogger<ProblemClassificationGetCommand> logger)
    : GlobalCommand<ProblemClassificationGetOptions>
{
    private const string CommandTitle = "Get Problem Classifications";
    private readonly ILogger<ProblemClassificationGetCommand> _logger = logger;

    public override string Name => "get";
    public override string Title => CommandTitle;
    public override string Description => "Get Azure support problem classifications for services";

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        
        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            var serviceName = options.ServiceName;

            _logger.LogInformation("Getting problem classifications for service: {ServiceName}", serviceName ?? "all services");

            var supportService = context.GetService<ISupportService>();
            
            var results = await supportService.GetProblemClassificationsAsync(
                serviceName,
                options.Tenant);

            var response = new ProblemClassificationGetCommandResult(results);

            _logger.LogInformation("Successfully retrieved {Count} problem classifications", results.Count);

            context.Response.Results = results?.Count > 0
                ? ResponseResult.Create(
                    response,
                    SupportJsonContext.Default.ProblemClassificationGetCommandResult)
                : null;
            
            return context.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get problem classifications for service: {ServiceName}",
                options.ServiceName ?? "all services");
            
            HandleException(context, ex);
            return context.Response;
        }
    }

    internal record ProblemClassificationGetCommandResult(List<ProblemClassificationInfo> Results);
}
