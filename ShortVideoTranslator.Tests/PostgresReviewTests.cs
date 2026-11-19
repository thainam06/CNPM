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

    [DisposablePostgresFact]
    public void LegacyPreparationPreservesSchemaAndRefusesAnyExistingRows()
    {
        var options = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SMARTTOUR_TEST_POSTGRES"));
        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal("smarttour_tests", options.Database);
        using var admin = new NpgsqlConnection(options.ConnectionString);
        admin.Open();
        var name = "smarttour_migration_" + Guid.NewGuid().ToString("N");
        using (var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin)) create.ExecuteNonQuery();
        try {
            options.Database = name; options.Pooling = false;
            using var db = new NpgsqlConnection(options.ConnectionString); db.Open();
            void Execute(string sql) { using var command = new NpgsqlCommand(sql, db); command.ExecuteNonQuery(); }
            long Count(string sql) { using var command = new NpgsqlCommand(sql, db); return (long)command.ExecuteScalar()!; }
            Execute("CREATE SCHEMA smarttour_demo; CREATE TABLE smarttour_demo.roles(id int PRIMARY KEY)");
            for (var i = 0; i < 14; i++) Execute($"CREATE TABLE smarttour_demo.fixture_{i}(id int PRIMARY KEY)");
            Execute("INSERT INTO smarttour_demo.fixture_13 VALUES (42)");
            var error = Assert.Throws<PostgresException>(() => Execute(Resource("007prepare")));
            Assert.Equal("P0001", error.SqlState);
            Execute("ROLLBACK");
            Assert.Equal(1, Count("SELECT count(*) FROM smarttour_demo.fixture_13"));
            Assert.Equal(0, Count("SELECT count(*) FROM pg_namespace WHERE nspname='smarttour_demo_legacy_15'"));
            Execute("DELETE FROM smarttour_demo.fixture_13");
            Execute(Resource("007prepare"));
            Assert.Equal(15, Count("SELECT count(*) FROM pg_tables WHERE schemaname='smarttour_demo_legacy_15'"));
            Execute(Resource("002")); Execute(Resource("005"));
            Execute(Resource("008"));
            Execute("INSERT INTO smarttour_demo.visitor_sessions(id,device_id,last_seen_at,ended_at) VALUES ('a1','device-a',now(),NULL),('a2','device-a',now()-interval '60 seconds',NULL),('b','device-b',now()-interval '60 seconds',NULL),('c','device-c',now(),now())");
            Assert.Equal(1, Count("SELECT count(*) FROM smarttour_demo.thiet_bi_truy_cap WHERE trang_thai='Đang hoạt động' AND so_tab_hoat_dong=1"));
            Assert.Equal(2, Count("SELECT count(*) FROM smarttour_demo.thiet_bi_truy_cap WHERE trang_thai='Đã ngắt kết nối'"));
            Execute("UPDATE smarttour_demo.visitor_sessions SET last_seen_at=now()-interval '60 seconds' WHERE id='a1'");
            Assert.Equal(0, Count("SELECT count(*) FROM smarttour_demo.thiet_bi_truy_cap WHERE trang_thai='Đang hoạt động'"));
            Execute("DELETE FROM smarttour_demo.visitor_sessions");
            Assert.Equal(0, Count("SELECT count(*) FROM smarttour_demo.thiet_bi_truy_cap"));
            using var store = new PostgresPlatformStore(options.ConnectionString);
            var report = JsonSerializer.SerializeToElement(store.CheckSchema());
            Assert.True(report.GetProperty("compatible").GetBoolean());
            Assert.True(report.GetProperty("empty").GetBoolean());
            Assert.Throws<PostgresException>(() => Execute(Resource("007prepare")));
            Execute("ROLLBACK");
            Assert.Equal(6, Count("SELECT count(*) FROM pg_tables WHERE schemaname='smarttour_demo'"));
        } finally {
            using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin); drop.ExecuteNonQuery();
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(PostgresReviewTests).Assembly.GetManifestResourceStream($"ReviewTest.{name}.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
