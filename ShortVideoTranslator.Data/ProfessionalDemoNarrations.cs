using System.Text.Json;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

internal static class ProfessionalDemoNarrations
{
    private sealed record Edit(string Name, string SourceUrl, Dictionary<string, string> Lead,
        Dictionary<string, string> Details, Dictionary<string, string> Closing);

    public static IReadOnlyList<DemoNarration> Build(IReadOnlyList<DemoNarration> previous)
    {
        using var stream = typeof(ProfessionalDemoNarrations).Assembly.GetManifestResourceStream(
            "ShortVideoTranslator.Data.Content.professional-narration-edits.json")
            ?? throw new InvalidDataException("Thiếu bộ nội dung thuyết minh.");
        var edits = JsonSerializer.Deserialize<Edit[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("Bộ nội dung thuyết minh không hợp lệ.");
        if (edits.Length != previous.Count || edits.Select(e => e.Name).Distinct().Count() != previous.Count)
            throw new InvalidDataException("Danh sách bài thuyết minh không khớp bộ địa điểm.");
        return Array.AsReadOnly(previous.Select(old => {
            var edit = edits.Single(e => e.Name == old.Name);
            if (!Uri.TryCreate(edit.SourceUrl, UriKind.Absolute, out var source) || source.Scheme != "https")
                throw new InvalidDataException("Bài thuyết minh cần nguồn HTTPS.");
            var texts = new Dictionary<string, string>();
            foreach (var language in NarrationLanguages.Codes)
            {
                var oldParagraphs = old.Texts[language].ReplaceLineEndings("\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
                var text = string.Join("\n\n", edit.Lead[language].Trim(), oldParagraphs[1].Trim(),
                    edit.Details[language].Trim(), edit.Closing[language].Trim());
                NarrationLanguages.ValidateText(text);
                texts.Add(language, text);
            }
            return new DemoNarration(old.Name, old.Texts["vi"], edit.SourceUrl, texts);
        }).ToArray());
    }
}
