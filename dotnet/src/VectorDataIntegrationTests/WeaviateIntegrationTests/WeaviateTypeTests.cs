// Copyright (c) Microsoft. All rights reserved.

using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using WeaviateIntegrationTests.Support;
using Xunit;

namespace WeaviateIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class WeaviateTypeTests(WeaviateTypeTests.Fixture fixture)
    : TypeTests<Guid>(fixture), IClassFixture<WeaviateTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();
    public override Task String() => base.String();
    public override Task Bool() => base.Bool();

    public override Task DateTime()
        => this.TestTypeStructAsync(
            new DateTime(2020, 1, 1, 12, 30, 45, DateTimeKind.Utc),
            new DateTime(2021, 2, 3, 13, 40, 55, DateTimeKind.Utc),
            instantiationExpression: () => new DateTime(2020, 1, 1, 12, 30, 45, DateTimeKind.Utc));

#if NET6_0_OR_GREATER
    public override Task DateOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateOnly());

    public override Task TimeOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.TimeOnly());
#endif

    public new class Fixture : TypeTests<Guid>.Fixture
    {
        public override TestStore TestStore => WeaviateTestStore.Instance;
    }
}
