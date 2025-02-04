// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using VectorDataSpecificationTests.Filter;
using PineconeIntegrationTests.Support;

namespace PineconeIntegrationTests.Filter;

public class PineconeFilterFixture : FilterFixtureBase<string>
{
    private PineconeContainerWrapper _containerWrapper;

    protected override string DistanceFunction => Microsoft.Extensions.VectorData.DistanceFunction.EuclideanSquaredDistance;
    // protected override string DistanceFunction => Microsoft.Extensions.VectorData.DistanceFunction.CosineSimilarity;
    // protected virtual string IndexKind => Microsoft.Extensions.VectorData.IndexKind.Hnsw;

    public override async Task InitializeAsync()
    {
        this._containerWrapper = await PineconeContainerWrapper.GetAsync();

        await base.InitializeAsync();
    }

    protected override IVectorStore GetVectorStore()
        => this._containerWrapper.DefaultVectorStore;

    public override async Task DisposeAsync()
    {
        await this._containerWrapper.DisposeAsync();
        await base.DisposeAsync();
    }
}
