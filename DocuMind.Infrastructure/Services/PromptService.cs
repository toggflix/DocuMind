using DocuMind.Core.Interfaces;
using DocuMind.Core.Models;
using DocuMind.Infrastructure.Data;

public class PromptService : IPromptService
{
    private readonly AppDbContext _db;

    public PromptService(AppDbContext db) // Constructor veritabanını almalı
    {
        _db = db;
        InitializeDefaultPersonas();
    }

    private void InitializeDefaultPersonas()
    {
        try
        {
            if (!_db.Personas.Any())
            {
                var defaults = new List<Persona>
                {
                    new Persona { Name = "Genel Asistan", SystemPrompt = "Sen bir asistansın.", IconKind = "Robot", IsDefault = true },
                    new Persona { Name = "Hukuk Danışmanı", SystemPrompt = "Sen bir avukatsın.", IconKind = "ScaleBalance", IsDefault = true }
                };
                _db.Personas.AddRange(defaults);
                _db.SaveChanges();
            }
        }
        catch { /* Hata yönetimi */ }
    }

    public List<Persona> GetAvailablePersonas()
    {
        try
        {
            return _db.Personas.ToList(); //
        }
        catch (Exception)
        {
            // Tablo henüz oluşmamışsa hata fırlatmasın, boş liste dönsün
            return new List<Persona>();
        }
    }
    public Persona GetDefaultPersona()
    {
        return _db.Personas.FirstOrDefault(p => p.IsDefault)
            ?? new Persona { Name = "Genel Asistan", SystemPrompt = "Sen bir asistansın.", IconKind = "Robot", IsDefault = true };
    }
}
