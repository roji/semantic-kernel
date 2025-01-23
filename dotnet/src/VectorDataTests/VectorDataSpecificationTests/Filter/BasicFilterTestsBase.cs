// Copyright (c) Microsoft. All rights reserved.

using Xunit;

namespace VectorDataSpecificationTests.Filter;

public abstract class BasicFilterTestsBase<TKey>(FilterFixtureBase<TKey> fixture)
    where TKey : notnull
{
    [Fact]
    public virtual async Task Equality_with_int()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => r.Int == 8 });

        // TODO: Have a test API which simply executes the same filter expression over an in-memory collection and compares the results,
        // instead of manually rewriting as an assertion
        Assert.All(results.Results, r => Assert.Equal(8, r.Record.Int));
    }

    [Fact]
    public virtual async Task Equality_with_null_string()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => r.String == null });

        Assert.All(results.Results, r => Assert.Null(r.Record.String));
    }

    [Fact]
    public virtual async Task And()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => r.Int == 8 && r.String == "foo" });

        Assert.All(results.Results, r =>
        {
            Assert.Equal(8, r.Record.Int);
            Assert.Equal("foo", r.Record.String);
        });
    }

    [Fact]
    public virtual async Task And_within_And()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => (r.Int == 8 && r.String == "foo") && r.Int2 == 80 });

        Assert.All(results.Results, r =>
        {
            Assert.Equal(8, r.Record.Int);
            Assert.Equal("foo", r.Record.String);
            Assert.Equal(80, r.Record.Int2);
        });
    }

    [Fact]
    public virtual async Task And_within_Or()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => (r.Int == 8 && r.String == "foo") || r.Int2 == 100 });

        Assert.All(results.Results, r => Assert.True(r.Record is { Int: 8, String: "foo" } || r.Record.Int2 == 100));
    }

    [Fact]
    public virtual async Task Or_within_And()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => (r.Int == 8 || r.Int == 9) && r.String == "foo" });

        Assert.All(results.Results, r =>
        {
            Assert.True(r.Record.Int is 8 or 9);
            Assert.Equal("foo", r.Record.String);
        });
    }

    [Fact]
    public virtual async Task Or()
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = r => r.Int == 8 || r.String == "foo" });

        Assert.All(
            results.Results,
            r => Assert.True(r.Record.Int == 8 || r.Record.String == "foo"));
    }
}
