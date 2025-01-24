// Copyright (c) Microsoft. All rights reserved.

using VectorDataSpecificationTests.Filter;
using Xunit;

namespace QdrantTests.Filter;

public class QdrantBasicFilterTests(QdrantFilterFixture fixture) : BasicFilterTestsBase<ulong>(fixture), IClassFixture<QdrantFilterFixture>
{
    public override Task And_within_Or()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.And_within_Or());
}
