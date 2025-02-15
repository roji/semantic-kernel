// Copyright (c) Microsoft. All rights reserved.

#if NET472
using System.Net.Http;
#endif
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Weaviate;
using VectorDataSpecificationTests.Support;
using WeaviateIntegrationTests.Support.TestContainer;

namespace WeaviateIntegrationTests.Support;

public sealed class WeaviateTestStore : TestStore
{
    public static WeaviateTestStore Instance { get; } = new();

    private readonly WeaviateContainer _container = new WeaviateBuilder().Build();
    public HttpClient? _httpClient { get; private set; }
    private WeaviateVectorStore? _defaultVectorStore;

    public HttpClient Client => this._httpClient ?? throw new InvalidOperationException("Not initialized");

    public override IVectorStore DefaultVectorStore => this._defaultVectorStore ?? throw new InvalidOperationException("Not initialized");

    public override string DefaultDistanceFunction => DistanceFunction.CosineDistance;

    public WeaviateVectorStore GetVectorStore(WeaviateVectorStoreOptions options)
        => new(this.Client, options);

    private WeaviateTestStore()
    {
    }

    protected override async Task StartAsync()
    {
        await this._container.StartAsync();
        this._httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{this._container.GetMappedPublicPort(WeaviateBuilder.WeaviateHttpPort)}/v1/") };
        this._defaultVectorStore = new CustomWeaviateVectorStore(this._httpClient);
    }

    protected override Task StopAsync()
        => this._container.StopAsync();

    // Custom WeaviateVectorStore that returns collections with IndexNullState, to allow filtering for null values
    private class CustomWeaviateVectorStore(HttpClient httpClient, WeaviateVectorStoreOptions? options = null)
        : WeaviateVectorStore(httpClient, options)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly WeaviateVectorStoreOptions _options = options ?? new();

        public override IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        {
            var recordCollection = new WeaviateVectorStoreRecordCollection<TRecord>(
                this._httpClient,
                name,
                new()
                {
                    VectorStoreRecordDefinition = vectorStoreRecordDefinition,
                    Endpoint = this._options.Endpoint,
                    ApiKey = this._options.ApiKey,
                    IndexNullState = true
                }) as IVectorStoreRecordCollection<TKey, TRecord>;

            return recordCollection!;
        }
    }
}
