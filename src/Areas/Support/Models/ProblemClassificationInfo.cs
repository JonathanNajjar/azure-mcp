// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.Support.Models;

public record ProblemClassificationInfo(
    string Id,
    string Name,
    string DisplayName,
    string ServiceName,
    string ServiceDisplayName);
