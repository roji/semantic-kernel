// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateCollectionSchemaInvertedIndexConfig
{
    [JsonPropertyName("indexNullState")]
    public bool IndexNullState { get; set; }
}
