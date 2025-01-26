// Copyright (c) Microsoft. All rights reserved.

using VectorDataSpecificationTests.Filter;
using Xunit;
using Xunit.Sdk;

namespace PostgresIntegrationTests.Filter;

public class PostgresBasicFilterTests(PostgresFilterFixture fixture) : BasicFilterTestsBase<int>(fixture), IClassFixture<PostgresFilterFixture>
{
    // Test sends: WHERE (NOT (("Int" = 8) OR ("String" = 'foo')))
    // There's a NULL string in the database, and relational null semantics in conjunction with negation makes the default implementation fail.
    public override Task Not_over_Or()
        => this.TestFilter(r => r.String != null && !(r.Int == 8 || r.String == "foo"));

    // As above, null semantics + negation
    public override Task NotEqual_with_string()
        => this.TestFilter(r => r.String != null && r.String != "foo");
}
