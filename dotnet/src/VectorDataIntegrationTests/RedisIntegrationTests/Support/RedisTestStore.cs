// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using VectorDataSpecificationTests.Support;

namespace RedisIntegrationTests.Support;

internal abstract class RedisTestStore : TestStore
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis/redis-stack")
        .Build();

    private IDatabase? _database;

    public IDatabase Database => this._database ?? throw new InvalidOperationException("Not initialized");

    public RedisVectorStore GetVectorStore(RedisVectorStoreOptions options)
        => new(this.Database, options);

    protected override async Task StartAsync()
    {
        await this._container.StartAsync();
        var redis = await ConnectionMultiplexer.ConnectAsync($"{this._container.Hostname}:{this._container.GetMappedPublicPort(6379)},connectTimeout=60000,connectRetry=5");
        this._database = redis.GetDatabase();
    }

    protected override Task StopAsync()
        => this._container.StopAsync();
}

internal sealed class RedisJsonTestStore : RedisTestStore
{
    private RedisVectorStore? _defaultVectorStore;

    public override IVectorStore DefaultVectorStore => this._defaultVectorStore ?? throw new InvalidOperationException("Not initialized");

    public static RedisJsonTestStore Instance { get; } = new();

    private RedisJsonTestStore()
    {
    }

    protected override async Task StartAsync()
    {
        await base.StartAsync();

        this._defaultVectorStore = new(this.Database, new() { StorageType = RedisStorageType.Json});
    }
}

internal sealed class RedisHashSetTestStore : RedisTestStore
{
    private RedisVectorStore? _defaultVectorStore;

    public override IVectorStore DefaultVectorStore => this._defaultVectorStore ?? throw new InvalidOperationException("Not initialized");

    public static RedisHashSetTestStore Instance { get; } = new();

    private RedisHashSetTestStore()
    {
    }

    protected override async Task StartAsync()
    {
        await base.StartAsync();

        this._defaultVectorStore = new(this.Database, new() { StorageType = RedisStorageType.HashSet });
    }
}
