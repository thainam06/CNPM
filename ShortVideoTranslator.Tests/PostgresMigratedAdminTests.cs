using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PostgresMigratedAdminTests
{
    private sealed class MigratedApplication(string connection) : TestApplication
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Postgres", ["Storage:PostgresConnectionString"] = connection
            }));
        }
    }

    [DisposablePostgresFact]
    public async Task ExistingAdminLogsInAndCrudPersistsAfterMigrationAndApplicationRestart()
    {
        using var fixture = new FifteenTableFixture();
        var archived = fixture.Snapshot();
        var migration = PostgresSixTableMigration.Run(fixture.ConnectionString, true, true, true);
        Assert.True(migration.Success);
        var originalUsers = fixture.Scalar<string>("SELECT jsonb_agg(to_jsonb(u) ORDER BY id)::text FROM smarttour_demo.users u");
        int createdId;
        using (var app = new MigratedApplication(fixture.ConnectionString))
        using (var client = app.CreateClient())
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/places")).StatusCode);
            await Login(client);
            var health = await client.GetFromJsonAsync<JsonElement>("/api/health");
            Assert.Equal("postgresql", health.GetProperty("storage").GetString());
            (await client.GetAsync("/api/health/ready")).EnsureSuccessStatusCode();
            var places = (await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!;
            Assert.NotEmpty(places);
            var candidate = places[0] with { Id = 0, Name = "Migration CRUD fixture", IsActive = false, OwnerId = null };
            var create = await client.PostAsJsonAsync("/api/admin/places", candidate);
            create.EnsureSuccessStatusCode();
            var created = (await create.Content.ReadFromJsonAsync<Place>())!;
            createdId = created.Id;
            Assert.DoesNotContain(places, p => p.Id == createdId);
            var updated = created with { Description = "Nội dung đã cập nhật sau migration." };
            (await client.PutAsJsonAsync($"/api/admin/places/{createdId}", updated)).EnsureSuccessStatusCode();
        }
        using (var restarted = new MigratedApplication(fixture.ConnectionString))
        using (var client = restarted.CreateClient())
        {
            await Login(client); // Same account/password; bootstrap did not replace it.
            var places = (await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!;
            Assert.Equal("Nội dung đã cập nhật sau migration.", places.Single(p => p.Id == createdId).Description);
            (await client.DeleteAsync($"/api/admin/places/{createdId}")).EnsureSuccessStatusCode();
            Assert.DoesNotContain((await client.GetFromJsonAsync<Place[]>("/api/admin/places"))!, p => p.Id == createdId);
        }
        Assert.Equal(originalUsers, fixture.Scalar<string>("SELECT jsonb_agg(to_jsonb(u) ORDER BY id)::text FROM smarttour_demo.users u"));
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(archived, fixture.Snapshot(PostgresSixTableMigration.ArchiveSchema)));
        Assert.Equal(6, fixture.Tables());
    }

    private static async Task Login(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "MigrationChanged123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());
    }
}
