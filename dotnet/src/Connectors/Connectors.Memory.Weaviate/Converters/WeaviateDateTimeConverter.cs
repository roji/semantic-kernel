// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

/// <summary>
/// Converts datetime type to RFC 3339 formatted string.
/// </summary>
internal sealed class WeaviateDateTimeConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        var dateTimeOffset = DateTimeOffset.Parse(dateString, CultureInfo.InvariantCulture);

        return dateTimeOffset.Offset == TimeSpan.Zero
            ? dateTimeOffset.DateTime
            : throw new NotSupportedException("Can't read timestamp with non-zero offset as DateTime. Read it as DateTimeOffset instead.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value.Kind is not DateTimeKind.Utc)
        {
            throw new NotSupportedException("Weaviate only supports writing DateTime with Kind=Utc");
        }

        writer.WriteStringValue(value.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
    }
}
