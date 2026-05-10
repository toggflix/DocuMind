using System;

namespace DocuMind.Core.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Role { get; set; } = "user"; // "user" veya "model"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsError { get; set; }

        // Bağlantı (Hangi oturumun mesajı?)
        public int ChatSessionId { get; set; }
        public ChatSession? ChatSession { get; set; }
        public bool IsUser => Role == "user";
    }
}
