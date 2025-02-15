// Copyright (c) Microsoft. All rights reserved.

using RedisIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace RedisIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class RedisJsonTypeTests(RedisJsonTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<RedisJsonTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Short() => base.Short();
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();
    public override Task String() => base.String();

    public override Task Float() => base.Float();
    public override Task Double() => base.Double();

    // Bool isn't filterable on Redis
    public override Task Bool()
        => this.TestTypeStructAsync(true, false, isFilterable: false);

    // Unsupported types
    public override Task Decimal()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.Decimal());

    public override Task Guid()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.Guid());

    public override Task DateTime()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.DateTime());

    public override Task DateTimeOffset()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.DateTimeOffset());

#if NET6_0_OR_GREATER
    public override Task DateOnly()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.DateOnly());

    public override Task TimeOnly()
        => Assert.ThrowsAsync<InvalidOperationException>(() => base.TimeOnly());
#endif

    protected override bool IsNullSupported => false;

    public new class Fixture : TypeTests<string>.Fixture
    {
        private int _collectionCounter = 1;

        public override TestStore TestStore => RedisJsonTestStore.Instance;

        public override string CollectionName => "TypeTests" + (this._collectionCounter++);
    }
}
