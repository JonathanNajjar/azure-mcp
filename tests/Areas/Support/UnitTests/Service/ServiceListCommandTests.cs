// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Support.Commands.Service;
using AzureMcp.Areas.Support.Models;
using AzureMcp.Areas.Support.Services;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzureMcp.Tests.Areas.Support.UnitTests.Service;

public class ServiceListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISupportService _service;
    private readonly ILogger<ServiceListCommand> _logger;
    private readonly ServiceListCommand _command;

    public ServiceListCommandTests()
    {
        _service = Substitute.For<ISupportService>();
        _logger = Substitute.For<ILogger<ServiceListCommand>>();

        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();

        _command = new(_logger);
    }

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = _command.GetCommand();
        Assert.Equal("list", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
        Assert.Contains("Azure services", command.Description);
    }

    [Theory]
    [InlineData("--subscription test-sub", true)]
    [InlineData("--subscription test-sub --tenant test-tenant", true)]
    [InlineData("", false)]
    public async Task ExecuteAsync_ValidatesInputCorrectly(string args, bool shouldSucceed)
    {
        // Arrange
        if (shouldSucceed)
        {
            var mockServices = new List<AzureServiceInfo>
            {
                new("service1", "Microsoft.Compute", "Virtual Machines", "Microsoft.Compute/virtualMachines"),
                new("service2", "Microsoft.Storage", "Storage Accounts", "Microsoft.Storage/storageAccounts")
            };
            _service.ListAzureServicesAsync(Arg.Any<string?>(), Arg.Any<AzureMcp.Options.RetryPolicyOptions?>())
                .Returns(mockServices);
        }

        var context = new CommandContext(_serviceProvider);
        var parseResult = _command.GetCommand().Parse(args);

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(shouldSucceed ? 200 : 400, response.Status);
        if (shouldSucceed)
        {
            Assert.NotNull(response.Results);
            Assert.Equal("Success", response.Message);
        }
        else
        {
            Assert.Contains("subscription", response.Message.ToLower());
        }
    }

    [Fact]
    public async Task ExecuteAsync_HandlesServiceErrors()
    {
        // Arrange
        _service.ListAzureServicesAsync(Arg.Any<string?>(), Arg.Any<AzureMcp.Options.RetryPolicyOptions?>())
            .Returns(Task.FromException<List<AzureServiceInfo>>(new Exception("Test error")));

        var context = new CommandContext(_serviceProvider);
        var parseResult = _command.GetCommand().Parse("--subscription test-sub");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(500, response.Status);
        Assert.Contains("Test error", response.Message);
        Assert.Contains("troubleshooting", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmptyResultsWhenNoServices()
    {
        // Arrange
        _service.ListAzureServicesAsync(Arg.Any<string?>(), Arg.Any<AzureMcp.Options.RetryPolicyOptions?>())
            .Returns(new List<AzureServiceInfo>());

        var context = new CommandContext(_serviceProvider);
        var parseResult = _command.GetCommand().Parse("--subscription test-sub");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Null(response.Results);
        Assert.Equal("Success", response.Message);
    }
}
