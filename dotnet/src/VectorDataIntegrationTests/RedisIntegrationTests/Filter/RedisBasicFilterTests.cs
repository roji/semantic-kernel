// Copyright (c) Microsoft. All rights reserved.

using VectorDataSpecificationTests.Filter;
using Xunit;

namespace RedisIntegrationTests.Filter;

public class RedisBasicFilterTests(RedisFilterFixture fixture) : BasicFilterTestsBase<string>(fixture), IClassFixture<RedisFilterFixture>
{
    public override Task Equal_with_null_string()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Equal_with_null_string());

    public override Task NotEqual_with_null_string()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Equal_with_null_string());

    public override Task Contains_over_inline_int_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_inline_int_array());

    public override Task Contains_over_inline_string_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_inline_string_array());

    public override Task Contains_over_captured_string_array()
        => Assert.ThrowsAsync<NotSupportedException>(() => base.Contains_over_captured_string_array());
}
