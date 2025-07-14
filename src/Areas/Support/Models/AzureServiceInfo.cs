// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.Support.Models;

public record AzureServiceInfo(
    string Id,
    string Name,
    string DisplayName,
    string ResourceType);
