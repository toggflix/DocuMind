using System.Threading.Tasks;

namespace DocuMind.Core.Interfaces
{
    public interface IReportingService
    {
        // Sohbet geçmişini metin dosyası olarak kaydeder
        Task ExportChatAsTextAsync(string content, string filePath);

        // Gelecekte buraya ExportAsWordAsync eklenebilir
    }
}