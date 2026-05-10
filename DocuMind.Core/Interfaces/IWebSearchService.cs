using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocuMind.Core.Interfaces
{
    public interface IWebSearchService
    {
        // Verilen soruyla ilgili internetten 3 tane özet bilgi bulur
        Task<List<string>> SearchAsync(string query);

        // Servisin aktif olup olmadığını (API Key var mı?) kontrol eder
        bool IsActive();
    }
}