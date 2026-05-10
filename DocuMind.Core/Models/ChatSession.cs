using System;
using System.Collections.Generic;

namespace DocuMind.Core.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Dosya adı
        public string FilePath { get; set; } = string.Empty; // Dosya yolu
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Bir oturumda bir sürü mesaj olur
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}