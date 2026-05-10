using Microsoft.EntityFrameworkCore;
using DocuMind.Core.Models;
using System.IO;
using System;

namespace DocuMind.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<Persona> Personas { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.EnsureCreated(); // Tablolar yoksa otomatik oluşturur
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Veritabanı dosyası "Belgelerim" klasöründe oluşsun
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuMind_DB.sqlite");
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            // Tembel Yükleme (Lazy Loading) açmak için (Proxies paketi gerekir ama şimdilik standart gidelim)
            // optionsBuilder.UseLazyLoadingProxies(); 
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Persona tablosundaki alanların zorunlu olduğunu ama boş string kabul ettiğini belirtiyoruz
            modelBuilder.Entity<Persona>().Property(p => p.Name).IsRequired();
            modelBuilder.Entity<Persona>().Property(p => p.SystemPrompt).IsRequired();

            // İlişkisel null hatalarını engellemek için
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}