// Copyright (c) Microsoft. All rights reserved.

using System.Linq.Expressions;
using Microsoft.Extensions.VectorData;
using VectorDataSpecificationTests.Support;
using VectorDataSpecificationTests.Xunit;
using Xunit;

namespace VectorDataSpecificationTests;

public class TypeTests<TKey>(TypeTests<TKey>.Fixture fixture)
    where TKey : notnull
{
    // Note: nullable value types are tested automatically within TestTypeStructAsync

    [ConditionalFact]
    public virtual Task Short()
        => this.TestTypeStructAsync<short>(8, 9);

    [ConditionalFact]
    public virtual Task Int()
        => this.TestTypeStructAsync(8, 9);

    [ConditionalFact]
    public virtual Task Long()
        => this.TestTypeStructAsync<long>(8, 9);

    [ConditionalFact]
    public virtual Task Float()
        => this.TestTypeStructAsync(8.5f, 9.5f);

    [ConditionalFact]
    public virtual Task Double()
        => this.TestTypeStructAsync(8.5, 9.5);

    [ConditionalFact]
    public virtual Task Decimal()
        => this.TestTypeStructAsync(8.5m, 9.5m);

    [ConditionalFact]
    public virtual Task String()
        => this.TestTypeAsync("foo", "bar");

    [ConditionalFact]
    public virtual Task Bool()
        => this.TestTypeStructAsync(true, false);

    [ConditionalFact]
    public virtual Task Guid()
        => this.TestTypeStructAsync(
            new Guid("603840bf-cf91-4521-8b8e-8b6a2e75910a"),
            new Guid("e9a97807-8cf0-4741-8ce3-82df676ca0f0"));

    [ConditionalFact]
    public virtual Task DateTime()
        => this.TestTypeStructAsync(
            new DateTime(2020, 1, 1, 12, 30, 45),
            new DateTime(2021, 2, 3, 13, 40, 55),
            instantiationExpression: () => new DateTime(2020, 1, 1, 12, 30, 45));

    [ConditionalFact]
    public virtual Task DateTimeOffset()
        => this.TestTypeStructAsync(
            new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.FromHours(2)),
            new DateTimeOffset(2021, 2, 3, 13, 40, 55, TimeSpan.FromHours(3)),
            instantiationExpression: () => new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.FromHours(2)));

#if NET6_0_OR_GREATER
    [ConditionalFact]
    public virtual Task DateOnly()
        => this.TestTypeStructAsync(
            new DateOnly(2020, 1, 1),
            new DateOnly(2021, 2, 3));

    [ConditionalFact]
    public virtual Task TimeOnly()
        => this.TestTypeStructAsync(
            new TimeOnly(12, 30, 45),
            new TimeOnly(13, 40, 55));
#endif

    protected virtual IVectorStoreRecordCollection<TKey, TypeTestsRecord<TTestType>> CreateCollection<TTestType>(
        string collectionName,
        VectorStoreRecordDefinition definition)
        => fixture.TestStore.DefaultVectorStore.GetCollection<TKey, TypeTestsRecord<TTestType>>(
            collectionName,
            definition);

    protected virtual IVectorStoreRecordCollection<TKey, TypeTestsRecord<TTestType>> CreateCollection<TTestType>(
        bool isFilterable)
        => this.CreateCollection<TTestType>(
            fixture.CollectionName,
            new()
            {
                Properties =
                [
                    new VectorStoreRecordKeyProperty(nameof(TypeTestsRecord<TTestType>.Key), typeof(TKey)),
                    new VectorStoreRecordVectorProperty(nameof(TypeTestsRecord<TTestType>.Vector), typeof(ReadOnlyMemory<float>?))
                    {
                        Dimensions = 3,
                        DistanceFunction = fixture.DefaultDistanceFunction,
                        IndexKind = fixture.DefaultIndexKind
                    },
                    new VectorStoreRecordDataProperty(nameof(TypeTestsRecord<TTestType>.Int), typeof(int)) { IsFilterable = true },

                    new VectorStoreRecordDataProperty(nameof(TypeTestsRecord<TTestType>.Value), typeof(TTestType)) { IsFilterable = isFilterable }
                ]
            });

    protected virtual bool IsNullSupported => true;

    protected virtual async Task TestTypeStructAsync<TTestType>(
        TTestType value1,
        TTestType value2,
        bool isFilterable = true,
        Expression<Func<TTestType>>? instantiationExpression = null)
        where TTestType : struct
    {
        // For value types, we also run the test on the nullable version
        await this.TestTypeAsync(value1, value2, isFilterable, instantiationExpression);
        await this.TestTypeAsync<TTestType?>(value1, value2, isFilterable);
    }

    protected virtual async Task TestTypeAsync<TTestType>(
        TTestType value1,
        TTestType value2,
        bool isFilterable = true,
        Expression<Func<TTestType>>? instantiationExpression = null)
    {
        var collection = this.CreateCollection<TTestType>(isFilterable);

        if (await collection.CollectionExistsAsync())
        {
            await collection.DeleteCollectionAsync();
        }

        await collection.CreateCollectionAsync();

        // Step 1: insert data

        // All records have the same vector
        var vector = new ReadOnlyMemory<float>([1, 2, 3]);
        var (key1, key2, key3) = (fixture.GenerateNextKey(), fixture.GenerateNextKey(), fixture.GenerateNextKey());

        List<TypeTestsRecord<TTestType>> testData =
        [
            new()
            {
                Key = key1,
                Vector = vector,
                Int = 1,
                Value = value1
            },
            new()
            {
                Key = key2,
                Vector = vector,
                Int = 2,
                Value = value2
            }
        ];

        if (default(TTestType) == null && this.IsNullSupported)
        {
            testData.Add(new()
            {
                Key = key3,
                Vector = vector,
                Int = 3,
                Value = (TTestType)(object?)null!
            });
        }

        // TODO: UpsertBatchAsync returns IAsyncEnumerable<TKey> (to support server-generated keys?), but this makes it quite hard to use:
        await foreach (var _ in collection.UpsertBatchAsync(testData))
        {
        }

        await fixture.TestStore.WaitForDataAsync(collection, recordCount: testData.Count, filter: r => r.Int > 0);

        // Step 2: Read the values back via GetAsync
        var record1 = await collection.GetAsync(key1);
        Assert.Equal(value1, record1!.Value); // TODO: Allow custom comparer

        // Step 3: Exercise filtering by the value, using a constant in the filter expression
        // Note: we need to manually build the expression tree since the equality operator can't be used over
        // unbounded generic types.
        var lambdaParameter = Expression.Parameter(typeof(TypeTestsRecord<TTestType>), "r");
        var filter = Expression.Lambda<Func<TypeTestsRecord<TTestType>, bool>>(
            Expression.Equal(
                Expression.Property(lambdaParameter, nameof(TypeTestsRecord<TTestType>.Value)),
                Expression.Constant(value1, typeof(TTestType))),
            lambdaParameter);

        if (!isFilterable)
        {
            // Type isn't filterable, verify exception
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                collection.VectorizedSearchAsync(
                    vector,
                    new() { NewFilter = filter, Top = 1 }));
            return;
        }

        var r = await collection.VectorizedSearchAsync(
            vector,
            new() { Top = 100 });

        var x = await r.Results.ToListAsync();

        var results = await collection.VectorizedSearchAsync(
            vector,
            new() { NewFilter = filter, Top = 1 });

        var record = (await results.Results.SingleAsync()).Record;
        Assert.Equal(1, record.Int);

        // Step 4: Exercise filtering by a null value
        if (default(TTestType) == null && this.IsNullSupported)
        {
            results = await collection.VectorizedSearchAsync(
                vector,
                new() { NewFilter = r => r.Value == null, Top = 1 });

            record = (await results.Results.SingleAsync()).Record;
            Assert.Equal(3, record.Int);
        }

        // Step 4: If an instantiation expression has been provided, integrate that into the filter tree and search
        // This is used to e.g. exercise filtering by r => r.DateTime == new DateTime(...))
        if (instantiationExpression is not null)
        {
            lambdaParameter = Expression.Parameter(typeof(TypeTestsRecord<TTestType>), "r");
            filter = Expression.Lambda<Func<TypeTestsRecord<TTestType>, bool>>(
                Expression.Equal(
                    Expression.Property(lambdaParameter, nameof(TypeTestsRecord<TTestType>.Value)),
                    instantiationExpression.Body),
                lambdaParameter);
            results = await collection.VectorizedSearchAsync(
                vector,
                new() { NewFilter = filter, Top = 1 });

            record = (await results.Results.SingleAsync()).Record;
            Assert.Equal(1, record.Int);
        }
    }

    public class TypeTestsRecord<TTestType>
    {
        public TKey Key { get; set; }
        public ReadOnlyMemory<float>? Vector { get; set; }
        public int Int { get; set; }

        public TTestType? Value { get; set; }
    }

    public abstract class Fixture : VectorStoreFixture
    {
        public virtual string CollectionName => "TypeTests";

        public virtual TKey GenerateNextKey()
            => base.GenerateNextKey<TKey>();
    }
}
