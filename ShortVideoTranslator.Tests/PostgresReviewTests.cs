using System.Text.Json;
using Npgsql;
using ShortVideoTranslator.Api.Services;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PostgresReviewTests
{
    [Fact]
    public void MissingPrivateConfigurationReturnsSafeDiagnosticWithoutConnecting()
    {
        var report = SupabaseReview.Run(() => throw new InvalidOperationException("Password=private-value"), true);
        Assert.False(report.Success);
        Assert.Equal("CONFIG", report.Code);
        Assert.Equal("configuration", report.Stage);
        Assert.Null(report.Connection);
        Assert.Empty(report.Sections);
        Assert.DoesNotContain("private-value", JsonSerializer.Serialize(report));
    }

    [DisposablePostgresFact]
    public void ReadOnlyReportChecksRealTablesWithoutBootstrappingOrLeakingState()
    {
        var adminOptions = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SMARTTOUR_TEST_POSTGRES"));
        Assert.Equal("127.0.0.1", adminOptions.Host);
        Assert.Equal("smarttour_tests", adminOptions.Database);
        var database = "smarttour_review_" + Guid.NewGuid().ToString("N");
        using var admin = new NpgsqlConnection(adminOptions.ConnectionString);
        admin.Open();
        using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin)) create.ExecuteNonQuery();
        var options = new NpgsqlConnectionStringBuilder(adminOptions.ConnectionString) { Database = database, Pooling = false };
        var folder = Path.Combine(Path.GetTempPath(), database);
        try
        {
            using var db = new NpgsqlConnection(options.ConnectionString);
            db.Open();
            foreach (var script in new[] { "002", "005" })
            {
                using var command = new NpgsqlCommand(Resource(script), db);
                command.ExecuteNonQuery();
            }
            var empty = SupabaseReview.Run(() => options.ConnectionString);
            Assert.False(empty.Success);
            Assert.Equal("REVIEW_ISSUES", empty.Code);
            Assert.Contains(empty.Issues, issue => issue.Contains("state envelope"));
            using (var count = new NpgsqlCommand("SELECT count(*) FROM smarttour_demo.users", db))
                Assert.Equal(0L, count.ExecuteScalar()); // Review did not bootstrap.

            var json = new JsonPlatformStore(Path.Combine(folder, "platform.json"));
            new PlatformService(json).Bootstrap("private-review-username", "PrivateReviewPassword123!");
            using var store = new PostgresPlatformStore(options.ConnectionString);
            store.Import(json.Read(state => state));
            var before = store.Read(state => JsonSerializer.Serialize(state));
            var report = SupabaseReview.Run(() => options.ConnectionString);
            Assert.True(report.Success, JsonSerializer.Serialize(report));
            Assert.Null(report.Connection);
            Assert.Equal("on", report.Sections.Single(s => s.Name == "transaction-1").Rows.Single()["transaction_read_only"]);
            Assert.Equal(44, report.Sections.Single(s => s.Name == "schema-2").Rows.Count);
            var serialized = JsonSerializer.Serialize(report);
            foreach (var secret in new[] { adminOptions.Password!, "private-review-username", "PrivateReviewPassword123!", json.Read(s => s.Users[0].PasswordHash) })
                Assert.DoesNotContain(secret, serialized);
            Assert.Equal(before, store.Read(state => JsonSerializer.Serialize(state)));

            var withInfo = SupabaseReview.Run(() => options.ConnectionString, true);
            Assert.True(withInfo.Success);
            Assert.Equal(options.Host, withInfo.Connection!.Host);
            Assert.Equal(options.Port, withInfo.Connection.Port);
            Assert.Equal(options.Database, withInfo.Connection.Database);
            Assert.Equal(options.Username, withInfo.Connection.User);
            Assert.DoesNotContain(adminOptions.Password!, JsonSerializer.Serialize(withInfo));

            // Execute all handoff data queries too, under enforced read-only mode.
            using (var tx = db.BeginTransaction())
            {
                using (var mode = new NpgsqlCommand("SET TRANSACTION READ ONLY", db, tx)) mode.ExecuteNonQuery();
                using var query = new NpgsqlCommand(Resource("02"), db, tx);
                using (var reader = query.ExecuteReader())
                    do { while (reader.Read()) { } } while (reader.NextResult());
                tx.Rollback();
            }
            using (var rename = new NpgsqlCommand("ALTER TABLE smarttour_demo.users RENAME COLUMN display_name TO unexpected_name", db))
                rename.ExecuteNonQuery();
            var mismatch = SupabaseReview.Run(() => options.ConnectionString);
            Assert.False(mismatch.Success);
            Assert.Contains("Column contract mismatch: users.", mismatch.Issues);
            Assert.DoesNotContain(mismatch.Sections, section => section.Name.StartsWith("integrity"));
            using (var change = new NpgsqlCommand("ALTER TABLE smarttour_demo.users RENAME COLUMN unexpected_name TO display_name; ALTER TABLE smarttour_demo.users ALTER COLUMN display_name TYPE integer USING 0", db))
                change.ExecuteNonQuery();
            var wrongType = SupabaseReview.Run(() => options.ConnectionString);
            Assert.False(wrongType.Success);
            Assert.Contains("Column type mismatch: users.", wrongType.Issues);
        }
        finally
        {
            using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
            drop.ExecuteNonQuery();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(PostgresReviewTests).Assembly.GetManifestResourceStream($"ReviewTest.{name}.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
