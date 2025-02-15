// Copyright (c) Microsoft. All rights reserved.

using SqliteIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace SqliteIntegrationTests;

public class SqliteTypeTests(SqliteTypeTests.Fixture fixture)
    : TypeTests<ulong>(fixture), IClassFixture<SqliteTypeTests.Fixture>
{
    // Unsupported types
    public override Task Float()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Float());

    public override Task Double()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Double());

    public override Task Decimal()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Decimal());

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
        public override TestStore TestStore => SqliteTestStore.Instance;
    }
}
