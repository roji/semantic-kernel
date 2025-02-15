// Copyright (c) Microsoft. All rights reserved.

using QdrantIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace QdrantIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class QdrantTypeTests(QdrantTypeTests.Fixture fixture)
    : TypeTests<ulong>(fixture), IClassFixture<QdrantTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();
    public override Task String() => base.String();
    public override Task Bool() => base.Bool();

    // Float and double aren't filterable on Qdrant
    public override Task Float()
        => this.TestTypeStructAsync(8.5f, 9.5f, isFilterable: false);

    public override Task Double()
        => this.TestTypeStructAsync(8.5, 9.5, isFilterable: false);

    // Unsupported types
    public override Task Short()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Short());

    public override Task Decimal()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Decimal());

    public override Task Guid()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Guid());

    public override Task DateTime()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateTime());

    public override Task DateTimeOffset()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateTimeOffset());

#if NET6_0_OR_GREATER
    public override Task DateOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateOnly());

    public override Task TimeOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.TimeOnly());
#endif

    public new class Fixture : TypeTests<ulong>.Fixture
    {
        public override TestStore TestStore => QdrantTestStore.Instance;
    }
}
