// Copyright (c) Microsoft. All rights reserved.

using System.Linq.Expressions;
using Xunit;

namespace VectorDataSpecificationTests.Filter;

public abstract class BasicFilterTestsBase<TKey>(FilterFixtureBase<TKey> fixture)
    where TKey : notnull
{
    #region Equality

    [Fact]
    public virtual Task Equal_with_int()
        => this.TestFilter(r => r.Int == 8);

    [Fact]
    public virtual Task Equal_with_string()
        => this.TestFilter(r => r.String == "foo");

    [Fact]
    public virtual Task Equal_with_string_containing_special_characters()
        => this.TestFilter(r => r.String == """with special"characters'and\stuff""");

    [Fact]
    public virtual Task Equal_with_string_is_not_Contains()
        => this.TestFilter(r => r.String == "with", expectZeroResults: true);

    [Fact]
    public virtual Task Equal_reversed()
        => this.TestFilter(r => 8 == r.Int);

    [Fact]
    public virtual Task Equal_with_null_reference_type()
        => this.TestFilter(r => r.String == null);

    [Fact]
    public virtual Task Equal_with_null_captured()
    {
        string? s = null;

        return this.TestFilter(r => r.String == s);
    }

    [Fact]
    public virtual Task NotEqual_with_int()
        => this.TestFilter(r => r.Int != 8);

    [Fact]
    public virtual Task NotEqual_with_string()
        => this.TestFilter(r => r.String != "foo");

    [Fact]
    public virtual Task NotEqual_reversed()
        => this.TestFilter(r => r.Int != 8);

    [Fact]
    public virtual Task NotEqual_with_null_referenceType()
        => this.TestFilter(r => r.String != null);

    [Fact]
    public virtual Task NotEqual_with_null_captured()
    {
        string? s = null;

        return this.TestFilter(r => r.String != s);
    }

    #endregion Equality

    #region Comparison

    [Fact]
    public virtual Task GreaterThan_with_int()
        => this.TestFilter(r => r.Int > 9);

    [Fact]
    public virtual Task GreaterThanOrEqual_with_int()
        => this.TestFilter(r => r.Int >= 9);

    [Fact]
    public virtual Task LessThan_with_int()
        => this.TestFilter(r => r.Int < 10);

    [Fact]
    public virtual Task LessThanOrEqual_with_int()
        => this.TestFilter(r => r.Int <= 10);

    #endregion Comparison

    #region Logical operators

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

    [Fact]
    public virtual Task Not_over_Equal()
        // ReSharper disable once NegativeEqualityExpression
        => this.TestFilter(r => !(r.Int == 8));

    [Fact]
    public virtual Task Not_over_NotEqual()
        // ReSharper disable once NegativeEqualityExpression
        => this.TestFilter(r => !(r.Int != 8));

    [Fact]
    public virtual Task Not_over_And()
        => this.TestFilter(r => !(r.Int == 8 && r.String == "foo"));

    [Fact]
    public virtual Task Not_over_Or()
        => this.TestFilter(r => !(r.Int == 8 || r.String == "foo"));

    #endregion Logical operators

    #region Contains

    [Fact]
    public virtual Task Contains_over_field_string_array()
        => this.TestFilter(r => r.Strings.Contains("x"));

    [Fact]
    public virtual Task Contains_over_inline_int_array()
        => this.TestFilter(r => new[] { 8, 10 }.Contains(r.Int));

    [Fact]
    public virtual Task Contains_over_inline_string_array()
        => this.TestFilter(r => new[] { "foo", "baz", "unknown" }.Contains(r.String));

    [Fact]
    public virtual Task Contains_over_captured_string_array()
    {
        var array = new[] { "foo", "baz", "unknown" };

        return this.TestFilter(r => array.Contains(r.String));
    }

    #endregion Contains

    [Fact]
    public virtual Task Captured_variable()
    {
        // ReSharper disable once ConvertToConstant.Local
        var i = 8;

        return this.TestFilter(r => r.Int == i);
    }

    protected virtual async Task TestFilter(
        Expression<Func<FilterRecord<TKey>, bool>> filter,
        bool expectZeroResults = false,
        bool expectAllResults = false)
    {
        var expected = fixture.TestData.AsQueryable().Where(filter).OrderBy(r => r.Key).ToList();

        if (expected.Count == 0 && !expectZeroResults)
        {
            Assert.Fail("The test returns zero results, and so is unreliable");
        }

        if (expected.Count == fixture.TestData.Count && !expectAllResults)
        {
            Assert.Fail("The test returns all results, and so is unreliable");
        }

        var results = await fixture.Collection.VectorizedSearchAsync(
            new ReadOnlyMemory<float>([1, 2, 3]),
            new()
            {
                NewFilter = filter,
                Top = 9999
            });

        var actual = await results.Results.Select(r => r.Record).OrderBy(r => r.Key).ToListAsync();

        Assert.Equal(expected, actual, (e, a) =>
            e.Int == a.Int &&
            e.String == a.String &&
            e.Int2 == a.Int2);
    }
}
