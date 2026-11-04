using System.Text.Json;
using System.Text.Json.Nodes;
using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public sealed class PostgresCodecTests
{
    [Fact]
    public void SixTablesRoundTripEveryPlatformCollectionWithoutExposingSecretsAsEvents()
    {
        var folder = Path.Combine(Path.GetTempPath(), "smarttour-codec-"+Guid.NewGuid().ToString("N"));
        try {
            var store = new JsonPlatformStore(Path.Combine(folder,"platform.json"));
            var service = new PlatformService(store); service.Bootstrap("codecadmin","TestOnly123!");
            var state = store.Read(s => s); var now = DateTimeOffset.UtcNow; var p = state.Places[0]; var user = state.Users[0];
            var hash = PlatformService.ContentHash(p.Description);
            state.TranslationDrafts.Add(new("draft",p.Id,"en",hash,"Draft","v1","approved",user.Id,now,"v1"));
            state.ApprovedTranslations.Add(new(hash,"en","Published","v1",user.Id,now));
            state.ApprovedTranslations.Add(new("unmatched-source","ja","Orphan","v0",user.Id,now));
            state.Events.Add(new("e1","browser","view",p.Id,"vi",now));
            state.Events.Add(new("e2","browser","heartbeat",null,"vi",now));
            state.Events.Add(new("e3","browser","play",int.MaxValue,"en",now));
            state.PresenceDevices.Add(new("browser",new("Chrome","Windows","Desktop"),now,now,"tab"));
            state.PresenceDepartures["old:tab"] = now;
            state.Reviews.Add(new("review","registration",user.Id,"pending",null));
            state.Notifications.Add(new("notification",user.Id,"Result",false,now));
            state.Menus.Add(new("menu",p.Id,"Menu","Description",123));
            state.AudioTasks.Add(new("task",p.Id,["vi"],"queued",0,null,user.Id,now,now,hash));
            state.OwnerIdentities.Add(new(user.Id,"encrypted",now));
            var tables = PostgresStateCodec.Encode(state);
            Assert.Equal(6,tables.Count);
            var restored = PostgresStateCodec.Decode(tables);
            Assert.Equivalent(state,restored,strict:true);
            Assert.DoesNotContain(restored.Events,e => e.Id == PostgresStateCodec.StateId);
            Assert.DoesNotContain(restored.Audit,e => e.Id == PostgresStateCodec.StateId);
        } finally { if (Directory.Exists(folder)) Directory.Delete(folder,true); }
    }
    [Theory]
    [InlineData("2020-11-04T10:11:12.1234567+07:00", "2020-11-04T03:11:12.123457+00:00")]
    [InlineData("2020-11-04T10:11:12.1234562+07:00", "2020-11-04T03:11:12.123456+00:00")]
    public void DatabaseTimestampRoundingPreservesExactSourceButDoesNotHideSqlEdits(string source, string database)
    {
        var original = DateTimeOffset.Parse(source);
        var state = new PlatformState();
        state.Events.Add(new("event", "browser", "view", null, "vi", original));
        state.Audit.Add(new("audit", "actor", "test", "fixture", original));
        state.PresenceDevices.Add(new("browser", new(), original, original, "tab"));
        var tables = PostgresStateCodec.Encode(state);
        var eventRow = tables["activity_logs"]!.AsArray().OfType<JsonObject>().Single(r => (string?)r["id"] == "event:event");
        var auditRow = tables["activity_logs"]!.AsArray().OfType<JsonObject>().Single(r => (string?)r["id"] == "audit:audit");
        eventRow["created_at"] = database; auditRow["created_at"] = database;
        tables["visitor_sessions"]![0]!["started_at"] = database;
        tables["visitor_sessions"]![0]!["last_seen_at"] = database;
        var restored = PostgresStateCodec.Decode(tables);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(state, options), JsonSerializer.SerializeToNode(restored, options)));
        var edited = original.AddHours(1);
        eventRow["created_at"] = edited;
        Assert.Equal(edited, PostgresStateCodec.Decode(tables).Events.Single().CreatedAt);
        // Old codec-v1 rows without precision metadata remain readable.
        eventRow["details"]!.AsObject().Remove("__createdAt");
        eventRow["created_at"] = database;
        Assert.Equal(DateTimeOffset.Parse(database), PostgresStateCodec.Decode(tables).Events.Single().CreatedAt);
    }

    [Fact]
    public void MissingOrUnsupportedEnvelopeFailsInsteadOfStartingEmpty()
    {
        var tables = PostgresStateCodec.Encode(new PlatformState());
        tables["activity_logs"]![0]!["details"]!["codec"] = 999;
        Assert.Throws<InvalidDataException>(() => PostgresStateCodec.Decode(tables));
        tables["activity_logs"] = new JsonArray();
        Assert.Throws<InvalidDataException>(() => PostgresStateCodec.Decode(tables));
    }
}
