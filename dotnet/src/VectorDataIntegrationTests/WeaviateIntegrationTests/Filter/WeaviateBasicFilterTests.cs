// Copyright (c) Microsoft. All rights reserved.

using VectorDataSpecificationTests.Filter;
using VectorDataSpecificationTests.Support;
using WeaviateIntegrationTests.Support;
using Xunit;
using Xunit.Sdk;

namespace WeaviateIntegrationTests.Filter;

public class WeaviateBasicFilterTests(WeaviateBasicFilterTests.Fixture fixture)
    : BasicFilterTests<Guid>(fixture), IClassFixture<WeaviateBasicFilterTests.Fixture>
{
    #region Not

    // Weaviate currently doesn't support NOT (https://github.com/weaviate/weaviate/issues/3683)
    public override Task Not_over_And()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Not_over_And());

    public override Task Not_over_Or()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Not_over_Or());

    #endregion

    #region Unsupported Contains scenarios

    public override Task Contains_over_captured_string_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_captured_string_array());

    public override Task Contains_over_inline_int_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_inline_int_array());

    public override Task Contains_over_inline_string_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_inline_int_array());

    public override Task Contains_over_inline_string_array_with_weird_chars()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_inline_string_array_with_weird_chars());

    #endregion

    // In Weaviate, string equality on multi-word textual properties depends on tokenization
    // (https://weaviate.io/developers/weaviate/api/graphql/filters#multi-word-queries-in-equal-filters)
    public override Task Equal_with_string_is_not_Contains()
        => Assert.ThrowsAsync<EqualException>(() => base.Equal_with_string_is_not_Contains());

    public new class Fixture : BasicFilterTests<Guid>.Fixture
    {
        public override TestStore TestStore => WeaviateTestStore.Instance;
    }
}
