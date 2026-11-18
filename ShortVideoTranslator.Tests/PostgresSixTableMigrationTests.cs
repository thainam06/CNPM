using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

internal sealed class FifteenTableFixture : IDisposable
{
    private readonly NpgsqlConnection admin;
    private readonly string database = "smarttour_migration_" + Guid.NewGuid().ToString("N");
    private readonly string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    public string ConnectionString { get; }
    public string? OriginalState { get; }
    public FifteenTableFixture(bool seed = true)
    {
        var options = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SMARTTOUR_TEST_POSTGRES"));
        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal("smarttour_tests", options.Database);
        admin = new NpgsqlConnection(options.ConnectionString);
        admin.Open();
        using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin)) create.ExecuteNonQuery();
        options.Database = database; options.Pooling = false;
        ConnectionString = options.ConnectionString;
        try
        {
            Execute(Resource("002")); Execute(Resource("005"));
            if (seed)
            {
                var local = new JsonPlatformStore(Path.Combine(folder, "platform.json"));
                var platform = new PlatformService(local);
                platform.Bootstrap("admin", "MigrationInitial123!");
                platform.ChangePassword(local.Read(s => s.Users[0].Id), "MigrationInitial123!", "MigrationChanged123!");
                local.Write(s => {
                    s.Places[0] = s.Places[0] with {
                        ImageUrl = "https://example.test/cover.jpg",
                        Images = [new("https://example.test/cover.jpg", "Fixture", "Fixture", "https://example.test/source")]
                    };
                    return true;
                });
                using var store = new PostgresPlatformStore(ConnectionString);
                store.Import(local.Read(s => s));
                OriginalState = store.Read(s => JsonSerializer.Serialize(s));
            }
            Execute(Resource("004"));
        }
        catch { Dispose(); throw; }
    }
    public void Execute(string sql)
    {
        using var db = new NpgsqlConnection(ConnectionString); db.Open();
        using var command = new NpgsqlCommand(sql, db); command.ExecuteNonQuery();
    }
    public T Scalar<T>(string sql)
    {
        using var db = new NpgsqlConnection(ConnectionString); db.Open();
        using var command = new NpgsqlCommand(sql, db); return (T)command.ExecuteScalar()!;
    }
    public long Tables(string schema = "smarttour_demo") => Scalar<long>($"SELECT count(*) FROM information_schema.tables WHERE table_schema='{schema}' AND table_type='BASE TABLE'");
    public JsonObject Snapshot(string schema = "smarttour_demo")
    {
        using var db = new NpgsqlConnection(ConnectionString); db.Open();
        var names = new List<string>();
        using (var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema=@schema AND table_type='BASE TABLE' ORDER BY table_name", db))
        {
            command.Parameters.AddWithValue("schema", schema);
            using var reader = command.ExecuteReader(); while (reader.Read()) names.Add(reader.GetString(0));
        }
        var result = new JsonObject();
        foreach (var name in names)
        {
            using var command = new NpgsqlCommand($"SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb)::text FROM {schema}.{name} t", db);
            result[name] = JsonNode.Parse((string)command.ExecuteScalar()!);
        }
        return result;
    }
    public void Dispose()
    {
        try { using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)", admin); drop.ExecuteNonQuery(); }
        finally { admin.Dispose(); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    internal static string Resource(string name)
    {
        using var stream = typeof(FifteenTableFixture).Assembly.GetManifestResourceStream($"ReviewTest.{name}.sql")!;
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
}

public class PostgresSixTableMigrationTests
{
    [Fact]
    public void ApplyRequiresBackupConfirmationBeforeTryingAnyConnection()
    {
        var result = PostgresSixTableMigration.Run("Password=private-fixture", apply: true);
        Assert.False(result.Success);
        Assert.Equal("BACKUP_REQUIRED", result.Code);
        Assert.DoesNotContain("private-fixture", JsonSerializer.Serialize(result));
    }

    [DisposablePostgresFact]
    public void PlanThenMigratePreservesOriginalArchiveAndEveryBackendCollection()
    {
        using var fixture = new FifteenTableFixture();
        var original = fixture.Snapshot();
        fixture.Execute(FifteenTableFixture.Resource("007"));
        var plan = PostgresSixTableMigration.Run(fixture.ConnectionString);
        Assert.True(plan.Success, JsonSerializer.Serialize(plan));
        Assert.Equal("PLAN_READY", plan.Code);
        Assert.True(plan.Initialized);
        Assert.Equal(15, plan.Counts.Count);
        Assert.Equal(15, fixture.Tables());
        Assert.Equal(0, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        Assert.True(JsonNode.DeepEquals(original, fixture.Snapshot()));
        var confirmation = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true);
        Assert.Equal("SNAPSHOT_CONFIRMATION_REQUIRED", confirmation.Code);
        Assert.Equal(15, fixture.Tables());
        var applied = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true);
        Assert.True(applied.Success);
        Assert.Equal("MIGRATED", applied.Code);
        Assert.Equal(6, fixture.Tables());
        Assert.Equal(15, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        Assert.True(JsonNode.DeepEquals(original, fixture.Snapshot(PostgresSixTableMigration.ArchiveSchema)));
        Assert.Equal(44L, fixture.Scalar<long>("SELECT count(*) FROM information_schema.columns WHERE table_schema='smarttour_demo'"));
        Assert.Equal(5L, fixture.Scalar<long>("SELECT count(*) FROM pg_constraint WHERE connamespace='smarttour_demo'::regnamespace AND contype='f' AND condeferrable"));
        Assert.Equal(6L, fixture.Scalar<long>("SELECT count(*) FROM pg_tables WHERE schemaname='smarttour_demo' AND rowsecurity"));
        using var store = new PostgresPlatformStore(fixture.ConnectionString);
        Assert.Equal(fixture.OriginalState, store.Read(s => JsonSerializer.Serialize(s)));
        store.ProbeWrite();
        Assert.Equal("ALREADY_COMPACT", PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true).Code);
        foreach (var table in new[] { "places", "place_images" })
        {
            var maximum = fixture.Scalar<long>($"SELECT max(id) FROM smarttour_demo.{table}");
            var next = fixture.Scalar<long>($"SELECT nextval(pg_get_serial_sequence('smarttour_demo.{table}','id'))");
            Assert.True(next > maximum);
        }
    }

    [DisposablePostgresFact]
    public void PopulatedUnmappedWorkflowsAndExternalDependenciesAreNotSilentlyDiscarded()
    {
        using var fixture = new FifteenTableFixture();
        fixture.Execute("INSERT INTO smarttour_demo.menu_items(id,place_id,name) SELECT 'fixture-menu',id,'Fixture' FROM smarttour_demo.places LIMIT 1");
        var before = fixture.Snapshot();
        var blocked = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true);
        Assert.Equal("MAPPING_REQUIRED", blocked.Code);
        Assert.Contains(blocked.Blockers, b => b.StartsWith("menu_items"));
        Assert.True(JsonNode.DeepEquals(before, fixture.Snapshot()));
        Assert.Equal(0, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        fixture.Execute("DELETE FROM smarttour_demo.menu_items WHERE id='fixture-menu'; CREATE TABLE public.outside_ref(id text, user_id text REFERENCES smarttour_demo.users(id))");
        var dependency = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true);
        Assert.Contains(dependency.Blockers, b => b.StartsWith("Other schemas"));
        fixture.Execute("DROP TABLE public.outside_ref; CREATE SCHEMA smarttour_demo_archive15");
        Assert.Contains(PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true).Blockers, b => b.StartsWith("Archive schema"));
        Assert.Equal(15, fixture.Tables());
    }

    [DisposablePostgresFact]
    public void FailureAfterArchiveRenameRollsBackAllDdlAndData()
    {
        using var fixture = new FifteenTableFixture();
        fixture.Execute("""
            DROP INDEX smarttour_demo.one_approved_narration_per_language;
            INSERT INTO smarttour_demo.place_narrations(id,place_id,language_code,narration_text,status,version)
            SELECT 'fixture-duplicate',id,'vi','Fixture','approved','fixture-duplicate' FROM smarttour_demo.places LIMIT 1;
            """);
        var before = fixture.Snapshot();
        Assert.Throws<PostgresException>(() => PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true));
        Assert.Equal(15, fixture.Tables());
        Assert.Equal(0, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        Assert.True(JsonNode.DeepEquals(before, fixture.Snapshot()));
    }

    [DisposablePostgresFact]
    public void EmptyLegacySchemaDoesNotCreateAccountsOrInventData()
    {
        using var fixture = new FifteenTableFixture(seed: false);
        var result = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true);
        Assert.True(result.Success);
        Assert.False(result.Initialized);
        Assert.Equal(6, fixture.Tables());
        Assert.Equal(0L, fixture.Scalar<long>("SELECT count(*) FROM smarttour_demo.users"));
        Assert.Equal(0L, fixture.Scalar<long>("SELECT count(*) FROM smarttour_demo.activity_logs"));
    }

    [DisposablePostgresFact]
    public void UnknownTablesMissingSnapshotAndWrongTypesBlockChanges()
    {
        using var fixture = new FifteenTableFixture();
        fixture.Execute("CREATE TABLE smarttour_demo.unexpected_fixture(id text)");
        Assert.Equal("UNSUPPORTED_SCHEMA", PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true).Code);
        fixture.Execute("DROP TABLE smarttour_demo.unexpected_fixture; DELETE FROM smarttour_demo.activity_logs WHERE id='system:platform-state:v1'");
        Assert.Equal("SNAPSHOT_REQUIRED", PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true).Code);
        fixture.Execute("ALTER TABLE smarttour_demo.users ALTER COLUMN display_name TYPE integer USING 0");
        Assert.Equal("COLUMN_MISMATCH", PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true).Code);
        Assert.Equal(15, fixture.Tables());
        Assert.Equal(0, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        Assert.Equal(1L, fixture.Scalar<long>("SELECT count(*) FROM smarttour_demo.users"));
    }

    [DisposablePostgresFact]
    public async Task WriterThatCommitsWhileMigrationWaitsIsIncludedInTheCopy()
    {
        using var fixture = new FifteenTableFixture();
        using var writer = new NpgsqlConnection(fixture.ConnectionString); writer.Open();
        using var tx = writer.BeginTransaction();
        using (var update = new NpgsqlCommand("UPDATE smarttour_demo.users SET display_name='Committed before migration lock' WHERE username='admin'", writer, tx))
            update.ExecuteNonQuery();
        var migration = Task.Run(() => PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true));
        var waiting = false;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            waiting = fixture.Scalar<bool>("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE datname=current_database() AND application_name='SmartTourSixTableMigration' AND wait_event_type='Lock')");
            if (waiting) break;
            await Task.Delay(20);
        }
        tx.Commit(); // Always release the writer before asserting/awaiting.
        var result = await migration;
        Assert.True(waiting, "Expected the migration to wait on the existing writer");
        Assert.True(result.Success);
        Assert.Equal("Committed before migration lock", fixture.Scalar<string>("SELECT display_name FROM smarttour_demo.users WHERE username='admin'"));
        Assert.Equal("Committed before migration lock", fixture.Scalar<string>("SELECT display_name FROM smarttour_demo_archive15.users WHERE username='admin'"));
    }

    [DisposablePostgresFact]
    public void RlsFilteredRowsCannotBeMistakenForAnEmptyDatabase()
    {
        using var fixture = new FifteenTableFixture();
        var role = "migration_visibility_" + Guid.NewGuid().ToString("N");
        fixture.Execute($"CREATE ROLE {role} NOLOGIN; GRANT USAGE ON SCHEMA smarttour_demo TO {role}; GRANT SELECT ON ALL TABLES IN SCHEMA smarttour_demo TO {role}");
        try
        {
            var options = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Options = "-c role=" + role };
            var report = PostgresSixTableMigration.Run(options.ConnectionString);
            Assert.False(report.Success);
            Assert.Equal("VISIBILITY_REQUIRED", report.Code);
            Assert.Empty(report.Counts);
            Assert.Equal(15, fixture.Tables());
            Assert.Equal(0, fixture.Tables(PostgresSixTableMigration.ArchiveSchema));
        }
        finally { fixture.Execute($"DROP OWNED BY {role}; DROP ROLE {role}"); }
    }
}
