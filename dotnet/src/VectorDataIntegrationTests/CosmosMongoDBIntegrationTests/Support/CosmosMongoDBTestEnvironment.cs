// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBMongoDB;
using MongoDB.Driver;

namespace CosmosNoSQLIntegrationTests.Support;

public static class CosmosMongoDBTestEnvironment
{
    public static MongoClient? s_client { get; private set; }
    public static IMongoDatabase? s_database { get; private set; }
    private static AzureCosmosDBMongoDBVectorStore? s_defaultVectorStore;

    public static MongoClient Client => s_client ?? throw new InvalidOperationException("Not initialized");
    public static IMongoDatabase Database => s_database ?? throw new InvalidOperationException("Not initialized");
    public static AzureCosmosDBMongoDBVectorStore DefaultVectorStore => s_defaultVectorStore ?? throw new InvalidOperationException("Not initialized");

    private static readonly string? s_connectionString;

    public static bool IsConnectionStringDefined => s_connectionString is not null;

    static CosmosMongoDBTestEnvironment()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        s_connectionString = configuration["AzureCosmosDBMongoDB:ConnectionString"];
    }

    public static async Task InitializeAsync()
    {
        if (s_client is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(s_connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured, set the AzureCosmosDBMongoDB:ConnectionString environment variable");
        }

        s_client = new MongoClient(s_connectionString);
        s_database = s_client.GetDatabase("VectorSearchTests");
        s_defaultVectorStore = new(s_database);
    }
}
