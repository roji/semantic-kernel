// Copyright (c) Microsoft. All rights reserved.

using System.Linq.Expressions;
using Xunit;

namespace VectorDataSpecificationTests.Filter;

public abstract class BasicFilterTestsBase<TKey>(FilterFixtureBase<TKey> fixture)
    where TKey : notnull
{
    protected virtual async Task TestFilter(Expression<Func<FilterRecord<TKey>, bool>> filter)
    {
        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new() { NewFilter = filter });

        var expected = await results.Results.Select(r => r.Record).OrderBy(r => r.Key).ToListAsync();
        var actual = fixture.TestData.AsQueryable().Where(filter).OrderBy(r => r.Key).ToList();

        Assert.Equal(expected, actual, (e, a) =>
            e.Int == a.Int &&
            e.String == a.String &&
            e.Int2 == a.Int2);
    }

    [Fact]
    public virtual Task Equality_with_int()
        => this.TestFilter(r => r.Int == 8);

    [Fact]
    public virtual Task Equality_with_null_string()
        => this.TestFilter(r => r.String == null);

    [Fact]
    public virtual Task And()
        => this.TestFilter(r => r.Int == 8 && r.String == "foo");

    [Fact]
    public virtual Task Or()
        => this.TestFilter(r => r.Int == 8 || r.String == "foo");

    [Fact]
    public virtual Task And_within_And()
        => this.TestFilter(r => (r.Int == 8 && r.String == "foo") && r.Int2 == 80);

    [Fact]
    public virtual Task And_within_Or()
        => this.TestFilter(r => (r.Int == 8 && r.String == "foo") || r.Int2 == 100);

    [Fact]
    public virtual Task Or_within_And()
        => this.TestFilter(r => (r.Int == 8 || r.Int == 9) && r.String == "foo");
}
