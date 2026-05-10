namespace DocuMind.Core.Models
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public byte[] EmbeddingBlob { get; set; } = Array.Empty<byte>(); // Vektör verisi
        public int PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public int WordCount { get; set; }

        public virtual Session? Session { get; set; }
    }
}
