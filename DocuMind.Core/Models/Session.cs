namespace DocuMind.Core.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- YENİ ANALİZ ALANLARI ---
        public string? Summary { get; set; }       // Belgenin kısa özeti
        public string? KeyConcepts { get; set; }   // Örn: "Hukuk, Sözleşme, Tazminat"
        public string? DocumentType { get; set; }  // Örn: "Akademik Makale"

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}