// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using MongoDBIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace MongoDBIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class MongoDBTypeTests(MongoDBTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<MongoDBTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();
    public override Task String() => base.String();
    public override Task Bool() => base.Bool();

    // Unspecified DateTime gets sent, the value read back is a converted UTC DateTime
    public override Task DateTime()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Short());

    // Unsupported types

    // Command aggregate failed: Operand type is not supported for $vectorSearch: decimal.
    public override Task Decimal()
        => Assert.ThrowsAsync<VectorStoreOperationException>(() => base.Decimal());

    public override Task Short()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Short());

    public override Task Guid()
        => Assert.ThrowsAsync<ArgumentException>(() => base.Guid());

    public override Task DateTimeOffset()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateTimeOffset());

#if NET6_0_OR_GREATER
    public override Task DateOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.DateOnly());

    public override Task TimeOnly()
        => Assert.ThrowsAsync<ArgumentException>(() => base.TimeOnly());
#endif

    // MongoDB does not support null checks in vector search pre-filters
    protected override bool IsNullSupported => false;

    public new class Fixture : TypeTests<string>.Fixture
    {
        public override TestStore TestStore => MongoDBTestStore.Instance;
    }
}
