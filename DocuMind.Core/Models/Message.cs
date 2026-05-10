namespace DocuMind.Core.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // EF Core'un "Persona/Session null" dememesi için '?' ekliyoruz
        public virtual Session? Session { get; set; }
    }
}