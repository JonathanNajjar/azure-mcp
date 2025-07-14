// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Options;

namespace AzureMcp.Areas.Support.Options.ProblemClassification;

public sealed class ProblemClassificationGetOptions : GlobalOptions
{
    public string? ServiceName { get; set; }
}
