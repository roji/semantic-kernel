﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using VectorDataSpecificationTests.Filter;
using Xunit;
using Xunit.Sdk;

namespace RedisIntegrationTests.Filter;

public class WeaviateBasicFilterTests(WeaviateFilterFixture fixture) : BasicFilterTestsBase<Guid>(fixture), IClassFixture<WeaviateFilterFixture>
{
    #region Filter by null

    // Null-state indexing needs to be set up, but that's not supported yet (#10358).
    // We could interact with Weaviate directly (not via the abstraction) to do this.

    public override Task Equal_with_null_reference_type()
        => Assert.ThrowsAsync<VectorStoreOperationException>(() => base.Equal_with_null_reference_type());

    public override Task Equal_with_null_captured()
        => Assert.ThrowsAsync<VectorStoreOperationException>(() => base.Equal_with_null_captured());

    public override Task NotEqual_with_null_captured()
        => Assert.ThrowsAsync<VectorStoreOperationException>(() => base.NotEqual_with_null_captured());

    public override Task NotEqual_with_null_referenceType()
        => Assert.ThrowsAsync<VectorStoreOperationException>(() => base.NotEqual_with_null_referenceType());

    #endregion

    #region Not

    // Weaviate currently doesn't support NOT (https://github.com/weaviate/weaviate/issues/3683)
    public override Task Not_over_Equal()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Not_over_Equal());

    public override Task Not_over_NotEqual()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Not_over_NotEqual());

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

    #endregion

    // In Weaviate, string equality on multi-word textual properties depends on tokenization
    // (https://weaviate.io/developers/weaviate/api/graphql/filters#multi-word-queries-in-equal-filters)
    public override Task Equal_with_string_is_not_Contains()
        => Assert.ThrowsAsync<EqualException>(() => base.Equal_with_string_is_not_Contains());
}
