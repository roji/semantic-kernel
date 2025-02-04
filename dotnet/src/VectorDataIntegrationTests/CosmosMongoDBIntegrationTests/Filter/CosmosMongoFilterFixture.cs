// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using VectorDataSpecificationTests.Filter;
using CosmosNoSQLIntegrationTests.Support;

namespace MongoDBIntegrationTests.Filter;

public class CosmosMongoFilterFixture : FilterFixtureBase<string>
{
    public override async Task InitializeAsync()
    {
        await CosmosMongoDBTestEnvironment.InitializeAsync();

        await base.InitializeAsync();
    }

    protected override IVectorStore GetVectorStore()
        => CosmosMongoDBTestEnvironment.DefaultVectorStore;

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
