// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Connectors.Pinecone;
using PineconeIntegrationTests.Support.TestContainer;
using Sdk = Pinecone;

namespace PineconeIntegrationTests.Support;

public class PineconeContainerWrapper : IAsyncDisposable
{
    private static readonly PineconeContainer s_container = new PineconeBuilder().Build();
    public static Sdk.PineconeClient? s_client { get; private set; }
    private static PineconeVectorStore? s_defaultVectorStore;

    private static readonly SemaphoreSlim s_lock = new(1, 1);
    private static int s_referenceCount;

    public Sdk.PineconeClient Client => s_client ?? throw new InvalidOperationException("Not initialized");
    public PineconeVectorStore DefaultVectorStore => s_defaultVectorStore ?? throw new InvalidOperationException("Not initialized");

    private PineconeContainerWrapper()
    {
    }

    public static async Task<PineconeContainerWrapper> GetAsync()
    {
        await s_lock.WaitAsync();
        try
        {
            if (s_referenceCount++ == 0)
            {
                await s_container.StartAsync();
                s_client = new Sdk.PineconeClient("pclocal", s_container.Uri);
                s_defaultVectorStore = new(s_client);
            }
        }
        finally
        {
            s_lock.Release();
        }

        return new();
    }

    public async ValueTask DisposeAsync()
    {
        await s_lock.WaitAsync();
        try
        {
            if (--s_referenceCount == 0)
            {
                await s_container.StopAsync();
            }
        }
        finally
        {
            s_lock.Release();
        }

        GC.SuppressFinalize(this);
    }
}
