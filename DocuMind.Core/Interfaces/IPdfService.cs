using System.Collections.Generic;

namespace DocuMind.Core.Interfaces
{
    // Core katmanında sadece "NE YAPILACAĞI" tanımlanır, "NASIL YAPILACAĞI" değil.
    public interface IPdfService
    {
        List<(int PageNumber, string Text)> ExtractPages(string filePath);
    }
}