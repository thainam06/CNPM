using System.Text.Json;
using System.Text.Json.Nodes;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// Six physical tables. Auxiliary workflows remain typed collections in the
// reserved activity_logs state envelope, never mixed into analytics/audit output.
public static class PostgresStateCodec
{
    public const string StateId = "system:platform-state:v1";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly (string, string)[] UserFields = [("id","id"),("username","username"),("displayName","display_name"),("passwordHash","password_hash"),("role","role"),("enabled","enabled")];
    private static readonly (string, string)[] PlaceFields = [("id","id"),("ownerId","owner_id"),("name","name"),("address","address"),("latitude","latitude"),("longitude","longitude"),("description","introduction")];
    private static readonly (string, string)[] PhotoFields = [("url","url"),("caption","caption")];

    private static JsonNode? Copy(JsonNode? value) => value?.DeepClone();
    private static JsonObject Object<T>(T value) => JsonSerializer.SerializeToNode(value, Json)!.AsObject();
    private static JsonObject Row<T>(T value, (string Model, string Column)[] fields, string extra, int order)
    {
        var rest = Object(value); var row = new JsonObject();
        foreach (var (model, column) in fields) { row[column] = Copy(rest[model]); rest.Remove(model); }
        rest["__order"] = order; row[extra] = rest; return row;
    }
    private static T Model<T>(JsonObject row, (string Model, string Column)[] fields, string extra)
    {
        var data = row[extra]!.DeepClone().AsObject(); data.Remove("__order");
        foreach (var (model, column) in fields) data[model] = Copy(row[column]);
        return data.Deserialize<T>(Json)!;
    }
    private static IEnumerable<JsonObject> Ordered(JsonArray rows, string extra) => rows.Select(n => n!.AsObject())
        .OrderBy(r => r[extra]?["__order"]?.GetValue<int>() ?? 0);

    public static JsonObject Encode(PlatformState state)
    {
        var users = new JsonArray(); var places = new JsonArray(); var images = new JsonArray();
        var narrations = new JsonArray(); var sessions = new JsonArray(); var logs = new JsonArray();
        var tables = new JsonObject { ["users"] = users, ["places"] = places, ["place_images"] = images,
            ["place_narrations"] = narrations, ["visitor_sessions"] = sessions, ["activity_logs"] = logs };
        var envelope = Object(state);
        foreach (var key in new[] { "users","places","translationDrafts","approvedTranslations","presenceDevices","audit","events" }) envelope.Remove(key);
        // Heartbeat history and unmatched approved sources have no FK target in the compact schema.
        envelope["eventHeartbeats"] = JsonSerializer.SerializeToNode(state.Events.Select((e,i) => new { value=e, order=i }).Where(x => x.value.Kind == "heartbeat"), Json);
        envelope["unmatchedTranslations"] = JsonSerializer.SerializeToNode(state.ApprovedTranslations.Select((a,i) => new { value=a, order=i })
            .Where(x => !state.Places.Any(p => PlatformService.ContentHash(p.Description) == x.value.SourceHash)), Json);
        logs.Add(new JsonObject { ["id"] = StateId, ["kind"] = "admin", ["user_id"] = null, ["device_id"] = null,
            ["place_id"] = null, ["consent"] = false, ["created_at"] = state.UpdatedAt, ["details"] = new JsonObject { ["codec"] = 1, ["state"] = envelope } });
        for (var i=0; i<state.Users.Count; i++) users.Add(Row(state.Users[i], UserFields, "settings", i));
        long photoId = 1;
        for (var i=0; i<state.Places.Count; i++) {
            var p = state.Places[i]; var row = Row(p, PlaceFields, "settings", i);
            row["settings"]!.AsObject().Remove("images"); row["settings"]!["__imagesNull"] = p.Images == null;
            places.Add(row);
            foreach (var (photo, index) in (p.Images ?? []).Select((photo,index) => (photo,index))) {
                var image = Row(photo, PhotoFields, "metadata", index);
                image["id"] = photoId++; image["place_id"] = p.Id; image["ordinal"] = index;
                image["is_cover"] = photo.Url == p.ImageUrl && !(images.OfType<JsonObject>().Any(r => r["place_id"]!.GetValue<int>() == p.Id && r["is_cover"]!.GetValue<bool>()));
                images.Add(image);
            }
            // Covers are distinct from additional gallery photos in the existing model.
            // A cover not in Images stays in places.settings to preserve exact semantics.
            narrations.Add(new JsonObject { ["id"] = "source:"+p.Id, ["place_id"] = p.Id, ["language_code"] = "vi",
                ["narration_text"] = p.Description, ["audio_path"] = null, ["status"] = "approved",
                ["version"] = PlatformService.ContentHash(p.Description), ["metadata"] = new JsonObject { ["type"] = "source" } });
        }
        var narrationRows = new Dictionary<string, JsonObject>();
        for (var i=0; i<state.TranslationDrafts.Count; i++) {
            var d = state.TranslationDrafts[i];
            var row = new JsonObject { ["id"] = "draft:"+d.Id, ["place_id"] = d.PlaceId, ["language_code"] = d.Language,
                ["narration_text"] = d.Text, ["audio_path"] = null, ["status"] = "draft", ["version"] = d.Version,
                ["metadata"] = new JsonObject { ["draft"] = Object(d), ["draftOrder"] = i } };
            row["metadata"]!["draft"]!.AsObject().Remove("text");
            narrationRows[$"{d.PlaceId}:{d.Language}:{d.Version}"] = row; narrations.Add(row);
        }
        for (var i=0; i<state.ApprovedTranslations.Count; i++) {
            var a = state.ApprovedTranslations[i];
            foreach (var p in state.Places.Where(p => PlatformService.ContentHash(p.Description) == a.SourceHash)) {
                var key = $"{p.Id}:{a.Language}:{a.Revision}";
                if (!narrationRows.TryGetValue(key, out var row)) {
                    row = new JsonObject { ["id"] = $"published:{p.Id}:{a.Language}:{a.Revision}", ["place_id"] = p.Id,
                        ["language_code"] = a.Language, ["narration_text"] = a.Text, ["audio_path"] = null,
                        ["version"] = a.Revision, ["metadata"] = new JsonObject() };
                    narrations.Add(row); narrationRows[key] = row;
                }
                row["status"] = "approved";
                // Keep a different draft text if a crafted/imported snapshot contains both.
                if (row["narration_text"]!.GetValue<string>() != a.Text) row["metadata"]!["draftText"] = Copy(row["narration_text"]);
                row["narration_text"] = a.Text;
                row["metadata"]!["approved"] = Object(a); row["metadata"]!["approved"]!.AsObject().Remove("text");
                row["metadata"]!["approvedOrder"] = i;
            }
        }
        foreach (var d in state.PresenceDevices) sessions.Add(new JsonObject { ["id"] = d.DeviceId+":"+(d.SessionId??d.DeviceId), ["device_id"] = d.DeviceId,
            ["started_at"] = d.FirstSeen, ["last_seen_at"] = d.LastSeen, ["ended_at"] = null,
            ["device_info"] = new JsonObject { ["descriptor"] = Object(d.Descriptor), ["sessionId"] = d.SessionId, ["__order"] = sessions.Count } });
        var placeIds = state.Places.Select(p => p.Id).ToHashSet(); var userIds = state.Users.Select(u => u.Id).ToHashSet();
        for (var i=0; i<state.Events.Count; i++) {
            var e = state.Events[i]; if (e.Kind == "heartbeat") continue;
            logs.Add(new JsonObject { ["id"] = "event:"+e.Id, ["kind"] = e.Kind, ["user_id"] = null, ["device_id"] = e.DeviceId,
                ["place_id"] = e.PlaceId is int id && placeIds.Contains(id) ? id : null, ["consent"] = true, ["created_at"] = e.CreatedAt,
                ["details"] = new JsonObject { ["eventId"] = e.Id, ["language"] = e.Language, ["originalPlaceId"] = e.PlaceId, ["__order"] = i } });
        }
        for (var i=0; i<state.Audit.Count; i++) {
            var a = state.Audit[i]; logs.Add(new JsonObject { ["id"] = "audit:"+a.Id, ["kind"] = "admin", ["user_id"] = userIds.Contains(a.ActorId) ? a.ActorId : null,
                ["device_id"] = null, ["place_id"] = null, ["consent"] = false, ["created_at"] = a.CreatedAt,
                ["details"] = new JsonObject { ["auditId"] = a.Id, ["actorId"] = a.ActorId, ["action"] = a.Action, ["resource"] = a.Resource, ["__order"] = i } });
        }
        return tables;
    }

    public static PlatformState Decode(JsonObject tables)
    {
        var logs = tables["activity_logs"]!.AsArray();
        var marker = logs.OfType<JsonObject>().SingleOrDefault(r => r["id"]!.GetValue<string>() == StateId)
            ?? throw new InvalidDataException("Database chưa được nhập dữ liệu SmartTour. Chạy công cụ migration trước.");
        if (marker["details"]?["codec"]?.GetValue<int>() != 1) throw new InvalidDataException("Phiên bản dữ liệu PostgreSQL không được hỗ trợ.");
        var envelope = marker["details"]!["state"]!.DeepClone().AsObject();
        var heartbeatRows = envelope["eventHeartbeats"]!.DeepClone().AsArray(); envelope.Remove("eventHeartbeats");
        var unmatched = envelope["unmatchedTranslations"]!.DeepClone().AsArray(); envelope.Remove("unmatchedTranslations");
        var state = envelope.Deserialize<PlatformState>(Json)!;
        state.Users = Ordered(tables["users"]!.AsArray(), "settings").Select(r => Model<PlatformUser>(r, UserFields, "settings")).ToList();
        var imageRows = tables["place_images"]!.AsArray().OfType<JsonObject>().ToArray();
        state.Places = Ordered(tables["places"]!.AsArray(), "settings").Select(r => {
            var p = Model<Place>(r, PlaceFields, "settings");
            return p with { Images = r["settings"]!["__imagesNull"]!.GetValue<bool>() ? null : imageRows.Where(img => img["place_id"]!.GetValue<int>() == p.Id)
                .OrderBy(img => img["ordinal"]!.GetValue<int>()).Select(img => Model<PlacePhoto>(img, PhotoFields, "metadata")).ToArray() };
        }).ToList();
        var drafts = new List<(int Order, TranslationDraft Value)>(); var approved = new List<(int Order, ApprovedTranslation Value)>();
        foreach (var r in tables["place_narrations"]!.AsArray().OfType<JsonObject>()) {
            var m = r["metadata"]!;
            if (m["draft"] != null) {
                var d = m["draft"]!.DeepClone().AsObject(); d["text"] = Copy(m["draftText"] ?? r["narration_text"]);
                drafts.Add((m["draftOrder"]!.GetValue<int>(), d.Deserialize<TranslationDraft>(Json)!));
            }
            if (m["approved"] != null) {
                var a = m["approved"]!.DeepClone().AsObject(); a["text"] = Copy(r["narration_text"]);
                approved.Add((m["approvedOrder"]!.GetValue<int>(), a.Deserialize<ApprovedTranslation>(Json)!));
            }
        }
        foreach (var r in unmatched) approved.Add((r!["order"]!.GetValue<int>(), r["value"]!.Deserialize<ApprovedTranslation>(Json)!));
        state.TranslationDrafts = drafts.OrderBy(x => x.Order).Select(x => x.Value).ToList();
        state.ApprovedTranslations = approved.DistinctBy(x => x.Order).OrderBy(x => x.Order).Select(x => x.Value).ToList();
        state.PresenceDevices = Ordered(tables["visitor_sessions"]!.AsArray(), "device_info").Where(r => r["ended_at"] == null).Select(r => new PresenceDevice(
            r["device_id"]!.GetValue<string>(), r["device_info"]!["descriptor"]!.Deserialize<DeviceDescriptor>(Json)!,
            r["started_at"]!.GetValue<DateTimeOffset>(), r["last_seen_at"]!.GetValue<DateTimeOffset>(), r["device_info"]!["sessionId"]?.GetValue<string>())).ToList();
        var eventRows = heartbeatRows.Select(r => (Order:r!["order"]!.GetValue<int>(), Value:r["value"]!.Deserialize<AnalyticsEvent>(Json)!)).ToList();
        foreach (var r in logs.OfType<JsonObject>().Where(r => r["kind"]!.GetValue<string>() != "admin")) {
            var d = r["details"]!; eventRows.Add((d["__order"]!.GetValue<int>(), new(d["eventId"]!.GetValue<string>(), r["device_id"]!.GetValue<string>(),
                r["kind"]!.GetValue<string>(), d["originalPlaceId"]?.GetValue<int>(), d["language"]!.GetValue<string>(), r["created_at"]!.GetValue<DateTimeOffset>())));
        }
        state.Events = eventRows.OrderBy(x => x.Order).Select(x => x.Value).ToList();
        state.Audit = Ordered(new JsonArray(logs.OfType<JsonObject>().Where(r => r["id"]!.GetValue<string>().StartsWith("audit:")).Select(r => Copy(r)).ToArray()), "details")
            .Select(r => { var d = r["details"]!; return new AuditEntry(d["auditId"]!.GetValue<string>(), d["actorId"]!.GetValue<string>(), d["action"]!.GetValue<string>(), d["resource"]!.GetValue<string>(), r["created_at"]!.GetValue<DateTimeOffset>()); }).ToList();
        return state;
    }
}
