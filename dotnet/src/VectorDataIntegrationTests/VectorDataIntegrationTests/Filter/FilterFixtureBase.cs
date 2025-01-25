// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using Xunit;

namespace VectorDataSpecificationTests.Filter;

public abstract class FilterFixtureBase<TKey> : IAsyncLifetime
    where TKey : notnull
{
    private List<FilterRecord<TKey>>? _testData;

    public virtual async Task InitializeAsync()
    {
        var vectorStore = this.GetVectorStore();
        this.Collection = vectorStore.GetCollection<TKey, FilterRecord<TKey>>(this.StoreName);

        if (await this.Collection.CollectionExistsAsync())
        {
            await this.Collection.DeleteCollectionAsync();
        }

        await this.Collection.CreateCollectionAsync();
        await this.SeedAsync();
    }

    public virtual IVectorStoreRecordCollection<TKey, FilterRecord<TKey>> Collection { get; private set; } = null!;

    protected abstract IVectorStore GetVectorStore();

    public List<FilterRecord<TKey>> TestData => this._testData ??= this.GetTestData();

    protected virtual List<FilterRecord<TKey>> GetTestData()
    {
        // All records have the same vector - this fixture is about testing criteria filtering only
        var vector = new ReadOnlyMemory<float>([1, 2, 3]);

        return
        [
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 8,
                String = "foo",
                Int2 = 80,
                Strings = ["x", "y"],
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 9,
                String = "bar",
                Int2 = 90,
                Strings = ["a", "b"],
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 9,
                String = "foo",
                Int2 = 9,
                Strings = ["x"],
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 10,
                String = null,
                Int2 = 100,
                Strings = ["x", "y", "z"],
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 11,
                String = "baz",
                Int2 = 101,
                Strings = ["y", "z"],
                Vector = vector
            }
        ];
    }

    protected virtual async Task SeedAsync()
    {
        // TODO: UpsertBatchAsync returns IAsyncEnumerable<TKey> (to support server-generated keys?), but this makes it quite hard to use:
        await foreach (var _ in this.Collection.UpsertBatchAsync(this.TestData))
        {
        }
    }

    protected abstract TKey GenerateNextKey();

    protected virtual string StoreName => "FilterTests";

    public virtual Task DisposeAsync() => Task.CompletedTask;
}

public class FilterRecord<TKey>
{
    [VectorStoreRecordKey]
    public required TKey Key { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required int Int { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required string? String { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required int Int2 { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required string[] Strings { get; set; }

    // TODO: Move this to an overridable function on the fixture, make dimensions configurable
    [VectorStoreRecordVector(3, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public required ReadOnlyMemory<float> Vector { get; set; }
}
