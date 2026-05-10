using DocuMind.Core.Models;
using DocuMind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;

namespace DocuMind.Infrastructure.Services
{
    public class DatabaseService
    {
        private readonly AppDbContext _context;

        public DatabaseService(AppDbContext context)
        {
            _context = context;
            _context.Database.EnsureCreated();
            EnsureSchema();
        }

        public Task EnsureReadyAsync()
        {
            _context.Database.EnsureCreated();
            EnsureSchema();
            return Task.CompletedTask;
        }

        private void EnsureSchema()
        {
            EnsureColumn("Sessions", "FilePath", "TEXT");
            EnsureColumn("Sessions", "Tags", "TEXT");
            EnsureColumn("Sessions", "Summary", "TEXT");
            EnsureColumn("Sessions", "KeyConcepts", "TEXT");
            EnsureColumn("Sessions", "DocumentType", "TEXT");
            EnsureColumn("DocumentChunks", "ChunkIndex", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn("DocumentChunks", "StartOffset", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn("DocumentChunks", "EndOffset", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn("DocumentChunks", "WordCount", "INTEGER NOT NULL DEFAULT 0");
        }

        private void EnsureColumn(string tableName, string columnName, string columnDefinition)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State == System.Data.ConnectionState.Closed;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                if (ColumnExists(connection, tableName, columnName)) return;
                using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private static bool ColumnExists(DbConnection connection, string tableName, string columnName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // --- OTURUM (SESSION) İŞLEMLERİ ---

        // Yeni bir sohbet başlat
        public async Task<Session> CreateSessionAsync(string title, string filePath)
        {
            var session = new Session
            {
                Title = title,
                FilePath = filePath,
                CreatedAt = DateTime.Now
            };

            // DÜZELTME: ChatSessions yerine Sessions
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        // Tüm geçmiş sohbetleri getir
        public async Task<List<Session>> GetSessionsAsync()
        {
            // DÜZELTME: ChatSessions yerine Sessions
            return await _context.Sessions
                .OrderByDescending(s => s.CreatedAt) // En yeniler üstte
                .ToListAsync();
        }

        public async Task<Session> CreateSessionAsync(string title, string filePath, string tags = "")
        {
            var session = new Session
            {
                Title = title,
                FilePath = filePath,
                Tags = tags, // Etiketler buraya gelecek
                CreatedAt = DateTime.Now
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        // Etiketleri sonradan güncellemek için bu metodu da ekle:
        public async Task UpdateSessionTagsAsync(int sessionId, string tags)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Tags = tags;
                await _context.SaveChangesAsync();
            }
        }
        // --- MESAJ İŞLEMLERİ ---

        // Mesajı kaydet
        public async Task SaveMessageAsync(int sessionId, bool isUser, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var message = new Message // DÜZELTME: ChatMessage yerine Message
            {
                SessionId = sessionId, // DÜZELTME: ChatSessionId yerine SessionId
                IsUser = isUser,       // DÜZELTME: Role string yerine bool IsUser
                Content = content,
                Timestamp = DateTime.Now
            };

            // DÜZELTME: ChatMessages yerine Messages
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        // Bir oturumun eski mesajlarını yükle
        public async Task<List<Message>> GetMessagesBySessionIdAsync(int sessionId)
        {
            return await _context.Messages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        // Oturumu ve mesajlarını sil
        public async Task DeleteSessionAsync(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                var messages = _context.Messages.Where(m => m.SessionId == sessionId);
                var chunks = _context.DocumentChunks.Where(c => c.SessionId == sessionId);
                _context.Messages.RemoveRange(messages);
                _context.DocumentChunks.RemoveRange(chunks);
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        // Sohbet Başlığını Değiştir
        public async Task RenameSessionAsync(int sessionId, string newTitle)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Title = newTitle;
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<Persona>> GetPersonasAsync()
        {
            return await _context.Personas.ToListAsync(); // Tüm rolleri getir
        }
        // DatabaseService.cs içine eklenecek metot
        public async Task UpdateSessionAnalysisAsync(int sessionId, string summary, string concepts, string docType)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Summary = summary;
                session.KeyConcepts = concepts;
                session.DocumentType = docType;

                await _context.SaveChangesAsync();
            }
        }

        public async Task AddPersonaAsync(Persona persona)
        {
            

            if (persona == null) return;

            // Eğer zorunlu alanlar boşsa varsayılan ata
            if (string.IsNullOrEmpty(persona.Name)) persona.Name = "Yeni Uzman";
            if (string.IsNullOrEmpty(persona.SystemPrompt)) persona.SystemPrompt = "Yardımcı ol.";

            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePersonaAsync(int id)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona != null && !persona.IsDefault) // Varsayılan roller silinemez
            {
                _context.Personas.Remove(persona);
                await _context.SaveChangesAsync();
            }
        }
    }
}
