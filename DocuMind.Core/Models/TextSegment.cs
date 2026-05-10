namespace DocuMind.Core.Models
{
    public class TextSegment
    {
        public string Text { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = System.Array.Empty<float>();

        // YENİ: Sayfa Numarası (Kanıt için şart)
        public int PageNumber { get; set; }
    }
}