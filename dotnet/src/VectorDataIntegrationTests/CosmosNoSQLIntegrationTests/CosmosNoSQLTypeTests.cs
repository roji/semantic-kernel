// Copyright (c) Microsoft. All rights reserved.

using CosmosNoSQLIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace CosmosNoSQLIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class CosmosNoSQLTypeTests(CosmosNoSQLTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<CosmosNoSQLTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();

    public override Task Float() => base.Float();
    public override Task Double() => base.Double();

    public override Task String() => base.String();
    public override Task Bool() => base.Bool();

    // Cosmos NoSQL doesn't support DateTimeOffset with Offset != 0
    public override Task DateTimeOffset()
        => this.TestTypeStructAsync(
            new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.Zero),
            new DateTimeOffset(2021, 2, 3, 13, 40, 55, TimeSpan.Zero),
            instantiationExpression: () => new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.Zero));

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
        public override TestStore TestStore => CosmosNoSqlTestStore.Instance;
    }
}
