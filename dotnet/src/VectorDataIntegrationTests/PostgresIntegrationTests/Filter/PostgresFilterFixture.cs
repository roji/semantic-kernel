﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using PostgresIntegrationTests.Support;
using VectorDataSpecificationTests.Filter;

namespace PostgresIntegrationTests.Filter;

public class PostgresFilterFixture : FilterFixtureBase<int>
{
    private PostgresContainerWrapper _containerWrapper;
    private int _nextKey = 1;

    public override async Task InitializeAsync()
    {
        this._containerWrapper = await PostgresContainerWrapper.GetAsync();

        await base.InitializeAsync();
    }

    protected override IVectorStore GetVectorStore()
        => this._containerWrapper.DefaultVectorStore;

    protected override int GenerateNextKey()
        => this._nextKey++;

    public override async Task DisposeAsync()
    {
        await this._containerWrapper.DisposeAsync();
        await base.DisposeAsync();
    }
}
