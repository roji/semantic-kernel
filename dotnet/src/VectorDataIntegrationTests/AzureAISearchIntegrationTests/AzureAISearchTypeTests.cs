// Copyright (c) Microsoft. All rights reserved.

using AzureAISearchIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace AzureAISearchIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class AzureAISearchTypeTests(AzureAISearchTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<AzureAISearchTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();

    public override Task Float() => base.Float();
    public override Task Double() => base.Double();

    public override Task String() => base.String();
    public override Task Bool() => base.Bool();

    public override Task DateTimeOffset() => base.DateTimeOffset();

    // Unsupported types
    public override Task Short()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Short());

    public override Task Decimal()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Decimal());

    public override Task Guid()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Guid());

    public override Task DateTime()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateTime());

#if NET6_0_OR_GREATER
    public override Task DateOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateOnly());

    public override Task TimeOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.TimeOnly());
#endif

    public new class Fixture : TypeTests<string>.Fixture
    {
        public override TestStore TestStore => AzureAISearchTestStore.Instance;

        // Azure AI Search only supports lowercase letters, digits or dashes.
        public override string CollectionName => "type-tests";
    }
}
