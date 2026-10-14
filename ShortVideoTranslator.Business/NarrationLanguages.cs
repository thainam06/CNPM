namespace ShortVideoTranslator.Business;

public static class NarrationLanguages
{
    public static readonly IReadOnlyList<string> Codes = Array.AsReadOnly(new[] { "vi", "en", "zh", "ja", "ko", "fr" });

    public static void Validate(string language)
    {
        if (!Codes.Contains(language))
            throw new ArgumentException("Ngôn ngữ chưa được hỗ trợ. Chọn vi, en, zh, ja, ko hoặc fr.");
    }

    public static void ValidateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Nội dung thuyết minh không được để trống.");
        if (text.Length > 3000) throw new ArgumentException("Nội dung tối đa 3000 ký tự.");
    }
}
