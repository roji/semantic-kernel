﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using QdrantIntegrationTests.Support;
using VectorDataSpecificationTests.Filter;

namespace QdrantIntegrationTests.Filter;

public class QdrantFilterFixture : FilterFixtureBase<ulong>
{
    private QdrantContainerWrapper _containerWrapper;
    private ulong _nextKey = 1;

    public override async Task InitializeAsync()
    {
        this._containerWrapper = await QdrantContainerWrapper.GetAsync();

        await base.InitializeAsync();
    }

    protected override IVectorStore GetVectorStore()
        => this._containerWrapper.DefaultVectorStore;

    protected override ulong GenerateNextKey()
        => this._nextKey++;

    public override async Task DisposeAsync()
    {
        await this._containerWrapper.DisposeAsync();
        await base.DisposeAsync();
    }
}
