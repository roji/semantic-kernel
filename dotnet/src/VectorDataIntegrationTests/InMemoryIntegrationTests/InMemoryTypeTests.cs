// Copyright (c) Microsoft. All rights reserved.

using InMemoryIntegrationTests.Support;
using VectorDataSpecificationTests;
using VectorDataSpecificationTests.Support;
using Xunit;

namespace InMemoryIntegrationTests;

// ReSharper disable RedundantOverriddenMember

public class InMemoryTypeTests(InMemoryTypeTests.Fixture fixture)
    : TypeTests<string>(fixture), IClassFixture<InMemoryTypeTests.Fixture>
{
    // Fully-supported types
    public override Task Short() => base.Short();
    public override Task Int() => base.Int();
    public override Task Long() => base.Long();

    public override Task Float() => base.Float();
    public override Task Double() => base.Double();
    public override Task Decimal() => base.Decimal();

    public override Task String() => base.String();
    public override Task Bool() => base.Bool();
    public override Task Guid() => base.Guid();

    public override Task DateTime() => base.DateTime();
    public override Task DateTimeOffset() => base.DateTimeOffset();

#if NET6_0_OR_GREATER
    public override Task DateOnly() => base.DateOnly();
    public override Task TimeOnly() => base.TimeOnly();
#endif

    public new class Fixture : TypeTests<string>.Fixture
    {
        public override TestStore TestStore => InMemoryTestStore.Instance;
    }
}
