// Copyright (c) Microsoft. All rights reserved.

using VectorData.ConformanceTests.Xunit;

namespace SqlServer.ConformanceTests.Support;

/// <summary>
/// Skips the test(s) when no external SQL Server connection string is configured (i.e. when using a testcontainer).
/// This is used for tests that require Azure SQL features not available in on-prem SQL Server (e.g. latest version vector indexes).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class SqlServerConnectionStringRequiredAttribute : Attribute, ITestCondition
{
    public ValueTask<bool> IsMetAsync() => new(SqlServerTestEnvironment.IsConnectionStringDefined);

    public string Skip { get; set; } = "An external SQL Server connection string is not configured. "
        + "Set SqlServer:ConnectionString to an Azure SQL connection string to run this test.";

    public string SkipReason
        => this.Skip;
}
