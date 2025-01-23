// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using Xunit;

namespace VectorDataSpecificationTests.Filter;

public abstract class FilterFixtureBase<TKey> : IAsyncLifetime
    where TKey : notnull
{
    public virtual async Task InitializeAsync()
    {
        var vectorStore = this.GetVectorStore();
        this.Collection = vectorStore.GetCollection<TKey, FilterRecord<TKey>>(this.StoreName);

        if (await this.Collection.CollectionExistsAsync())
        {
            await this.Collection.DeleteCollectionAsync();
        }

        await this.Collection.CreateCollectionAsync();
        await this.Seed();
    }

    public virtual IVectorStoreRecordCollection<TKey, FilterRecord<TKey>> Collection { get; private set; } = null!;

    protected abstract IVectorStore GetVectorStore();

    protected virtual async Task Seed()
    {
        // All records have the same vector - this fixture is about testing criteria filtering only
        var vector = new ReadOnlyMemory<float>([1, 2, 3]);

        FilterRecord<TKey>[] records =
        [
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 8,
                String = "foo",
                Int2 = 80,
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 9,
                String = "bar",
                Int2 = 90,
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 9,
                String = "foo",
                Int2 = 9,
                Vector = vector
            },
            new()
            {
                Key = this.GenerateNextKey(),
                Int = 10,
                String = null,
                Int2 = 100,
                Vector = vector
            }
        ];

        // TODO: UpsertBatchAsync returns IAsyncEnumerable<TKey> (to support server-generated keys?), but this makes it quite hard to use:
        await foreach (var _ in this.Collection.UpsertBatchAsync(records))
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

    // TODO: Move this to an overridable function on the fixture, make dimensions configurable
    [VectorStoreRecordVector(3, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public required ReadOnlyMemory<float> Vector { get; set; }
}
