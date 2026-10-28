using System.Text;

namespace ShortVideoTranslator.Business;

// MyMemory queries must remain below 500 UTF-8 bytes, including Unicode text.
// Preserve the existing 480-byte budget and word-boundary splitting behavior.
public static class TranslationTextSplitter
{
    public static IReadOnlyList<string> Split(string text)
    {
        // Split at word boundaries where possible; preserve Unicode scalar values.
        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            if (Encoding.UTF8.GetByteCount(current.ToString()) + rune.Utf8SequenceLength > 480)
            {
                var buffer = current.ToString();
                var boundary = buffer.LastIndexOf(' ');
                if (boundary > 0)
                {
                    chunks.Add(buffer[..boundary]);
                    current.Clear().Append(buffer[(boundary + 1)..]);
                }
                else { chunks.Add(buffer); current.Clear(); }
            }
            current.Append(rune.ToString());
        }
        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }
}
