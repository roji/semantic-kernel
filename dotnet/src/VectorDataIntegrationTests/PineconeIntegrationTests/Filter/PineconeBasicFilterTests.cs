// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using VectorDataSpecificationTests.Filter;
using Xunit;
using Xunit.Sdk;

namespace PineconeIntegrationTests.Filter;

public class PineconeBasicFilterTests(PineconeFilterFixture fixture) : BasicFilterTestsBase<string>(fixture), IClassFixture<PineconeFilterFixture>
{
}
