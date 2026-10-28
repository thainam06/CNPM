namespace ShortVideoTranslator.Business;

// Edge-TTS voice selection is separate from process execution and audio cleanup.
public static class NarrationVoiceCatalog
{
    public static string Resolve(string language) => language switch
    {
        "vi" => "vi-VN-HoaiMyNeural",
        "en" => "en-US-JennyNeural",
        "zh" => "zh-CN-XiaoxiaoNeural",
        "ja" => "ja-JP-NanamiNeural",
        "ko" => "ko-KR-SunHiNeural",
        "fr" => "fr-FR-DeniseNeural",
        _ => throw new ArgumentException("Ngôn ngữ chưa được hỗ trợ.")
    };
}
