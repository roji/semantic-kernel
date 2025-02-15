// Copyright (c) Microsoft. All rights reserved.

using RedisIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace RedisIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class RedisHashSetTypeTests(RedisHashSetTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<RedisHashSetTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();
    public override Task String() => base.String();

    public override Task Float() => base.Float();
    public override Task Double() => base.Double();

    // Bool isn't filterable on Redis
    public override Task Bool()
        => this.TestTypeStructAsync(true, false, isFilterable: false);

    // Unsupported types
    // TODO: The Json store throws InvalidOperation for unsupported types, not ArgumentExcewption
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

    protected override bool IsNullSupported => false;

    public new class Fixture : TypeTests<string>.Fixture
    {
        private int _collectionCounter = 1;

        public override TestStore TestStore => RedisHashSetTestStore.Instance;

        public override string CollectionName => "TypeTests" + (this._collectionCounter++);
    }
}
