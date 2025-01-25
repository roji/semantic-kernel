// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using Microsoft.Extensions.VectorData;
using RedisIntegrationTests.Support;
using VectorDataSpecificationTests.Filter;

namespace RedisIntegrationTests.Filter;

public class RedisFilterFixture : FilterFixtureBase<string>
{
    private RedisContainerWrapper _containerWrapper;
    private ulong _nextKey = 1;

    public override async Task InitializeAsync()
    {
        this._containerWrapper = await RedisContainerWrapper.GetAsync();

        await base.InitializeAsync();
    }

    protected override IVectorStore GetVectorStore()
        => this._containerWrapper.DefaultVectorStore;

    protected override string GenerateNextKey()
        => (this._nextKey++).ToString(CultureInfo.InvariantCulture);

    public override async Task DisposeAsync()
    {
        await this._containerWrapper.DisposeAsync();
        await base.DisposeAsync();
    }
}
